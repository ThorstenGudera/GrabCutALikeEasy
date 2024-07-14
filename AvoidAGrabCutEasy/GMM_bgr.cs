using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AvoidAGrabCutEasy
{
    //translation from python:
    //https://github.com/MoetaYuko/GrabCut
    public class GMM_bgr
    {
        private int _nComp;
        private int _nFeatures;
        private int[] _nSamples;
        internal double[] _coefs;
        internal double[][] _means;
        internal double[][][] _covariances;

        public int KMeansIterations { get; internal set; }
        public bool UseKpp { get; internal set; }
        public int[] Labels { get; private set; }
        public ListSelectionMode SelectionMode { get; private set; }

        private BackgroundWorker _bgw;

        public GMM_bgr(double[][] X, Point[] pts, int nComp, int nFeatures, int kMeansIterations, bool useKpp, ListSelectionMode selMode,
            int kmInitIters, int w, int h, double kmInitW, double kmInitH, bool kmInitRnd, bool backgroundGmm, double gamma,
            bool labMode, BackgroundWorker bgw)
        {
            this._nComp = nComp;
            this._nFeatures = nFeatures;
            this._nSamples = new int[nComp];

            this._coefs = new double[nComp];
            this._means = new double[nComp][];
            this._covariances = new double[nComp][][];
            this.KMeansIterations = kMeansIterations;
            this.UseKpp = useKpp;
            this.SelectionMode = selMode;

            if (bgw != null)
                this._bgw = bgw;

            InitWKMeans(X, pts, kmInitIters, w, h, kmInitW, kmInitH, kmInitRnd, gamma, labMode, backgroundGmm);
        }

        //make sure, the kmeans labels are found with some "precision", ie, for the same pic,
        //make sure, we get almost the same labels for each new iteration
        public void InitWKMeans(double[][] X, Point[] pts, int kmInitIters, int w, int h, double kmInitW, double kmInitH, 
            bool kmInitRnd, double gamma, bool labMode, bool backgroundGmm)
        {
            KMeans_bgr km = new KMeans_bgr(X, pts, this._nComp, this.UseKpp, this.SelectionMode, kmInitIters, false, w, h, 
                kmInitW, kmInitH, kmInitRnd, gamma, labMode, backgroundGmm);

            if (!CheckMeans(km.Means)) //shouldnt return false
                km = new KMeans_bgr(X, pts, this._nComp, false, 0, kmInitIters, false, w, h, kmInitW, kmInitH, kmInitRnd, gamma, labMode, backgroundGmm);

            if (this.KMeansIterations > 0)
            {
                int r = 100 / this.KMeansIterations;

                for (int i = 0; i < this.KMeansIterations; i++)
                {
                    km.AssignSamples();
                    km.UpdateMeans();

                    if (this._bgw != null && this._bgw.WorkerReportsProgress)
                        this._bgw.ReportProgress(10 + i * r);
                }
            }

            int[] labels = km.Labels;
            //Console.WriteLine("knLabls: " + labels.Where(a => a > 0).Count().ToString());

            this.Labels = km.Labels;

            if (this._bgw != null && this._bgw.WorkerReportsProgress)
                this._bgw.ReportProgress(100);

            Fit(X, labels);
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

        private double[] CalcScore(double[][] X, int ci)
        {
            double[] score = new double[X.Length];
            int l = X.Length;

            if (this._coefs[ci] > 0)
            {
                double[][] diff = new double[l][];
                Parallel.For(0, l, y =>
                {
                    diff[y] = new double[] { X[y][0] - this._means[ci][0], X[y][1] - this._means[ci][1], X[y][2] - this._means[ci][2] };
                });

                double[] db = diff.Select(x => x[0]).ToArray();
                double[] dg = diff.Select(x => x[1]).ToArray();
                double[] dr = diff.Select(x => x[2]).ToArray();

                double[][] invC = LinalgInv(this._covariances[ci]); //cov!

                double[] dotB = LinalgDot(invC[0], db, dg, dr);
                double[] dotG = LinalgDot(invC[1], db, dg, dr);
                double[] dotR = LinalgDot(invC[2], db, dg, dr);

                double[][] dotP = new double[l][];

                Parallel.For(0, l, i =>
                {
                    dotP[i] = new double[] { dotB[i], dotG[i], dotR[i] };
                });

                double[] mult = Einsum(diff, dotP);  //1dim lang

                double detC = LinalgDet(this._covariances[ci]);
                double detD = Math.Sqrt(detC);
                double ll = Math.Sqrt(Math.PI * 2.0);

                Parallel.For(0, l, i =>
                {
                    score[i] = Math.Exp(-0.5 * mult[i]) / ll / Math.Max(detD, 0.0000000001);
                });
            }

            return score; //ok
        }

        public double[] CalcProb(double[][] X)
        {
            double[][] prob = new double[this._nComp][];

            for (int i = 0; i < this._nComp; i++)
                prob[i] = CalcScore(X, i);

            double[] result = LinalgDot(this._coefs, prob);

            return result;
        }

        public int[] WhichComponent(double[][] X)
        {
            double[][] prob = new double[this._nComp][];

            for (int i = 0; i < this._nComp; i++)
                prob[i] = CalcScore(X, i);

            double[][] probT = new double[prob[0].Length][];

            //for (int i = 0; i < prob[0].Length; i++)
            //    probT[i] = prob.Select(x => x[i]).ToArray();

            Parallel.For(0, prob[0].Length, i =>
            {
                probT[i] = prob.Select(x => x[i]).ToArray();
            });

            int[] result = null;

            if (probT != null && probT.Length > 0)
                result = ArgMax(probT);

            return result; //ok
        }

        public void Fit(double[][] X, int[] labels)
        {
            int l = this._nSamples.Length;
            int l2 = labels.Length;

            Parallel.For(0, l, i =>
            //for (int i = 0; i < l; i++)
            {
                this._nSamples[i] = 0;
                this._coefs[i] = 0;
            });

            Dictionary<int, int> d = new Dictionary<int, int>();
            for (int i = 0; i < l; i++)
            {
                int count = labels.Where(x => x == i).Count();
                d.Add(i, count);
                this._nSamples[i] = count;
            }

            int a = this._nSamples.Sum();

            Parallel.For(0, l, i =>
            //for (int i = 0; i < l; i++)
            {
                double variance = 0.01;

                int n = d[i];
                if (n > 0)
                {
                    this._coefs[i] = (double)n / (double)a;

                    double avgB = 0;
                    double avgG = 0;
                    double avgR = 0;

                    List<double> B = new List<double>();
                    List<double> G = new List<double>();
                    List<double> R = new List<double>();

                    for (int j = 0; j < l2; j++)
                    {
                        if (labels[j] == i)
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

                    if (this._nSamples[i] <= 1.0)
                    {
                        this._covariances[i] = new double[3][];
                        for (int j = 0; j < this._covariances[i].Length; j++)
                            this._covariances[i][j] = new double[] { 0, 0, 0 };
                    }
                    else
                        this._covariances[i] = ComputeCovariances(new double[][] { B.ToArray(), G.ToArray(), R.ToArray() });

                    double det = LinalgDet(this._covariances[i]);

                    if (det <= 0)
                    {
                        this._covariances[i] = new double[3][];
                        for (int j = 0; j < this._covariances[i].Length; j++)
                            this._covariances[i][j] = new double[] { 0, 0, 0 };

                        this._covariances[i][0][0] = variance;
                        this._covariances[i][1][1] = variance;
                        this._covariances[i][2][2] = variance;

                        det = LinalgDet(this._covariances[i]);
                    }
                }
                else
                {
                    this._means[i] = new double[] { 0, 0, 0 };
                    this._covariances[i] = new double[3][];
                    for (int j = 0; j < this._covariances[i].Length; j++)
                        this._covariances[i][j] = new double[] { 0, 0, 0 };
                }
            });

            //ok
        }

        private double[][] ComputeCovariances(double[][] bgr)
        {
            int n = bgr[0].Length;
            double[] means = new double[bgr.Length];
            double[][] covariance = new double[bgr.Length][];
            for (int i = 0; i < means.Length; i++)
            {
                means[i] = bgr[i].Average();
                covariance[i] = new double[bgr.Length];
            }

            for (int i = 0; i < means.Length; i++)
                for (int j = 0; j < means.Length; j++)
                {
                    double variance = 0;
                    for (int l = 0; l < n; l++)
                        variance += (bgr[i][l] - means[i]) * (bgr[j][l] - means[j]);
                    variance /= (n - 1);
                    covariance[i][j] = variance;
                }

            return covariance; //ok
        }

        private int[] ArgMax(double[][] probT)
        {
            int l = probT.Length;
            int l2 = probT[0].Length;
            int[] result = new int[l];

            for (int j = 0; j < l; j++)
            {
                List<double> z = probT[j].ToList();
                result[j] = z.IndexOf(z.Max());
            }

            return result;
        }

        private double[] LinalgDot(double[] r, double[][] lc)
        {
            int l = r.Length;
            int l2 = lc[0].Length;

            double[] result = new double[l2];

            Parallel.For(0, l2, i =>
            {
                for (int j = 0; j < l; j++)
                    result[i] += lc[j][i] * r[j];
            });

            //if (round)
            //    Parallel.For(0, result.Length, j =>
            //    {
            //        result[j] = Math.Round(result[j], 6);
            //    });

            return result;
        }

        private double[] LinalgDot(double[] r, double[] l0, double[] l1, double[] l2)
        {
            int l = l0.Length;
            double[] result = new double[l];

            Parallel.For(0, l, i =>
            {
                result[i] = r[0] * l0[i] + r[1] * l1[i] + r[2] * l2[i];
            });

            return result;
        }

        private double LinalgDet(double[][] values)
        {
            //test, if we need the transpose, see Node.cs in GetAlphaMatte
            //double[][] valsT = MatrixTranspose(values);
            //double d1 =  MatrixInverse.MatrixDeterminant(valsT);
            //double d2 = MatrixInverse.MatrixDeterminant(values);

            //if (d1 == d2)
            //    d1 = d1;

            return MatrixInverse.MatrixDeterminant(values);
        }

        //from https://visualstudiomagazine.com/Articles/2023/11/01/gaussian-mixture-model-data-clustering.aspx -> code download
        private double[][] MatrixTranspose(double[][] m)
        {
            // helper for LocalMatGaussianPdf()
            int nr = m.Length;
            int nc = m[0].Length;
            double[][] result = LocalMatCreate(nc, nr);  // note
            for (int i = 0; i < nr; ++i)
                for (int j = 0; j < nc; ++j)
                    result[j][i] = m[i][j];
            return result;
        }

        //from https://visualstudiomagazine.com/Articles/2023/11/01/gaussian-mixture-model-data-clustering.aspx -> code download
        private double[][] LocalMatCreate(int rows, int cols)
        {
            // used by many of the helper functions
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        private double[] Einsum(double[][] a, double[][] b)
        {
            //if (a.Length != b.Length)
            //    return null;

            int c = a.Length;
            double[] result = new double[c];
            double[][] tmp = new double[c][];

            Parallel.For(0, c, i =>
            {
                tmp[i] = new double[a[0].Length];
                for (int j = 0; j < tmp[i].Length; j++)
                    tmp[i][j] = a[i][j] * b[i][j];
                result[i] = tmp[i].Sum();
            });

            return result;
        }

        private double[][] LinalgInv(double[][] values)
        {
            //test, if we need the transpose, see Node.cs in GetAlphaMatte
            //double[][] valsT = MatrixTranspose(values);
            //double[][] d1 = MatrixTranspose(MatrixInverse.MatrixInv(valsT));
            //double[][] d2 = MatrixInverse.MatrixInv(values);

            //if (CompArr(d1,d2))
            //    d1 = d1;

            return MatrixInverse.MatrixInv(values);
        }

        private bool CompArr(double[][] d1, double[][] d2)
        {
            for (int y = 0; y < d1[0].Length; y++)
                for (int x = 0; x < d1.Length; x++)
                    if (d1[x][y] != d2[x][y])
                        return false;

            return true;
        }
    }
}
