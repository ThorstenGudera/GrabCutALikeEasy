using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetAlphaMatte
{
    public class OrchardBouman_bgr
    {
        public List<double[]> Mu { get; private set; }
        public List<List<double[]>> Sigma { get; private set; }
        internal List<Node> Nodes { get; private set; }

        public void ClusterPt(List<double[]> pixels, List<double> weights, double minVar)
        {
            this.Mu = new List<double[]>();
            this.Sigma = new List<List<double[]>>();
            this.Nodes = new List<Node>();
            this.Nodes.Add(new Node(pixels, weights));

            while (this.Nodes.Max(x => x.Lambda) > minVar)
            {
                try
                {
                    this.Nodes = Split(this.Nodes);
                }
                catch 
                {
                    break;
                }
            }

            for (int i = 0; i < this.Nodes.Count; i++)
            {
                this.Sigma.Add(new List<double[]>());

                this.Mu.Add(this.Nodes[i].Mu);
                for (int j = 0; j < this.Nodes[i].Covariances.Length; j++)
                {
                    this.Sigma[i].Add(new double[this.Nodes[i].Covariances.Length]);

                    for (int k = 0; k < this.Nodes[i].Covariances[0].Length; k++)
                        this.Sigma[i][j][k] = this.Nodes[i].Covariances[j][k];
                }
            }
        }

        private List<Node> Split(List<Node> nodes)
        {
            List<double> lmd = nodes.Select(a => a.Lambda).ToList();
            int idx_max = lmd.IndexOf(lmd.Max());

            Node ci = nodes[idx_max];

            double[] prd = MatrixVectorProduct(ci.X.ToArray(), ci.E);
            double dPrd = LinalgDot(ci.Mu, ci.E);

            BitArray bits = new BitArray(ci.X.Count, false);
            for (int i = 0; i < ci.X.Count; i++)
            {
                bits[i] = prd[i] <= dPrd;
                //Console.WriteLine(bits[i].ToString());
            }

            List<double[]> pxA = new List<double[]>();
            List<double> wghtsA = new List<double>();
            List<double[]> pxB = new List<double[]>();
            List<double> wghtsB = new List<double>();

            for (int i = 0; i < ci.X.Count; i++)
            {
                if (bits.Get(i))
                {
                    double[] d = new double[3];
                    for (int j = 0; j < 3; j++)
                        d[j] = ci.X[i][j];
                    pxA.Add(d);
                    wghtsA.Add(ci.Weights[i]);
                }
                else
                {
                    double[] d = new double[3];
                    for (int j = 0; j < 3; j++)
                        d[j] = ci.X[i][j];
                    pxB.Add(d);
                    wghtsB.Add(ci.Weights[i]);
                }
            }

            if (pxA.Count > 0 && pxB.Count > 0) //if not needed, when all functions are implemented correctly...
            {
                Node nA = new Node(pxA, wghtsA);
                Node nB = new Node(pxB, wghtsB);

                nodes.RemoveAt(idx_max);
                nodes.Add(nA);
                nodes.Add(nB);
            }
            else
                throw new Exception("Array contains no Elements.");

            return nodes;
        }

        private double LinalgDot(double[] r, double[] l)
        {
            double result = 0;

            for (int i = 0; i < l.Length; i++)
                result += r[i] * l[i];

            return result;
        }

        public static double[] MatrixVectorProduct(double[][] matrixA, double[] matrixB)
        {
            int aRows = matrixA.Length;
            int aCols = matrixA[0].Length;
            int bRows = matrixB.Length;

            double[] result = new double[aRows];

            for (int i = 0; i < aRows; i++)
            {
                for (int j = 0; j < bRows; j++)
                    result[i] += matrixA[i][j] * matrixB[j];
            }

            return result;
        }
    }
}
