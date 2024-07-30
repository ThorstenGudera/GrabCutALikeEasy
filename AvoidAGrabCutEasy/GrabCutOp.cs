using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    //translation from python:
    //https://github.com/MoetaYuko/GrabCut
    public class GrabCutOp : IDisposable
    {
        private int _w;
        private int _h;
        private double[,] _lv;
        private double[,] _ulv;
        private double[,] _uv;
        private double[,] _urv;
        private int _source;
        private int _sink;
        private List<Point> _BGIndexes;
        private List<Point> _FGIndexes;
        private GMM_bgr _bgGmm;
        private GMM_bgr _fgGmm;
        private List<double[]> _bgValues;
        private List<double[]> _fgValues;
        private List<double> _graphCapacity;

        private object _lockObject = new object();
        private DirectedGraph _dg;
        private DirectedGraph _rG;
        private int[] _bgLabels;
        private int[] _fgLabels;

        private int[] _result2;
        private bool _addOnlySourceAndSink;

        public Bitmap Bmp { get; set; }
        public int Gmm_comp { get; internal set; }
        public double Gamma { get; internal set; }
        public int NumIters { get; internal set; }
        public bool RectMode { get; internal set; }
        public Rectangle Rc { get; internal set; }
        public BackgroundWorker BGW { get; internal set; }
        public int[,] Mask { get; internal set; }
        public int MaxIter { get; internal set; } = Int32.MaxValue / 3;
        public List<int> Result { get; internal set; }
        public bool ScribbleMode { get; internal set; }
        public List<StartNode> StartNodes { get; private set; } = new List<StartNode> { };
        public int QATH { get; internal set; } = 1000000;
        public Dictionary<int, Dictionary<int, List<List<Point>>>> Scribbles { get; internal set; }
        public bool CGwithQE { get; internal set; } = true;
        public bool QuickEstimation { get; set; }
        public bool EightAdj { get; internal set; }
        internal ListSelectionMode SelectionMode { get; set; }
        public bool SkipLearn { get; internal set; }
        public bool GammaChanged { get; internal set; }
        public double Threshold { get; set; } = 13;
        public bool UseThreshold { get; set; } = false;
        public double ProbMult1 { get; internal set; } = 1.0;
        public bool MultCapacitiesForTLinks { get; internal set; }
        public bool CastIntCapacitiesForTLinks { get; internal set; }
        public double MultTLinkCapacity { get; internal set; }
        public double NumItems { get; internal set; }
        public double NumCorrect { get; internal set; }
        public double NumItems2 { get; internal set; }
        public double NumCorrect2 { get; internal set; }
        public PushRelabelFifo Alg { get; internal set; }
        public double KMInitW { get; internal set; } = 2;
        public double KMInitH { get; internal set; } = 2;
        public BoykovKolmogorov AlgBK { get; internal set; }

        public event EventHandler<string> ShowInfo;

        public GrabCutOp()
        {

        }

        public int Init()
        {
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(0);

            if (this.Bmp != null)
            {
                this._w = this.Bmp.Width;
                this._h = this.Bmp.Height;

                //first get a representaion of the current state by defining a "mask" array for storing the
                //pixel states: bg, fg, probably bg, probably fg as 0, 1, 2, 3

                if (this.RectMode || this.ScribbleMode)
                    this.Mask = new int[this._w, this._h];

                if (this.RectMode && this.ScribbleMode)
                {
                    //rect part
                    Parallel.For(0, this._h, y =>
                    {
                        for (int x = 0; x < this._w; x++)
                        {
                            if (this.Rc.Contains(x, y))
                                this.Mask[x, y] = 3;
                            else
                                this.Mask[x, y] = 0;
                        }
                    });
                    //scribble part
                    foreach (int i in this.Scribbles.Keys)
                    {
                        Dictionary<int, List<List<Point>>> allPts = this.Scribbles[i]; //dict = <BG/FG, <wh, pts>>

                        foreach (int wh in allPts.Keys)
                        {
                            int wh2 = wh / 2;

                            for (int j = 0; j < allPts[wh].Count; j++)
                            {
                                List<Point> pts = allPts[wh][j];

                                foreach (Point pt in pts)
                                    Rect(pt, i, wh2);
                            }
                        }
                    }
                }
                //BG = 0, FG = 1, PrBG = 2, PrFG = 3
                else if (this.RectMode)
                    //for (int y = 0; y < this._h; y++)
                    Parallel.For(0, this._h, y =>
                    {
                        for (int x = 0; x < this._w; x++)
                        {
                            if (this.Rc.Contains(x, y))
                                this.Mask[x, y] = 3;
                            else
                                this.Mask[x, y] = 0;
                        }
                    });
                else if (this.ScribbleMode)
                {
                    Parallel.For(0, this._h, y =>
                    {
                        for (int x = 0; x < this._w; x++)
                            this.Mask[x, y] = 3;
                    });

                    foreach (int i in this.Scribbles.Keys)
                    {
                        Dictionary<int, List<List<Point>>> allPts = this.Scribbles[i]; //dict = <BG/FG, <wh, pts>>

                        foreach (int wh in allPts.Keys)
                        {
                            int wh2 = wh / 2;

                            for (int j = 0; j < allPts[wh].Count; j++)
                            {
                                List<Point> pts = allPts[wh][j];

                                foreach (Point pt in pts)
                                    Rect(pt, i, wh2);
                            }
                        }
                    }
                }

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(20);

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                if (this.Mask == null)
                    return -5;

                //ShowMaskToBmp();

                //do a first classification, i.e. determine the known and unknown states for the pixels,
                //store these values in arrays and list to use later
                int cl = Classify();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(30);

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                if (cl != 0)
                    return cl;

                //now setup some array for the smootheness function, being used later for the n-links
                //(the links between the graph nodes that represent [existing] locations (e.g. pixels) in our image-data)
                this._lv = new double[(this._w - 1), this._h];
                this._ulv = new double[(this._w - 1), this._h - 1];
                this._uv = new double[this._w, this._h - 1];
                this._urv = new double[(this._w - 1), this._h - 1];

                this._source = this._w * this._h;
                this._sink = this._source + 1;

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(50);

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                //now compute the values for the pixels and its immediate neighbors
                //to get the representation of the smoothenes part of the energy function to minimize
                //we basically get the needed derivatives and then compute values that
                //represent the current "is_egde_state" of the pixels to compute the capacities for the n-links in the graph later
                if (this.CGwithQE || !this.QuickEstimation)
                    CalcBeta();

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                //now init the Gmms [the machinery for the Data-term of the energy function]
                InitGMMs();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);

                return 0;
            }

            return -3;
        }

        private void ShowMaskToBmp()
        {
            using (Bitmap bmp = new Bitmap(this.Mask.GetLength(0), this.Mask.GetLength(1)))
            {
                for (int y = 0; y < this.Mask.GetLength(1); y++)
                    for (int x = 0; x < this.Mask.GetLength(0); x++)
                    {
                        if (this.Mask[x, y] == 0)
                            bmp.SetPixel(x, y, Color.Black);
                        if (this.Mask[x, y] == 1)
                            bmp.SetPixel(x, y, Color.White);
                        if (this.Mask[x, y] == 2)
                            bmp.SetPixel(x, y, Color.Gray);
                        if (this.Mask[x, y] == 3)
                            bmp.SetPixel(x, y, Color.Yellow);
                    }

                using (Form fff = new Form())
                {
                    fff.BackgroundImage = bmp;
                    fff.BackgroundImageLayout = ImageLayout.Zoom;
                    fff.ShowDialog();
                }
            }
        }

        private unsafe void CalcBeta()
        {
            int w = this._w;
            int h = this._h;

            //get the derivatives
            int[,] leftDiff = new int[(w - 1) * 3, h];
            int[,] upDiff = new int[w * 3, h - 1];
            int[,] upleftDiff = { };
            int[,] uprightDiff = { };

            if (this.EightAdj)
            {
                upleftDiff = new int[(w - 1) * 3, h - 1];
                uprightDiff = new int[(w - 1) * 3, h - 1];
            }

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            Parallel.For(0, h, y =>
            //for(int y = 0; y < h; y++)
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;

                for (int x = 0; x < w - 1; x++)
                {
                    leftDiff[x * 3, y] = p[4] - p[0];
                    leftDiff[x * 3 + 1, y] = p[5] - p[1];
                    leftDiff[x * 3 + 2, y] = p[6] - p[2];

                    p += 4;
                }
            });

            //Bitmap bmp2 = new Bitmap(w - 1, h);
            //BitmapData bD2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //int strd2 = bD2.Stride;

            //byte* pb2 = (byte*)bD2.Scan0;

            //for (int y = 0; y < bmp2.Height; y++)
            //{
            //    for (int x = 0; x < bmp2.Width; x++)
            //    {
            //        pb2[x * 4 + y * strd2] = (byte)((Math.Abs(leftDiff[x * 3, y]) + Math.Abs(leftDiff[x * 3 + 1, y]) + Math.Abs(leftDiff[x * 3 + 2, y])) * 4);
            //        pb2[x * 4 + y * strd2 + 3] = 255;
            //    }
            //}

            //bmp2.UnlockBits(bD2);

            //Form fff2 = new Form();
            //fff2.BackgroundImage = bmp2;
            //fff2.BackgroundImageLayout = ImageLayout.Zoom;
            //fff2.ShowDialog();
            //bmp2.Dispose();

            if (this.EightAdj)
                Parallel.For(0, h - 1, y =>
                {
                    byte* p = (byte*)bmData.Scan0;
                    p += y * stride;

                    for (int x = 0; x < w - 1; x++)
                    {
                        upleftDiff[x * 3, y] = p[4 + stride] - p[0];
                        upleftDiff[x * 3 + 1, y] = p[5 + stride] - p[1];
                        upleftDiff[x * 3 + 2, y] = p[6 + stride] - p[2];

                        p += 4;
                    }
                });

            Parallel.For(0, h - 1, y =>
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;

                for (int x = 0; x < w; x++)
                {
                    upDiff[x * 3, y] = p[stride] - p[0];
                    upDiff[x * 3 + 1, y] = p[1 + stride] - p[1];
                    upDiff[x * 3 + 2, y] = p[2 + stride] - p[2];

                    p += 4;
                }
            });

            if (this.EightAdj)
                Parallel.For(0, h - 1, y =>
                {
                    byte* p = (byte*)bmData.Scan0;
                    p += y * stride;

                    for (int x = 0; x < w - 1; x++)
                    {
                        uprightDiff[x * 3, y] = p[stride - 4] - p[0];
                        uprightDiff[x * 3 + 1, y] = p[stride - 3] - p[1];
                        uprightDiff[x * 3 + 2, y] = p[stride - 2] - p[2];

                        p += 4;
                    }
                });

            this.Bmp.UnlockBits(bmData);

            //compute a first weight to switch the exponential term in the formula appropriately between high and low contrast
            double beta = 0;
            int yMax = leftDiff.GetLength(1);
            int xMax = leftDiff.GetLength(0) / 3;

            Parallel.For(0, yMax, () => 0.0, (y, loopState, localBeta) =>
            {
                for (int x = 0; x < xMax; x++)
                {
                    for (int j = 0; j < 3; j++)
                        localBeta += Math.Pow(leftDiff[x + j, y], 2);
                }

                return localBeta;
            }, (localBeta) =>
            {
                lock (this._lockObject)
                {
                    beta += localBeta;
                }
            });

            if (this.EightAdj)
            {
                yMax = upleftDiff.GetLength(1);
                xMax = upleftDiff.GetLength(0) / 3;

                Parallel.For(0, yMax, () => 0.0, (y, loopState, localBeta) =>
                {
                    for (int x = 0; x < xMax; x++)
                    {
                        for (int j = 0; j < 3; j++)
                            localBeta += Math.Pow(upleftDiff[x + j, y], 2);
                    }
                    return localBeta;
                }, (localBeta) =>
                {
                    lock (this._lockObject)
                    {
                        beta += localBeta;
                    }
                });
            }

            yMax = upDiff.GetLength(1);
            xMax = upDiff.GetLength(0) / 3;

            Parallel.For(0, yMax, () => 0.0, (y, loopState, localBeta) =>
            {
                for (int x = 0; x < xMax; x++)
                {
                    for (int j = 0; j < 3; j++)
                        localBeta += Math.Pow(upDiff[x + j, y], 2);
                }
                return localBeta;
            }, (localBeta) =>
            {
                lock (this._lockObject)
                {
                    beta += localBeta;
                }
            });

            if (this.EightAdj)
            {
                yMax = uprightDiff.GetLength(1);
                xMax = uprightDiff.GetLength(0) / 3;

                Parallel.For(0, yMax, () => 0.0, (y, loopState, localBeta) =>
                {
                    for (int x = 0; x < xMax; x++)
                    {
                        for (int j = 0; j < 3; j++)
                            localBeta += Math.Pow(uprightDiff[x + j, y], 2);
                    }
                    return localBeta;
                }, (localBeta) =>
                {
                    lock (this._lockObject)
                    {
                        beta += localBeta;
                    }
                });
            }

            //Console.WriteLine("beta01: " + beta.ToString());

            //for both int and dbl graphs
            //if (!this.EightAdj)
            //    beta = 1.0 / (2.0 * beta / (3 * w * h - 2 * (w + h) + 2));
            //else
            //    beta = 1.0 / (2.0 * beta / (6 * w * h - 2 * (w + h) + 2));

            //beta = 1.0 / (2.0 * beta / (4 * w * h - 3 * (w + h) + 2));
            //beta = 2 * w * h / (2 * beta);  

            if (!this.EightAdj)
            {
                beta = (5 * w * h - 2 * (w + h)) / (2 * beta);
            }
            else
            {
                beta = (8 * w * h - 2 * (w + h)) / (2 * beta);
            }

            //Console.WriteLine("beta: " + beta.ToString());

            OnShowInfo("beta: " + beta.ToString());

            //now compute the function's values, this is, where the parameter "gamma" increases, or decreases
            //the weight of the field at the current location
            yMax = leftDiff.GetLength(1);
            xMax = leftDiff.GetLength(0) / 3;

            Console.WriteLine(this._lv.GetLength(0) == xMax);

            Parallel.For(0, yMax, y =>
            //for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    double sum = 0;
                    for (int j = 0; j < 3; j++)
                        sum += Math.Pow(leftDiff[x * 3 + j, y], 2);
                    //this._lv[x, y] = this.Gamma * Math.Exp(-beta * sum / this.Xi);

                    this._lv[x, y] = this.Gamma * Math.Exp(-beta * sum);
                }
            });

            if (this.EightAdj)
            {
                yMax = upleftDiff.GetLength(1);
                xMax = upleftDiff.GetLength(0) / 3;

                Parallel.For(0, yMax, y =>
                {
                    for (int x = 0; x < xMax; x++)
                    {
                        double sum = 0;
                        for (int j = 0; j < 3; j++)
                            sum += Math.Pow(upleftDiff[x * 3 + j, y], 2);
                        //this._ulv[x, y] = this.Gamma / Math.Sqrt(2) * Math.Exp(-beta * sum / this.Xi);

                        this._ulv[x, y] = this.Gamma / Math.Sqrt(2) * Math.Exp(-beta * sum);
                    }
                });
            }

            yMax = upDiff.GetLength(1);
            xMax = upDiff.GetLength(0) / 3;

            Parallel.For(0, yMax, y =>
            {
                for (int x = 0; x < xMax; x++)
                {
                    double sum = 0;
                    for (int j = 0; j < 3; j++)
                        sum += Math.Pow(upDiff[x * 3 + j, y], 2);
                    //this._uv[x, y] = this.Gamma * Math.Exp(-beta * sum / this.Xi);

                    this._uv[x, y] = this.Gamma * Math.Exp(-beta * sum);
                }
            });

            if (this.EightAdj)
            {
                yMax = uprightDiff.GetLength(1);
                xMax = uprightDiff.GetLength(0) / 3;

                Parallel.For(0, yMax, y =>
                {
                    for (int x = 0; x < xMax; x++)
                    {
                        double sum = 0;
                        for (int j = 0; j < 3; j++)
                            sum += Math.Pow(uprightDiff[x * 3 + j, y], 2);
                        //this._urv[x, y] = this.Gamma / Math.Sqrt(2) * Math.Exp(-beta * sum / this.Xi);

                        this._urv[x, y] = this.Gamma / Math.Sqrt(2) * Math.Exp(-beta * sum);
                    }
                });
            }
        }

        private void OnShowInfo(string message)
        {
            ShowInfo?.Invoke(this, message);
        }

        private unsafe void InitGMMs()
        {
            //init the GaussianMixtureModels
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(0);
            this._bgGmm = new GMM_bgr(this._bgValues.ToArray(), this._BGIndexes.ToArray(), this.Gmm_comp, 3 /* b, g, r */,
                0, true, this.SelectionMode, 10, this._w, this._h, this.KMInitW,
                this.KMInitH, false, false, 1, false, this.BGW);
            //if (this.BGW != null && this.BGW.WorkerReportsProgress)
            //    this.BGW.ReportProgress(50);
            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                this.BGW.CancelAsync();
            this._fgGmm = new GMM_bgr(this._fgValues.ToArray(), this._FGIndexes.ToArray(), this.Gmm_comp, 3,
                0, true, this.SelectionMode, 10, this._w, this._h, this.KMInitW,
                this.KMInitH, false, false, 1, false, this.BGW);
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(100);
        }

        private void Rect(Point point, int j, int wh)
        {
            for (int y = point.Y - wh; y < point.Y + wh; y++)
            {
                for (int x = point.X - wh; x < point.X + wh; x++)
                {
                    if (x >= 0 && x < this._w && y >= 0 && y < this._h)
                        this.Mask[x, y] = j;
                }
            }
        }

        private unsafe int Classify()
        {
            int w = this._w;
            int h = this._h;

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            if (this._BGIndexes == null)
                this._BGIndexes = new List<Point>();

            this._BGIndexes.Clear();

            if (this._FGIndexes == null)
                this._FGIndexes = new List<Point>();

            this._FGIndexes.Clear();

            this._bgValues = new List<double[]>();
            this._fgValues = new List<double[]>();

            for (int y = 0; y < h; y++)
            {
                byte* p = (byte*)bmData.Scan0;

                for (int x = 0; x < w; x++)
                {
                    if ((this.Mask[x, y] & 0x01) != 1)
                    {
                        this._BGIndexes.Add(new Point(x, y));
                        this._bgValues.Add(new double[] { p[x * 4 + y * stride], p[x * 4 + y * stride + 1], p[x * 4 + y * stride + 2] });
                    }
                    else
                    {
                        this._FGIndexes.Add(new Point(x, y));
                        this._fgValues.Add(new double[] { p[x * 4 + y * stride], p[x * 4 + y * stride + 1], p[x * 4 + y * stride + 2] });
                    }
                }
            }

            this.Bmp.UnlockBits(bmData);

            //add at least one bg point for each Component
            if (this._BGIndexes.Count == 0)
            {
                int cnt = 0;
                for (int y = 0; y < this.Mask.GetLength(1); y++)
                {
                    bool f = false;
                    for (int x = 0; x < this.Mask.GetLength(0); x++)
                    {
                        if (this.Mask[x, y] == 2 || this.Mask[x, y] == 3)
                        {
                            this.Mask[x, y] = 0;
                            cnt++;
                        }

                        if (cnt == this.Gmm_comp)
                        {
                            f = true;
                            break;
                        }
                    }
                    if (f)
                        break;
                }
            }
            if (this._FGIndexes.Count == 0)
            {
                int cnt = 0;
                for (int y = 0; y < this.Mask.GetLength(1); y++)
                {
                    bool f = false;
                    for (int x = 0; x < this.Mask.GetLength(0); x++)
                    {
                        if (this.Mask[x, y] == 2 || this.Mask[x, y] == 3)
                        {
                            this.Mask[x, y] = 1;
                            cnt++;
                        }

                        if (cnt == this.Gmm_comp)
                        {
                            f = true;
                            break;
                        }
                    }
                    if (f)
                        break;
                }
            }

            if (this._BGIndexes.Count == 0)
                return -1;

            if (this._FGIndexes.Count == 0)
                return -3;

            return 0;
        }

        public int Run()
        {
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(10);

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                return 101;

            if (this._bgGmm == null || this._fgGmm == null)
                return 102;

            for (int i = 0; i < this.NumIters; i++)
            {
                OnShowInfo("Iteration " + (i + 1).ToString() + " of " + this.NumIters.ToString());

                if (!this.SkipLearn)
                {
                    if (!AssignSamplesAndFit())
                        return 100;
                }

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(15);

                //compute the directed graph
                int j = ConstructGraphFull();
                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(30);
                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    return 101;

                if (j != 0)
                    return j;

                //and run the estimation
                EstimateSegmentation();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);

                //if (i < this.NumIters - 1)
                //    this.SkipLearn = true;
                //else
                this.SkipLearn = false;
            }

            return 0;
        }

        public int RunMinCut()
        {
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(10);

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                return 101;

            if (this._bgGmm == null || this._fgGmm == null)
                return 102;

            for (int i = 0; i < this.NumIters; i++)
            {
                OnShowInfo("Iteration " + (i + 1).ToString() + " of " + this.NumIters.ToString());

                if (!this.SkipLearn)
                {
                    if (!AssignSamplesAndFit())
                        return 100;
                }

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(15);

                //compute the directed graph
                MessageBox.Show("Computing a real MinCut without a specialized Algorithm may take a long time. You can cancel the operation at any time.");
                int j = ConstructGraphOld();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(30);
                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    return 101;

                if (j != 0)
                    return j;

                this._addOnlySourceAndSink = true;

                //and run the estimation
                EstimateSegmentationBK();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);

                //if (i < this.NumIters - 1)
                //    this.SkipLearn = true;
                //else
                this.SkipLearn = false;
            }

            return 0;
        }

        private unsafe bool AssignSamplesAndFit()
        {
            if (this._BGIndexes == null || this._FGIndexes == null)
                return false;

            if (this._w > 0 && this._h > 0)
            {
                BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, this._w, this._h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmData.Stride;

                byte* p = (byte*)bmData.Scan0;

                double[][] bgVals = new double[this._BGIndexes.Count][];
                int cnt = this._BGIndexes.Count;
                Parallel.For(0, cnt, j =>
                {
                    int address = this._BGIndexes[j].X * 4 + this._BGIndexes[j].Y * stride;
                    bgVals[j] = new double[] { p[address], p[address + 1], p[address + 2] };
                });
                int[] bgLabels = this._bgGmm.WhichComponent(bgVals);

                if (bgLabels == null)
                    return false;

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(20);

                double[][] fgVals = new double[this._FGIndexes.Count][];
                cnt = this._FGIndexes.Count;
                Parallel.For(0, cnt, j =>
                {
                    int address = this._FGIndexes[j].X * 4 + this._FGIndexes[j].Y * stride;
                    fgVals[j] = new double[] { p[address], p[address + 1], p[address + 2] };
                });
                int[] fgLabels = this._fgGmm.WhichComponent(fgVals);

                if (fgLabels == null)
                    return false;

                this.Bmp.UnlockBits(bmData);

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(40);

                this._bgLabels = bgLabels;
                this._fgLabels = fgLabels;

                this._bgGmm.Fit(bgVals, bgLabels);

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(70);

                this._fgGmm.Fit(fgVals, fgLabels);

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);

                return true;
            }
            else
                return false;
        }

        private unsafe int ConstructGraphFull()
        {
            if (this.GammaChanged)
            {
                if (this.CGwithQE || !this.QuickEstimation)
                    CalcBeta();
                this.GammaChanged = false;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(20);

            int begin = 0;
            int begin2 = 0;

            this._result2 = null;

            List<Point> bGIndexes = new List<Point>();
            List<Point> fGIndexes = new List<Point>();
            List<Point> pRIndexes = new List<Point>();
            List<Point> pRIndexesFull = new List<Point>();

            List<Point> pRFIndexes = new List<Point>();
            List<Point> pRBIndexes = new List<Point>();

            int w = this._w;
            int h = this._h;

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                return -1;
            }

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;

            List<Tuple<int, int>> edges = new List<Tuple<int, int>>();

            #region fullGraph

            //fill the list for the known and unknown parts of the data
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (this.Mask[x, y] == 0)
                        bGIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 1)
                        fGIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 2)
                    {
                        pRIndexes.Add(new Point(x, y));
                        pRBIndexes.Add(new Point(x, y));
                    }
                    else if (this.Mask[x, y] == 3)
                    {
                        pRIndexes.Add(new Point(x, y));
                        pRFIndexes.Add(new Point(x, y));
                    }
                }

            //Console.WriteLine(bGIndexes.Count.ToString());
            //Console.WriteLine(fGIndexes.Count.ToString());
            //Console.WriteLine(pRIndexes.Count.ToString());

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(30);
            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            // bis hierher mit py app gleich
            //ShowIndexesImage(bGIndexes, fGIndexes, pRIndexes);

            //this is the main list used for computing/storing the graph-capacities
            this._graphCapacity = new List<double>();

            //the Data term (and so, the terminal nodes and its capacities)
            //t - links -->
            // https://www.researchgate.net/publication/230837921_Exact_Maximum_A_Posteriori_Estimation_for_Binary_Images
            //innerhalb des Graphen links zu Nodes des graphen(keine Adressen des Bildes)
            IEnumerable<Tuple<int, int>> v = pRIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            List<Tuple<int, int>> vf = v.ToList();

            //get and tweak the probaabilities of the pixels being fg or bg
            double[][] prVals = new double[v.Count()][];
            int cnt = v.Count();
            Parallel.For(0, cnt, j =>
            {
                int x = vf[j].Item2 % w;
                int y = vf[j].Item2 / w;
                int address = x * 4 + y * stride;
                prVals[j] = new double[] { p[address], p[address + 1], p[address + 2] };
            });
            double[] d = this._bgGmm.CalcProb(prVals);
            for (int j = 0; j < d.Length; j++)
                d[j] *= this.ProbMult1;

            IEnumerable<double> dTmp = d.Except(d.Where(a => a == 0));
            double d1 = 0;
            if (dTmp.Count() > 0)
                d1 = Math.Log(dTmp.Max());

            int l = d.Length;
            double[] d2 = this._fgGmm.CalcProb(prVals);

            OnShowInfo("d1 = " + d1.ToString());

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(40);

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            int[] z = new int[this.Mask.GetLength(0) * this.Mask.GetLength(1)];

            //take the negative logs as penalties (take the bg_probabilities for computing the fg_capacities and vice versa)
            Parallel.For(0, l, () => new InnerListObject(), (i, loopState, innerList) =>
            {
                if (d[i] == 0)
                    d[i] = 0.0000000001;
                if (d2[i] == 0)
                    d2[i] = 0.0000000001;

                if (this.UseThreshold)
                {
                    d[i] = Math.Log(d[i]) < -this.Threshold ? d[i] : 0;
                }

                double vv = Math.Log(d[i] / d2[i]);

                if (double.IsInfinity(vv))
                    vv = 0.0000001;

                Point ind = pRIndexes[i];

                if (vv > 0)
                {
                    innerList.Edges.Add(Tuple.Create(this._source, ind.X + ind.Y * w));
                    if (this.MultCapacitiesForTLinks)
                    {
                        if (CastIntCapacitiesForTLinks)
                            innerList.Capacities.Add((int)(vv * this.MultTLinkCapacity));
                        else
                            innerList.Capacities.Add(vv * this.MultTLinkCapacity);
                    }
                    else
                        innerList.Capacities.Add(vv);
                }
                else
                {
                    z[ind.X + ind.Y * w] = 1;
                    innerList.Edges.Add(Tuple.Create(ind.X + ind.Y * w, this._sink));
                    if (this.MultCapacitiesForTLinks)
                    {
                        if (CastIntCapacitiesForTLinks)
                            innerList.Capacities.Add((int)(-vv * this.MultTLinkCapacity));
                        else
                            innerList.Capacities.Add(-vv * this.MultTLinkCapacity);
                    }
                    else
                        innerList.Capacities.Add(-vv);
                }

                return innerList;
            }, (innerList) =>
            {
                //make sure, the edges get the correct capacities, means, add both chonks of data in the same processing cycles
                lock (this._lockObject)
                {
                    edges.AddRange(innerList.Edges);
                    this._graphCapacity.AddRange(innerList.Capacities);
                }
            });

            //if we want to use only the first guess, we dont need to set up the graph and the n-links and so can return here
            if (this.QuickEstimation && !this.CGwithQE)
            {
                //no Mincut needed.
                this.Bmp.UnlockBits(bmData);
                this._result2 = z;

                //GetTestPic(z);
                return 0;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(45);

            //since we use a directed graph, make sur, each node is connected to the most of 1 terminal 
            //(the other wa connections are commented out for the known states of the pixels)

            //v = bGIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            //edges.AddRange(v);
            //this._graphCapacity.AddRange(Enumerable.Repeat(0.0, v.Count()));

            //
            v = bGIndexes.Select(ind => Tuple.Create(ind.X + ind.Y * w, this._sink));
            edges.AddRange(v);
            this._graphCapacity.AddRange(Enumerable.Repeat(9.0 * this.Gamma, v.Count()));

            //
            v = fGIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            edges.AddRange(v);
            this._graphCapacity.AddRange(Enumerable.Repeat(9.0 * this.Gamma, v.Count()));

            //
            //v = fGIndexes.Select(ind => Tuple.Create(ind.X + ind.Y * w, this._sink));
            //edges.AddRange(v);
            //this._graphCapacity.AddRange(Enumerable.Repeat(0.0, v.Count()));

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(50);
            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            //get a reference number, needed later when we construct the graph
            begin = edges.Count;

            //n-links 
            //get the edges and its capacities for the "inner(ly)_connected_vertices most likely for the pixels in the image"
            Tuple<int, int>[] indexes = new Tuple<int, int>[(w - 1) * h];
            Tuple<int, int>[] indexes2 = new Tuple<int, int>[(w - 1) * h];

            //for (int y = 0; y < h; y++)
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w - 1; x++)
                {
                    indexes[y * (w - 1) + x] = new Tuple<int, int>(y * (w - 1) + x + 1 + y, y * (w - 1) + x + y);
                    indexes2[y * (w - 1) + x] = new Tuple<int, int>(y * (w - 1) + x + y, y * (w - 1) + x + 1 + y);
                }
            });

            edges.AddRange(indexes);
            edges.AddRange(indexes2);
            double[] lv1 = new double[(w - 1) * h];
            double[] lv2 = new double[(w - 1) * h];
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < (w - 1); x++)
                {
                    lv1[y * (w - 1) + x] = this._lv[x, y];
                    lv2[y * (w - 1) + x] = this._lv[x, y];
                }
            });
            this._graphCapacity.AddRange(lv1);
            this._graphCapacity.AddRange(lv2);

            begin2 = edges.Count;

            if (this.EightAdj)
            {
                indexes = new Tuple<int, int>[(w - 1) * (h - 1)];
                indexes2 = new Tuple<int, int>[(w - 1) * (h - 1)];

                //for (int y = 0; y < h - 1; y++)
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                    {
                        indexes[y * (w - 1) + x] = new Tuple<int, int>((y + 1) * w + x + 1, y * w + x);
                        indexes2[y * (w - 1) + x] = new Tuple<int, int>(y * w + x, (y + 1) * w + x + 1);
                    }
                });

                edges.AddRange(indexes);
                edges.AddRange(indexes2);
                double[] ulv1 = new double[(w - 1) * (h - 1)];
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                        ulv1[y * (w - 1) + x] = this._ulv[x, y];
                });
                this._graphCapacity.AddRange(ulv1);
                this._graphCapacity.AddRange(ulv1);
            }

            indexes = new Tuple<int, int>[w * (h - 1)];
            indexes2 = new Tuple<int, int>[w * (h - 1)];

            //for (int y = 0; y < (h - 1); y++)
            Parallel.For(0, h - 1, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    indexes[y * w + x] = new Tuple<int, int>((y + 1) * w + x, y * w + x);
                    indexes2[y * w + x] = new Tuple<int, int>(y * w + x, (y + 1) * w + x);
                }
            });

            edges.AddRange(indexes);
            edges.AddRange(indexes2);
            double[] uv1 = new double[w * (h - 1)];
            double[] uv2 = new double[w * (h - 1)];
            Parallel.For(0, h - 1, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    uv1[y * w + x] = this._uv[x, y];
                    uv2[y * w + x] = this._uv[x, y];
                }
            });
            this._graphCapacity.AddRange(uv1);
            this._graphCapacity.AddRange(uv2);

            if (this.EightAdj)
            {
                indexes = new Tuple<int, int>[(w - 1) * (h - 1)];
                indexes2 = new Tuple<int, int>[(w - 1) * (h - 1)];

                //for (int y = 0; y < h - 1; y++)
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                    {
                        indexes[y * (w - 1) + x] = new Tuple<int, int>((y + 1) * w + x, y * w + x + 1);
                        indexes2[y * (w - 1) + x] = new Tuple<int, int>(y * w + x + 1, (y + 1) * w + x);
                    }
                });

                edges.AddRange(indexes);
                edges.AddRange(indexes2);
                double[] urv1 = new double[(w - 1) * (h - 1)];
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                        urv1[y * (w - 1) + x] = this._urv[x, y];
                });
                this._graphCapacity.AddRange(urv1);
                this._graphCapacity.AddRange(urv1);
            }

            //MessageBox.Show(this._graphCapacity.Count.ToString() + " -> " + (5 * w * h - 2 * (w + h)).ToString() + " -> " + (2 * w * h).ToString());

            //do a intermediate check
            if (edges.Count != this._graphCapacity.Count)
                return -1;

            //bool normalizeCapacity = true;
            //if (normalizeCapacity)
            //{
            //    double maxCap = this._graphCapacity.Max();
            //    double minCap = this._graphCapacity.Min();

            //    double diff = maxCap - minCap;

            //    for(int j = 0; j < this._graphCapacity.Count; j++)
            //    {
            //        this._graphCapacity[j] -= minCap;
            //        this._graphCapacity[j] /= diff;
            //    }
            //}

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(60);
            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }
            #endregion

            //now setup the graph(s
            ////for each node, store its address and a Dictionary that holds these values for all of the neighbors of the current node)

            //Console.WriteLine("graphCapacity.Count = " + this._graphCapacity.Count.ToString());

            Dictionary<int, Dictionary<int, double>> graph = new Dictionary<int, Dictionary<int, double>>();
            for (int i = 0; i < this._graphCapacity.Count; i++)
            {
                //if (this._graphCapacity[i] > 0) //only add positive edges
                if (!graph.ContainsKey(edges[i].Item1))
                {
                    Dictionary<int, double> list = new Dictionary<int, double>();
                    list.Add(edges[i].Item2, this._graphCapacity[i]);
                    graph.Add(edges[i].Item1, list);
                }
                else
                    if (!graph[edges[i].Item1].ContainsKey(edges[i].Item2))
                    graph[edges[i].Item1].Add(edges[i].Item2, this._graphCapacity[i]);
            }

            if (!graph.ContainsKey(this._sink))
            {
                Dictionary<int, double> list2 = new Dictionary<int, double>();
                graph.Add(this._sink, list2);
            }

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            int grphVertices = graph.Count;

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(80);
            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            //test the algs
            //DirectedGraph dg1 = new DirectedGraph(6);

            //dg1.addEdge(0, 1, 16);
            //dg1.addEdge(0, 2, 13);
            //dg1.addEdge(1, 2, 10);
            //dg1.addEdge(2, 1, 4);
            //dg1.addEdge(1, 3, 12);
            //dg1.addEdge(3, 2, 9);
            //dg1.addEdge(2, 4, 14);
            //dg1.addEdge(4, 5, 4);
            //dg1.addEdge(4, 3, 7);
            //dg1.addEdge(3, 5, 20);

            //PushRelabelFifo f = new PushRelabelFifo(dg1, 0, 5);
            //f = new PushRelabelFifo(dg1, 0, 5);
            //double d32479 = f.FIFOPushRelabelStd(100000, false, null);
            //MessageBox.Show(d32479.ToString());

            //DirectedGraph dg22 = new DirectedGraph(6);

            //dg22.addEdge(0, 1, 16);
            //dg22.addEdge(0, 2, 13);
            //dg22.addEdge(1, 2, 10);
            //dg22.addEdge(2, 1, 4);
            //dg22.addEdge(1, 3, 12);
            //dg22.addEdge(3, 2, 9);
            //dg22.addEdge(2, 4, 14);
            //dg22.addEdge(4, 5, 4);
            //dg22.addEdge(4, 3, 7);
            //dg22.addEdge(3, 5, 20);

            //BoykovKolmogorov ff = new BoykovKolmogorov(dg22, 0, 5);
            //double d48 = ff.RunMinCut();
            //MessageBox.Show(d48.ToString());

            //now create the graph from the dictionary
            //also, setup the residual graph for the estimation, this could also be done in the maxflow algorithm
            DirectedGraph dg = new DirectedGraph(grphVertices);
            //residual graph for alg
            DirectedGraph dg2 = new DirectedGraph(grphVertices);
            foreach (int j in graph.Keys)
            {
                int from = j;

                Dictionary<int, double> list = graph[from];

                foreach (int a in list.Keys)
                {
                    dg.addEdge(j, a, list[a]);
                    dg2.addEdge(j, a, list[a]);
                }
            }

            for (int u = 0; u < dg2.vertices; u++)
            {
                Dictionary<int, double> ll = dg2.adjacencyList[u];
                try
                {
                    foreach (int vv in ll.Keys)
                    {
                        if (!dg2.hasEdge(vv, u))
                            dg2.addEdge(vv, u, 0);

                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                    return -25;
                }
            }

            this._dg = dg;
            this._rG = dg2;

            this.Bmp.UnlockBits(bmData);

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(95);

            //this._graph = graph;
            //Console.WriteLine(this._graph.Count.ToString());

            //double factor = 255.0 / this._graphCapacity.Skip(begin).Max();

            //Bitmap bmp1 = new Bitmap(w, h);
            //BitmapData bD = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //int strd = bD.Stride;

            //Bitmap bmp2 = new Bitmap(w, h);
            //BitmapData bD2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //int strd2 = bD2.Stride;

            //byte* pb1 = (byte*)bD.Scan0;
            //byte* pb2 = (byte*)bD2.Scan0;

            //for (int j = begin; j < begin + ((begin2 - begin) / 2); j++)
            //{
            //    int x = edges[j].Item2 % bmp1.Width;
            //    int y = edges[j].Item2 / bmp1.Width;

            //    int tmp = x + y * (bmp1.Width - 1);

            //    //if (x < bmp1.Width && y < bmp1.Height)
            //    {
            //        pb1[x * 4 + y * strd] = (byte)Math.Max(Math.Min(this._graphCapacity[j] * factor, 255), 0);
            //        pb1[x * 4 + y * strd + 3] = 255;
            //    }
            //}

            //for (int j = begin2; j < begin2 + ((begin2 - begin) / 2); j++)
            //{
            //    int x = edges[j].Item2 % bmp2.Width;
            //    int y = edges[j].Item2 / bmp2.Width;

            //    if (x < bmp2.Width && y < bmp2.Height)
            //    {
            //        pb2[x * 4 + y * strd + 2] = (byte)Math.Max(Math.Min(this._graphCapacity[j] * factor, 255), 0);
            //        pb2[x * 4 + y * strd + 3] = 255;
            //    }
            //}

            //bmp1.UnlockBits(bD);
            //bmp2.UnlockBits(bD2);

            //Form ff = new Form();
            //ff.BackgroundImage = bmp1;
            //ff.BackgroundImageLayout = ImageLayout.Zoom;
            //ff.ShowDialog();

            //ff.BackgroundImage = bmp2;
            //ff.BackgroundImageLayout = ImageLayout.Zoom;
            //ff.ShowDialog();

            //bmp1.Dispose();
            //bmp2.Dispose();
            return 0;
        }

        private unsafe int ConstructGraphOld()
        {
            if (this.GammaChanged)
            {
                if (this.CGwithQE || !this.QuickEstimation)
                    CalcBeta();
                this.GammaChanged = false;
            }

            int begin = 0;
            int begin2 = 0;

            this._result2 = null;

            List<Point> bGIndexes = new List<Point>();
            List<Point> fGIndexes = new List<Point>();
            List<Point> pRIndexes = new List<Point>();
            List<Point> pRIndexesFull = new List<Point>();

            int w = this._w;
            int h = this._h;

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                return -1;
            }

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;

            List<Tuple<int, int>> edges = new List<Tuple<int, int>>();

            #region fullGraph

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (this.Mask[x, y] == 0)
                        bGIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 1)
                        fGIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 2)
                        pRIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 3)
                        pRIndexes.Add(new Point(x, y));
                }

            Console.WriteLine(bGIndexes.Count.ToString());
            Console.WriteLine(fGIndexes.Count.ToString());
            Console.WriteLine(pRIndexes.Count.ToString());

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(30);

            // bis hierher mit py app gleich

            this._graphCapacity = new List<double>();

            //t-links
            //innerhalb des Graphen links zu Nodes des graphen (keine Adressen des Bildes)
            IEnumerable<Tuple<int, int>> v = pRIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            edges.AddRange(v);
            double[][] prVals = new double[v.Count()][];
            int cnt = v.Count();
            Parallel.For(0, cnt, j =>
            {
                int x = edges[(int)j].Item2 % w;
                int y = edges[(int)j].Item2 / w;
                int address = x * 4 + y * stride;
                prVals[j] = new double[] { p[address], p[address + 1], p[address + 2] };
            });
            double[] d = this._bgGmm.CalcProb(prVals);
            for (int j = 0; j < d.Length; j++)
                d[j] *= this.ProbMult1;

            int[] z = new int[this.Mask.GetLength(0) * this.Mask.GetLength(1)];

            int l = d.Length;
            Parallel.For(0, l, i =>
            {
                if (d[i] == 0)
                    d[i] = 0.0000000001;
                d[i] = -Math.Log(d[i]);

                //d[i] = d[0] > 0 ? -Math.Log(d[i]) : Math.Log(d[i]);

                if (this.UseThreshold)
                {
                    d[i] = d[i] > this.Threshold ? d[i] : 0;
                }

                if (this.MultCapacitiesForTLinks)
                {
                    if (CastIntCapacitiesForTLinks)
                        d[i] = (int)(d[i] * this.MultTLinkCapacity);
                    else
                        d[i] *= this.MultTLinkCapacity;
                }
            });

            //this._graphCapacity.AddRange(d.Select(a => (a * this.IntMult)));
            this._graphCapacity.AddRange(d);

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(40);

            v = pRIndexes.Select(ind => Tuple.Create(ind.X + ind.Y * w, this._sink));
            edges.AddRange(v);
            d = this._fgGmm.CalcProb(prVals);

            l = d.Length;
            Parallel.For(0, l, i =>
            {
                if (d[i] == 0)
                    d[i] = 0.0000000001;
                d[i] = -Math.Log(d[i]);

                //d[i] = d[0] > 0 ? -Math.Log(d[i]) : Math.Log(d[i]);

                if (this.MultCapacitiesForTLinks)
                {
                    if (CastIntCapacitiesForTLinks)
                        d[i] = (int)(d[i] * this.MultTLinkCapacity);
                    else
                        d[i] *= this.MultTLinkCapacity;
                }
            });

            //this._graphCapacity.AddRange(d.Select(a => (a * this.IntMult)));
            this._graphCapacity.AddRange(d);

            for (int i = 0; i < pRIndexes.Count; i++)
                if (this._graphCapacity[i] > this._graphCapacity[i + pRIndexes.Count])
                    z[edges[i].Item2] = 1;

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            if (this.QuickEstimation && !this.CGwithQE)
            {
                //no Mincut needed.
                this.Bmp.UnlockBits(bmData);
                this._result2 = z;

                //GetTestPic(z);
                return 0;
            }

            v = bGIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            edges.AddRange(v);
            this._graphCapacity.AddRange(Enumerable.Repeat(0.0, v.Count()));

            //
            v = bGIndexes.Select(ind => Tuple.Create(ind.X + ind.Y * w, this._sink));
            edges.AddRange(v);
            this._graphCapacity.AddRange(Enumerable.Repeat(9.0 * this.Gamma, v.Count()));

            //
            v = fGIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            edges.AddRange(v);
            this._graphCapacity.AddRange(Enumerable.Repeat(9.0 * this.Gamma, v.Count()));

            //
            v = fGIndexes.Select(ind => Tuple.Create(ind.X + ind.Y * w, this._sink));
            edges.AddRange(v);
            this._graphCapacity.AddRange(Enumerable.Repeat(0.0, v.Count()));

            begin = edges.Count;

            //n-links 
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(50);

            Tuple<int, int>[] indexes = new Tuple<int, int>[(w - 1) * h];
            Tuple<int, int>[] indexes2 = new Tuple<int, int>[(w - 1) * h];

            //for (int y = 0; y < h; y++)
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w - 1; x++)
                {
                    indexes[y * (w - 1) + x] = new Tuple<int, int>(y * (w - 1) + x + 1 + y, y * (w - 1) + x + y);
                    indexes2[y * (w - 1) + x] = new Tuple<int, int>(y * (w - 1) + x + y, y * (w - 1) + x + 1 + y);
                }
            });

            edges.AddRange(indexes);
            edges.AddRange(indexes2);
            double[] lv1 = new double[(w - 1) * h];
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < (w - 1); x++)
                    lv1[y * (w - 1) + x] = this._lv[x, y];
            });
            this._graphCapacity.AddRange(lv1);
            this._graphCapacity.AddRange(lv1);

            begin2 = edges.Count;

            if (this.EightAdj)
            {
                indexes = new Tuple<int, int>[(w - 1) * (h - 1)];
                indexes2 = new Tuple<int, int>[(w - 1) * (h - 1)];

                //for (int y = 0; y < h - 1; y++)
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                    {
                        indexes[y * (w - 1) + x] = new Tuple<int, int>((y + 1) * w + x + 1, y * w + x);
                        indexes2[y * (w - 1) + x] = new Tuple<int, int>(y * w + x, (y + 1) * w + x + 1);
                    }
                });

                edges.AddRange(indexes);
                edges.AddRange(indexes2);
                double[] ulv1 = new double[(w - 1) * (h - 1)];
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                        ulv1[y * (w - 1) + x] = this._ulv[x, y];
                });
                this._graphCapacity.AddRange(ulv1);
                this._graphCapacity.AddRange(ulv1);
            }

            indexes = new Tuple<int, int>[w * (h - 1)];
            indexes2 = new Tuple<int, int>[w * (h - 1)];

            //for (int y = 0; y < (h - 1); y++)
            Parallel.For(0, h - 1, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    indexes[y * w + x] = new Tuple<int, int>((y + 1) * w + x, y * w + x);
                    indexes2[y * w + x] = new Tuple<int, int>(y * w + x, (y + 1) * w + x);
                }
            });

            edges.AddRange(indexes);
            edges.AddRange(indexes2);
            double[] uv1 = new double[w * (h - 1)];
            Parallel.For(0, h - 1, y =>
            {
                for (int x = 0; x < w; x++)
                    uv1[y * w + x] = this._uv[x, y];
            });
            this._graphCapacity.AddRange(uv1);
            this._graphCapacity.AddRange(uv1);

            if (this.EightAdj)
            {
                indexes = new Tuple<int, int>[(w - 1) * (h - 1)];
                indexes2 = new Tuple<int, int>[(w - 1) * (h - 1)];

                //for (int y = 0; y < h - 1; y++)
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                    {
                        indexes[y * (w - 1) + x] = new Tuple<int, int>((y + 1) * w + x, y * w + x + 1);
                        indexes2[y * (w - 1) + x] = new Tuple<int, int>(y * w + x + 1, (y + 1) * w + x);
                    }
                });

                edges.AddRange(indexes);
                edges.AddRange(indexes2);
                double[] urv1 = new double[(w - 1) * (h - 1)];
                Parallel.For(0, h - 1, y =>
                {
                    for (int x = 0; x < w - 1; x++)
                        urv1[y * (w - 1) + x] = this._urv[x, y];
                });
                this._graphCapacity.AddRange(urv1);
                this._graphCapacity.AddRange(urv1);
            }

            if (edges.Count != this._graphCapacity.Count)
                return -1;

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(60);
            #endregion

            Console.WriteLine("graphCapacity.Count = " + this._graphCapacity.Count.ToString());

            Dictionary<int, Dictionary<int, double>> graph = new Dictionary<int, Dictionary<int, double>>();
            for (int i = 0; i < this._graphCapacity.Count; i++)
            {
                //if (this._graphCapacity[i] > 0) //only add positive edges
                if (!graph.ContainsKey(edges[i].Item1))
                {
                    Dictionary<int, double> list = new Dictionary<int, double>();
                    list.Add(edges[i].Item2, this._graphCapacity[i]);
                    graph.Add(edges[i].Item1, list);
                }
                else
                    if (!graph[edges[i].Item1].ContainsKey(edges[i].Item2))
                    graph[edges[i].Item1].Add(edges[i].Item2, this._graphCapacity[i]);
            }

            if (!graph.ContainsKey(this._sink))
            {
                Dictionary<int, double> list2 = new Dictionary<int, double>();
                graph.Add(this._sink, list2);
            }

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(80);

            int grphVertices = graph.Count;

            DirectedGraph dg = new DirectedGraph(grphVertices);
            //residual graph for alg
            DirectedGraph dg2 = new DirectedGraph(grphVertices);
            foreach (int j in graph.Keys)
            {
                int from = j;
                Dictionary<int, double> list = graph[from];
                foreach (int a in list.Keys)
                {
                    dg.addEdge(j, a, list[a]);
                    dg2.addEdge(j, a, list[a]);
                }
            }

            for (int u = 0; u < dg2.vertices; u++)
            {
                Dictionary<int, double> ll = dg2.adjacencyList[u];
                foreach (int vv in ll.Keys)
                {
                    if (!dg2.hasEdge(vv, u))
                        dg2.addEdge(vv, u, 0);
                }
            }

            this._dg = dg;
            this._rG = dg2;

            this.Bmp.UnlockBits(bmData);

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(100);

            //this._graph = graph;
            //Console.WriteLine(this._graph.Count.ToString());

            //double factor = 255.0 / this._graphCapacity.Skip(begin).Max();

            //Bitmap bmp1 = new Bitmap(w, h);
            //BitmapData bD = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //int strd = bD.Stride;

            //Bitmap bmp2 = new Bitmap(w, h);
            //BitmapData bD2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //int strd2 = bD2.Stride;

            //byte* pb1 = (byte*)bD.Scan0;
            //byte* pb2 = (byte*)bD2.Scan0;

            //for (int j = begin; j < begin + ((begin2 - begin) / 2); j++)
            //{
            //    int x = edges[j].Item2 % bmp1.Width;
            //    int y = edges[j].Item2 / bmp1.Width;

            //    int tmp = x + y * (bmp1.Width - 1);

            //    //if (x < bmp1.Width && y < bmp1.Height)
            //    {
            //        pb1[x * 4 + y * strd] = (byte)Math.Max(Math.Min(this._graphCapacity[j] * factor, 255), 0);
            //        pb1[x * 4 + y * strd + 3] = 255;
            //    }
            //}

            //for (int j = begin2; j < begin2 + ((begin2 - begin) / 2); j++)
            //{
            //    int x = edges[j].Item2 % bmp2.Width;
            //    int y = edges[j].Item2 / bmp2.Width;

            //    if (x < bmp2.Width && y < bmp2.Height)
            //    {
            //        pb2[x * 4 + y * strd + 2] = (byte)Math.Max(Math.Min(this._graphCapacity[j] * factor, 255), 0);
            //        pb2[x * 4 + y * strd + 3] = 255;
            //    }
            //}

            //bmp1.UnlockBits(bD);
            //bmp2.UnlockBits(bD2);

            //Form ff = new Form();
            //ff.BackgroundImage = bmp1;
            //ff.BackgroundImageLayout = ImageLayout.Zoom;
            //ff.ShowDialog();

            //ff.BackgroundImage = bmp2;
            //ff.BackgroundImageLayout = ImageLayout.Zoom;
            //ff.ShowDialog();

            //bmp1.Dispose();
            //bmp2.Dispose();
            return 0;
        }

        //now run the maxflow algorithm and do some processing of the results and the used arrays (mask etc)
        private unsafe void EstimateSegmentation()
        {
            int w = this._w;
            int h = this._h;
            List<int> r = new List<int>();
            if (QuickEstimation && this._result2 != null)
            {
                for (int i = 0; i < this._result2.Length; i++)
                    if (this._result2[i] == 1)
                        r.Add(i);
            }
            else
            {
                PushRelabelFifo f = new PushRelabelFifo(_dg, _source, _sink);
                //PushRelabelGT f = new PushRelabelGT(_dg, _source, _sink);
                f.GetSourcePartition = false;
                f.NumItems = this.NumItems;
                f.NumCorrect = this.NumCorrect;
                f.NumItems2 = this.NumItems2;
                f.NumCorrect2 = this.NumCorrect2;
                f.residualGraph = this._rG;
                f.ShowInfo += F_ShowInfo;
                f.MaxIter = this.MaxIter;
                f.QATH = this.QATH;

                this.Alg = f;

                f.FIFOPushRelabel(this.QuickEstimation, this.BGW);

                r = f.Result;

                //ShowRToBmp(r);
            }

            this._dg = null;
            this._rG = null;

            if (r != null)
            {
                List<int> z = Enumerable.Range(0, w * h).Except(r).ToList();

                //get the set of unknown_state locations
                Dictionary<int, int> pRIndexes = new Dictionary<int, int>();

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (this.Mask[x, y] == 2)
                            pRIndexes.Add(x + y * this._w, 2);
                        else if (this.Mask[x, y] == 3)
                            pRIndexes.Add(x + y * this._w, 3);
                    }

                int l = z.Count;

                //OnShowInfo("UpdatingMask");

                //set the values in the mask

                //for (int j = 0; j < l; j++)
                Parallel.For(0, l, j =>
                {
                    int x = z[j] % this._w;
                    int y = z[j] / this._w;

                    if (pRIndexes.ContainsKey(z[j]))  //z is the negative of the resulting fg pixels
                    {
                        if (this.Mask[x, y] != 0 && this.Mask[x, y] != 1)
                            this.Mask[x, y] = 2;
                    }
                    else
                    {
                        if (this.Mask[x, y] != 0 && this.Mask[x, y] != 1)
                            this.Mask[x, y] = 3;
                    }
                });

                //ShowMaskToBmp();

                //do a pre-classification for the next iteration, if needed
                Classify();
                this.Result = z;

                //ShowMaskToBmp();

                //Console.WriteLine((r.Count == pRIndexes.Count).ToString());
            }
            else
                this.Result = new List<int>();
        }

        private unsafe void EstimateSegmentationBK()
        {
            int w = this._w;
            int h = this._h;
            List<int> r = new List<int>();
            List<int> r2 = new List<int>();
            if (QuickEstimation && this._result2 != null)
            {
                for (int i = 0; i < this._result2.Length; i++)
                    if (this._result2[i] != 1)
                        r.Add(i);
            }
            else
            {
                //CheckStartNodes();

                BoykovKolmogorov f = new BoykovKolmogorov(_dg, _rG, _source, _sink, w, h, StartNodes, this._addOnlySourceAndSink);
                f.BGW = this.BGW;
                f.GetSourcePartition = true;
                f.NumItems = this.NumItems;
                f.NumCorrect = this.NumCorrect;
                f.ShowInfo += F_ShowInfo;

                this.AlgBK = f;

                f.MaxCycles = Int32.MaxValue;
                f.RunMinCut();
                r = f.Result;

                if (f.BreakResult != null)
                    MessageBox.Show("Processing cancelled at " + f.BreakResult.counter.ToString() + " Iterations.");
                if (f.ReversedResult)
                    MessageBox.Show("Incomplete Path. Reversed Result returned.");
            }

            this._dg = null;
            this._rG = null;

            if (r != null)
            {
                List<int> z = Enumerable.Range(0, w * h).Except(r).ToList();

                Dictionary<int, int> pRIndexes = new Dictionary<int, int>();

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (this.Mask[x, y] == 2)
                            pRIndexes.Add(x + y * this._w, 2);
                        else if (this.Mask[x, y] == 3)
                            pRIndexes.Add(x + y * this._w, 3);
                    }

                int l = z.Count;

                //OnShowInfo("UpdatingMask");

                //for (int j = 0; j < l; j++)
                Parallel.For(0, l, j =>
                {
                    int x = z[j] % this._w;
                    int y = z[j] / this._w;

                    if (pRIndexes.ContainsKey(z[j]))  //z is the negative of the resulting fg pixels
                    {
                        if (this.Mask[x, y] != 0 && this.Mask[x, y] != 1)
                            this.Mask[x, y] = 2;
                    }
                    else
                    {
                        if (this.Mask[x, y] != 0 && this.Mask[x, y] != 1)
                            this.Mask[x, y] = 3;
                    }
                });

                //ShowMaskToBmp();

                Classify();
                this.Result = z;

                Console.WriteLine((r.Count == pRIndexes.Count).ToString());
            }
            else
                this.Result = new List<int>();
        }

        private void F_ShowInfo(object sender, string e)
        {
            OnShowInfo(e);
        }

        internal unsafe void SetAllPointsInMask(Dictionary<int, List<Tuple<List<Point>, int>>> allPoints, bool setPFGToFG)
        {
            if (setPFGToFG)
            {
                for (int y = 0; y < this._h; y++)
                    for (int x = 0; x < this._w; x++)
                    {
                        if (this.Mask[x, y] == 3)
                            this.Mask[x, y] = 1;
                    }
            }

            if (this.Mask != null)
            {
                foreach (int j in allPoints.Keys)
                {
                    List<Tuple<List<Point>, int>> pts = allPoints[j];

                    for (int i = 0; i < pts.Count; i++)
                    {
                        List<Point> cP = pts[i].Item1;
                        int wh = pts[i].Item2 / 2;

                        for (int l = 0; l < cP.Count; l++)
                            Rect(cP[l], j, wh);
                    }
                }

                Classify();

                //ShowMaskToBmp();
            }
        }

        internal void ReInitScribbles()
        {
            if (this.Mask != null)
            {
                if (this.RectMode && this.ScribbleMode)
                {
                    //rect part
                    Parallel.For(0, this._h, y =>
                    {
                        for (int x = 0; x < this._w; x++)
                        {
                            if (this.Rc.Contains(x, y))
                            {
                                if (this.Mask[x, y] != 1)
                                    this.Mask[x, y] = 3;
                            }
                            else
                                this.Mask[x, y] = 0;
                        }
                    });
                    //scribble part
                    foreach (int i in this.Scribbles.Keys)
                    {
                        Dictionary<int, List<List<Point>>> allPts = this.Scribbles[i]; //dict = <BG/FG, <wh, pts>>

                        foreach (int wh in allPts.Keys)
                        {
                            int wh2 = wh / 2;

                            for (int j = 0; j < allPts[wh].Count; j++)
                            {
                                List<Point> pts = allPts[wh][j];

                                foreach (Point pt in pts)
                                    RectReInit(pt, i, wh2);
                            }
                        }
                    }
                }
                //BG = 0, FG = 1, PrBG = 2, PrFG = 3
                else if (this.RectMode)
                    //for (int y = 0; y < this._h; y++)
                    Parallel.For(0, this._h, y =>
                    {
                        for (int x = 0; x < this._w; x++)
                        {
                            if (this.Rc.Contains(x, y))
                            {
                                if (this.Mask[x, y] != 1)
                                    this.Mask[x, y] = 3;
                            }
                            else
                                this.Mask[x, y] = 0;
                        }
                    });
                else if (this.ScribbleMode)
                {
                    Parallel.For(0, this._h, y =>
                    {
                        for (int x = 0; x < this._w; x++)
                            if (this.Mask[x, y] != 1)
                                this.Mask[x, y] = 3;
                    });

                    foreach (int i in this.Scribbles.Keys)
                    {
                        Dictionary<int, List<List<Point>>> allPts = this.Scribbles[i]; //dict = <BG/FG, <wh, pts>>

                        foreach (int wh in allPts.Keys)
                        {
                            int wh2 = wh / 2;

                            for (int j = 0; j < allPts[wh].Count; j++)
                            {
                                List<Point> pts = allPts[wh][j];

                                foreach (Point pt in pts)
                                    RectReInit(pt, i, wh2);
                            }
                        }
                    }
                }
            }
        }

        private void RectReInit(Point point, int j, int wh)
        {
            for (int y = point.Y - wh; y < point.Y + wh; y++)
            {
                for (int x = point.X - wh; x < point.X + wh; x++)
                {
                    if (x >= 0 && x < this._w && y >= 0 && y < this._h)
                        if (this.Mask[x, y] != 1)
                            this.Mask[x, y] = j;
                }
            }
        }

        private void RectReInit(Point point, int j, int wh, PointF whFactors)
        {
            int whX = (int)(wh * whFactors.X);
            int whY = (int)(wh * whFactors.Y);

            for (int y = point.Y - whY; y < point.Y + whY; y++)
            {
                for (int x = point.X - whX; x < point.X + whX; x++)
                {
                    if (x >= 0 && x < this._w && y >= 0 && y < this._h)
                        if (this.Mask[x, y] != 1)
                            this.Mask[x, y] = j;
                }
            }
        }

        public void Dispose()
        {
            if (this.Bmp != null)
                this.Bmp.Dispose();
        }

        //##################################
        public int InitWithTrimap(Bitmap trimap)
        {
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(0);

            if (this.Bmp != null && trimap != null)
            {
                this._w = this.Bmp.Width;
                this._h = this.Bmp.Height;

                //first get a representaion of the current state by defining a "mask" array for storing the
                //pixel states: bg, fg, probably bg, probably fg as 0, 1, 2, 3
                this.Mask = new int[this._w, this._h];

                //BG = 0, FG = 1, PrBG = 2, PrFG = 3
                ReadIn(trimap, this.Mask);

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(20);

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                if (this.Mask == null)
                    return -5;

                //ShowMaskToBmp();

                //do a first classification, i.e. determine the known and unknown states for the pixels,
                //store these values in arrays and list to use later
                int cl = Classify();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(30);

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                if (cl != 0)
                    return cl;

                this._source = this._w * this._h;
                this._sink = this._source + 1;

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(50);

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    this.BGW.CancelAsync();

                //now init the Gmms [the machinery for the Data-term of the energy function]
                InitGMMs();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);

                return 0;
            }

            return -3;
        }

        private unsafe void ReadIn(Bitmap trimap, int[,] mask)
        {
            int w = trimap.Width;
            int h = trimap.Height;
            BitmapData bmTr = trimap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmTr.Stride;

            Parallel.For(0, h, y =>
            {
                byte* p = (byte*)bmTr.Scan0;
                p += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (p[0] < 5)
                        mask[x, y] = 0;
                    else if (p[0] > 250)
                        mask[x, y] = 1;
                    else
                        mask[x, y] = 3;

                    p += 4;
                }
            });

            trimap.UnlockBits(bmTr);
        }

        public int RunBoundary()
        {
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(10);

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                return 101;

            if (this._bgGmm == null || this._fgGmm == null)
                return 102;

            OnShowInfo("Iteration " + (1).ToString() + " of " + this.NumIters.ToString());

            if (!this.SkipLearn)
            {
                if (!AssignSamplesAndFit())
                    return 100;
            }

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(15);

            //compute the directed graph
            int j = ComputeAlpha();

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                return 101;

            if (j != 0)
                return j;

            EstimateSegmentationAlpha();

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(100);

            return 0;
        }

        private unsafe int ComputeAlpha()
        {
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(20);

            this._result2 = null;

            List<Point> bGIndexes = new List<Point>();
            List<Point> fGIndexes = new List<Point>();
            List<Point> pRIndexes = new List<Point>();
            List<Point> pRIndexesFull = new List<Point>();

            List<Point> pRFIndexes = new List<Point>();
            List<Point> pRBIndexes = new List<Point>();

            int w = this._w;
            int h = this._h;

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                return -1;
            }

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;

            List<Tuple<int, int>> edges = new List<Tuple<int, int>>();

            //fill the list for the known and unknown parts of the data
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (this.Mask[x, y] == 0)
                        bGIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 1)
                        fGIndexes.Add(new Point(x, y));
                    else if (this.Mask[x, y] == 2)
                    {
                        pRIndexes.Add(new Point(x, y));
                        pRBIndexes.Add(new Point(x, y));
                    }
                    else if (this.Mask[x, y] == 3)
                    {
                        pRIndexes.Add(new Point(x, y));
                        pRFIndexes.Add(new Point(x, y));
                    }
                }

            //Console.WriteLine(bGIndexes.Count.ToString());
            //Console.WriteLine(fGIndexes.Count.ToString());
            //Console.WriteLine(pRIndexes.Count.ToString());

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(30);
            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            //ShowIndexesImage(bGIndexes, fGIndexes, pRIndexes);

            //this is the main list used for computing/storing the graph-capacities
            this._graphCapacity = new List<double>();

            //the Data term
            //t - links -->
            // https://www.researchgate.net/publication/230837921_Exact_Maximum_A_Posteriori_Estimation_for_Binary_Images
            //innerhalb des Graphen links zu Nodes des graphen(keine Adressen des Bildes)
            IEnumerable<Tuple<int, int>> v = pRIndexes.Select(ind => Tuple.Create(this._source, ind.X + ind.Y * w));
            List<Tuple<int, int>> vf = v.ToList();

            //get and tweak the probabilities of the pixels being fg or bg
            double[][] prVals = new double[v.Count()][];
            int cnt = v.Count();
            Parallel.For(0, cnt, j =>
            {
                int x = vf[j].Item2 % w;
                int y = vf[j].Item2 / w;
                int address = x * 4 + y * stride;
                prVals[j] = new double[] { p[address], p[address + 1], p[address + 2] };
            });

            double[] d = this._bgGmm.CalcProb(prVals);
            for (int j = 0; j < d.Length; j++)
                d[j] *= this.ProbMult1;

            IEnumerable<double> dTmp = d.Except(d.Where(a => a == 0));
            double d1 = 0;
            if (dTmp.Count() > 0)
                d1 = Math.Log(dTmp.Max());

            int l = d.Length;
            double[] d2 = this._fgGmm.CalcProb(prVals);

            OnShowInfo("d1 = " + d1.ToString());

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(40);

            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
            {
                this.Bmp.UnlockBits(bmData);
                return -1;
            }

            int[] z = new int[this.Mask.GetLength(0) * this.Mask.GetLength(1)];

            //take the negative logs as penalties (take the bg_probabilities for computing the fg_capacities and vice versa)
            Parallel.For(0, l, () => new InnerListObject(), (i, loopState, innerList) =>
            {
                if (d[i] == 0)
                    d[i] = 0.0000000001;
                if (d2[i] == 0)
                    d2[i] = 0.0000000001;

                if (this.UseThreshold)
                {
                    d[i] = Math.Log(d[i]) < -this.Threshold ? d[i] : 0;
                }

                double vv = Math.Log(d[i] / d2[i]);

                if (double.IsInfinity(vv))
                    vv = 0.0000001;

                Point ind = pRIndexes[i];

                if (vv > 0)
                {
                    innerList.Edges.Add(Tuple.Create(this._source, ind.X + ind.Y * w));
                    if (this.MultCapacitiesForTLinks)
                    {
                        if (CastIntCapacitiesForTLinks)
                            innerList.Capacities.Add((int)(vv * this.MultTLinkCapacity));
                        else
                            innerList.Capacities.Add(vv * this.MultTLinkCapacity);
                    }
                    else
                        innerList.Capacities.Add(vv);
                }
                else
                {
                    z[ind.X + ind.Y * w] = 1;
                    innerList.Edges.Add(Tuple.Create(ind.X + ind.Y * w, this._sink));
                    if (this.MultCapacitiesForTLinks)
                    {
                        if (CastIntCapacitiesForTLinks)
                            innerList.Capacities.Add((int)(-vv * this.MultTLinkCapacity));
                        else
                            innerList.Capacities.Add(-vv * this.MultTLinkCapacity);
                    }
                    else
                        innerList.Capacities.Add(-vv);
                }

                return innerList;
            }, (innerList) =>
            {
                //make sure, the edges get the correct capacities, means, add both chonks of data in the same processing cycles
                lock (this._lockObject)
                {
                    edges.AddRange(innerList.Edges);
                    this._graphCapacity.AddRange(innerList.Capacities);
                }
            });

            //if we want to use only the first guess, we dont need to set up the graph and the n-links and so can return here
            //no Mincut needed.
            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(95);

            this.Bmp.UnlockBits(bmData);
            this._result2 = z;

            return 0;
        }

        private unsafe void EstimateSegmentationAlpha()
        {
            int w = this._w;
            int h = this._h;
            //List<int> r = new List<int>();

            //for (int i = 0; i < this._result2.Length; i++)
            //    r.Add(i);

            List<int> r = this._result2.ToList();

            if (r != null)
                this.Result = r;
            else
                this.Result = new List<int>();
        }
    }
}