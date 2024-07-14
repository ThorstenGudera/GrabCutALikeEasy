using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using ChainCodeFinder;
using System.Reflection;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel;

namespace GetAlphaMatte
{
    //translation from https://github.com/MarcoForte/bayesian-matting
    //and extended by translating parts of https://github.com/varunjain3/BayesianMatting
    public class BayesianMatteOp : IDisposable
    {
        private int _w;
        private int _h;
        private List<Point> _BGIndexes;
        private List<Point> _FGIndexes;
        private List<Point> _UKIndexes;

        public Bitmap Bmp { get; private set; }
        public Bitmap Trimap { get; private set; }
        public Bitmap Alpha { get; private set; }
        public Bitmap Foreground { get; private set; }
        public Bitmap Background { get; private set; }
        public Bitmap UnknownMaskBmp { get; private set; }
        public int WindowSize { get; private set; } = 25;
        public double StdDev { get; private set; } = 8;
        public double[,] GaussianKernel { get; private set; }
        public int MinNeighbors { get; private set; } = 10;
        public int WindowMaxSize { get; private set; } = 501;
        public BackgroundWorker BGW { get; set; }
        public bool AutoBreak { get; set; } = false;
        public int AutoBreakSize { get; set; } = 100;
        public List<ExtPoint> UnsolvedPoints { get; private set; }

        public static int Cnt = 0;

        //public bool RunSerially { get; internal set; }

        private object _lockObject = new object();

        public event EventHandler<ProgressEventArgs> UpdateProgress;
        public event EventHandler<string> ShowInfo;

        public BayesianMatteOp(Bitmap bmp, Bitmap trimap)
        {
            this._w = bmp.Width;
            this._h = bmp.Height;

            this.Bmp = bmp;
            this.Trimap = trimap;
            this.Foreground = new Bitmap(this._w, this._h);
            this.Background = new Bitmap(this._w, this._h);
            this.UnknownMaskBmp = new Bitmap(this._w, this._h);

            this.GaussianKernel = GetGaussianKernel(this.WindowSize, this.StdDev);
            ReadTrimap();
            ReadInBitmaps();
            GetUnknownMaskBmp();

            this.Alpha = new Bitmap(this._w, this._h);
            using (Graphics gx = Graphics.FromImage(this.Alpha))
                gx.Clear(Color.Black);

            BitmapData bData = this.Alpha.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bData.Stride;

            unsafe
            {
                byte* p = (byte*)bData.Scan0;
                foreach (Point pt in this._FGIndexes)
                    p[pt.X * 4 + pt.Y * stride] = p[pt.X * 4 + pt.Y * stride + 1] = p[pt.X * 4 + pt.Y * stride + 2] = 255;
            }

            this.Alpha.UnlockBits(bData);
        }

        private unsafe void GetUnknownMaskBmp()
        {
            BitmapData bData = this.UnknownMaskBmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bData.Stride;

            byte* p = (byte*)bData.Scan0;
            for (int j = 0; j < this._UKIndexes.Count; j++)
            {
                p[this._UKIndexes[j].X * 4 + this._UKIndexes[j].Y * stride] =
                p[this._UKIndexes[j].X * 4 + this._UKIndexes[j].Y * stride + 1] =
                p[this._UKIndexes[j].X * 4 + this._UKIndexes[j].Y * stride + 2] =
                p[this._UKIndexes[j].X * 4 + this._UKIndexes[j].Y * stride + 3] = 255;
            }

            this.UnknownMaskBmp.UnlockBits(bData);
        }

        private unsafe void ReadInBitmaps()
        {
            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmFG = this.Foreground.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmBG = this.Background.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            int cnt = this._FGIndexes.Count;
            Parallel.For(0, cnt, j =>
            {
                byte* p = (byte*)bmData.Scan0;
                byte* pF = (byte*)bmFG.Scan0;
                int x = this._FGIndexes[j].X;
                int y = this._FGIndexes[j].Y;

                pF[x * 4 + y * stride] = p[x * 4 + y * stride];
                pF[x * 4 + y * stride + 1] = p[x * 4 + y * stride + 1];
                pF[x * 4 + y * stride + 2] = p[x * 4 + y * stride + 2];
                pF[x * 4 + y * stride + 3] = 255;
            });

            int cnt2 = this._BGIndexes.Count;
            Parallel.For(0, cnt2, j =>
            {
                byte* p = (byte*)bmData.Scan0;
                byte* pB = (byte*)bmBG.Scan0;
                int x = this._BGIndexes[j].X;
                int y = this._BGIndexes[j].Y;

                pB[x * 4 + y * stride] = p[x * 4 + y * stride];
                pB[x * 4 + y * stride + 1] = p[x * 4 + y * stride + 1];
                pB[x * 4 + y * stride + 2] = p[x * 4 + y * stride + 2];
                pB[x * 4 + y * stride + 3] = 255;
            });

            this.Bmp.UnlockBits(bmData);
            this.Foreground.UnlockBits(bmFG);
            this.Background.UnlockBits(bmBG);
        }

        public unsafe int ComputeBayesianMatte2(double colVar, int allPtsCount)
        {
            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;
            //int cnt = 0;

            BayesianMatteOp.Cnt = 0;

            BitmapData bmAlpha = this.Alpha.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmFG = this.Foreground.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmBG = this.Background.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmTrimap = this.Trimap.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte* p = (byte*)bmData.Scan0;

            Console.WriteLine(p[0].ToString() + "," + p[1].ToString() + "," + p[2].ToString());

            Console.WriteLine(this._UKIndexes.Count.ToString());
            List<ExtPoint> pts = GetRegions(this.UnknownMaskBmp);

            Console.WriteLine((pts.Count == this._UKIndexes.Count).ToString());

            ProgressEventArgs pe = new ProgressEventArgs(pts.Count, 0, 10);
            int updateEach = 500;
            int ic = 0;

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Alpha.UnlockBits(bmAlpha);
                this.Foreground.UnlockBits(bmFG);
                this.Background.UnlockBits(bmBG);
                this.Trimap.UnlockBits(bmTrimap);
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(10);

            int nextBoundary = 0;

            if (pts.Count > 0)
            {
                int sum = pts.Sum(a => a.V);

                while (sum < this._UKIndexes.Count)
                {
                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    {
                        this.Alpha.UnlockBits(bmAlpha);
                        this.Foreground.UnlockBits(bmFG);
                        this.Background.UnlockBits(bmBG);
                        this.Trimap.UnlockBits(bmTrimap);
                        this.Bmp.UnlockBits(bmData);
                        return -1;
                    }

                    if (sum >= nextBoundary)
                    {
                        nextBoundary += sum + updateEach;
                        ShowInfo?.Invoke(this, "### Iteration: " + BayesianMatteOp.Cnt.ToString() + " of " + allPtsCount + ". WindowSize: " + WindowSize.ToString());
                    }

                    int last_n = sum;

                    if (/* !this.RunSerially && */ pts.Where(a => a.V == 0).Count() > 1000)
                    {
                        //for (int j = 0; j < pts.Count; j++)
                        Parallel.For(0, pts.Count, (j, loopState) =>
                        {
                            if (pts[j].V == 1)
                                return; // continue;

                            int x = pts[j].X;
                            int y = pts[j].Y;

                            double[] c = new double[3] { p[x * 4 + y * stride + 2] / 255.0, p[x * 4 + y * stride + 1] / 255.0, p[x * 4 + y * stride] / 255.0 };
                            int minNb = this.MinNeighbors;

                            double[,] alpha = GetAlphaWindow(pts[j].Point, bmTrimap);

                            double[,] alphaFG = new double[this.WindowSize, this.WindowSize];
                            double[,] alphaBG = new double[this.WindowSize, this.WindowSize];
                            for (int y2 = 0; y2 < this.WindowSize; y2++)
                                for (int x2 = 0; x2 < this.WindowSize; x2++)
                                {
                                    if (alpha[x2, y2] == 1.0)
                                        alphaFG[x2, y2] = this.GaussianKernel[x2, y2]; // alpha[x2, y2] * alpha[x2, y2] * this.GaussianKernel[x2, y2];
                                    else if (alpha[x2, y2] == 0.0)
                                        alphaBG[x2, y2] = this.GaussianKernel[x2, y2]; // (1.0 - alpha[x2, y2]) * (1.0 - alpha[x2, y2]) * this.GaussianKernel[x2, y2];
                                    else
                                        alpha[x2, y2] = double.MinValue;
                                }

                            double[,] f = GetWindow(bmFG, bmTrimap, pts[j].Point, 1);
                            List<double[]> fg_px = new List<double[]>();
                            List<double> fg_wghts = new List<double>();
                            for (int y2 = 0; y2 < this.WindowSize; y2++)
                                for (int x2 = 0; x2 < this.WindowSize; x2++)
                                    if (alphaFG[x2, y2] > 0)
                                    {
                                        fg_px.Add(new double[] { f[x2 * 3 + 2, y2] / 255.0, f[x2 * 3 + 1, y2] / 255.0, f[x2 * 3, y2] / 255.0 });
                                        fg_wghts.Add(alphaFG[x2, y2]);
                                    }

                            double[,] b = GetWindow(bmBG, bmTrimap, pts[j].Point, 0);
                            List<double[]> bg_px = new List<double[]>();
                            List<double> bg_wghts = new List<double>();
                            for (int y2 = 0; y2 < this.WindowSize; y2++)
                                for (int x2 = 0; x2 < this.WindowSize; x2++)
                                    if (alphaBG[x2, y2] > 0)
                                    {
                                        bg_px.Add(new double[] { b[x2 * 3 + 2, y2] / 255.0, b[x2 * 3 + 1, y2] / 255.0, b[x2 * 3, y2] / 255.0 });
                                        bg_wghts.Add(alphaBG[x2, y2]);
                                    }

                            if (fg_wghts.Count < minNb || bg_wghts.Count < minNb)
                                return; // continue;

                            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                                return;

                            OrchardBouman_bgr obf = new OrchardBouman_bgr();
                            obf.ClusterPt(fg_px, fg_wghts, 0.05);
                            List<double[]> muF = obf.Mu;
                            List<List<double[]>> sigmaF = obf.Sigma;
                            obf.ClusterPt(bg_px, bg_wghts, 0.05);
                            List<double[]> muB = obf.Mu;
                            List<List<double[]>> sigmaB = obf.Sigma;

                            double alphaInit = alpha.Cast<double>().Where(a => a != double.MinValue).Average();

                            EQSysResult res = Solve(muF.ToArray(), sigmaF, muB.ToArray(), sigmaB, c, colVar, alphaInit, 50, 1e-6);

                            SetPixels(res, bmAlpha, bmFG, bmBG, x, y, stride);

                            pts[j].V = 1;

                            Interlocked.Increment(ref ic);
                            Interlocked.Increment(ref BayesianMatteOp.Cnt);

                            if (ic % updateEach == 0)
                            {
                                lock (_lockObject)
                                {
                                    if (pe.ImgWidthHeight < Int32.MaxValue)
                                        pe.CurrentProgress += updateEach;

                                    try
                                    {
                                        if ((int)pe.CurrentProgress % pe.PrgInterval == 0)
                                            OnUpdateProgress(pe);
                                    }
                                    catch
                                    {

                                    }

                                    //if (sum >= nextBoundary)
                                    {
                                        //nextBoundary += sum + updateEach;
                                        ShowInfo?.Invoke(this, "### Iteration: " + BayesianMatteOp.Cnt.ToString() + " of " + allPtsCount + ". WindowSize: " + WindowSize.ToString());
                                    }
                                }
                            }
                        });

                        if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                        {
                            this.Alpha.UnlockBits(bmAlpha);
                            this.Foreground.UnlockBits(bmFG);
                            this.Background.UnlockBits(bmBG);
                            this.Trimap.UnlockBits(bmTrimap);
                            this.Bmp.UnlockBits(bmData);
                            return -1;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < pts.Count; j++)
                        {
                            if (pts[j].V == 1)
                                continue;

                            int x = pts[j].X;
                            int y = pts[j].Y;

                            double[] c = new double[3] { p[x * 4 + y * stride + 2] / 255.0, p[x * 4 + y * stride + 1] / 255.0, p[x * 4 + y * stride] / 255.0 };
                            int minNb = this.MinNeighbors;

                            double[,] alpha = GetAlphaWindow(pts[j].Point, bmTrimap);

                            double[,] alphaFG = new double[this.WindowSize, this.WindowSize];
                            double[,] alphaBG = new double[this.WindowSize, this.WindowSize];
                            for (int y2 = 0; y2 < this.WindowSize; y2++)
                                for (int x2 = 0; x2 < this.WindowSize; x2++)
                                {
                                    if (alpha[x2, y2] == 1.0)
                                        alphaFG[x2, y2] = this.GaussianKernel[x2, y2]; // alpha[x2, y2] * alpha[x2, y2] * this.GaussianKernel[x2, y2];
                                    else if (alpha[x2, y2] == 0.0)
                                        alphaBG[x2, y2] = this.GaussianKernel[x2, y2]; // (1.0 - alpha[x2, y2]) * (1.0 - alpha[x2, y2]) * this.GaussianKernel[x2, y2];
                                    else
                                        alpha[x2, y2] = double.MinValue;
                                }

                            double[,] f = GetWindow(bmFG, bmTrimap, pts[j].Point, 1);
                            List<double[]> fg_px = new List<double[]>();
                            List<double> fg_wghts = new List<double>();
                            for (int y2 = 0; y2 < this.WindowSize; y2++)
                                for (int x2 = 0; x2 < this.WindowSize; x2++)
                                    if (alphaFG[x2, y2] > 0)
                                    {
                                        fg_px.Add(new double[] { f[x2 * 3 + 2, y2] / 255.0, f[x2 * 3 + 1, y2] / 255.0, f[x2 * 3, y2] / 255.0 });
                                        fg_wghts.Add(alphaFG[x2, y2]);
                                    }

                            double[,] b = GetWindow(bmBG, bmTrimap, pts[j].Point, 0);
                            List<double[]> bg_px = new List<double[]>();
                            List<double> bg_wghts = new List<double>();
                            for (int y2 = 0; y2 < this.WindowSize; y2++)
                                for (int x2 = 0; x2 < this.WindowSize; x2++)
                                    if (alphaBG[x2, y2] > 0)
                                    {
                                        bg_px.Add(new double[] { b[x2 * 3 + 2, y2] / 255.0, b[x2 * 3 + 1, y2] / 255.0, b[x2 * 3, y2] / 255.0 });
                                        bg_wghts.Add(alphaBG[x2, y2]);
                                    }

                            if (fg_wghts.Count < minNb || bg_wghts.Count < minNb)
                                continue;

                            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                            {
                                this.Alpha.UnlockBits(bmAlpha);
                                this.Foreground.UnlockBits(bmFG);
                                this.Background.UnlockBits(bmBG);
                                this.Trimap.UnlockBits(bmTrimap);
                                this.Bmp.UnlockBits(bmData);
                                return -1;
                            }

                            OrchardBouman_bgr obf = new OrchardBouman_bgr();
                            obf.ClusterPt(fg_px, fg_wghts, 0.05);
                            List<double[]> muF = obf.Mu;
                            List<List<double[]>> sigmaF = obf.Sigma;
                            obf.ClusterPt(bg_px, bg_wghts, 0.05);
                            List<double[]> muB = obf.Mu;
                            List<List<double[]>> sigmaB = obf.Sigma;

                            double alphaInit = alpha.Cast<double>().Where(a => a != double.MinValue).Average();

                            EQSysResult res = Solve(muF.ToArray(), sigmaF, muB.ToArray(), sigmaB, c, colVar, alphaInit, 50, 1e-6);

                            SetPixels(res, bmAlpha, bmFG, bmBG, x, y, stride);

                            pts[j].V = 1;

                            ic++;
                            BayesianMatteOp.Cnt++;

                            if (ic % updateEach == 0)
                            {
                                if (pe.ImgWidthHeight < Int32.MaxValue)
                                    pe.CurrentProgress += updateEach;

                                try
                                {
                                    if ((int)pe.CurrentProgress % pe.PrgInterval == 0)
                                        OnUpdateProgress(pe);
                                }
                                catch
                                {

                                }

                                //if (sum >= nextBoundary)
                                {
                                    //nextBoundary += sum + updateEach;
                                    ShowInfo?.Invoke(this, "### Iteration: " + BayesianMatteOp.Cnt.ToString() + " of " + allPtsCount + ". WindowSize: " + WindowSize.ToString());
                                }
                            }
                        }
                    }

                    sum = pts.Sum(a => a.V);

                    if (sum == last_n)
                    {
                        //Console.WriteLine(sum.ToString());
                        this.WindowSize += 2;
                        this.GaussianKernel = GetGaussianKernel(this.WindowSize, this.StdDev);
                    }
                    else
                    {
                        if (this.WindowSize > 25)
                        {
                            this.WindowSize = 25;
                            this.GaussianKernel = GetGaussianKernel(this.WindowSize, this.StdDev);
                        }
                    }

                    if (this.AutoBreak)
                    {
                        if (this.WindowSize > this.AutoBreakSize)
                            break;
                    }
                    else
                    {
                        if (this.WindowSize > Math.Max(this._w, this._h))
                            break;
                    }
                }
            }

            List<ExtPoint> points = pts.Where(a => a.V == 0).ToList();
            this.UnsolvedPoints = points;

            this.Alpha.UnlockBits(bmAlpha);
            this.Foreground.UnlockBits(bmFG);
            this.Background.UnlockBits(bmBG);
            this.Trimap.UnlockBits(bmTrimap);
            this.Bmp.UnlockBits(bmData);

            return 0;
        }

        public virtual void OnUpdateProgress(ProgressEventArgs e)
        {
            UpdateProgress?.Invoke(this, e);
        }

        public unsafe int ComputeBayesianMatte(double colVar)
        {
            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;
            //int cnt = 0;

            BayesianMatteOp.Cnt = 0;

            BitmapData bmAlpha = this.Alpha.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmFG = this.Foreground.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmBG = this.Background.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmTrimap = this.Trimap.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte* p = (byte*)bmData.Scan0;

            Console.WriteLine(p[0].ToString() + "," + p[1].ToString() + "," + p[2].ToString());

            Console.WriteLine(this._UKIndexes.Count.ToString());
            List<ExtPoint> pts = GetRegions(this.UnknownMaskBmp);

            Console.WriteLine((pts.Count == this._UKIndexes.Count).ToString());

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Alpha.UnlockBits(bmAlpha);
                this.Foreground.UnlockBits(bmFG);
                this.Background.UnlockBits(bmBG);
                this.Trimap.UnlockBits(bmTrimap);
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(10);

            double prg = 0;
            double d = 90.0 / pts.Count;

            int nextBoundary = 0;
            int ic = 0;
            int updateEach = 500;

            if (pts.Count > 0)
            {
                int sum = pts.Sum(a => a.V);
                while (sum < this._UKIndexes.Count)
                {
                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    {
                        this.Alpha.UnlockBits(bmAlpha);
                        this.Foreground.UnlockBits(bmFG);
                        this.Background.UnlockBits(bmBG);
                        this.Trimap.UnlockBits(bmTrimap);
                        this.Bmp.UnlockBits(bmData);
                        return -1;
                    }

                    if (sum >= nextBoundary)
                    {
                        nextBoundary += sum + updateEach;
                        ShowInfo?.Invoke(this, "### Iteration: " + sum.ToString() + " of " + this._UKIndexes.Count + ". WindowSize: " + WindowSize.ToString());
                    }

                    int last_n = sum;

                    for (int j = 0; j < pts.Count; j++)
                    {
                        if (pts[j].V == 1)
                            continue;

                        int x = pts[j].X;
                        int y = pts[j].Y;

                        double[] c = new double[3] { p[x * 4 + y * stride + 2] / 255.0, p[x * 4 + y * stride + 1] / 255.0, p[x * 4 + y * stride] / 255.0 };
                        int minNb = this.MinNeighbors;

                        double[,] alpha = GetAlphaWindow(pts[j].Point, bmTrimap);

                        double[,] alphaFG = new double[this.WindowSize, this.WindowSize];
                        double[,] alphaBG = new double[this.WindowSize, this.WindowSize];
                        for (int y2 = 0; y2 < this.WindowSize; y2++)
                            for (int x2 = 0; x2 < this.WindowSize; x2++)
                            {
                                if (alpha[x2, y2] == 1.0)
                                    alphaFG[x2, y2] = this.GaussianKernel[x2, y2]; // alpha[x2, y2] * alpha[x2, y2] * this.GaussianKernel[x2, y2];
                                else if (alpha[x2, y2] == 0.0)
                                    alphaBG[x2, y2] = this.GaussianKernel[x2, y2]; // (1.0 - alpha[x2, y2]) * (1.0 - alpha[x2, y2]) * this.GaussianKernel[x2, y2];
                                else
                                    alpha[x2, y2] = double.MinValue;
                            }

                        double[,] f = GetWindow(bmFG, bmTrimap, pts[j].Point, 1);
                        List<double[]> fg_px = new List<double[]>();
                        List<double> fg_wghts = new List<double>();
                        for (int y2 = 0; y2 < this.WindowSize; y2++)
                            for (int x2 = 0; x2 < this.WindowSize; x2++)
                                if (alphaFG[x2, y2] > 0)
                                {
                                    fg_px.Add(new double[] { f[x2 * 3 + 2, y2] / 255.0, f[x2 * 3 + 1, y2] / 255.0, f[x2 * 3, y2] / 255.0 });
                                    fg_wghts.Add(alphaFG[x2, y2]);
                                }

                        double[,] b = GetWindow(bmBG, bmTrimap, pts[j].Point, 0);
                        List<double[]> bg_px = new List<double[]>();
                        List<double> bg_wghts = new List<double>();
                        for (int y2 = 0; y2 < this.WindowSize; y2++)
                            for (int x2 = 0; x2 < this.WindowSize; x2++)
                                if (alphaBG[x2, y2] > 0)
                                {
                                    bg_px.Add(new double[] { b[x2 * 3 + 2, y2] / 255.0, b[x2 * 3 + 1, y2] / 255.0, b[x2 * 3, y2] / 255.0 });
                                    bg_wghts.Add(alphaBG[x2, y2]);
                                }

                        if (fg_wghts.Count < minNb || bg_wghts.Count < minNb)
                            continue;

                        if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                        {
                            this.Alpha.UnlockBits(bmAlpha);
                            this.Foreground.UnlockBits(bmFG);
                            this.Background.UnlockBits(bmBG);
                            this.Trimap.UnlockBits(bmTrimap);
                            this.Bmp.UnlockBits(bmData);
                            return -1;
                        }

                        if (ic % updateEach == 0)
                        {
                            //if (sum >= nextBoundary)
                            {
                                //nextBoundary += sum + 500;
                                ShowInfo?.Invoke(this, "### Iteration: " + sum.ToString() + " of " + this._UKIndexes.Count + ". WindowSize: " + WindowSize.ToString());
                            }
                        }

                        ic++;

                        OrchardBouman_bgr obf = new OrchardBouman_bgr();
                        obf.ClusterPt(fg_px, fg_wghts, 0.05);
                        List<double[]> muF = obf.Mu;
                        List<List<double[]>> sigmaF = obf.Sigma;
                        obf.ClusterPt(bg_px, bg_wghts, 0.05);
                        List<double[]> muB = obf.Mu;
                        List<List<double[]>> sigmaB = obf.Sigma;

                        double alphaInit = alpha.Cast<double>().Where(a => a != double.MinValue).Average();

                        EQSysResult res = Solve(muF.ToArray(), sigmaF, muB.ToArray(), sigmaB, c, colVar, alphaInit, 50, 1e-6);

                        SetPixels(res, bmAlpha, bmFG, bmBG, x, y, stride);

                        pts[j].V = 1;

                        prg += d;

                        if (this.BGW != null && this.BGW.WorkerReportsProgress && (int)prg % 10 == 0)
                            this.BGW.ReportProgress(10 + (int)prg);
                    };

                    sum = pts.Sum(a => a.V);
                    if (sum == last_n)
                    {
                        //Console.WriteLine(sum.ToString());
                        this.WindowSize += 2;
                        this.GaussianKernel = GetGaussianKernel(this.WindowSize, this.StdDev);
                    }
                    else
                    {
                        if (this.WindowSize > 25)
                        {
                            this.WindowSize = 25;
                            this.GaussianKernel = GetGaussianKernel(this.WindowSize, this.StdDev);
                        }
                    }

                    //if (this.WindowSize < this.WindowMaxSize)
                    //    break;

                    if (this.AutoBreak)
                    {
                        if (this.WindowSize > this.AutoBreakSize)
                            break;
                    }
                    else
                    {
                        if (this.WindowSize > Math.Max(this._w, this._h))
                            break;
                    }
                }
            }

            List<ExtPoint> points = pts.Where(a => a.V == 0).ToList();
            this.UnsolvedPoints = points;

            this.Alpha.UnlockBits(bmAlpha);
            this.Foreground.UnlockBits(bmFG);
            this.Background.UnlockBits(bmBG);
            this.Trimap.UnlockBits(bmTrimap);
            this.Bmp.UnlockBits(bmData);

            return 0;
        }

        private unsafe List<ExtPoint> GetRegions(Bitmap b)
        {
            List<ExtPoint> res = new List<ExtPoint>();

            BitmapData bmData = b.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;

            for (int y = 0; y < this._h; y++)
                for (int x = 0; x < this._w; x++)
                    if (p[x * 4 + y * stride] == 255)
                        res.Add(new ExtPoint(x, y, 0));

            b.UnlockBits(bmData);

            return res;
        }

        private unsafe void SetPixels(EQSysResult res, BitmapData bmAlpha, BitmapData bmFG, BitmapData bmBG, int x, int y, int stride)
        {
            byte* pAlpha = (byte*)bmAlpha.Scan0;
            pAlpha[x * 4 + y * stride] = pAlpha[x * 4 + y * stride + 1] = pAlpha[x * 4 + y * stride + 2] = (byte)Math.Max(Math.Min(res.AlphaMax, 255), 0);
            pAlpha[x * 4 + y * stride + 3] = 255;
            byte* pFG = (byte*)bmFG.Scan0;
            pFG[x * 4 + y * stride] = (byte)Math.Max(Math.Min(res.FMax[2], 255), 0);
            pFG[x * 4 + y * stride + 1] = (byte)Math.Max(Math.Min(res.FMax[1], 255), 0);
            pFG[x * 4 + y * stride + 2] = (byte)Math.Max(Math.Min(res.FMax[0], 255), 0);
            pFG[x * 4 + y * stride + 3] = 255;
            byte* pBG = (byte*)bmBG.Scan0;
            pBG[x * 4 + y * stride] = (byte)Math.Max(Math.Min(res.BMax[2], 255), 0);
            pBG[x * 4 + y * stride + 1] = (byte)Math.Max(Math.Min(res.BMax[1], 255), 0);
            pBG[x * 4 + y * stride + 2] = (byte)Math.Max(Math.Min(res.BMax[0], 255), 0);
            pBG[x * 4 + y * stride + 3] = 255;
        }

        private unsafe void RemoveOutline(List<ChainCode> boundary)
        {
            BitmapData bmData = this.UnknownMaskBmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;

            if (boundary.Count > 0)
            {
                //boundary = boundary.OrderByDescending(z => z.Coord.Count).ToList();

                for (int i = 0; i < boundary.Count; i++)
                {
                    List<Point> pts = boundary[i].Coord;

                    for (int j = 0; j < pts.Count; j++)
                    {
                        //only alpha needed, since we scan over alpha when getting the boundary
                        p[pts[j].X * 4 + pts[j].Y * stride] = p[pts[j].X * 4 + pts[j].Y * stride + 1] = p[pts[j].X * 4 + pts[j].Y * stride + 2] = 0;
                        p[pts[j].X * 4 + pts[j].Y * stride + 3] = 0;
                    }
                }
            }

            this.UnknownMaskBmp.UnlockBits(bmData);
        }

        private List<ChainCode> GetBoundary(Bitmap bmp)
        {
            ChainFinder cf = new ChainFinder();
            cf.AllowNullCells = true;
            List<ChainCode> l = cf.GetOutline(bmp, 0, false, 0, false, 0, false);
            return l;
        }

        private EQSysResult Solve(double[][] muf, List<List<double[]>> sigmaf, double[][] mub, List<List<double[]>> sigmab,
            double[] c, double sigmaPt, double alphaInit, int maxIter, double minLike)
        {
            double[] FMax = new double[3];
            double[] BMax = new double[3];
            double alphaMax = 0.0;

            double maxLike = double.MinValue;
            double invsgma2 = 1.0 / (sigmaPt * sigmaPt);

            for (int i = 0; i < muf.Length; i++)
            {
                double[] mu_Fi = muf[i];
                double[][] invSigma_Fi = MatrixInverse.MatrixInv(sigmaf[i].ToArray());

                for (int j = 0; j < mub.Length; j++)
                {
                    double[] mu_Bi = mub[j];
                    double[][] invSigma_Bi = MatrixInverse.MatrixInv(sigmab[j].ToArray());

                    double alpha = alphaInit;
                    int iter = 1;
                    double lastLike = double.MinValue;

                    double like = 0;

                    while (iter < maxIter && Math.Abs(like - lastLike) > minLike)
                    {
                        double[][] A11 = GetA11(invSigma_Fi, alpha, invsgma2);
                        double[][] A12 = GetA12(alpha, invsgma2);
                        double[][] A22 = GetA22(invSigma_Bi, alpha, invsgma2);

                        double[][] A = new double[A11.Length * 2][];
                        for (int l = 0; l < 3; l++)
                        {
                            A[l] = new double[A.Length];

                            for (int ll = 0; ll < 3; ll++)
                            {
                                A[l][ll] = A11[l][ll];
                                A[l][ll + 3] = A12[l][ll];
                            }
                        }
                        for (int l = 0; l < 3; l++)
                        {
                            A[l + 3] = new double[A.Length];

                            for (int ll = 0; ll < 3; ll++)
                            {
                                A[l + 3][ll] = A12[l][ll];
                                A[l + 3][ll + 3] = A22[l][ll];
                            }
                        }

                        double[] b1 = new double[3];
                        double[] b2 = new double[3];
                        double[] b = new double[6];

                        double[] b01 = MatrixVectorProduct(invSigma_Fi, mu_Fi);
                        double[] b02 = MatrixVectorProduct(invSigma_Bi, mu_Bi);

                        for (int l = 0; l < 3; l++)
                        {
                            b1[l] = b01[l] / 3 + c[l] * alpha * invsgma2;
                            b2[l] = b02[l] / 3 + c[l] * (1.0 - alpha) * invsgma2;
                        }

                        for (int l = 0; l < 3; l++)
                        {
                            b[l] = b1[l];
                            b[l + 3] = b2[l];
                        }

                        double[] X = MatrixEigen.MatLinSolveQR(A, b);

                        double[] f0 = X.Take(3).Select(z => Math.Min(z, 1.0)).ToArray();
                        double[] fg = f0.Select(z => Math.Max(z, 0.0)).ToArray();

                        double[] b0 = X.Skip(3).Select(z => Math.Min(z, 1.0)).ToArray();
                        double[] bg = b0.Select(z => Math.Max(z, 0.0)).ToArray();

                        //alpha
                        double[] fmb = new double[3];
                        for (int l = 0; l < 3; l++)
                            fmb[l] = fg[l] - bg[l];
                        double[] cmb = new double[3];
                        for (int l = 0; l < 3; l++)
                            cmb[l] = c[l] - bg[l];

                        double a00 = (fmb[0] * cmb[0] + fmb[1] * cmb[1] + fmb[2] * cmb[2]) / fmb.Select(z => z * z).ToArray().Sum();
                        double a0 = Math.Min(a00, 1.0);
                        alpha = Math.Max(a0, 0.0);

                        //likelihoods
                        double[] lc01 = fg.Select(z => z * alpha).ToArray();
                        double[] lc02 = bg.Select(z => z * (1.0 - alpha)).ToArray();
                        double[] lc00 = new double[3];
                        for (int l = 0; l < 3; l++)
                            lc00[l] = Math.Pow(c[l] - lc01[l] - lc02[l], 2);

                        double l_C = -lc00.Sum() * invsgma2;

                        double[] lf01 = new double[3];
                        for (int l = 0; l < 3; l++)
                            lf01[l] = fg[l] - mu_Fi[l];
                        double[] lf02 = MatrixVectorProduct(invSigma_Fi, lf01);
                        double lf00 = 0;
                        for (int l = 0; l < 3; l++)
                            lf00 += lf01[l] * lf02[l];

                        double l_F = -lf00 / 2.0;

                        double[] lb01 = new double[3];
                        for (int l = 0; l < 3; l++)
                            lb01[l] = bg[l] - mu_Bi[l];
                        double[] lb02 = MatrixVectorProduct(invSigma_Bi, lb01);
                        double lb00 = 0;
                        for (int l = 0; l < 3; l++)
                            lb00 += lb01[l] * lb02[l];

                        double l_B = -lb00 / 2.0;

                        like = l_C + l_F + l_B;

                        if (like > maxLike)
                        {
                            alphaMax = alpha;
                            maxLike = like;
                            FMax = fg.Select(z => z).ToArray();
                            BMax = bg.Select(z => z).ToArray();
                        }

                        if (iter >= maxIter || Math.Abs(like - lastLike) <= minLike)
                            break;

                        lastLike = like;
                        iter++;
                    }
                }
            }

            alphaMax *= 255.0;
            FMax = FMax.Select(z => z * 255.0).ToArray();
            BMax = BMax.Select(z => z * 255.0).ToArray();

            return new EQSysResult(FMax, BMax, alphaMax);
        }

        public static double[] MatrixVectorProduct(double[][] matrixA, double[] matrixB)
        {
            int aRows = matrixA.Length;
            int aCols = matrixA[0].Length;
            int bRows = matrixB.Length;

            double[] result = new double[aRows];

            for (var i = 0; i < aRows; i++)
            {
                for (var j = 0; j < bRows; j++)
                {
                    for (var k = 0; k < aCols; k++)
                        result[i] += matrixA[i][k] * matrixB[k];
                }
            }

            return result;
        }

        private double[][] GetA22(double[][] invSigma_Bi, double alpha, double invsgma2)
        {
            double[][] a22 = new double[invSigma_Bi.Length][];
            for (int i = 0; i < a22.Length; i++)
            {
                a22[i] = new double[3];
                for (int j = 0; j < a22[0].Length; j++)
                    if (i == j)
                        a22[i][j] = invSigma_Bi[i][j] + ((1.0 - alpha) * (1.0 - alpha) * invsgma2);
                    else
                        a22[i][j] = invSigma_Bi[i][j];
            }

            return a22;
        }

        private unsafe double[][] GetA12(double alpha, double invsgma2)
        {
            double[][] a12 = new double[3][];
            for (int i = 0; i < a12.Length; i++)
            {
                a12[i] = new double[3];
                for (int j = 0; j < a12[0].Length; j++)
                    if (i == j)
                        a12[i][j] = (alpha * (1.0 - alpha) * invsgma2);
            }

            return a12;
        }

        private double[][] GetA11(double[][] invSigma_Fi, double alpha, double invsgma2)
        {
            double[][] a11 = new double[invSigma_Fi.Length][];
            for (int i = 0; i < a11.Length; i++)
            {
                a11[i] = new double[3];
                for (int j = 0; j < a11[0].Length; j++)
                    if (i == j)
                        a11[i][j] = invSigma_Fi[i][j] + (alpha * alpha * invsgma2);
                    else
                        a11[i][j] = invSigma_Fi[i][j];
            }

            return a11;
        }

        private unsafe double[,] GetWindow(Bitmap b, Point location, int fgbga)
        {
            double[,] res = new double[this.WindowSize * 3, this.WindowSize];

            BitmapData bData = b.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmData = this.Trimap.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int wh = this.WindowSize / 2;
            int stride = bData.Stride;
            int x = location.X;
            int y = location.Y;

            int extract = 0;
            if (fgbga == 1)
                extract = 255;
            else if (fgbga == 2)
                extract = Color.Gray.R;

            byte* p = (byte*)bData.Scan0;
            byte* pt = (byte*)bmData.Scan0;

            for (int y2 = y - wh, y4 = 0; y2 <= y + wh; y2++, y4++)
                if (y2 >= 0 && y2 < this._h && y4 >= 0 && y4 < this.WindowSize)
                    for (int x2 = x - wh, x4 = 0; x2 <= x + wh; x2++, x4++)
                        if (x2 >= 0 && x2 < this._w && x4 >= 0 && x4 < this.WindowSize)
                        {
                            if (pt[x2 * 4 + y2 * stride] == extract)
                            {
                                res[x4 * 3, y4] = p[x2 * 4 + y2 * stride];
                                res[x4 * 3 + 1, y4] = p[x2 * 4 + y2 * stride + 1];
                                res[x4 * 3 + 2, y4] = p[x2 * 4 + y2 * stride + 2];
                            }
                        }

            b.UnlockBits(bData);
            this.Trimap.UnlockBits(bmData);

            return res;
        }

        private unsafe double[,] GetWindow(BitmapData bData, BitmapData bmData, Point location, int fgbga)
        {
            //hier
            double[,] res = new double[this.WindowSize * 3, this.WindowSize];

            //BitmapData bData = b.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            //BitmapData bmData = this.Trimap.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int wh = this.WindowSize / 2;
            int stride = bData.Stride;
            int x = location.X;
            int y = location.Y;

            int extract = 0;
            if (fgbga == 1)
                extract = 255;
            else if (fgbga == 2)
                extract = Color.Gray.R;

            byte* p = (byte*)bData.Scan0;
            byte* pt = (byte*)bmData.Scan0;

            for (int y2 = y - wh, y4 = 0; y2 <= y + wh; y2++, y4++)
                if (y2 >= 0 && y2 < this._h && y4 >= 0 && y4 < this.WindowSize)
                    for (int x2 = x - wh, x4 = 0; x2 <= x + wh; x2++, x4++)
                        if (x2 >= 0 && x2 < this._w && x4 >= 0 && x4 < this.WindowSize)
                        {
                            if (pt[x2 * 4 + y2 * stride] == extract)
                            {
                                res[x4 * 3, y4] = p[x2 * 4 + y2 * stride];
                                res[x4 * 3 + 1, y4] = p[x2 * 4 + y2 * stride + 1];
                                res[x4 * 3 + 2, y4] = p[x2 * 4 + y2 * stride + 2];

                                //if (x2 >= 1 /*&& x2 < this._w - 1 && y2 >= 1 && y2 < this._h - 1*/)
                                //{
                                //    res[x4 * 3, y4] = (double)p[x2 * 4 + y2 * stride] + ((double)p[x2 * 4 + y2 * stride] - (double)p[x2 * 4 + y2 * stride - 4]) / 2.0;
                                //    res[x4 * 3 + 1, y4] = (double)p[x2 * 4 + y2 * stride + 1] + ((double)p[x2 * 4 + y2 * stride + 1] - (double)p[x2 * 4 + y2 * stride + 1 - 4]) / 2.0;
                                //    res[x4 * 3 + 2, y4] = (double)p[x2 * 4 + y2 * stride + 2] + ((double)p[x2 * 4 + y2 * stride + 2] - (double)p[x2 * 4 + y2 * stride + 2 - 4]) / 2.0;
                                //}
                                //else if (x2 < this._w - 1)
                                //{
                                //    res[x4 * 3, y4] = (double)p[x2 * 4 + y2 * stride] + ((double)p[x2 * 4 + y2 * stride] - (double)p[x2 * 4 + y2 * stride + 4]) / 2.0;
                                //    res[x4 * 3 + 1, y4] = (double)p[x2 * 4 + y2 * stride + 1] + ((double)p[x2 * 4 + y2 * stride + 1] - (double)p[x2 * 4 + y2 * stride + 1 + 4]) / 2.0;
                                //    res[x4 * 3 + 2, y4] = (double)p[x2 * 4 + y2 * stride + 2] + ((double)p[x2 * 4 + y2 * stride + 2] - (double)p[x2 * 4 + y2 * stride + 2 + 4]) / 2.0;
                                //}
                                //else
                                //{
                                //    res[x4 * 3, y4] = p[x2 * 4 + y2 * stride];
                                //    res[x4 * 3 + 1, y4] = p[x2 * 4 + y2 * stride + 1];
                                //    res[x4 * 3 + 2, y4] = p[x2 * 4 + y2 * stride + 2];
                                //}
                            }
                        }

            //b.UnlockBits(bData);
            //this.Trimap.UnlockBits(bmData);

            return res;
        }

        private unsafe double[,] GetAlphaWindow(Point location, BitmapData bmData)
        {
            double[,] res = new double[this.WindowSize, this.WindowSize];

            //BitmapData bmData = this.Trimap.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int wh = this.WindowSize / 2;
            int stride = bmData.Stride;
            int x = location.X;
            int y = location.Y;

            byte* p = (byte*)bmData.Scan0;

            for (int y2 = y - wh, y4 = 0; y2 <= y + wh; y2++, y4++)
                if (y2 >= 0 && y2 < this._h && y4 >= 0 && y4 < this.WindowSize)
                    for (int x2 = x - wh, x4 = 0; x2 <= x + wh; x2++, x4++)
                        if (x2 >= 0 && x2 < this._w && x4 >= 0 && x4 < this.WindowSize)
                        {
                            //if (p[x2 * 4 + y2 * stride] > 128 || p[x2 * 4 + y2 * stride + 1] > 128 || p[x2 * 4 + y2 * stride + 2] > 128)
                            //    res[x4, y4] = 1;
                            //if (p[x2 * 4 + y2 * stride] < 128 || p[x2 * 4 + y2 * stride + 1] < 128 || p[x2 * 4 + y2 * stride + 2] < 128)
                            //    res[x4, y4] = 0;

                            //if (x4 == 16 && y4 == 19)
                            //    MessageBox.Show(p[x2 * 4 + y2 * stride].ToString());
                            //zzzz
                            res[x4, y4] = (double)p[x2 * 4 + y2 * stride] / 255.0;
                        }

            //this.Trimap.UnlockBits(bmData);

            return res;
        }

        private double[,] GetGaussianKernel(int windowSize, double stdDev)
        {
            double[,] Kernel = new double[windowSize, windowSize];

            int Radius = windowSize / 2;

            double a = (2.0 * stdDev * stdDev);
            //double Sum = 0.0;

            for (int y = 0; y <= Kernel.GetLength(1) - 1; y++)
            {
                for (int x = 0; x <= Kernel.GetLength(0) - 1; x++)
                {
                    double dist = Math.Sqrt((x - Radius) * (x - Radius) + (y - Radius) * (y - Radius));
                    Kernel[x, y] = Math.Exp(-dist * dist / a);

                    //Sum += Kernel[x, y];
                }
            }

            //for (int y = 0; y <= Kernel.GetLength(1) - 1; y++)
            //{
            //    for (int x = 0; x <= Kernel.GetLength(0) - 1; x++)
            //        Kernel[x, y] /= Sum;
            //}

            return Kernel;
        }

        private unsafe void ReadTrimap()
        {
            BitmapData bmData = this.Trimap.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            if (this._BGIndexes == null)
                this._BGIndexes = new List<Point>();

            this._BGIndexes.Clear();

            if (this._FGIndexes == null)
                this._FGIndexes = new List<Point>();

            this._FGIndexes.Clear();

            if (this._UKIndexes == null)
                this._UKIndexes = new List<Point>();

            this._UKIndexes.Clear();

            //BG = 0, FG = 1, unknown = Int32.MinValue
            //for (int y = 0; y < this._h; y++)
            for (int y = 0; y < this._h; y++)
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;
                for (int x = 0; x < this._w; x++)
                {
                    if (p[0] == 255)
                        this._FGIndexes.Add(new Point(x, y));
                    else if (p[0] == 0)
                        this._BGIndexes.Add(new Point(x, y));
                    else
                        this._UKIndexes.Add(new Point(x, y));

                    p += 4;
                }
            }

            this.Trimap.UnlockBits(bmData);
        }

        //private unsafe int Classify()
        //{
        //    int w = this._w;
        //    int h = this._h;

        //    BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        //    int stride = bmData.Stride;

        //    if (this._BGIndexes == null)
        //        this._BGIndexes = new List<Point>();

        //    this._BGIndexes.Clear();

        //    if (this._FGIndexes == null)
        //        this._FGIndexes = new List<Point>();

        //    this._FGIndexes.Clear();

        //    if (this._UKIndexes == null)
        //        this._UKIndexes = new List<Point>();

        //    this._UKIndexes.Clear();

        //    //this._bgValues = new List<double[]>();
        //    //this._fgValues = new List<double[]>();
        //    //this._ukValues = new List<double[]>();

        //    for (int y = 0; y < h; y++)
        //    {
        //        byte* p = (byte*)bmData.Scan0;

        //        for (int x = 0; x < w; x++)
        //        {
        //            if (this.Mask[x, y] == 1)
        //            {
        //                this._BGIndexes.Add(new Point(x, y));
        //                //this._bgValues.Add(new double[] { p[x * 4 + y * stride], p[x * 4 + y * stride + 1], p[x * 4 + y * stride + 2] });
        //            }
        //            else if (this.Mask[x, y] == 0)
        //            {
        //                this._FGIndexes.Add(new Point(x, y));
        //                //this._fgValues.Add(new double[] { p[x * 4 + y * stride], p[x * 4 + y * stride + 1], p[x * 4 + y * stride + 2] });
        //            }
        //            else if (this.Mask[x, y] == Int32.MinValue)
        //            {
        //                this._UKIndexes.Add(new Point(x, y));
        //                //this._ukValues.Add(new double[] { p[x * 4 + y * stride], p[x * 4 + y * stride + 1], p[x * 4 + y * stride + 2] });
        //            }
        //        }
        //    }

        //    this.Bmp.UnlockBits(bmData);

        //    if (this._BGIndexes.Count == 0)
        //        return -1;

        //    if (this._FGIndexes.Count == 0)
        //        return -3;

        //    if (this._UKIndexes.Count == 0)
        //        return -5;

        //    return 0;
        //}

        public void Dispose()
        {
            if (this.Bmp != null)
                this.Bmp.Dispose();
            if (this.Trimap != null)
                this.Trimap.Dispose();
            if (this.Alpha != null)
                this.Alpha.Dispose();
            if (this.Foreground != null)
                this.Foreground.Dispose();
            if (this.Background != null)
                this.Background.Dispose();
            if (this.UnknownMaskBmp != null)
                this.UnknownMaskBmp.Dispose();
        }
    }
}
