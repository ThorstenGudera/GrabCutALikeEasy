using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace GetAlphaMatte
{
    internal class Node
    {
        public List<double[]> X { get; }
        public List<double> Weights { get; }
        public double W { get; private set; }
        public double[] Mu { get; private set; }
        public double[][] Covariances { get; private set; }
        public double Lambda { get; private set; }
        public double[] E { get; private set; }

        public Node(List<double[]> pixels, List<double> weights)
        {
            this.X = pixels;
            this.Weights = weights;
            this.W = weights.Sum();

            int l = pixels.Count;

            double[] mu0 = Einsum(X.ToArray(), weights.ToArray());
            this.Mu = mu0.Select(a => a / W).ToArray();

            double[][] diff = new double[l][];
            //todo change to serially loop when parallelizing the whole computation
            //Parallel.For(0, l, y =>
            //{
            //    diff[y] = new double[] { X[y][0] - this.Mu[0], X[y][1] - this.Mu[1], X[y][2] - this.Mu[2] };
            //});

            for(int y = 0; y < l; y++)
                diff[y] = new double[] { X[y][0] - this.Mu[0], X[y][1] - this.Mu[1], X[y][2] - this.Mu[2] };

            double[][] t = Einsum2(diff, this.Weights.Select(a => Math.Sqrt(a)).ToArray());

            this.Covariances = new double[3][];
            ComputeCovariances(t);

            double[] v = new double[3];
            double[][] d = new double[3][];
            for(int i = 0; i < 3; i++)
                d[i] = new double[3];

            //the transpose needed, since we compute [][]s in order [x][y] (= [cols][rows]) and the LinAlg people do [y][x]
            double[][] cov = MatrixTranspose(this.Covariances);

            //MatrixEigen.Eigen(this.Covariances, out v, out d);
            MatrixEigen.Eigen(cov, out v, out d);

            //for (int i = 0; i < 3; i++)
            //    for (int j = 0; j < 3; j++)
            //        Console.WriteLine(d[i][j]);

            double[] vA = v.Select(a => Math.Abs(a)).ToArray();

            this.Lambda = vA.Max();
            this.E = d[ArgMax(vA)];
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

        private int ArgMax(double[] d)
        {
            int l = d.Length;

            List<double> z = d.ToList();
            return z.IndexOf(z.Max(a => Math.Abs(a)));
        }

        private void ComputeCovariances(double[][] t)
        {
            for (int i = 0; i < t[0].Length; i++)
            {
                this.Covariances[i] = new double[t[0].Length];
                for (int j = 0; j < t[0].Length; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < t.Length; k++)
                    {
                        sum += t[k][i] * t[k][j];
                    }

                    this.Covariances[i][j] = sum / W + 1e-5 * (i == j ? 1 : 0);
                }
            }
        }

        private double[] Einsum(double[][] a, double[] b)
        {
            int c = a.Length;
            double[] result = new double[a[0].Length];

            for (int j = 0; j < a[0].Length; j++)
            {
                double sum = 0;
                for (int i = 0; i < c; i++)
                {
                    sum += a[i][j] * b[i];
                }

                result[j] = sum;
            }

            return result;
        }

        private double[][] Einsum2(double[][] a, double[] b)
        {
            int c = a.Length;
            double[][] result = new double[c][];

            for (int i = 0; i < c; i++)
            {
                result[i] = new double[3];
                for (int j = 0; j < 3; j++)
                {
                    result[i][j] = a[i][j] * b[i];
                }
            }

            return result;
        }
    }
}