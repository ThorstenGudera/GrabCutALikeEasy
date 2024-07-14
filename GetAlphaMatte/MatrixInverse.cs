using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetAlphaMatte
{
    // https://social.msdn.microsoft.com/forums/en-us/45eb46cd-02af-42ad-aaf2-95ce42ecfdd7/test-run-matrix-inversion-using-c?forum=msdnmagazine#95334279-a9d2-4197-b9b2-74fe94f806e9
    // code of theses methods is from
    // https://msdn.microsoft.com/en-us/magazine/mt736457.aspx
    // see also:
    // https://en.wikipedia.org/wiki/Crout_matrix_decomposition
    // and especially
    // https://en.wikipedia.org/wiki/LU_decomposition
    using System;

    public class MatrixInverse
    {
        public static double[][] MatrixInv(double[][] matrix)
        {
            // assumes determinant is not 0
            // that is, the matrix does have an inverse
            int n = matrix.Length;
            double[][] result = MatrixCreate(n, n); // make a copy of matrix
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                    result[i][j] = matrix[i][j];
            }

            double[][] lum = null; // combined lower & upper
            int[] perm = null;
            int toggle;
            toggle = MatrixDecompose(matrix, out lum, out perm);

            double[] b = new double[n];
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }

                double[] x = Helper(lum, b);
                for (var j = 0; j < n; j++)
                    result[j][i] = x[j];
            }
            return result;
        } // MatrixInverse

        private static int MatrixDecompose(double[][] m, out double[][] lum, out int[] perm)
        {
            // Crout's LU decomposition for matrix determinant and inverse
            // stores combined lower & upper in lum][
            // stores row permuations into perm[]
            // returns +1 or -1 according to even or odd number of row permutations
            // lower gets dummy 1.0s on diagonal (0.0s above)
            // upper gets lum values on diagonal (0.0s below)

            int toggle = +1; // even (+1) or odd (-1) row permutatuions
            int n = m.Length;

            // make a copy of m][ into result lu][
            lum = MatrixCreate(n, n);
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                    lum[i][j] = m[i][j];
            }

            // make perm[]
            perm = new int[n - 1 + 1];
            for (var i = 0; i < n; i++)
                perm[i] = i;

            for (var j = 0; j <= n - 2; j++) // process by column. note n-1
            {
                double max = Math.Abs(lum[j][j]);
                int piv = j;

                for (var i = j + 1; i < n; i++) // find pivot index
                {
                    double xij = Math.Abs(lum[i][j]);
                    if (xij > max)
                    {
                        max = xij;
                        piv = i;
                    }
                } // i

                if (piv != j)
                {
                    double[] tmp = lum[piv]; // swap rows j, piv
                    lum[piv] = lum[j];
                    lum[j] = tmp;

                    int t = perm[piv]; // swap perm elements
                    perm[piv] = perm[j];
                    perm[j] = t;

                    toggle = -toggle;
                }

                double xjj = lum[j][j];
                if (xjj != 0.0)
                {
                    for (var i = j + 1; i < n; i++)
                    {
                        double xij = lum[i][j] / xjj;
                        lum[i][j] = xij;
                        for (var k = j + 1; k < n; k++)
                            lum[i][k] -= xij * lum[j][k];
                    }
                }
            } // j

            return toggle;
        } // MatrixDecompose

        private static double[] Helper(double[][] luMatrix, double[] b) // helper
        {
            int n = luMatrix.Length;
            double[] x = new double[n - 1 + 1];
            b.CopyTo(x, 0);

            for (var i = 1; i < n; i++)
            {
                double sum = x[i];
                for (var j = 0; j <= i - 1; j++)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1][n - 1];
            for (var i = n - 2; i >= 0; i += -1)
            {
                double sum = x[i];
                for (var j = i + 1; j < n; j++)
                    sum -= luMatrix[i][j] * x[j];
                x[i] = sum / luMatrix[i][i];
            }

            return x;
        } // Helper

        // fixed "-1" in array declarations were missing
        private static double[][] MatrixCreate(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (var i = 0; i < rows; i++)
                result[i] = new double[cols];
            return result;
        }

        public static double MatrixDeterminant(double[][] matrix)
        {
            double[][] lum = null;
            int[] perm = null;
            int toggle = MatrixDecompose(matrix, out lum, out perm);
            double result = toggle;
            for (var i = 0; i <= lum.Length - 1; i++)
                result *= lum[i][i];
            return result;
        }

        // just for testing
        public static double[][] MatrixProduct(double[][] matrixA, double[][] matrixB, bool round)
        {
            int aRows = matrixA.Length;
            int aCols = matrixA[0].Length;
            int bRows = matrixB.Length;
            int bCols = matrixB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");

            double[][] result = MatrixCreate(aRows, bCols);

            for (var i = 0; i <= aRows - 1; i++) // each row of A
            {
                for (var j = 0; j <= bCols - 1; j++) // each col of B
                {
                    for (var k = 0; k <= aCols - 1; k++) // could use k < bRows
                        result[i][j] += matrixA[i][k] * matrixB[k][j];
                }
            }

            if (round)
            {
                for (int i = 0; i <= result.Length - 1; i++)
                {
                    for (int j = 0; j <= result[i].Length - 1; j++)
                        result[i][j] = Math.Round(result[i][j], 4);
                }
            }

            return result;
        }
    }

}
