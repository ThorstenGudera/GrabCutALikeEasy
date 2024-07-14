using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    public class KMeans_bgr
    {
        private int _amntClusters = 5;
        private double[][] _data = null;

        private Random _rnd;

        private double[][] _means = null;

        private List<Centroid> _centroids = new List<Centroid>();

        public int[] Labels { get; set; }
        public double[][] Means
        {
            get { return _means; }
        }

        internal ListSelectionMode SelectionMode { get; private set; }

        //### Test ###
        private bool _mode2 = false;
        //private bool _testMode = false;
        //### End Test ###

        public double KMInitW { get; private set; } = 1.0;
        public double KMInitH { get; private set; } = 1.0;
        public bool InitRnd { get; private set; } = false;
        public bool KMCompactGmm { get; internal set; } = false;
        public bool KMLABMode { get; internal set; } = false;
        public double KMCompactGamma { get; internal set; } = 1.0;

        private int _w;
        private int _h;

        public KMeans_bgr(double[][] X, Point[] pts, int amntClusters, bool useKpp, ListSelectionMode selMode,
            int kmInitIters, bool kmInitNoStartIteration, int w, int h, double kmInitW, double kmInitH, bool kmInitRnd,
            double kmCompactGamma, bool kmLabMode, bool kmCompactGmm)
        {
            this._w = w;
            this._h = h;
            this.KMInitW = kmInitW;
            this.KMInitH = kmInitH;
            this.InitRnd = kmInitRnd;
            this.KMCompactGmm = kmCompactGmm;
            this.KMCompactGamma = kmCompactGamma;
            this.KMLABMode = kmLabMode;

            this._amntClusters = amntClusters;
            this._data = CopyData(X);

            if (!useKpp)
            {
                InitMeans();
                AssignSamples();
            }
            else
            {
                this.SelectionMode = selMode;

                if (this.SelectionMode != ListSelectionMode.None)
                {
                    List<double[][]> meansTmp = new List<double[][]>();
                    List<int[]> labelsTmp = new List<int[]>();
                    List<double> powerTmp = new List<double>();
                    List<List<Centroid>> centroidsTmp = new List<List<Centroid>>();

                    int cnt = 0;
                    int n = this._data.Length;

                    if (kmInitIters <= 1)
                    {
                        KMeans_bgr tmp = new KMeans_bgr(X, pts, amntClusters, true, this.SelectionMode, w, h,
                            kmInitW, kmInitH, kmInitRnd, kmCompactGamma, kmLabMode, kmCompactGmm);

                        this.Labels = tmp.Labels;
                        this._means = tmp.Means;
                    }

                    for (int i = 0; i < kmInitIters; i++)
                    {
                        KMeans_bgr tmp = new KMeans_bgr(X, pts, amntClusters, true, this.SelectionMode, w, h,
                            kmInitW, kmInitH, kmInitRnd, kmCompactGamma, kmLabMode, kmCompactGmm);

                        //tmp._means = CheckMeansNotNull(tmp._means);

                        if (tmp._means != null && CheckMeans(tmp._means))
                        {
                            if (this._mode2)
                            {
                                tmp.Lloyd(pts, 10);

                                tmp.Labels = new int[n];

                                double cumPw = 0;

                                for (int j = 0; j < n; j++)
                                {
                                    tmp.Labels[j] = Nearest(pts[j], tmp._centroids, amntClusters);
                                    cumPw += GetEuclideanDistance(pts[j], tmp._centroids[tmp.Labels[j]].Pt, true);
                                }

                                meansTmp.Add(tmp._means);
                                labelsTmp.Add(tmp.Labels);
                                powerTmp.Add(cumPw);
                                centroidsTmp.Add(tmp._centroids);
                            }
                            else
                            {
                                meansTmp.Add(tmp._means);
                                centroidsTmp.Add(tmp._centroids);
                                double cumPw = 0;

                                for (int j = 0; j < n; j++)
                                    cumPw += GetEuclideanDistance(pts[j], tmp._centroids[tmp.Labels[j]].Pt, true);
                                powerTmp.Add(cumPw);

                                int selected = powerTmp.IndexOf(powerTmp.Min());
                                this._centroids = centroidsTmp[selected];
                            }
                        }
                        else
                        {
                            //shouldnt go into
                            if (cnt < kmInitIters)
                                i--;

                            cnt++;
                        }
                    }

                    if (this._mode2)
                    {
                        int selected = powerTmp.IndexOf(powerTmp.Min());
                        this.Labels = labelsTmp[selected];
                        this._means = meansTmp[selected];
                        List<Centroid> centroidsSelected = centroidsTmp[selected];
                        if (this.KMCompactGmm)
                            AssignSamples2(pts, centroidsSelected, this.KMCompactGamma, this._data.Length / 10.0);
                        else
                            AssignSamples();
                    }
                    else if (meansTmp.Count > 0 && kmInitIters > 1)
                    {
                        this._means = new double[this._amntClusters][];

                        for (int i = 0; i < amntClusters; i++)
                        {
                            this._means[i] = new double[meansTmp[0][0].Length];

                            for (int j = 0; j < meansTmp[0][0].Length; j++)
                            {
                                double[] dd = meansTmp.Select(x => x[i][j]).ToArray();

                                if (this.SelectionMode == ListSelectionMode.Min)
                                {
                                    double d = dd.Select(x => !double.IsNaN(x) && !double.IsInfinity(x) ? x : 0).Min();
                                    this._means[i][j] = d;
                                }
                                else if (this.SelectionMode == ListSelectionMode.Max)
                                {
                                    double d = dd.Select(x => !double.IsNaN(x) && !double.IsInfinity(x) ? x : 0).Max();
                                    this._means[i][j] = d;
                                }
                                else if (this.SelectionMode == ListSelectionMode.Average)
                                {
                                    double d = dd.Select(x => !double.IsNaN(x) && !double.IsInfinity(x) ? x : 0).Average();
                                    this._means[i][j] = d;
                                }
                            }
                        }

                        if (this.KMLABMode)
                        {
                            ConvertMeans();
                            ConvertData();
                        }

                        if (this.KMCompactGmm)
                            AssignSamples2(pts, this._centroids, this.KMCompactGamma, this._data.Length / 10.0);
                        else
                            AssignSamples();

                        //for (int j = 0; j < 100; j++)
                        //{
                        //    AssignSamples();
                        //    UpdateMeans();
                        //}
                    }
                    else if (kmInitIters > 1)
                    {
                        InitMeans();
                        AssignSamples();
                    }
                }
                else
                {
                    InitKpp(X, pts, amntClusters);
                }
            }
        }

        //private double[][] CheckMeansNotNull(double[][] means)
        //{
        //    List<double[]> l = new List<double[]>();
        //    for (int i = means.Length - 1; i >= 0; i--)
        //        if(means[i] != null)
        //            l.Add(means[i]);

        //    return l.ToArray();
        //}

        public KMeans_bgr(double[][] X, Point[] pts, int amntClusters, bool useKpp, ListSelectionMode selMode, int w, int h,
            double kmInitW, double kmInitH, bool kmInitRnd, double kmCompactGamma, bool kmLabMode, bool kmCompactGmm)
        {
            this._w = w;
            this._h = h;
            this.KMInitW = kmInitW;
            this.KMInitH = kmInitH;
            this.InitRnd = kmInitRnd;
            this.KMCompactGmm = kmCompactGmm;
            this.KMCompactGamma = kmCompactGamma;
            this.KMLABMode = kmLabMode;
            this.KMLABMode = kmLabMode;

            this._amntClusters = amntClusters;
            this._data = CopyData(X);

            if (!useKpp)
            {
                InitMeans();
                AssignSamples();
            }
            else
            {
                InitKpp(X, pts, amntClusters);
            }

        }

        #region kpp
        private void InitKpp(double[][] X, Point[] pts, int amntClusters)
        {
            this._rnd = new Random();
            int n = X.Length;

            if (amntClusters == 1 || n <= 0)
            {
                this._centroids.Add(new Centroid(pts[this._rnd.Next(n)]));
                this.Labels = new int[n];
                InitMeans2(X);
                return;
            }
            if (amntClusters >= n)
            {
                this._centroids.Add(new Centroid(pts[this._rnd.Next(n)]));
                this.Labels = new int[n];
                InitMeans2(X);
                return;
            }

            double[] shortestDist = new double[n];
            double[] cumulativeDist = new double[n];

            if (this.InitRnd)
            {
                int j = this._rnd.Next(n);
                this._centroids.Add(new Centroid(pts[j]));
            }
            else
                this._centroids.Add(new Centroid(new Point((int)(this._w / this.KMInitW), (int)(this._h / this.KMInitH))));

            for (int i = 1; i < amntClusters; i++)
            {
                for (int l = 0; l < n; l++)
                {
                    shortestDist[l] = double.MaxValue;
                    for (int p = 0; p < this._centroids.Count; p++)
                    {
                        double dTmp = GetEuclideanDistance(pts[l], this._centroids[p].Pt, false);
                        shortestDist[l] = Math.Min(shortestDist[l], dTmp);
                    }
                }

                double sum = 0.0;
                for (int l = 0; l < n; l++)
                {
                    sum += shortestDist[l];
                    cumulativeDist[l] = sum;
                }

                ////random = (float)rand() / (float)RAND_MAX * sum;
                //double random = (double)this._rnd.Next(Int32.MaxValue) / (double)Int32.MaxValue * sum;
                //int selectedIndex = BisectionSearch(cumulativeDist, n, random);
                //this._centroids.Add(new Centroid(pts[selectedIndex]));

                //better results
                Centroid nextCentroid = new Centroid(pts[ArgMax(shortestDist.ToList())]);
                this._centroids.Add(nextCentroid);
                shortestDist = new double[n];
            }

            this.Labels = new int[n];

            //for (int i = 0; i < n; i++)
            Parallel.For(0, n, i =>
            {
                this.Labels[i] = Nearest(pts[i], this._centroids, amntClusters);
            });

            InitMeans2(X);
        }

        /*----------------------------------------------------------------------
          bisectionSearch

          This function makes a bisectional search of an array of values that are
          ordered in increasing order, and returns the index of the first element
          greater than the search value passed as a parameter.

          This code is adapted from code by Andy Allinger given to the public
          domain, which was in turn adapted from public domain code for spline 
          evaluation by Rondall Jones (Sandia National Laboratories).

          Input:
                x	A pointer to an array of values in increasing order to be searched.
                n	The number of elements in the input array x.
                v	The search value.
          Output:
                Returns the index of the first element greater than the search value, v.
        ----------------------------------------------------------------------*/
        private int BisectionSearch(double[] x, int n, double v)
        {
            int il, ir, i;

            if (n < 1)
            {
                return 0;
            }
            /* If v is less than x(0) or greater than x(n-1)  */
            if (v < x[0])
            {
                return 0;
            }
            else if (v > x[n - 1])
            {
                return n - 1;
            }

            /*bisection search */
            il = 0;
            ir = n - 1;

            i = (il + ir) / 2;
            while (i != il)
            {
                if (x[i] <= v)
                {
                    il = i;
                }
                else
                {
                    ir = i;
                }
                i = (il + ir) / 2;
            }

            if (x[i] <= v)
                i = ir;
            return i;
        }

        private void InitMeans2(double[][] X)
        {
            if (this._means == null)
                this._means = new double[this._amntClusters][];

            Dictionary<int, int> d = new Dictionary<int, int>();
            for (int i = 0; i < this._means.Length; i++)
            {
                int count = this.Labels.Where(x => x == i).Count();
                d.Add(i, count);
            }

            Parallel.For(0, this._means.Length, i =>
            //for (int i = 0; i < this._means.Length; i++)
            {
                int n = d[i];
                if (n > 0)
                {
                    double avgB = 0;
                    double avgG = 0;
                    double avgR = 0;

                    List<double> B = new List<double>();
                    List<double> G = new List<double>();
                    List<double> R = new List<double>();

                    for (int j = 0; j < this.Labels.Length; j++)
                    {
                        if (this.Labels[j] == i)
                        {
                            B.Add(X[j][0]);
                            G.Add(X[j][1]);
                            R.Add(X[j][2]);
                        }
                    }

                    if (n > 0)
                    {
                        avgB = B.Average();
                        avgG = G.Average();
                        avgR = R.Average();
                    }

                    this._means[i] = new double[] { avgB, avgG, avgR };
                }
                else
                    this._means[i] = new double[] { 0, 0, 0 };
            });
        }

        private int Nearest(Point pt, List<Centroid> cent, int n_cluster)
        {
            int i, clusterIndex;
            double d, min_d;

            min_d = double.MaxValue;
            clusterIndex = -1;
            for (i = 0; i < n_cluster; i++)
            {
                d = GetEuclideanDistance(cent[i].Pt, pt, false);
                if (d < min_d)
                {
                    min_d = d;
                    clusterIndex = i;
                }
            }
            return clusterIndex;
        }

        private int ArgMax(List<double> z)
        {
            return z.IndexOf(z.Max());
        }

        private double GetEuclideanDistance(Point pt, Point refPt, bool squared)
        {
            if (squared)
                return (refPt.X - pt.X) * (refPt.X - pt.X) + (refPt.Y - pt.Y) * (refPt.Y - pt.Y);
            else
                return Math.Sqrt((refPt.X - pt.X) * (refPt.X - pt.X) + (refPt.Y - pt.Y) * (refPt.Y - pt.Y));
        }

        #endregion

        //adapted from: https://rosettacode.org/wiki/K-means%2B%2B_clustering the C code
        private void Lloyd(Point[] pts, int maxIters)
        {
            int i, clusterIndex;
            int changes;
            int acceptable = 1;    /* The maximum point changes acceptable. */

            if (this._amntClusters == 1 || pts.Length <= 0 || this._amntClusters > pts.Length)
                return;

            if (maxIters < 1)
                maxIters = 1;

            do
            {
                /* Calculate the centroid of each cluster.
                  ----------------------------------------*/

                /* Initialize the x, y and cluster totals. */
                for (i = 0; i < this._amntClusters; i++)
                {
                    this._centroids[i].Group = 0;     /* used to count the cluster members. */
                    this._centroids[i].X = 0;         /* used for x value totals. */
                    this._centroids[i].Y = 0;         /* used for y value totals. */
                }

                /* Add each observation's x and y to its cluster total. */
                for (i = 0; i < pts.Length; i++)
                {
                    clusterIndex = this.Labels[i];
                    this._centroids[clusterIndex].Group++;
                    this._centroids[clusterIndex].X += pts[i].X;
                    this._centroids[clusterIndex].Y += pts[i].Y;
                }

                /* Divide each cluster's x and y totals by its number of data points. */
                for (i = 0; i < this._amntClusters; i++)
                {
                    this._centroids[i].X /= this._centroids[i].Group;
                    this._centroids[i].Y /= this._centroids[i].Group;

                    this._centroids[i].Pt = new Point((int)this._centroids[i].X, (int)this._centroids[i].Y);
                }

                /* Find each data point's nearest centroid */
                changes = 0;
                for (i = 0; i < pts.Length; i++)
                {
                    clusterIndex = Nearest(pts[i], this._centroids, this._amntClusters);
                    if (clusterIndex != this.Labels[i])
                    {
                        this.Labels[i] = clusterIndex;
                        changes++;
                    }
                }

                InitMeans2(this._data);

                maxIters--;
            } while ((changes > acceptable) && (maxIters > 0));

            for (i = 0; i < this._amntClusters; i++)
                this._centroids[i].Group = i;
        }

        public void AssignSamples()
        {
            int l = this._data.Length;

            this.Labels = new int[l];

            //for (int i = 0; i < l; i++)
            Parallel.For(0, l, i =>
            {
                double minDist = double.MaxValue;
                double[] d = this._data[i];

                for (int j = 0; j < this._amntClusters; j++)
                {
                    double dist = GetDistance(d, this._means[j]);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        this.Labels[i] = j;
                    }
                }
            });
        }

        public void AssignSamples2(Point[] pts, List<Centroid> centroids, double gamma, double rel)
        {
            int l = this._data.Length;

            this.Labels = new int[l];

            //for (int i = 0; i < l; i++)
            Parallel.For(0, l, i =>
            {
                double[] d = this._data[i];

                List<double> d2 = new List<double>();
                List<double> d4 = new List<double>();
                double sum = 0;

                for (int j = 0; j < this._amntClusters; j++)
                {
                    double dist = GetDistance(d, this._means[j]);
                    d2.Add(dist);
                    double dist2 = GetEuclideanDistance(pts[i], centroids[j].Pt, true);
                    d4.Add(dist2);
                    sum += dist2;
                }

                //bool divide = false;
                //List<double> tmp = new List<double>();
                //double f1 = d4.Min();
                //tmp.Add(f1);
                //double f2 = d4.Except(tmp).Min();
                //double diff = Math.Abs(f2 - f1);
                //if (diff < rel)
                //    divide = true;

                ////evtl nur Teilen, wenn die Differenz der 2 kürzesten Strecken unter einem Schwellenwert liegen (pts.Length / iregendwas)
                //if (divide)
                for (int j = 0; j < this._amntClusters; j++)
                {
                    d4[j] /= sum;
                    d4[j] = Math.Pow(d4[j], gamma);
                }

                for (int j = 0; j < this._amntClusters; j++)
                    d2[j] *= d4[j];

                int clusterIndex = d2.IndexOf(d2.Min());
                this.Labels[i] = clusterIndex;
            });
        }

        private double GetDistance(double[] d, double[] means)
        {
            double dist = Math.Sqrt((d[0] - means[0]) * (d[0] - means[0]) +
                (d[1] - means[1]) * (d[1] - means[1]) +
                (d[2] - means[2]) * (d[2] - means[2]));

            return dist;
        }

        public void UpdateMeans()
        {
            int l2 = this.Labels.Length;

            Parallel.For(0, this._amntClusters, i =>
            //for (int i = 0; i < this._amntClusters; i++)
            {
                int n = this.Labels.Where(x => x == i).Count();

                double avgBOrL = 0;
                double avgGOrA = 0;
                double avgROrB = 0;

                List<double> BOrL = new List<double>();
                List<double> GOrA = new List<double>();
                List<double> ROrB = new List<double>();

                for (int j = 0; j < l2; j++)
                {
                    if (this.Labels[j] == i)
                    {
                        BOrL.Add(this._data[j][0]);
                        GOrA.Add(this._data[j][1]);
                        ROrB.Add(this._data[j][2]);
                    }
                }

                if (n > 0)
                {
                    avgBOrL = BOrL.Average();
                    avgGOrA = GOrA.Average();
                    avgROrB = ROrB.Average();

                    this._means[i] = new double[] { avgBOrL, avgGOrA, avgROrB };
                }
            });
        }

        private void InitMeans()
        {
            int ly = this._data.Length;
            int lx = this._data[0].Length;

            int dist = ly / this._amntClusters;
            this._means = new double[this._amntClusters][];

            this._means[0] = this._data[this._data.Length / 2];

            int rows = 2;
            int cols = Math.Max((this._amntClusters - 1) / rows, 1);
            int offSet = this._data.Length / rows / cols / 2;

            this._rnd = new Random();
            int w = this._data.Length / rows;

            for (int i = 1, c = 0, r = 0; i < this._means.Length; i++, c++)
            {
                if (c >= rows)
                {
                    r++;
                    c = 0;
                }

                int xy = w / cols * c + offSet / 2 + r * w + _rnd.Next(offSet);

                if (xy >= this._data.Length)
                    xy = this._data.Length - 1;

                this._means[i] = new double[lx];

                for (int l = 0; l < lx; l++)
                    this._means[i][l] = this._data[xy][l];
            }
        }

        private double[][] CopyData(double[][] x)
        {
            int l = x.Length;
            int l2 = x[0].Length;
            double[][] data = new double[l][];

            Parallel.For(0, l, i =>
            {
                double[] tmp = new double[l2];

                for (int j = 0; j < l2; j++)
                    tmp[j] = x[i][j];

                data[i] = tmp;
            });

            return data;
        }

        private double[] GetData(RGBLab.LAB lAB)
        {
            return new double[3] { lAB.L, lAB.A, lAB.B };
        }

        private void ConvertMeans()
        {
            for (int i = 0; i < this._means.Length; i++)
                this._means[i] = GetData(RGBLab.RGB2LAB(this._means[i]));
        }

        private void ConvertData()
        {
            for (int i = 0; i < this._data.Length; i++)
                this._data[i] = GetData(RGBLab.RGB2LAB(this._data[i]));
        }

        private bool CheckMeans(double[][] means)
        {
            for (int j = 0; j < means.Length; j++)
            {
                double[] d = means[j];

                for (int i = 0; i < d.Length; i++)
                    if (double.IsNaN(d[i]) || double.IsInfinity(d[i]))
                        return false;
            }

            return true;
        }
    }
}
