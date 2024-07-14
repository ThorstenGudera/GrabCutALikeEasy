using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetAlphaMatte
{
    //code copied from: https://jamesmccaffrey.wordpress.com/2023/10/13/matrix-eigenvectors-from-scratch-using-csharp/
    //see also wiki pages for Matrix decomposition
    public class MatrixEigen
    {
        public static void Eigen(double[][] M, out double[] eigenVals, out double[][] eigenVecs)
        {
            // compute eigenvalues and eigenvectors at the same time
            int n = M.Length;
            double[][] X = MatCopy(M);  // mat must be square
            double[][] Q; double[][] R;
            double[][] pq = MatIdentity(n);
            int maxCt = 10000;

            int ct = 0;
            while (ct < maxCt)
            {
                MatDecomposeQR(X, out Q, out R, false);
                pq = MatProduct(pq, Q);
                X = MatProduct(R, Q);  // note order
                ++ct;

                if (MatIsUpperTri(X, 1.0e-15) == true) // precision changed from e-8 to e-15
                    break;
            }

            // eigenvalues are diag elements of X
            double[] evals = new double[n];
            for (int i = 0; i < n; ++i)
                evals[i] = X[i][i];

            // eigenvectors are columns of pq
            double[][] evecs = MatCopy(pq);

            eigenVals = evals;
            eigenVecs = evecs;
        }

        public static double[] Eigenvalues(double[][] A)
        {
            // just eigenvalues only
            int n = A.Length;
            double[][] X = MatCopy(A);  // mat must be square
            double[][] Q; double[][] R;
            int maxCt = 10000;

            int ct = 0;
            while (ct < maxCt)
            {
                MatDecomposeQR(X, out Q, out R, false);
                X = MatProduct(R, Q);  // note order
                ++ct;

                if (MatIsUpperTri(X, 1.0e-8) == true)
                    break;
            }

            //if (ct >= maxCt)
            //    Console.WriteLine("Warn: Eigenvalues convergence ");

            double[] result = new double[n];
            for (int j = 0; j < n; ++j)
                result[j] = X[j][j];
            return result;
        }

        // ------------------------------------------------------

        public static double[] MatEigenVecFromEigenVal(
          double[][] A, double eigenVal)
        {
            // I suspect this funcion has bugs -- do not use!!
            // A is the source matrix that produced eigenvalue
            double tol = 1.0e-6; // stopping criteria
            int maxIter = 1000;
            double noise = 1.0e-8; // avoid inverse failure
            int n = A.Length;
            double[] curr = new double[n];
            for (int i = 0; i < n; ++i)
                curr[i] = 1.0;
            double[] prev;

            double[][] AminusEigenvec = MatCopy(A);
            for (int i = 0; i < n; ++i)
                AminusEigenvec[i][i] -= eigenVal + noise;

            int iter = 0;
            while (iter < maxIter)
            {
                prev = curr;
                curr = MatLinSolveQR(AminusEigenvec, prev);

                // normalize curr sign and length
                if (curr[0] < 0.0)  // OK but not necessary
                    curr = VecNegation(curr);

                double norm = VecNorm(curr);
                for (int j = 0; j < n; ++j)
                    curr[j] /= norm;

                ++iter;
                if (VecEqual(prev, curr, tol) == true)
                    break;

                double[] negatedCurr = VecNegation(curr);
                if (VecEqual(prev, negatedCurr, tol) == true)
                    break;
            }

            //if (iter >= maxIter)
            //    Console.WriteLine("Warn: Eigenvec max iterations ");

            return curr;
        }

        // ------------------------------------------------------

        public static double[] MatLinSolveQR(double[][] A, double[] b)
        {
            // solve general system of equations using QR
            // A * x = b
            // Q*R * x = b
            // inv(Q*R) * Q*R * x = inv(Q*R) * b
            // x = inv(R) * inv(Q) * b
            // x = inv(R) * tr(Q) * b where R is upper tri
            double[][] Q; double[][] R;
            MatDecomposeQR(A, out Q, out R, false);
            double[][] Ri = MatInverseUpperTri(R);
            double[][] Qt = MatTranspose(Q);
            double[][] B = VecToMat(b, b.Length, 1);
            double[][] RiQt = MatProduct(Ri, Qt);
            double[][] RiQtB = MatProduct(RiQt, B);
            return MatToVec(RiQtB);
        }

        // ------------------------------------------------------

        public static double[][] MatInverseUpperTri(double[][] upper)
        {
            int n = upper.Length;  // must be square matrix
            double[][] result = MatIdentity(n);

            for (int k = 0; k < n; ++k)
            {
                for (int j = 0; j < n; ++j)
                {
                    for (int i = 0; i < k; ++i)
                    {
                        result[j][k] -= result[j][i] * upper[i][k];
                    }
                    result[j][k] /= upper[k][k];
                }
            }
            return result;
        }

        // ------------------------------------------------------

        static double[][] MatTranspose(double[][] m)
        {
            int nr = m.Length;
            int nc = m[0].Length;
            double[][] result = MatCreate(nc, nr);  // note
            for (int i = 0; i < nr; ++i)
                for (int j = 0; j < nc; ++j)
                    result[j][i] = m[i][j];
            return result;
        }

        // ------------------------------------------------------

        static double[] MatToVec(double[][] m)
        {
            int rows = m.Length;
            int cols = m[0].Length;
            double[] result = new double[rows * cols];
            int k = 0;
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                {
                    result[k++] = m[i][j];
                }
            return result;
        }

        // ------------------------------------------------------

        public static void MatDecomposeQR(double[][] mat,
          out double[][] q, out double[][] r,
          bool standardize)
        {
            // QR decomposition, Householder algorithm.
            // assumes square matrix

            int n = mat.Length;  // assumes mat is nxn
            int nCols = mat[0].Length;
            //if (n != nCols) Console.WriteLine("M not square ");

            double[][] Q = MatIdentity(n);
            double[][] R = MatCopy(mat);
            for (int i = 0; i < n - 1; ++i)
            {
                double[][] H = MatIdentity(n);
                double[] a = new double[n - i];
                int k = 0;
                for (int ii = i; ii < n; ++ii)  // last part col [i]
                    a[k++] = R[ii][i];

                double normA = VecNorm(a);
                if (a[0] < 0.0) { normA = -normA; }
                double[] v = new double[a.Length];
                for (int j = 0; j < v.Length; ++j)
                    v[j] = a[j] / (a[0] + normA);
                v[0] = 1.0;

                double[][] h = MatIdentity(a.Length);
                double vvDot = VecDot(v, v);
                double[][] alpha = VecToMat(v, v.Length, 1);
                double[][] beta = VecToMat(v, 1, v.Length);
                double[][] aMultB = MatProduct(alpha, beta);

                for (int ii = 0; ii < h.Length; ++ii)
                    for (int jj = 0; jj < h[0].Length; ++jj)
                        h[ii][jj] -= (2.0 / vvDot) * aMultB[ii][jj];

                // copy h into lower right of H
                int d = n - h.Length;
                for (int ii = 0; ii < h.Length; ++ii)
                    for (int jj = 0; jj < h[0].Length; ++jj)
                        H[ii + d][jj + d] = h[ii][jj];

                Q = MatProduct(Q, H);
                R = MatProduct(H, R);
            } // i

            if (standardize == true)
            {
                // standardize so R diagonal is all positive
                double[][] D = MatCreate(n, n);
                for (int i = 0; i < n; ++i)
                {
                    if (R[i][i] < 0.0) D[i][i] = -1.0;
                    else D[i][i] = 1.0;
                }
                Q = MatProduct(Q, D);
                R = MatProduct(D, R);
            }

            q = Q;
            r = R;

        } // QR decomposition

        // ------------------------------------------------------

        public static double[][] MatCreate(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        // ------------------------------------------------------

        public static double[][] MatIdentity(int n)
        {
            double[][] result = MatCreate(n, n);
            for (int i = 0; i < n; ++i)
                result[i][i] = 1.0;
            return result;
        }

        // ------------------------------------------------------

        public static double[][] MatCopy(double[][] m)
        {
            int nRows = m.Length; int nCols = m[0].Length;
            double[][] result = MatCreate(nRows, nCols);
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    result[i][j] = m[i][j];
            return result;
        }

        // ------------------------------------------------------

        public static double[][] MatProduct(double[][] matA,
          double[][] matB)
        {
            int aRows = matA.Length;
            int aCols = matA[0].Length;
            int bRows = matB.Length;
            int bCols = matB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");

            double[][] result = MatCreate(aRows, bCols);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k)
                        result[i][j] += matA[i][k] * matB[k][j];

            return result;
        }

        // ------------------------------------------------------

        public static bool MatIsUpperTri(double[][] mat,
          double tol)
        {
            int n = mat.Length;
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < i; ++j)
                {  // check lower vals
                    if (Math.Abs(mat[i][j]) > tol)
                    {
                        return false;
                    }
                }
            }
            return true;

        }

        // ------------------------------------------------------

        public static void MatShow(double[][] m,
          int dec, int wid)
        {
            for (int i = 0; i < m.Length; ++i)
            {
                for (int j = 0; j < m[0].Length; ++j)
                {
                    double v = m[i][j];
                    if (Math.Abs(v) < 1.0e-8) v = 0.0;  // hack
                    Console.Write(v.ToString("F" + dec).
                      PadLeft(wid));
                }
                //Console.WriteLine("");
            }
        }

        // ------------------------------------------------------

        public static double VecDot(double[] v1, double[] v2)
        {
            double result = 0.0;
            int n = v1.Length;
            for (int i = 0; i < n; ++i)
                result += v1[i] * v2[i];
            return result;
        }

        // ------------------------------------------------------

        public static double VecNorm(double[] vec)
        {
            int n = vec.Length;
            double sum = 0.0;
            for (int i = 0; i < n; ++i)
                sum += vec[i] * vec[i];
            return Math.Sqrt(sum);
        }

        // ------------------------------------------------------

        public static bool VecEqual(double[] v1, double[] v2,
          double tol)
        {
            for (int i = 0; i < v1.Length; ++i)
                if (Math.Abs(v1[i] - v2[i]) > tol)
                    return false;
            return true;
        }

        // ------------------------------------------------------

        public static double[] VecNegation(double[] vec)
        {
            int n = vec.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; ++i)
                result[i] = -vec[i];
            return result;
        }

        // ------------------------------------------------------

        public static double[][] VecToMat(double[] vec,
          int nRows, int nCols)
        {
            double[][] result = MatCreate(nRows, nCols);
            int k = 0;
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    result[i][j] = vec[k++];
            return result;
        }

        // ------------------------------------------------------

        public static void VecShow(double[] vec, int dec, int wid)
        {
            for (int i = 0; i < vec.Length; ++i)
            {
                double x = vec[i];
                if (Math.Abs(x) < 1.0e-8) x = 0.0;
                Console.Write(x.ToString("F" + dec).
                  PadLeft(wid));
            }
            //Console.WriteLine("");
        }
    }
}
