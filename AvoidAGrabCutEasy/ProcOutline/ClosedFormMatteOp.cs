using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace GetAlphaMatte
{
    //c# port from: https://github.com/MarcoForte/closed-form-matting
    //extended a bit
    public class ClosedFormMatteOp : IDisposable
    {
        private int _w;
        private int _h;

        public Bitmap Bmp { get; private set; }
        public Bitmap Trimap { get; private set; }
        public Bitmap MaskBitmap { get; private set; }
        public double[][] R { get; private set; } //matrix A
        public List<List<double>> RF { get; private set; } //matrix values, first container
        //public List<List<int>> ColIndices { get; private set; }
        //public List<int> RowIndices { get; private set; }
        public double[] N { get; private set; } //diag values
        public Dictionary<int, Dictionary<int, int>> IndexMappings { get; private set; }

        private Dictionary<int, Dictionary<int, int>> _pos;
        public double[] B { get; private set; } //B-Vector
        public double TrimapConfidence { get; private set; } = 100.0;

        // amount of GS_SOR iterations to determine the first guess for CG
        private const int PRE_OP_GSSOR = 20;

        public event EventHandler<string> ShowInfo;
        public event EventHandler<GetAlphaMatte.ProgressEventArgs> ShowProgess;

        private object _lockObject = new object();

        public bool CancellationPending
        {
            get
            {
                return _cancellationPending;
            }
            set
            {
                _cancellationPending = value;
            }
        }
        private bool _cancellationPending = false;

        public BlendParameters BlendParameters { get;  set; }

        public ClosedFormMatteOp(Bitmap bmp, Bitmap trimap)
        {
            this._w = bmp.Width;
            this._h = bmp.Height;

            this.Bmp = bmp;
            this.Trimap = trimap;
        }

        public unsafe void GetMattingLaplacian(double eps)
        {
            int w = this._w;
            int h = this._h;

            int winWH = 3;
            int winWHH = 1;

            int winWH2 = winWH * winWH;
            int wh = w * h;

            this.R = new double[wh][];
            this.RF = new List<List<double>>();  //Enumerable.Repeat(double.MinValue, wh * winWH2 * winWH2).ToArray(); // new double[wh * winWH2 * winWH2];
            List<List<int>> colIndices = new List<List<int>>();
            List<int> rowIndices = new List<int>();

            for (int i = 0; i < wh; i++)
            {
                colIndices.Add(new List<int>());
                this.R[i] = new double[winWH2 * winWH2 + 1];
                for (int j = 0; j < this.R[i].Length; j++)
                    this.R[i][j] = double.MinValue;
                this.RF.Add(new List<double>());
            }

            Bitmap maskBmp = GenerateMaskImg();
            Dilate(maskBmp, 3);

            if (this.BlendParameters != null)
            {
                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerSupportsCancellation && this.BlendParameters.BGW.CancellationPending)
                    return;

                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerReportsProgress)
                    this.BlendParameters.BGW.ReportProgress(10);
            }

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmMask = maskBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            //################################

            BitArray processedWindows = new BitArray(w * h, false);

            //for (int y = winWHH; y < h - winWHH; y++)
            Parallel.For(winWHH, h - winWHH, (y, loopState) =>
            {
                byte* p = (byte*)bmData.Scan0;
                byte* pT = (byte*)bmMask.Scan0;

                if (this.BlendParameters != null)
                {
                    if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerSupportsCancellation && this.BlendParameters.BGW.CancellationPending)
                        loopState.Break();
                }

                for (int x = winWHH; x < w - winWHH; x++)
                {
                    if (pT[y * stride + x * 4] > 0)
                    {
                        //mu & var
                        double[] winCB = new double[winWH2];
                        double[] winCG = new double[winWH2];
                        double[] winCR = new double[winWH2];

                        double[] mu = new double[winWH];

                        for (int yW = y - winWHH, cY = 0; yW <= y + winWHH; yW++, cY++)
                        {
                            for (int xW = x - winWHH, cX = 0; xW <= x + winWHH; xW++, cX++)
                            {
                                winCB[cY * winWH + cX] = (double)p[yW * stride + xW * 4] / 255.0;
                                winCG[cY * winWH + cX] = (double)p[yW * stride + xW * 4 + 1] / 255.0;
                                winCR[cY * winWH + cX] = (double)p[yW * stride + xW * 4 + 2] / 255.0;
                            }
                        }

                        mu[0] = winCB.Average();
                        mu[1] = winCG.Average();
                        mu[2] = winCR.Average();

                        double[][] winI = new double[winWH2][];
                        for (int i = 0; i < winWH2; i++)
                        {
                            winI[i] = new double[3] { winCB[i], winCG[i], winCR[i] };
                        }

                        double[][] winIT = new double[winWH][];
                        winIT[0] = winCB;
                        winIT[1] = winCG;
                        winIT[2] = winCR;

                        double[][] winIwinI = MatrixInverse.MatrixProduct(winIT, winI, false);

                        for (int i = 0; i < winWH; i++)
                        {
                            for (int j = 0; j < winWH; j++)
                            {
                                winIwinI[i][j] /= winWH2;
                            }
                        }

                        double[][] mumu = new double[winWH][];

                        for (int i = 0; i < mu.Length; i++)
                        {
                            mumu[i] = new double[winWH];

                            for (int j = 0; j < mu.Length; j++)
                            {
                                mumu[i][j] = mu[i] * mu[j];
                            }
                        }

                        double[][] winImu = new double[winWH][];

                        for (int i = 0; i < winWH; i++)
                        {
                            winImu[i] = new double[winWH];

                            for (int j = 0; j < winWH; j++)
                            {
                                winImu[i][j] = winIwinI[i][j] - mumu[i][j];

                                if (i == j)
                                    winImu[i][j] += eps / winWH2;
                            }
                        }

                        //img - mu
                        double[][] img_minus_mu = new double[winWH2][];
                        for (int yW = y - winWHH, cY = 0; yW <= y + winWHH; yW++, cY++)
                        {
                            for (int xW = x - winWHH, cX = 0; xW <= x + winWHH; xW++, cX++)
                            {
                                double[] r = new double[3];

                                r[0] = p[yW * stride + xW * 4] / 255.0 - mu[0];
                                r[1] = p[yW * stride + xW * 4 + 1] / 255.0 - mu[1];
                                r[2] = p[yW * stride + xW * 4 + 2] / 255.0 - mu[2];

                                img_minus_mu[cY * winWH + cX] = r;
                            }
                        }

                        //A * x = B
                        double[][] A = winImu;

                        //########################################################
                        //test
                        //var ja = MatrixInverse.MatrixInv(A);
                        //var winI2 = img_minus_mu;

                        //var winI2T = new double[winI2[0].Length][];

                        //for (int i = 0; i < winI2T.Length; i++)
                        //    winI2T[i] = new double[winI2.Length];

                        //for (int i = 0; i < winI2T.Length; i++)
                        //    for (int j = 0; j < winI2.Length; j++)
                        //        winI2T[i][j] = winI2[j][i];

                        ////var t = MatrixInverse.MatrixProduct(winI2, ja, false);
                        ////var tv = MatrixInverse.MatrixProduct(t, winI2T, false);

                        //var t = MatrixInverse.MatrixProduct(ja, winI2T, false);
                        //var tv = MatrixInverse.MatrixProduct(winI2, t, false);

                        //for (int i = 0; i < tv.Length; i++)
                        //{
                        //    double[] d = tv[i];

                        //    for (int j = 0; j < d.Length; j++)
                        //        d[j] += 1;

                        //    for (int j = 0; j < d.Length; j++)
                        //    {
                        //        d[j] /= 9;

                        //        //if (i == j)
                        //        //    d[j] += 1;
                        //    }

                        //    tv[i] = d;
                        //}

                        //tv = tv;

                        //keep commented out
                        //var D = new double[tv.Length];
                        //for (int i = 0; i < A.Length; i++)
                        //{
                        //    var d = A[i].Sum();
                        //    D[i] = d;
                        //}

                        //for (int i = 0; i < tv.Length; i++)
                        //    tv[i][i] = D[i];

                        //for (int i = 0; i < tv.Length; i++)
                        //{
                        //    var d = tv[i];

                        //    for (int j = 0; j < d.Length; j++)
                        //        if (i != j)
                        //            d[j] *= -1;

                        //    tv[i] = d;
                        //}
                        //end test
                        //########################################################

                        double[][] X = new double[img_minus_mu.Length][];
                        double[][] B = new double[img_minus_mu.Length][];
                        double[][] BT = new double[3][];

                        for (int i = 0; i < img_minus_mu.Length; i++)
                        {
                            double[] b = new double[3];

                            b[0] = img_minus_mu[i][0];
                            b[1] = img_minus_mu[i][1];
                            b[2] = img_minus_mu[i][2];

                            B[i] = b;

                            double[] xx = MatrixEigen.MatLinSolveQR(A, b);

                            X[i] = xx;
                        }

                        for (int i = 0; i < 3; i++)
                        {
                            BT[i] = new double[img_minus_mu.Length];

                            for (int j = 0; j < img_minus_mu.Length; j++)
                            {
                                BT[i][j] = B[j][i];
                            }
                        }

                        //Y, vals
                        double[][] Y = MatrixInverse.MatrixProduct(X, BT, false);
                        double[][] vals = new double[img_minus_mu.Length][];

                        for (int i = 0; i < B.Length; i++)
                        {
                            for (int j = 0; j < B.Length; j++)
                            {
                                Y[i][j] += 1; //Y now is Z
                                Y[i][j] *= -1.0 / winWH2;

                                if (i == j)
                                    Y[i][j] += 1;
                            }
                        }

                        //Y now is vals
                        vals = Y;

                        lock (this._lockObject)
                        {
                            int rowIndex = y * w + x;

                            for (int i = 0; i < vals.Length; i++)
                            {
                                double[] rowVals = vals[i];
                                for (int j = 0; j < rowVals.Length; j++)
                                    this.RF[rowIndex].Add(rowVals[j]);
                            }

                            processedWindows.Set(y * w + x, true);
                        }

                        //double[] values = new double[winWH2 * winWH2];

                        //for (int i = 0; i < winWH2; i++)
                        //    for (int j = 0; j < winWH2; j++)
                        //        values[i * winWH2 + j] = vals[i][j];  //bis hier ok

                        //System.IO.File.WriteAllLines(@"C:\Users\Thorsten\source\repos\py\Matting\closed-form-matting-master\closed-form-matting-master\closed_form_matting\cfm" + (x + y * w).ToString() + ".txt",
                        //    values.Select(a => double.Parse(a.ToString()).ToString()));
                    }
                }
            });

            if (this.BlendParameters != null)
            {
                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerSupportsCancellation && this.BlendParameters.BGW.CancellationPending)
                {
                    maskBmp.UnlockBits(bmMask);
                    this.Bmp.UnlockBits(bmData);
                    return;
                }

                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerReportsProgress)
                    this.BlendParameters.BGW.ReportProgress(50);
            }

            //now fill R
            //first, get the indices for rows and columns
            for (int j = 0; j < processedWindows.Count; j++)
            {
                int y = j / w;
                int x = j % w;

                if (processedWindows.Get(j))
                {
                    for (int cRow = y - winWHH; cRow <= y + winWHH; cRow++)
                    {
                        for (int cCol = x - winWHH; cCol <= x + winWHH; cCol++)
                        {
                            int colIndx = cRow * w + cCol;
                            colIndices[y * w + x].Add(colIndx);

                            int[] l = Enumerable.Repeat(colIndx, winWH2).ToArray();
                            rowIndices.AddRange(l);
                        }
                    }
                }
            }

            for (int l = 0; l < colIndices.Count; l++)
            {
                if (processedWindows.Get(l))
                {
                    int[] indices = new int[winWH2];
                    colIndices[l].CopyTo(indices, 0);

                    for (int r = 1; r < winWH2; r++)
                        colIndices[l].AddRange(indices);
                }
            }

            if (this.BlendParameters != null)
            {
                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerSupportsCancellation && this.BlendParameters.BGW.CancellationPending)
                {
                    maskBmp.UnlockBits(bmMask);
                    this.Bmp.UnlockBits(bmData);
                    return;
                }

                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerReportsProgress)
                    this.BlendParameters.BGW.ReportProgress(60);
            }

            int ww = winWH2 * winWH2;
            List<double> rf = new List<double>();
            List<List<double>> rt = this.RF.Where(a => a.Count == ww).ToList();
            for (int i = 0; i < rt.Count; i++)
                rf.AddRange(rt[i]);

            List<List<int>> ci0 = colIndices.Where(a => a.Count > 0).ToList();
            List<int> ci = ci0.SelectMany(a => a).ToList();

            //mapping, first: Column in Dense Matrix to Column in Sparse Matrix
            //in row a of this.R, item b is mapped to column c of colindexes
            Dictionary<int, Dictionary<int, int>> pos = new Dictionary<int, Dictionary<int, int>>();

            //add items for all rows
            for (int j = 0; j < processedWindows.Count; j++)
                if (!pos.ContainsKey(j))
                    pos.Add(j, new Dictionary<int, int>());

            for (int i = 0; i < rf.Count; i++)
            {
                int x = ci[i];

                if (pos.ContainsKey(rowIndices[i]))
                {
                    if (pos[rowIndices[i]].ContainsKey(x))
                    {
                        if (this.R[rowIndices[i]][pos[rowIndices[i]][x]] == double.MinValue)
                            this.R[rowIndices[i]][pos[rowIndices[i]][x]] = 0;
                        this.R[rowIndices[i]][pos[rowIndices[i]][x]] += rf[i];
                    }
                    else
                    {
                        if (pos[rowIndices[i]] != null && pos[rowIndices[i]].Count > 0)
                        {
                            pos[rowIndices[i]].Add(x, pos[rowIndices[i]].Count);
                            if (this.R[rowIndices[i]][pos[rowIndices[i]][x]] == double.MinValue)
                                this.R[rowIndices[i]][pos[rowIndices[i]][x]] = 0;
                            this.R[rowIndices[i]][pos[rowIndices[i]][x]] += rf[i];
                        }
                        else
                        {
                            pos[rowIndices[i]].Add(x, 0);
                            if (this.R[rowIndices[i]][pos[rowIndices[i]][x]] == double.MinValue)
                                this.R[rowIndices[i]][pos[rowIndices[i]][x]] = 0;
                            this.R[rowIndices[i]][pos[rowIndices[i]][x]] += rf[i];
                        }
                    }
                }
                else
                {
                    pos.Add(rowIndices[i], new Dictionary<int, int>());
                    pos[rowIndices[i]].Add(x, 0);
                    if (this.R[rowIndices[i]][pos[rowIndices[i]][x]] == double.MinValue)
                        this.R[rowIndices[i]][pos[rowIndices[i]][x]] = 0;
                    this.R[rowIndices[i]][pos[rowIndices[i]][x]] += rf[i];
                }
            }

            if (this.BlendParameters != null)
            {
                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerSupportsCancellation && this.BlendParameters.BGW.CancellationPending)
                {
                    maskBmp.UnlockBits(bmMask);
                    this.Bmp.UnlockBits(bmData);
                    return;
                }

                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerReportsProgress)
                    this.BlendParameters.BGW.ReportProgress(80);
            }

            rowIndices = null;
            colIndices = null;

            pos = pos.OrderBy(x => x.Key).ToDictionary(a => a.Key, a => a.Value);

            //if you like to... when debugging
            //for (int j = 0; j < pos.Count; j++)
            //{
            //    int l = pos.ElementAt(j).Key;
            //    pos[l] = pos[l].OrderBy(x => x.Key).ToDictionary(a => a.Key, a => a.Value);
            //}

            //now get the diagonal values (N) and the b-vector
            this.B = new double[wh];
            this.N = new double[wh];

            BitmapData bmT = this.Trimap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            //this._prior_conf = new double[wh];
            Parallel.For(0, h, y =>
            {
                byte* pt = (byte*)bmT.Scan0;
                pt += y * stride;

                for (int x = 0; x < w; x++)
                {
                    //this._prior_conf[y * w + x] = (double)pt[0] / 255.0 > 0.9 ? 1 : 0;
                    this.N[y * w + x] = this.TrimapConfidence * (pt[0] > 250 ? 1.0 : (pt[0] < 5 ? 1.0 : 0.0));
                    this.B[y * w + x] = (double)pt[0] / 255.0 * this.N[y * w + x];
                    pt += 4;
                }
            });
            this.Trimap.UnlockBits(bmT);

            for (int i = 0; i < this.N.Length; i++)
            {
                if (pos.ContainsKey(i))
                {
                    if (pos[i].ContainsKey(i))
                    {
                        int col = pos[i][i];
                        if (this.R[i][col] == double.MinValue)
                            this.R[i][col] = 0;
                        this.R[i][col] += this.N[i];
                        this.N[i] = this.R[i][col];
                    }
                    else
                    {
                        int col = 0;
                        if (this.R[i][col] == double.MinValue)
                        {
                            this.R[i][col] = this.N[i] != 0 ? this.N[i] : this.TrimapConfidence;
                            this.N[i] = this.R[i][col];

                            if (!pos[i].ContainsKey(i))
                                pos[i].Add(i, col);
                        }
                    }
                }
            }

            //mapping, Column in Sparse Matrix to Column in Dense Matrix
            //now create a dictionary with the inner values switched, since we need to map the Array elements of this.R[x] to the columnNumbers of the dense Matrix
            Dictionary<int, Dictionary<int, int>> posR = new Dictionary<int, Dictionary<int, int>>();

            foreach (int j in pos.Keys)
            {
                posR.Add(j, new Dictionary<int, int>());

                foreach (int l in pos[j].Keys)
                {
                    if (!posR[j].ContainsKey(pos[j][l]))
                        posR[j].Add(pos[j][l], l);
                }
            }

            this.IndexMappings = posR;
            this.IndexMappings = this.IndexMappings.OrderBy(x => x.Key).ToDictionary(a => a.Key, a => a.Value);
            this._pos = pos;

            if (this.BlendParameters != null)
            {
                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerSupportsCancellation && this.BlendParameters.BGW.CancellationPending)
                {
                    maskBmp.UnlockBits(bmMask);
                    this.Bmp.UnlockBits(bmData);
                    return;
                }

                if (this.BlendParameters.BGW != null && this.BlendParameters.BGW.WorkerReportsProgress)
                    this.BlendParameters.BGW.ReportProgress(100);
            }

            maskBmp.UnlockBits(bmMask);
            this.Bmp.UnlockBits(bmData);

            Bitmap bOld = this.MaskBitmap;
            this.MaskBitmap = maskBmp;
            if (bOld != null)
                bOld.Dispose();
            bOld = null;
        }

        public unsafe void Dilate(Bitmap bmp, int wh)
        {
            using (Bitmap bC = new Bitmap(bmp))
            {
                int[,] krnl = GetKernel(wh);

                BitmapData bmSrc = bC.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bC.Width, bC.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                int stride = bmData.Stride;

                int nWidth = bmp.Width;
                int nHeight = bmp.Height;

                Parallel.For(0, nHeight, (y, loopState) =>
                {
                    //if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    //    loopState.Break();

                    byte* p = (byte*)bmData.Scan0;
                    byte* pSrc = (byte*)bmSrc.Scan0;

                    int pos = y * stride;

                    for (int x = 0; x < nWidth; x++)
                    {
                        p[pos] = p[pos + 1] = p[pos + 2] = bDilate(bmSrc, new Point(x, y), krnl);
                        p[pos + 3] = (byte)255; //pSrc[pos + 3];

                        pos += 4;
                    }
                });

                bmp.UnlockBits(bmData);
                bC.UnlockBits(bmSrc);
            }
        }

        private int[,] GetKernel(int wh)
        {
            int[,] krnl = new int[wh, wh];

            for (int y = 0; y < wh; y++)
                for (int x = 0; x < wh; x++)
                    krnl[x, y] = 1;

            return krnl;
        }

        private unsafe byte bDilate(BitmapData bmSrc, Point pt, int[,] krnl)
        {
            int stride = bmSrc.Stride;
            int nWidth = bmSrc.Width;
            int nHeight = bmSrc.Height;

            byte* pSrc = (byte*)bmSrc.Scan0;
            int rH = krnl.GetLength(1) / 2;
            int cH = krnl.GetLength(0) / 2;

            int rH2 = rH;
            if ((krnl.GetLength(1) & 0x01) != 1)
                rH2--;
            int cH2 = cH;
            if ((krnl.GetLength(0) & 0x01) != 1)
                cH2--;

            int x = pt.X;
            int y = pt.Y;

            byte b = 0;

            for (int r = -rH; r <= rH2; r++)
            {
                if (y + r >= 0 && y + r < nHeight)
                {
                    for (int c = -cH; c <= cH2; c++)
                    {
                        if (x + c >= 0 && x + c < nWidth)
                        {
                            if (krnl[c + cH, r + rH] == 1)
                                b = Math.Max(b, pSrc[(r + y) * stride + (c + x) * 4]);
                        }
                    }
                }
            }

            return b;
        }

        private unsafe Bitmap GenerateMaskImg()
        {
            int w = this._w;
            int h = this._h;
            Bitmap mask = new Bitmap(w, h);
            BitmapData bmData = mask.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmT = this.Trimap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmT.Stride;

            Parallel.For(0, h, y =>
            //for(int y = 0; y < h; y++)
            {
                byte* p = (byte*)bmData.Scan0;
                byte* pT = (byte*)bmT.Scan0;
                p += y * stride;
                pT += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (pT[0] > 3 && pT[0] < 252)
                        p[0] = p[1] = p[2] = p[3] = 255;
                    else
                    {
                        p[0] = p[1] = p[2] = 0;
                        p[3] = 255;
                    }

                    p += 4;
                    pT += 4;
                }
            });

            this.Trimap.UnlockBits(bmT);
            mask.UnlockBits(bmData);

            return mask;
        }

        public static double[][] Testm()
        {
            double[][] A = new double[3][];

            A[0] = new double[3] { 1.17, 0.00000000e+00, 0.00000000e+00 };
            A[1] = new double[3] { 0.00000000e+00, 3.42, 3.41 };
            A[2] = new double[3] { 0.00000000e+00, 3.41, 3.42 };

            double[] b = new double[3] { 4.35, -3.48, -3.48 };
            double[] b2 = new double[3] { -3.48, 4.35, -3.48 };

            double[] x1 = MatrixEigen.MatLinSolveQR(A, b);
            double[] x2 = MatrixEigen.MatLinSolveQR(A, b2);

            double[][] x = new double[3][];

            x[0] = x1;
            x[1] = x2;
            //x[2] = x3;

            return x;
        }

        public void Dispose()
        {
            this.R = null;
            this.N = null;
            this.B = null;
            this.IndexMappings = null;
            this._pos = null;
            this.RF = null;

            if (this.Bmp != null)
                this.Bmp.Dispose();
            if (this.Trimap != null)
                this.Trimap.Dispose();
            if (this.MaskBitmap != null)
                this.MaskBitmap.Dispose();
        }

        public Bitmap SolveSystemGaussSeidel()
        {
            // matrix  elements
            double[] n = this.N;
            double[][] r = this.R;

            // b-vector
            double[] b = this.B;

            if(this.B == null)
                return null; 

            Dictionary<int, Dictionary<int, int>> indMaps = this.IndexMappings;

            Bitmap mask = this.MaskBitmap;
            Dictionary<int, Dictionary<int, int>> pos = this._pos;
            if (this.BlendParameters == null)
                return null;

            this.BlendParameters.AutoSOR = true;
            this.BlendParameters.AutoSORMode = AutoSORMode.WidthRelated;

            this.BlendParameters.PE = new ProgressEventArgs(this.BlendParameters.MaxIterations, 20, 100);

            double[] x_Cur = new double[b.Length];
            double[] r_Cur = new double[b.Length];

            double w = this.ComputeWForSOR(this.BlendParameters.AutoSOR, this.BlendParameters.AutoSORMode, b.Length, this._w, this._h, this.BlendParameters.MinPixelAmount);

            double[] result = GaussSeidelForPoissonEq_Div2(b, r, this.BlendParameters.MaxIterations, true,
                this.BlendParameters.DesiredMaxLinearError, w, this.BlendParameters.BGW, this.BlendParameters.PE, indMaps);

            Bitmap bOut2 = new Bitmap(this._w, this._h);

            BitmapData bmData = bOut2.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            unsafe
            {
                Parallel.For(0, this._h, y =>
                {
                    byte* p = (byte*)bmData.Scan0;
                    p += y * stride;

                    for (int x = 0; x < this._w; x++)
                    {
                        p[0] = p[1] = p[2] = (byte)Math.Max(Math.Min(result[y * this._w + x] * 255.0, 255), 0);
                        p[3] = 255;

                        p += 4;
                    }
                });
            }

            bOut2.UnlockBits(bmData);

            return bOut2;
        }

        public Bitmap SolveSystemGMRES()
        {
            // matrix  elements
            double[] n = this.N;
            double[][] r = this.R;

            // b-vector
            double[] b = this.B;

            if (this.B == null)
                return null;

            Dictionary<int, Dictionary<int, int>> indMaps = this.IndexMappings;

            Bitmap mask = this.MaskBitmap;
            Dictionary<int, Dictionary<int, int>> pos = this._pos;
            if (this.BlendParameters == null)
                return null;

            this.BlendParameters.AutoSOR = true;
            this.BlendParameters.AutoSORMode = AutoSORMode.WidthRelated;

            this.BlendParameters.PE = new ProgressEventArgs(this.BlendParameters.MaxIterations, 20, 100);
             
            double maxError = this.BlendParameters.DesiredMaxLinearError;
            bool preRelax = true;
            int cnt = 0;
            int innerIterations = this.BlendParameters.InnerIterations;
            int restart = this.BlendParameters.MaxIterations;

            double[] x_Cur = new double[b.Length];
            double[] x_CurRes = new double[b.Length];
            double[] r_Cur = new double[b.Length];
            double w = this.ComputeWForSOR(this.BlendParameters.AutoSOR, this.BlendParameters.AutoSORMode, b.Length, this._w, this._h, this.BlendParameters.MinPixelAmount);

            if (preRelax)
            {
                x_Cur = GaussSeidelForPoissonEq_Div_CG(b, r, PRE_OP_GSSOR, true,
                                this.BlendParameters.DesiredMaxLinearError, indMaps);
                GetResidual(r_Cur, x_Cur, b, r, indMaps);
            }
            else
                b.CopyTo(r_Cur, 0);

            r_Cur = JacobiPreconditioner_Apply(r_Cur, r, indMaps);
            double eVal = maxError;  
            double eValRes = maxError;

            // inner Alg following wiki.de: https://de.wikipedia.org/wiki/GMRES-Verfahren
            // outer Alg following my idea ...

            for (var z2 = 1; z2 <= restart; z2++)
            {
                if (this.BlendParameters.BGW.CancellationPending)
                    break;

                double[][] V = new double[innerIterations + 1][];
                double rNorm = GetNorm(r_Cur);

                if (rNorm == 0)
                    rNorm = 1;

                double[] t = new double[b.Length];
                V[0] = t;
                //for (int i = 0; i < b.Length; i++)
                Parallel.For(0, b.Length, i =>
                {
                    V[0][i] = r_Cur[i] / rNorm;
                });

                double[,] H = new double[innerIterations + 1, innerIterations];
                //double[,] H_old = new double[innerIterations + 1, innerIterations];
                double[][] wl = new double[innerIterations + 1][];
                double[] cs = new double[innerIterations + 1];
                double[] sn = new double[innerIterations + 1];
                double[] y = new double[innerIterations + 1];
                double[] gamma = new double[innerIterations + 1];
                gamma[0] = rNorm;

                int cnt2 = 0;

                if (z2 % 5 == 0 || z2 == 2)
                    ShowInfo?.Invoke(this, "### Restart: " + z2.ToString() + ". Max Error: " + Math.Abs(eVal).ToString());

                for (int j = 0; j <= innerIterations - 1; j++)
                {
                    if (this.BlendParameters.BGW.CancellationPending)
                        break;

                    // Arnoldi
                    double[] q = MultiplyA(V[j], r, indMaps);
                    q = JacobiPreconditioner_Apply(q, r, indMaps);

                    for (int i = 0; i <= j; i++)
                        H[i, j] = ScPr(V[i], q);

                    wl[j] = ComputeWj(q, H, V, j);
                    H[j + 1, j] = GetNorm(wl[j]);

                    if (H[j + 1, j] == 0)
                    {
                        innerIterations = j;
                        break;
                    }

                    //Parallel.For(0, innerIterations + 1, i2 =>
                    //{
                    //    for (int j2 = 0; j2 < innerIterations; j2++)
                    //        H_old[i2, j2] = H[i2, j2];
                    //});

                    // Givens
                    //serially is faster than parallelly
                    for (int i = 0; i < j; i++)
                    //Parallel.For(0, j, (i, loopState) =>
                    {
                        //    if (this.CancellationPending)
                        //        loopState.Break();

                        if (this.BlendParameters.BGW.CancellationPending)
                            break;

                        ApplyRot(H, cs, sn, i, j);
                    }//);

                    double beta = Math.Sqrt(H[j, j] * H[j, j] + H[j + 1, j] * H[j + 1, j]);
                    if (H[j, j] == 0)
                    {
                        sn[j + 1] = 1;
                        cs[j + 1] = 0;
                    }
                    sn[j + 1] = H[j + 1, j] / beta;
                    cs[j + 1] = H[j, j] / beta;

                    H[j, j] = beta;

                    gamma[j + 1] = -sn[j + 1] * gamma[j];
                    gamma[j] *= cs[j + 1];

                    // H(j, j) = cs(j + 1) * H(j, j) + sn(j + 1) * H(j + 1, j) '= beta

                    //lock (this._lockObject)
                    //{
                    //    if (_blendParameters.PE != null)
                    //    {
                    //        if (_blendParameters.PE.ImgWidthHeight < Int32.MaxValue)
                    //            _blendParameters.PE.CurrentProgress += 10 / (innerIterations / 5);
                    //        try
                    //        {
                    //            if (System.Convert.ToInt32(_blendParameters.PE.CurrentProgress) % _blendParameters.PE.PrgInterval == 0)
                    //                OnShowProgress(_blendParameters.PE.CurrentProgress);
                    //        }
                    //        catch
                    //        {
                    //        }
                    //    }
                    //}

                    eVal = gamma[j + 1];
                    cnt2 = j;

                    if (maxError != this.BlendParameters.DesiredMaxLinearError)
                        maxError = this.BlendParameters.DesiredMaxLinearError;

                    if (Math.Abs(gamma[j + 1]) / rNorm >= (maxError / rNorm) && j < innerIterations - 1)
                    {
                        V[j + 1] = Pr(wl[j], 1.0 / H[j + 1, j]);
                        H[j + 1, j] = 0;

                        if (this.BlendParameters.Sleep && this.BlendParameters.SleepAmount > 0)
                            Thread.Sleep(this.BlendParameters.SleepAmount);
                    }
                    else
                    {
                        for (int i = j; i >= 0; i--)
                        {
                            //if (this.BlendParameters.BGW.CancellationPending)
                            //    break;

                            double d = 0;

                            for (int l = i + 1; l <= j; l++)
                                d += H[i, l] * y[l];

                            y[i] = (gamma[i] - d) / H[i, i];
                        }

                        double[] rz = new double[b.Length - 1 + 1];

                        for (int i = 0; i <= j; i++)
                        {
                            //if (this.BlendParameters.BGW.CancellationPending)
                            //    break;

                            double[] z = Pr(V[i], y[i]);
                            for (int l = 0; l <= b.Length - 1; l++)
                                rz[l] += z[l];
                        }

                        for (int l = 0; l <= b.Length - 1; l++)
                            x_Cur[l] += rz[l];

                        break;
                    }
                }

                if (Math.Abs(eVal) / rNorm < (maxError / rNorm) || z2 == restart - 1)
                {
                    cnt = z2 * innerIterations + cnt2 + 1;
                    break;
                }
                else
                {
                    GetResidual(r_Cur, x_Cur, b, r, indMaps);
                    r_Cur = JacobiPreconditioner_Apply(r_Cur, r, indMaps);
                    cnt = (z2 - 1) * innerIterations + cnt2 + 1;

                    //diverging?
                    if (Math.Abs(eVal) < eValRes)
                        x_Cur.CopyTo(x_CurRes, 0);

                    if (Math.Abs(eVal) > 1000000000)
                    {
                        x_Cur = x_CurRes;
                        break;
                    }
                }
            }

            Bitmap bOut = new Bitmap(this._w, this._h);

            BitmapData bmData2 = bOut.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride2 = bmData2.Stride;

            unsafe
            {
                Parallel.For(0, this._h, y =>
                {
                    byte* p = (byte*)bmData2.Scan0;
                    p += y * stride2;

                    for (int x = 0; x < this._w; x++)
                    {
                        p[0] = p[1] = p[2] = (byte)Math.Max(Math.Min(x_Cur[y * this._w + x] * 255.0, 255), 0);
                        p[3] = 255;

                        p += 4;
                    }
                });
            }

            bOut.UnlockBits(bmData2);

            return bOut;
        }

        //internal Bitmap SolveSystemCSparse()
        //{
        //    /*
        //        from: https://en.wikipedia.org/wiki/Sparse_matrix

        //        <quote>

        //        Compressed sparse column (CSC or CCS)

        //        CSC is similar to CSR except that values are read first by column, a row index is stored for each value, 
        //        and column pointers are stored. For example, CSC is (val, row_ind, col_ptr), where val is an array of the
        //        (top-to-bottom, then left-to-right) non-zero values of the matrix; row_ind is the row indices corresponding 
        //        to the values; and, col_ptr is the list of val indexes where each column starts. 
        //        The name is based on the fact that column index information is compressed relative to the COO format. 
        //        One typically uses another format (LIL, DOK, COO) for construction. This format is efficient for arithmetic 
        //        operations, column slicing, and matrix-vector products. This is the traditional format for specifying a 
        //        sparse matrix in MATLAB (via the sparse function).

        //        <end quote>

        //        Hint: Use python (scipy.sparse.coo_matrix) to create a coo matrix and convert it to csc, 
        //        then look into the indices and indptrs.
        //    */

        //    int w = this._w;
        //    int h = this._h;

        //    int winWH = 3;

        //    int winWH2 = winWH * winWH;
        //    int wh = w * h;
        //    int ww = winWH2 * winWH2;

        //    double[] values = new double[R.Length * ww];
        //    int[] cin = new int[R.Length + 1];
        //    int[] rin = new int[R.Length * ww];

        //    for (int i = 0; i < rin.Length; i++)
        //    {
        //        values[i] = double.MinValue;
        //        rin[i] = Int32.MinValue;
        //    }

        //    //Setup the three arrays to pass to the Matrix creating method
        //    //values with values
        //    //cin as the column pointers
        //    //rin as the row indices

        //    //List<int> rowStart = new List<int>();
        //    //int[] colStart = new int[this.R.Length];

        //    //for (int i = 0; i < colStart.Length; i++)
        //    //    colStart[i] = Int32.MinValue;

        //    //foreach (int i in pos.Keys)
        //    //    foreach (int j in pos[i].Keys)
        //    //        if (colStart[j] == Int32.MinValue)
        //    //            colStart[j] = i;

        //    //cumulative sum, needed, since the rows are of different lenghts
        //    int imc = 0;

        //    for (int i = 0; i < this.IndexMappings.Count; i++)
        //    {
        //        cin[i] = imc;

        //        //rowStart.Add(this.IndexMappings[i][0]);

        //        if (this.IndexMappings.ContainsKey(i))
        //            for (int j = 0; j < this.IndexMappings[i].Count; j++)
        //            {
        //                if (this.R[i][j] != double.MinValue)
        //                {
        //                    int col = j;
        //                    //int row = i;

        //                    if (values[i * ww + col] == double.MinValue)
        //                    {
        //                        values[i * ww + col] = this.R[i][j]; // this.R[col][pos[col][i]];
        //                        rin[i * ww + col] = this.IndexMappings[i][j];
        //                    }
        //                }
        //            }

        //        if (this.IndexMappings.ContainsKey(i))
        //            imc += this.IndexMappings[i].Count;
        //    }

        //    values = values.Where(x => x != double.MinValue).ToArray();
        //    rin = rin.Where(x => x != Int32.MinValue).ToArray();
        //    cin[cin.Length - 1] = rin.Length;

        //    //setup the matrix
        //    var Amx = new CSparse.Double.SparseMatrix(this.R.Length, this.R.Length);
        //    Amx.ColumnPointers = cin.ToArray();
        //    Amx.RowIndices = rin.ToArray();
        //    Amx.Values = values.ToArray();

        //    var dm = DulmageMendelsohn.Generate(Amx, 1);
        //    if (dm.StructuralRank < cin.Length - 1)
        //        MessageBox.Show("rank");

        //    //let CSparse solve the system
        //    var ij = CSparse.Double.Factorization.SparseLU.Create(Amx, ColumnOrdering.MinimumDegreeAtPlusA, 1.0);
        //    double[] res = CSparse.Double.Vector.Create(Amx.ColumnCount, 0.0);
        //    ij.Solve(this.B, res);

        //    /*
        //        Note: I downloaded the nuget package, extracted the dll, copied it to the project folder and referenced it directly.
                
        //        You can also:

        //        from: https://learn.microsoft.com/en-us/answers/questions/1302681/could-not-load-file-or-assembly-system-runtime-com

        //        The error message does mention that "could not load file or assembly … The system cannot find the file specified".
        //        If you try to install the System.Runtime.CompilerServices.Unsafe.dll assembly(version 4.5.3 corresponding to 
        //        assembly version 4.0.4.1, you can run ildasm in Developer Command Prompt for Visual Studio and use ildasm tool 
        //        to check the assembly version of the dll file) and register it into GAC, the error should disappear.
        //        Please try to download the Nuget package from here: System.Runtime.CompilerServices.Unsafe. After downloading, 
        //        you will get a .nupkg file, move it to a clean folder. Rename the file extension to .zip, open it, open the 
        //        lib folder, open the net461 folder and you should see the dll file, copy the folder path, let’s say the path is 
        //        C:\XXXX\XXXX.
        //        Run Developer Command Prompt for Visual Studio as administrator, and run following commands:

        //        cd C:\XXXX\XXXX
        //        gacutil /i System.Runtime.CompilerServices.Unsafe.dll

                
        //        Alternatively:

        //        from: https://stackoverflow.com/questions/62764744/could-not-load-file-or-assembly-system-runtime-compilerservices-unsafe

        //        If you use Net Framework projects with xxx.config file, you could use bindingRedirect.

        //        Add these in app.config file or web.config file:

        //        <configuration>  
        //           <runtime>  
        //              <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">  
        //                 <dependentAssembly>  
        //                    <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe"  
        //                                      publicKeyToken="b03f5f7f11d50a3a"  
        //                                      culture="neutral" />  
        //                    <bindingRedirect oldVersion="0.0.0.0-4.0.4.1"  
        //                                     newVersion="4.0.4.1"/>  
        //                 </dependentAssembly>  
        //              </assemblyBinding>  
        //           </runtime>  
        //        </configuration> 
        //     */

        //    //create the output bmp
        //    Bitmap bOut = new Bitmap(this._w, this._h);

        //    BitmapData bmData = bOut.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        //    int stride = bmData.Stride;

        //    unsafe
        //    {
        //        Parallel.For(0, this._h, y =>
        //        {
        //            byte* p = (byte*)bmData.Scan0;
        //            p += y * stride;

        //            for (int x = 0; x < this._w; x++)
        //            {
        //                p[0] = p[1] = p[2] = (byte)Math.Max(Math.Min(res[y * this._w + x] * 255.0, 255), 0);
        //                p[3] = 255;

        //                p += 4;
        //            }
        //        });
        //    }

        //    bOut.UnlockBits(bmData);

        //    return bOut;
        //}

        private double[] GaussSeidelForPoissonEq_Div2(double[] b, double[][] r, int maxIterations,
            bool autoStop, double autoStopTolerance, double w, BackgroundWorker bgw, ProgressEventArgs pe,
            Dictionary<int, Dictionary<int, int>> indMaps)
        {
            double[] xVector = (Enumerable.Repeat(0.0, b.Length).ToArray());
            double[] xVectorOld = (Enumerable.Repeat(0.0, b.Length).ToArray());          
            double[] xVectorRes = (Enumerable.Repeat(0.0, b.Length).ToArray());

            int l = b.Length;
            double w1 = 1 - w;

            double maxErrOld = double.MaxValue;
            bool firstCopy = false;

            for (int p = 0; p < maxIterations; p++)
            {
                if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                    break;

                //for (int i = 0; i < l; i++) // rows
                //parallelizing this way is still a bit faster than serial processing (on my machine),
                //but not too much, for hard_to_solve pictures, since we have to lock in each iteration.
                Parallel.For(0, l, i =>
                {
                    double sigma = 0;
                    int d = 0;

                    if (indMaps.ContainsKey(i))
                    {
                        for (int q = 0; q < indMaps[i].Count(); q++)
                            //if (r[i][q] != double.MinValue)
                            //{
                            if (indMaps[i][q] != i)
                                sigma += r[i][q] * xVector[indMaps[i][q]];
                            else //if (indMaps[i][q] == i)
                                d = q;
                        //}
                        //else
                        //{
                        //    d = d;
                        //}
                    }

                    //xVector[i] = (w1 * xVector[i]) + (w * ((b[i] - sigma) / r[i][d]));

                    lock (this._lockObject)
                        xVector[i] += w * (((b[i] - sigma) / r[i][d]) - xVector[i]);
                });

                if (this.BlendParameters.Sleep && this.BlendParameters.SleepAmount > 0)
                    Thread.Sleep(this.BlendParameters.SleepAmount);

                if(autoStopTolerance != this.BlendParameters.DesiredMaxLinearError)
                    autoStopTolerance = this.BlendParameters.DesiredMaxLinearError;

                double maxErr = double.MaxValue;

                if (autoStop)
                {
                    if (CheckResult(xVector, xVectorOld, autoStopTolerance, ref maxErr))
                    {
                        //#if DEBUG
                        //                        Console.WriteLine("AutoStop at iteration: " + p.ToString());
                        //#endif
                        return xVector;
                    }

                    //diverging?
                    if (maxErr < maxErrOld)
                        xVector.CopyTo(xVectorRes, 0);

                    if (!firstCopy && maxErr > 1000000)
                    {
                        w = 1.0;
                        xVectorRes.CopyTo(xVector, 0);
                        firstCopy = true;
                    }

                    if (maxErr > 1000000000)
                        return xVectorRes;

                }

                xVector.CopyTo(xVectorOld, 0);

                lock (this._lockObject)
                {
                    if (pe != null)
                    {
                        if (pe.ImgWidthHeight < Int32.MaxValue)
                            pe.CurrentProgress += 1;
                        try
                        {
                            if (System.Convert.ToInt32(pe.CurrentProgress) % pe.PrgInterval == 0)
                            {
                                OnShowProgress(pe.CurrentProgress);
                                ShowInfo?.Invoke(this, "# " + p.ToString() + " Iterations finished. Max Error: " + maxErr.ToString("N7"));
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return xVector;
        }

        private void OnShowProgress(double currentProgress)
        {
            ShowProgess?.Invoke(this, this.BlendParameters.PE);
        }

        private double[] GaussSeidelForPoissonEq_Div_CG(double[] b, double[][] r, int maxIterations,
            bool autoStop, double autoStopTolerance, Dictionary<int, Dictionary<int, int>> indMaps)
        {
            double[] xVector = (Enumerable.Repeat(0.0, b.Length).ToArray());
            double[] xVectorOld = (Enumerable.Repeat(0.0, b.Length).ToArray());

            int l = b.Length;
            double w = Math.Max(Math.Min(2.0 / (1.0 + Math.Sin(Math.PI / (this._w + 1))), 2), 1);

            for (int p = 0; p < maxIterations; p++)
            {
                if (this.CancellationPending)
                    break;

                for (int i = 0; i < l; i++) // rows
                {
                    if (indMaps.ContainsKey(i))
                    {
                        double sigma = 0;

                        int diag = i;
                        int d = 0;

                        if (indMaps.ContainsKey(diag))
                        {
                            //slow but works
                            for (int q = 0; q < indMaps[diag].Count(); q++)
                                if (indMaps[diag][q] != diag)
                                    sigma += r[i][q] * xVector[indMaps[diag][q]];
                                else if (indMaps[diag][q] == diag)
                                    d = q;

                            //xVector[i] = xVectorOld[d] + (w * (b[i] + sigma - (r[i][d] * xVectorOld[d]))) / r[i][d];
                            //slow, but works
                            xVector[i] = (b[i] - sigma) / r[i][d];
                        }
                        else
                            xVector[i] = b[i];
                    }
                }

                double maxErr = double.MaxValue;

                if (autoStop)
                {
                    if (CheckResult(xVector, xVectorOld, autoStopTolerance, ref maxErr))
                        // #If DEBUG Then
                        // Console.WriteLine("AutoStop at iteration: " & p.ToString())
                        // #End If
                        return xVector;
                }

                xVector.CopyTo(xVectorOld, 0);
            }

            return xVector;
        }

        private bool CheckResult(double[] solvedVector, double[] solvedVectorA, double autoStopTolerance)
        {
            bool b = false;
            double max = 0;

            for (int i = 0; i <= solvedVector.Length - 1; i++)
            {
                double e = Math.Abs(solvedVector[i] - solvedVectorA[i]);

                if (e > autoStopTolerance)
                {
                    b = true;
                    break;
                }

                if (e > max)
                    max = e;
            }
            // #If DEBUG Then
            // Console.WriteLine("max error at current iteration: " & max.ToString())
            // #End If
            return !b;
        }

        private bool CheckResult(double[] solvedVector, double[] solvedVectorA, double autoStopTolerance, ref double err)
        {
            bool b = false;
            double max = 0;

            for (int i = 0; i <= solvedVector.Length - 1; i++)
            {
                double e = Math.Abs(solvedVector[i] - solvedVectorA[i]);

                if (e > autoStopTolerance)
                {
                    max = e;
                    b = true;
                    break;
                }

                if (e > max)
                    max = e;
            }

            err = max;

            // #If DEBUG Then
            // Console.WriteLine("max error at current iteration: " & max.ToString())
            // #End If
            return !b;
        }

        private double[] JacobiPreconditioner_Apply(double[] r, double[][] A, Dictionary<int, Dictionary<int, int>> indMaps)
        {
            double[] rHat = (Enumerable.Repeat(0.0, r.Length).ToArray());
            r.CopyTo(rHat, 0);

            int l = r.Length;

            //Jacobi preconditioner
            //for (int i = 0; i < l; i++) // rows
            Parallel.For(0, l, i =>
            {
                int diag = 0;

                if (indMaps.ContainsKey(i))
                {
                    for (int q = 0; q < indMaps[i].Count(); q++)
                    {
                        if (indMaps[i][q] == i)
                            diag = q;
                    }

                    double d = rHat[indMaps[i][diag]] / A[i][diag];

                    if (!double.IsInfinity(d) && !double.IsNaN(d))
                        rHat[i] = d;
                }
            });

            return rHat;
        }

        private void GetResidual(double[] r_Cur, double[] x_Cur, double[] bVector, double[][] r, Dictionary<int, Dictionary<int, int>> indMaps)
        {
            if (r_Cur != null)
            {
                for (int i = 0; i < r_Cur.Length; i++)
                {
                    double v = 0;

                    if (r != null)
                    {
                        if (indMaps.ContainsKey(i))
                        {
                            int d = 0;

                            for (int q = 0; q < indMaps[i].Count(); q++)
                                if (indMaps[i][q] != i)
                                    v += r[i][q] * x_Cur[indMaps[i][q]];
                                else if (indMaps[i][q] == i)
                                    d = q;

                            r_Cur[i] = bVector[i] - ((r[i][d] * x_Cur[indMaps[i][d]]) + v);
                        }
                    }
                }
            }
        }

        private void ApplyRot(double[,] h, double[] cs, double[] sn, int i, int j)
        {
            double temp = cs[i + 1] * h[i, j] + sn[i + 1] * h[i + 1, j];
            h[i + 1, j] = -sn[i + 1] * h[i, j] + cs[i + 1] * h[i + 1, j];
            h[i, j] = temp;
        }

        private void ApplyRot(double[,] h, double[,] h_old, double[] cs, double[] sn, int i, int j)
        {
            double temp = cs[i + 1] * h[i, j] + sn[i + 1] * h_old[i + 1, j];
            h[i + 1, j] = -sn[i + 1] * h[i, j] + cs[i + 1] * h_old[i + 1, j];
            h[i, j] = temp;
        }

        private double[] ComputeWj(double[] q, double[,] H, double[][] V, int j)
        {
            double[] w = new double[q.Length];
            double[] rz = new double[q.Length];

            for (int i = 0; i <= j; i++)
            {
                double[] z = Pr(V[i], H[i, j]);
                for (int l = 0; l < q.Length; l++)
                    rz[l] += z[l];
            }

            //for (int l = 0; l < q.Length; l++)
            Parallel.For(0, q.Length, l =>
            {
                w[l] = q[l] - rz[l];
            });

            return w;
        }

        private double[] MultiplyA(double[] y, double[][] r, Dictionary<int, Dictionary<int, int>> indMaps)
        {
            double[] z = new double[y.Length];

            //for (int i = 0; i < z.Length; i++)
            Parallel.For(0, z.Length, i =>
            {
                double v = 0;

                if (indMaps.ContainsKey(i))
                {
                    for (int q = 0; q < indMaps[i].Count(); q++)
                        v += r[i][q] * y[indMaps[i][q]];

                    z[i] = v;
                }
            });

            return z;
        }

        private double[] Pr(double[] v, double d)
        {
            double[] l = new double[v.Length];
            for (int i = 0; i < v.Length; i++)
                l[i] = v[i] * d;

            return l;
        }

        private double ScPr(double[] v1, double[] v2)
        {
            if (v1.Length != v2.Length)
                throw new Exception("Vectors must be of same length.");

            double d = 0.0;
            //for (int i = 0; i < v1.Length; i++)
            Parallel.For(0, v1.Length, () => 0.0, (i, loopState, iD) =>
            {
                iD += v1[i] * v2[i];
                return iD;
            }, (iD =>
            {
                lock (this._lockObject)
                    d += iD;
            }));

            return d;
        }

        private double GetNorm(double[] r)
        {
            double e = 0.0;
            //for (int i = 0; i < r.Length; i++)
            Parallel.For(0, r.Length, () => 0.0, (i, loopState, iD) =>
            {
                iD += r[i] * r[i];
                return iD;
            },
            (iD =>
            {
                lock (this._lockObject)
                    e += iD;
            }));

            return Math.Sqrt(e);
        }

        private double ComputeWForSOR(bool computeWeightForSOR, AutoSORMode autoSORMode, int pixelsCount, int upperImgWidth, int upperImgHeight, int minPxAmount)
        {
            if (!computeWeightForSOR)
            {
                double upperImgAmount = upperImgWidth * 4.0 / 3.0;
                return Math.Max(Math.Min(2 / (1 + Math.Sin(Math.PI / (Math.Sqrt(upperImgAmount)))), 2), 1);
            }
            else if (autoSORMode == AutoSORMode.OneAndAHalf)
                return 1.5;
            else
            {
                // note that this is stronger than in the Multigrid implementation
                if (minPxAmount == 0)
                    minPxAmount = 12;
                pixelsCount = Math.Max(pixelsCount, minPxAmount);
                double ratio = pixelsCount / (double)(upperImgWidth * upperImgHeight);
                double upperImgAmount = upperImgWidth * ratio;
                if (autoSORMode == AutoSORMode.SqrtWidthRelated)
                {
                    upperImgAmount = Math.Sqrt(upperImgAmount);
                    upperImgAmount *= 12;
                }
                return Math.Max(Math.Min(2 / (1 + Math.Sin(Math.PI / upperImgAmount)), 2), 1);
            }
        }
    }
}
