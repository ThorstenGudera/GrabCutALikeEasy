using ChainCodeFinder;
using ColorCurves;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvoidAGrabCutEasy.ProcOutline
{
    public class BoundaryMattingOP : IDisposable
    {
        public BackgroundWorker BGW { get; internal set; }
        public Bitmap BmpWork { get; private set; }
        public Bitmap BmpOrig { get; private set; }

        public List<List<BoundaryObject>> BoundaryObjects { get; private set; }

        //public event EventHandler<string> ShowInfo;
        public event EventHandler<string> BoundaryError;

        private object _lockObject = new object();
        private int _normalDistToCheck;
        private int _widthInside;
        private int _widthOutside;
        private List<ChainCode> _boundaries;
        private float _measureDist = 2f;
        private double _alphaStartValue = 192.0;
        private BlendType _blendType;
        private double[] _kernelVector;
        private Bitmap _lastCroppedPic;

        public BoundaryMattingOP(Bitmap b4Copy, Bitmap bmpOrig)
        {
            BmpWork = new Bitmap(b4Copy);
            BmpOrig = new Bitmap(bmpOrig);
        }

        public BoundaryMattingOP()
        {
        }

        public void Init(int normalDistToCheck, int widthInside, int widthOutside, float measureColDistDist, double alphaStartValue, BlendType blendType)
        {
            this._normalDistToCheck = normalDistToCheck;
            this._widthInside = widthInside;
            this._widthOutside = widthOutside;
            this._measureDist = measureColDistDist;
            this._alphaStartValue = alphaStartValue;
            this._blendType = blendType;

            if (this.BmpWork != null)
            {
                List<ChainCode> l = GetBoundary(this.BmpWork);

                this._boundaries = l;

                if (this.BoundaryObjects == null)
                    this.BoundaryObjects = new List<List<BoundaryObject>>();

                this.BoundaryObjects.Clear();

                foreach (ChainCode c in l)
                {
                    List<Point> cc = c.Coord;
                    List<BoundaryObject> objs = new List<BoundaryObject>();
                    this.BoundaryObjects.Add(objs);

                    for (int j = 0; j < cc.Count; j++)
                    {
                        //location
                        BoundaryObject bo = new BoundaryObject();
                        bo.Location = cc[j];

                        //normal
                        PointF n = GetNormal(cc, j, this._normalDistToCheck);

                        double f = 1.0;
                        if (Math.Abs(n.X) > Math.Abs(n.Y))
                            f = 1.0 / Math.Abs(n.X);
                        else
                            f = 1.0 / Math.Abs(n.Y);
                        bo.NormalAmounts = new PointF((float)(n.X * f), (float)(n.Y * f));

                        //innerPixels
                        int realInnerWidth = 0;
                        bo.InnerPixels = new List<Point>();
                        for (int i = 0, ii = 1; i < this._widthInside; i++, ii++)
                        {
                            int pX = (int)(cc[j].X + 0.5 + (bo.NormalAmounts.X * ii)); //compute from the center of the pixel
                            int pY = (int)(cc[j].Y + 0.5 + (bo.NormalAmounts.Y * ii));

                            int indx = cc.IndexOf(new Point(pX, pY));

                            if (indx == -1)
                            {
                                realInnerWidth++;
                                bo.InnerPixels.Add(new Point(pX, pY));
                            }
                            else
                                break;
                        }

                        //outerPixels
                        int realOuterWidth = 0;
                        bo.OuterPixels = new List<Point>();

                        if (this._blendType != BlendType.Both)
                        {
                            //always skipped --> realOuterWidth = 0, remove
                            for (int i = 0, ii = 1; i < realOuterWidth; i++, ii++)
                            {
                                int pX = (int)(cc[j].X + 0.5 - (bo.NormalAmounts.X * ii)); //compute from the center of the pixel
                                int pY = (int)(cc[j].Y + 0.5 - (bo.NormalAmounts.Y * ii));

                                int indx = cc.IndexOf(new Point(pX, pY));

                                if (indx == -1)
                                {
                                    realOuterWidth++;
                                    bo.OuterPixels.Add(new Point(pX, pY));
                                }
                                else
                                    break;
                            }

                            //base alphas
                            int[] alphaBaseValues = new int[bo.OuterPixels.Count];

                            //for (int i = 0; i < alphaBaseValues.Length; i++)
                            //    alphaBaseValues[i] = GetAlphaBaseValue(i, alphaBaseValues.Length);

                            double[] kernel = GetVector(alphaBaseValues.Length * 2 + 1);

                            for (int i = 0; i < alphaBaseValues.Length; i++)
                                alphaBaseValues[i] = (int)kernel[alphaBaseValues.Length + 1 + i];

                            bo.AlphaBaseValues = alphaBaseValues;
                        }
                        else
                        {
                            for (int i = 0, ii = 1; i < this._widthOutside; i++, ii++)
                            {
                                int pX = (int)(cc[j].X + 0.5 - (bo.NormalAmounts.X * ii)); //compute from the center of the pixel
                                int pY = (int)(cc[j].Y + 0.5 - (bo.NormalAmounts.Y * ii));

                                int indx = cc.IndexOf(new Point(pX, pY));

                                if (indx == -1)
                                {
                                    realOuterWidth++;
                                    bo.OuterPixels.Add(new Point(pX, pY));
                                }
                                else
                                    break;
                            }

                            int[] alphaBaseValues = new int[bo.OuterPixels.Count + bo.InnerPixels.Count];

                            double[] kernel = GetVector(alphaBaseValues.Length * 2 + 1, this._alphaStartValue, bo.InnerPixels.Count);

                            for (int i = 0; i < alphaBaseValues.Length; i++)
                                alphaBaseValues[i] = (int)kernel[alphaBaseValues.Length + 1 + i];

                            bo.AlphaBaseValues = alphaBaseValues;
                        }

                        objs.Add(bo);
                    }
                }

                //ColorDistance/ColorGradient at boundary of segmentation for all items of one list
                GetColorDistances(BoundaryObjects);
            }
        }

        public double[] GetVector(int Length)
        {
            if ((Length & 0x01) != 1)
                throw new Exception("Length must be odd");

            double[] KernelVector = new double[Length];

            int Radius = Length / 2;

            double a = -2.0 * Radius * Radius / Math.Log(0.001 /*Weight*/);
            double Sum = 0.0;

            for (int x = 0; x < KernelVector.Length; x++)
            {
                double dist = Math.Abs(x - Radius);
                KernelVector[x] = Math.Exp(-dist * dist / a);
                Sum += KernelVector[x];
            }

            for (int x = 0; x < KernelVector.Length; x++)
            {
                KernelVector[x] *= this._alphaStartValue;
            }

            return KernelVector;
        }

        public double[] GetVector(int Length, double alphaStartValue, int outerFirstPosition)
        {
            if ((Length & 0x01) != 1)
                throw new Exception("Length must be odd");

            //just compute a rough estimation
            double[] h = new double[Length];
            int z = Length / 2;
            h[z + outerFirstPosition] = alphaStartValue / 255.0;
            h[z - outerFirstPosition] = alphaStartValue / 255.0;
            h[0] = 0.001;
            h[h.Length - 1] = 0.001;
            h[z] = 1.0;

            double mean = z;
            double stdSq = 0;

            for (int i = 0; i < h.Length; i++)
                stdSq += Math.Pow(i - mean, 2) * h[i];

            double StD = Math.Sqrt(stdSq);

            double[] KernelVector = new double[Length];
            //double Sum = 0.0;

            for (int x = 0; x < KernelVector.Length; x++)
            {
                double l = Math.Abs(x - mean);
                KernelVector[x] = (1.0 / (StD * Math.Sqrt(Math.PI * 2.0))) * Math.Exp(-0.5 * Math.Pow(l / StD, 2));
                //Sum += KernelVector[x];
            }

            //stdSq = 0;

            //for (int i = 0; i < KernelVector.Length; i++)
            //    stdSq += Math.Pow(i - mean, 2) * KernelVector[i];

            //StD = Math.Sqrt(stdSq);

            //for (int x = 0; x < KernelVector.Length; x++)
            //{
            //    double l = Math.Abs(x - mean);
            //    KernelVector[x] = (1.0 / (StD * Math.Sqrt(Math.PI * 2.0))) * Math.Exp(-0.5 * Math.Pow(l / StD, 2));
            //    //Sum += KernelVector[x];
            //}

            double mult = 255 / KernelVector[z];
            for (int x = 0; x < KernelVector.Length; x++)
                KernelVector[x] *= mult;

            this._kernelVector = KernelVector;

            return KernelVector;
        }

        private void GetColorDistances(List<List<BoundaryObject>> boundaryObjects)
        {
            if (this.BmpOrig != null)
            {
                int w = this.BmpOrig.Width;
                int h = this.BmpOrig.Height;
                BitmapData bmData = this.BmpOrig.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmData.Stride;

                if (boundaryObjects != null && boundaryObjects.Count > 0)
                {
                    foreach (List<BoundaryObject> lBo in boundaryObjects)
                    {
                        if (lBo != null && lBo.Count > 0)
                        {
                            foreach (BoundaryObject bo in lBo)
                            {
                                double d = GetColorDistance(bo, bmData, w, h, stride);
                                bo.ColorDistance = d;
                                bo.AlphaCorrectionColDist = 1.0 - (d / Math.Sqrt(195075)); //sqrt (255^2 * 3)
                            }
                        }
                    }
                }

                this.BmpOrig.UnlockBits(bmData);
            }
        }

        private unsafe double GetColorDistance(BoundaryObject bo, BitmapData bmData, int w, int h, int stride)
        {
            Point loc = bo.Location;
            PointF normal = bo.NormalAmounts;

            int x = loc.X;
            int y = loc.Y;

            int innerLocX = (int)(x + normal.X * this._measureDist);
            int innerLocY = (int)(y + normal.Y * this._measureDist);

            int outerLocX = (int)(x - normal.X * this._measureDist);
            int outerLocY = (int)(y - normal.Y * this._measureDist);

            double d = 0;

            if (innerLocX >= 0 && innerLocX < w && innerLocY >= 0 && innerLocY < h &&
                outerLocX >= 0 && outerLocX < w && outerLocY >= 0 && outerLocY < h)
            {
                byte* p = (byte*)bmData.Scan0;

                int bI = p[innerLocX * 4 + innerLocY * stride];
                int gI = p[innerLocX * 4 + innerLocY * stride + 1];
                int rI = p[innerLocX * 4 + innerLocY * stride + 2];

                int bO = p[outerLocX * 4 + outerLocY * stride];
                int gO = p[outerLocX * 4 + outerLocY * stride + 1];
                int rO = p[outerLocX * 4 + outerLocY * stride + 2];

                d = Math.Sqrt((bI - bO) * (bI - bO) + (gI - gO) * (gI - gO) + (rI - rO) * (rI - rO));
            }

            return d;
        }

        public Bitmap ProcessImage(ColorSource cs, double numFactorOuterPx, int blur) //todo: check pointer addresses for being inside the pictures
        {
            Bitmap bOut = null;

            if (this.BmpWork != null)
            {
                bOut = new Bitmap(this.BmpWork);

                if (this._blendType == BlendType.Outwards && this._widthOutside > 0) //blend outwards
                {
                    List<List<ChainCode>> newOutlines = new List<List<ChainCode>>();

                    //extend 1 px widthOutside times, get boundaries at each iteration
                    using (Bitmap orig = new Bitmap(this.BmpOrig))
                    {
                        for (int o = 0; o < this._widthOutside; o++)
                        {
                            Bitmap bTmp4 = ExtOutline(bOut, orig, 1, this.BGW);

                            Bitmap bOld2 = bOut;
                            bOut = bTmp4;
                            if (bOld2 != null)
                            {
                                bOld2.Dispose();
                                bOld2 = null;
                            }

                            List<ChainCode> newOutline = GetBoundary(bOut);
                            newOutlines.Add(newOutline);
                        }
                    }

                    Bitmap bDiff = null;
                    using (Bitmap bTmp = new Bitmap(bOut), bTmp2 = new Bitmap(this.BmpWork))
                        bDiff = Subtract(bTmp, bTmp2);

                    using (Bitmap bmpBl = new Bitmap(this.BmpOrig))
                    {
                        using (Graphics gx = Graphics.FromImage(bmpBl))
                            gx.Clear(Color.Black);

                        //Bitmap to search in (all alpha vals not set in the following loops are 255)
                        using (Bitmap bOutC = new Bitmap(this.BmpWork))
                        using (Bitmap bTmp = ExtOutline(bOutC, bmpBl, Math.Max(this._widthOutside, 0), this.BGW))
                        {
                            BitmapData bmD = bOut.LockBits(new Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                            BitmapData bmD2 = bTmp.LockBits(new Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                            BitmapData bmOrig = this.BmpOrig.LockBits(new Rectangle(0, 0, this.BmpOrig.Width, this.BmpOrig.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            int stride = bmD.Stride;

                            BitmapData bmDiff = bDiff.LockBits(new Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                            unsafe
                            {
                                byte* p = (byte*)bmD.Scan0;
                                byte* p2 = (byte*)bmD2.Scan0;
                                byte* pOrig = (byte*)bmOrig.Scan0;

                                byte* pDiff = (byte*)bmDiff.Scan0;

                                double boundaryVal = 1.0 - numFactorOuterPx;

                                for (int j = 0; j < this._boundaries.Count; j++)
                                {
                                    if (ChainFinder.IsInnerOutline(this._boundaries[j]))
                                    {
                                        //set alpha values for inner outlines
                                        List<BoundaryObject> objs = this.BoundaryObjects[j];

                                        for (int i = 0; i < objs.Count; i++) //coords
                                        {
                                            for (int l = 0; l < objs[i].OuterPixels.Count; l++)
                                            {
                                                if (objs[i].OuterPixels[l].X > 0 && objs[i].OuterPixels[l].X < bOut.Width && objs[i].OuterPixels[l].Y > 0 && objs[i].OuterPixels[l].Y < bOut.Height)
                                                {
                                                    PointF pt = objs[i].NormalAmounts;
                                                    int r = objs[i].AlphaBaseValues.Length - 1;
                                                    //int addr = (int)(objs[i].OuterPixels[l].X + pt.X) * 4 + (int)(objs[i].OuterPixels[l].Y + pt.Y) * stride + 3;

                                                    int addr = (int)(objs[i].Location.X - (l + 1) * pt.X) * 4 + (int)(objs[i].Location.Y - (l + 1) * pt.Y) * stride + 3; ;
                                                    int addr2 = addr;
                                                    int addr4 = addr;

                                                    if (cs == ColorSource.Boundary)
                                                        //addr = objs[i].Location.X * 4 + objs[i].Location.Y * stride + 3;
                                                        addr2 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                    else if (cs == ColorSource.Both)
                                                    {
                                                        addr2 = (int)(objs[i].Location.X - (l + 1) * pt.X) * 4 + (int)(objs[i].Location.Y - (l + 1) * pt.Y) * stride + 3;
                                                        addr4 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                    }

                                                    if (l < objs[i].AlphaBaseValues.Length)
                                                    {
                                                        //read color from orig
                                                        if (cs == ColorSource.OuterPixels)
                                                        {
                                                            p[addr - 3] = pOrig[addr - 3];
                                                            p[addr - 2] = pOrig[addr - 2];
                                                            p[addr - 1] = pOrig[addr - 1];
                                                        }
                                                        if (cs == ColorSource.Boundary)
                                                        {
                                                            p[addr - 3] = pOrig[addr2 - 3];
                                                            p[addr - 2] = pOrig[addr2 - 2];
                                                            p[addr - 1] = pOrig[addr2 - 1];
                                                        }
                                                        else if (cs == ColorSource.Both)
                                                        {
                                                            p[addr - 3] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 3] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                            p[addr - 2] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 2] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                            p[addr - 1] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 1] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                        }

                                                        p[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);
                                                        p2[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);

                                                        pDiff[addr] = 255;

                                                        //set surrounding pixels
                                                        int x = (addr - 3) % stride / 4;
                                                        int y = (addr - 3) / stride;

                                                        if (x > 0 && pDiff[(x - 1) * 4 + y * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[(x - 1) * 4 + y * stride] = pOrig[(x - 1) * 4 + y * stride];
                                                                pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[(x - 1) * 4 + y * stride + 1];
                                                                pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[(x - 1) * 4 + y * stride + 2];
                                                                pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[(x - 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[(x - 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[(x - 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[(x - 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                        }
                                                        if (x < bOut.Width - 1 && pDiff[(x + 1) * 4 + y * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[(x + 1) * 4 + y * stride] = pOrig[(x + 1) * 4 + y * stride];
                                                                pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[(x + 1) * 4 + y * stride + 1];
                                                                pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[(x + 1) * 4 + y * stride + 2];
                                                                pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[(x + 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[(x + 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[(x + 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[(x + 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                        }
                                                        if (y > 0 && pDiff[x * 4 + (y - 1) * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[x * 4 + (y - 1) * stride] = pOrig[x * 4 + (y - 1) * stride];
                                                                pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[x * 4 + (y - 1) * stride + 1];
                                                                pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[x * 4 + (y - 1) * stride + 2];
                                                                pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[x * 4 + (y - 1) * stride] = pOrig[addr2 - 3];
                                                                pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[x * 4 + (y - 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y - 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y - 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                            }
                                                        }
                                                        if (y < bOut.Height - 1 && pDiff[x * 4 + (y + 1) * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[x * 4 + (y + 1) * stride] = pOrig[x * 4 + (y + 1) * stride];
                                                                pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[x * 4 + (y + 1) * stride + 1];
                                                                pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[x * 4 + (y + 1) * stride + 2];
                                                                pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[x * 4 + (y + 1) * stride] = pOrig[addr2 - 3];
                                                                pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[x * 4 + (y + 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y + 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y + 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        p[addr] = 0;
                                                        p2[addr] = 0;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //set fixed alpha values
                                        List<BoundaryObject> objs = this.BoundaryObjects[j];

                                        for (int i = 0; i < objs.Count; i++) //coords
                                        {
                                            for (int l = 0; l < objs[i].OuterPixels.Count; l++)
                                            {
                                                if (objs[i].OuterPixels[l].X > 0 && objs[i].OuterPixels[l].X < bOut.Width && objs[i].OuterPixels[l].Y > 0 && objs[i].OuterPixels[l].Y < bOut.Height)
                                                {
                                                    PointF pt = objs[i].NormalAmounts;

                                                    int addr = objs[i].OuterPixels[l].X * 4 + objs[i].OuterPixels[l].Y * stride + 3;
                                                    int addr2 = addr;
                                                    int addr4 = addr;

                                                    if (cs == ColorSource.Boundary)
                                                        //addr2 = objs[i].Location.X * 4 + objs[i].Location.Y * stride + 3;
                                                        //addr2 = (int)(objs[i].Location.X - pt.X) * 4 + (int)(objs[i].Location.Y - pt.Y) * stride + 3;
                                                        addr2 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                    else if (cs == ColorSource.Both)
                                                    {
                                                        addr2 = objs[i].OuterPixels[l].X * 4 + objs[i].OuterPixels[l].Y * stride + 3;
                                                        addr4 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                    }

                                                    if (l < objs[i].AlphaBaseValues.Length)
                                                    {
                                                        //read color from orig
                                                        if (cs == ColorSource.OuterPixels)
                                                        {
                                                            p[addr - 3] = pOrig[addr - 3];
                                                            p[addr - 2] = pOrig[addr - 2];
                                                            p[addr - 1] = pOrig[addr - 1];
                                                        }
                                                        if (cs == ColorSource.Boundary)
                                                        {
                                                            p[addr - 3] = pOrig[addr2 - 3];
                                                            p[addr - 2] = pOrig[addr2 - 2];
                                                            p[addr - 1] = pOrig[addr2 - 1];
                                                        }
                                                        else if (cs == ColorSource.Both)
                                                        {
                                                            p[addr - 3] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 3] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                            p[addr - 2] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 2] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                            p[addr - 1] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 1] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                        }

                                                        p[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);
                                                        p2[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);

                                                        //set surrounding pixels
                                                        int x = (addr - 3) % stride / 4;
                                                        int y = (addr - 3) / stride;

                                                        if (x > 0 && pDiff[(x - 1) * 4 + y * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[(x - 1) * 4 + y * stride] = pOrig[(x - 1) * 4 + y * stride];
                                                                pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[(x - 1) * 4 + y * stride + 1];
                                                                pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[(x - 1) * 4 + y * stride + 2];
                                                                pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[(x - 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[(x - 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[(x - 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[(x - 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                        }
                                                        if (x < bOut.Width - 1 && pDiff[(x + 1) * 4 + y * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[(x + 1) * 4 + y * stride] = pOrig[(x + 1) * 4 + y * stride];
                                                                pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[(x + 1) * 4 + y * stride + 1];
                                                                pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[(x + 1) * 4 + y * stride + 2];
                                                                pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[(x + 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[(x + 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[(x + 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[(x + 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                            }
                                                        }
                                                        if (y > 0 && pDiff[x * 4 + (y - 1) * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[x * 4 + (y - 1) * stride] = pOrig[x * 4 + (y - 1) * stride];
                                                                pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[x * 4 + (y - 1) * stride + 1];
                                                                pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[x * 4 + (y - 1) * stride + 2];
                                                                pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[x * 4 + (y - 1) * stride] = pOrig[addr2 - 3];
                                                                pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[x * 4 + (y - 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y - 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y - 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                            }
                                                        }
                                                        if (y < bOut.Height - 1 && pDiff[x * 4 + (y + 1) * stride + 3] != 255)
                                                        {
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                pDiff[x * 4 + (y + 1) * stride] = pOrig[x * 4 + (y + 1) * stride];
                                                                pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[x * 4 + (y + 1) * stride + 1];
                                                                pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[x * 4 + (y + 1) * stride + 2];
                                                                pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                pDiff[x * 4 + (y + 1) * stride] = pOrig[addr2 - 3];
                                                                pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[addr2 - 2];
                                                                pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[addr2 - 1];
                                                                pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                pDiff[x * 4 + (y + 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y + 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y + 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        p[addr] = 0;
                                                        p2[addr] = 0;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                //interpolate in between the fixed alpha values, so this is a blur in the orbital direction
                                for (int j = 0; j < newOutlines.Count; j++)
                                {
                                    List<ChainCode> ol = newOutlines[j];
                                    for (int i = 0; i < ol.Count; i++)
                                    {
                                        List<Point> pts = ol[i].Coord;

                                        for (int l = 0; l < pts.Count; l++)
                                        {
                                            if (l > 0)
                                            {
                                                int addr = pts[l].X * 4 + pts[l].Y * stride + 3;

                                                if (p2[addr] < 255)
                                                {
                                                    int z = (l + 1) % pts.Count;
                                                    int addr2 = pts[z].X * 4 + pts[z].Y * stride + 3;

                                                    while (p2[addr2] == 255)
                                                    {
                                                        z++;
                                                        if (z < pts.Count)
                                                            addr2 = pts[z].X * 4 + pts[z].Y * stride + 3;
                                                        else
                                                        {
                                                            int z2 = z % pts.Count;
                                                            addr2 = pts[z2].X * 4 + pts[z2].Y * stride + 3;
                                                        }
                                                    }

                                                    int dist = z - 1 - l;

                                                    if (dist != 0)
                                                    {
                                                        double step = ((double)p2[addr2] - (double)p2[addr]) / (double)dist;

                                                        for (int a = l + 1, w = 1; a < z; a++, w++)
                                                        {
                                                            if (a >= pts.Count)
                                                            {
                                                                a -= pts.Count;
                                                                z = z % pts.Count;
                                                            }

                                                            int val = (int)((double)p2[addr] + step * w);
                                                            int addr4 = pts[a].X * 4 + pts[a].Y * stride + 3;
                                                            p[addr4] = (byte)Math.Max(Math.Min(val, 255), 0);

                                                            //prevent endless loops, not needed since we change both a and z if a > pts.Count
                                                            if (w > pts.Count * 2)
                                                                break;
                                                        }
                                                    }

                                                    if (z > l)
                                                        l = z - 1; //l will be incremented by one in the loop's head, so we use -1
                                                }
                                            }
                                        }
                                    }
                                }

                                //now check for orphan pixels (pixels that are nor touched by the previous operations)
                                int wdth = bOut.Width;
                                int hght = bOut.Height;

                                Parallel.For(0, hght, y =>
                                {
                                    for (int x = 0; x < wdth; x++)
                                    {
                                        if (pDiff[x * 4 + y * stride + 3] > 0 && p[x * 4 + y * stride + 3] == 255)
                                        {
                                            int sum = 0;
                                            int cnt = 0;

                                            if (x > 0 && p[(x - 1) * 4 + y * stride + 3] != 255)
                                            {
                                                sum += p[(x - 1) * 4 + y * stride + 3];
                                                cnt++;
                                            }
                                            if (x < wdth && p[(x + 1) * 4 + y * stride + 3] != 255)
                                            {
                                                sum += p[(x + 1) * 4 + y * stride + 3];
                                                cnt++;
                                            }
                                            if (y > 0 && p[x * 4 + (y - 1) * stride + 3] != 255)
                                            {
                                                sum += p[x * 4 + (y - 1) * stride + 3];
                                                cnt++;
                                            }
                                            if (y < hght && p[x * 4 + (y + 1) * stride + 3] != 255)
                                            {
                                                sum += p[x * 4 + (y + 1) * stride + 3];
                                                cnt++;
                                            }

                                            if (sum > 0)
                                            {
                                                sum /= cnt;
                                                p[x * 4 + y * stride + 3] = (byte)Math.Max(Math.Min(sum, 255), 0);
                                            }
                                        }

                                        if (pDiff[x * 4 + y * stride + 3] == 255 && p[x * 4 + y * stride + 3] == 0)
                                        {
                                            int sum = 0;
                                            int cnt = 0;

                                            if (x > 0 && p[(x - 1) * 4 + y * stride + 3] != 255)
                                            {
                                                sum += p[(x - 1) * 4 + y * stride + 3];
                                                cnt++;
                                            }
                                            if (x < wdth - 1 && p[(x + 1) * 4 + y * stride + 3] != 255)
                                            {
                                                sum += p[(x + 1) * 4 + y * stride + 3];
                                                cnt++;
                                            }
                                            if (y > 0 && p[x * 4 + (y - 1) * stride + 3] != 255)
                                            {
                                                sum += p[x * 4 + (y - 1) * stride + 3];
                                                cnt++;
                                            }
                                            if (y < hght - 1 && p[x * 4 + (y + 1) * stride + 3] != 255)
                                            {
                                                sum += p[x * 4 + (y + 1) * stride + 3];
                                                cnt++;
                                            }

                                            if (sum > 0)
                                            {
                                                sum /= cnt;
                                                p[x * 4 + y * stride] = pDiff[x * 4 + y * stride];
                                                p[x * 4 + y * stride + 1] = pDiff[x * 4 + y * stride + 1];
                                                p[x * 4 + y * stride + 2] = pDiff[x * 4 + y * stride + 2];
                                                p[x * 4 + y * stride + 3] = (byte)Math.Max(Math.Min(sum, 255), 0);
                                            }
                                        }
                                    }
                                });

                                bDiff.UnlockBits(bmDiff);
                                bDiff.Dispose();
                                bDiff = null;
                            }

                            bOut.UnlockBits(bmD);
                            bTmp.UnlockBits(bmD2);
                            this.BmpOrig.UnlockBits(bmOrig);

                            //do a general blur with an average kernel and redraw the part that shouldn't be blurred
                            if (this._widthOutside > 0)
                            {
                                if (blur != 0 && ((blur & 0x01) != 1))
                                    blur++;

                                if (blur != 0 && blur < 3)
                                    blur = 3;

                                fipbmp fip = new fipbmp();
                                fip.ProgressPlus += Fip_ProgressPlus;

                                if (blur > 0)
                                    fip.SmoothByAveragingA(bOut, blur, this.BGW);

                                fip.ProgressPlus -= Fip_ProgressPlus;
                            }

                            using (Graphics gx = Graphics.FromImage(bOut))
                            {
                                gx.CompositingMode = CompositingMode.SourceOver;
                                gx.DrawImage(this.BmpWork, 0, 0);
                            }
                        }
                    }
                }
                if (this._blendType == BlendType.Inwards && this._widthInside > 0) //blend inwards
                {
                    //feather
                    Bitmap bmp = new Bitmap(bOut.Width, bOut.Height);
                    Feather(bOut, this._widthInside, true, bmp);
                    Bitmap bOld = this._lastCroppedPic;
                    this._lastCroppedPic = bmp;
                    if (bOld != null)
                    {
                        bOld.Dispose();
                        bOld = null;
                    }
                    //blur
                    if (blur != 0 && ((blur & 0x01) != 1))
                        blur++;

                    if (blur != 0 && blur < 3)
                        blur = 3;

                    fipbmp fip = new fipbmp();
                    fip.ProgressPlus += Fip_ProgressPlus;

                    if (blur > 0)
                    {
                        fip.SmoothByAveragingA(bOut, blur, this.BGW);
                        using (Graphics gx = Graphics.FromImage(bOut))
                            gx.DrawImage(this._lastCroppedPic, 0, 0);
                    }

                    fip.ProgressPlus -= Fip_ProgressPlus;
                }
                if (this._blendType == BlendType.Both && this._widthInside > 0 && this._widthOutside > 0) //blend outwards and inwards
                {
                    //1) feather
                    double[] alpha = new double[this._widthInside];
                    int lngth2 = this._kernelVector.Length / 2;
                    for (int i = 0; i < alpha.Length; i++)
                        alpha[i] = this._kernelVector[lngth2 + i];
                    alpha = alpha.Reverse().ToArray();
                    using (Bitmap bOut2 = new Bitmap(bOut))
                    {
                        Feather(bOut2, alpha);

                        //blur
                        //if (blur != 0 && ((blur & 0x01) != 1))
                        //    blur++;

                        //if (blur != 0 && blur < 3)
                        //    blur = 3;

                        //fipbmp fip = new fipbmp();
                        //fip.ProgressPlus += Fip_ProgressPlus;

                        //if (blur > 0)
                        //{
                        //    fip.SmoothByAveragingA(bOut, blur, this.BGW);
                        //    using (Graphics gx = Graphics.FromImage(bOut))
                        //        gx.DrawImage(this._lastCroppedPic, 0, 0);
                        //}

                        //fip.ProgressPlus -= Fip_ProgressPlus;

                        //2)
                        List<List<ChainCode>> newOutlines = new List<List<ChainCode>>();

                        //extend 1 px widthOutside times, get boundaries at each iteration
                        using (Bitmap orig = new Bitmap(this.BmpOrig))
                        {
                            for (int o = 0; o < this._widthOutside; o++)
                            {
                                Bitmap bTmp4 = ExtOutline(bOut, orig, 1, this.BGW);

                                Bitmap bOld2 = bOut;
                                bOut = bTmp4;
                                if (bOld2 != null)
                                {
                                    bOld2.Dispose();
                                    bOld2 = null;
                                }

                                List<ChainCode> newOutline = GetBoundary(bOut);
                                newOutlines.Add(newOutline);
                            }
                        }

                        Bitmap bDiff = null;
                        using (Bitmap bTmp = new Bitmap(bOut), bTmp2 = new Bitmap(this.BmpWork))
                            bDiff = Subtract(bTmp, bTmp2);

                        using (Bitmap bmpBl = new Bitmap(this.BmpOrig))
                        {
                            using (Graphics gx = Graphics.FromImage(bmpBl))
                                gx.Clear(Color.Black);

                            //Bitmap to search in (all alpha vals not set in the following loops are 255)
                            using (Bitmap bOutC = new Bitmap(this.BmpWork))
                            using (Bitmap bTmp = ExtOutline(bOutC, bmpBl, Math.Max(this._widthOutside, 0), this.BGW))
                            {
                                BitmapData bmD = bOut.LockBits(new Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                                BitmapData bmD2 = bTmp.LockBits(new Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                                BitmapData bmOrig = this.BmpOrig.LockBits(new Rectangle(0, 0, this.BmpOrig.Width, this.BmpOrig.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                                int stride = bmD.Stride;

                                BitmapData bmDiff = bDiff.LockBits(new Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                                unsafe
                                {
                                    byte* p = (byte*)bmD.Scan0;
                                    byte* p2 = (byte*)bmD2.Scan0;
                                    byte* pOrig = (byte*)bmOrig.Scan0;

                                    byte* pDiff = (byte*)bmDiff.Scan0;

                                    double boundaryVal = 1.0 - numFactorOuterPx;

                                    for (int j = 0; j < this._boundaries.Count; j++)
                                    {
                                        if (ChainFinder.IsInnerOutline(this._boundaries[j]))
                                        {
                                            //set alpha values for inner outlines
                                            List<BoundaryObject> objs = this.BoundaryObjects[j];

                                            for (int i = 0; i < objs.Count; i++) //coords
                                            {
                                                for (int l = 0; l < objs[i].OuterPixels.Count; l++)
                                                {
                                                    //reset AlphaBaseVals
                                                    int[] alphaVals = new int[objs[i].OuterPixels.Count];
                                                    for (int ll = objs[i].InnerPixels.Count, jj = 0; ll < objs[i].AlphaBaseValues.Length; ll++, jj++)
                                                        alphaVals[jj] = objs[i].AlphaBaseValues[ll];
                                                    objs[i].AlphaBaseValues = alphaVals;

                                                    if (objs[i].OuterPixels[l].X > 0 && objs[i].OuterPixels[l].X < bOut.Width && objs[i].OuterPixels[l].Y > 0 && objs[i].OuterPixels[l].Y < bOut.Height)
                                                    {
                                                        PointF pt = objs[i].NormalAmounts;
                                                        int r = objs[i].AlphaBaseValues.Length - 1;
                                                        //int addr = (int)(objs[i].OuterPixels[l].X + pt.X) * 4 + (int)(objs[i].OuterPixels[l].Y + pt.Y) * stride + 3;

                                                        int addr = (int)(objs[i].Location.X - (l + 1) * pt.X) * 4 + (int)(objs[i].Location.Y - (l + 1) * pt.Y) * stride + 3; ;
                                                        int addr2 = addr;
                                                        int addr4 = addr;

                                                        if (cs == ColorSource.Boundary)
                                                            //addr = objs[i].Location.X * 4 + objs[i].Location.Y * stride + 3;
                                                            addr2 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                        else if (cs == ColorSource.Both)
                                                        {
                                                            addr2 = (int)(objs[i].Location.X - (l + 1) * pt.X) * 4 + (int)(objs[i].Location.Y - (l + 1) * pt.Y) * stride + 3;
                                                            addr4 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                        }

                                                        if (l < objs[i].AlphaBaseValues.Length)
                                                        {
                                                            //read color from orig
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                p[addr - 3] = pOrig[addr - 3];
                                                                p[addr - 2] = pOrig[addr - 2];
                                                                p[addr - 1] = pOrig[addr - 1];
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                p[addr - 3] = pOrig[addr2 - 3];
                                                                p[addr - 2] = pOrig[addr2 - 2];
                                                                p[addr - 1] = pOrig[addr2 - 1];
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                p[addr - 3] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 3] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                p[addr - 2] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 2] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                p[addr - 1] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 1] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                            }

                                                            p[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);
                                                            p2[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);

                                                            pDiff[addr] = 255;

                                                            //set surrounding pixels
                                                            int x = (addr - 3) % stride / 4;
                                                            int y = (addr - 3) / stride;

                                                            if (x > 0 && pDiff[(x - 1) * 4 + y * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[(x - 1) * 4 + y * stride] = pOrig[(x - 1) * 4 + y * stride];
                                                                    pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[(x - 1) * 4 + y * stride + 1];
                                                                    pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[(x - 1) * 4 + y * stride + 2];
                                                                    pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[(x - 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                    pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[(x - 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[(x - 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[(x - 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                            }
                                                            if (x < bOut.Width - 1 && pDiff[(x + 1) * 4 + y * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[(x + 1) * 4 + y * stride] = pOrig[(x + 1) * 4 + y * stride];
                                                                    pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[(x + 1) * 4 + y * stride + 1];
                                                                    pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[(x + 1) * 4 + y * stride + 2];
                                                                    pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[(x + 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                    pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[(x + 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[(x + 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[(x + 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                            }
                                                            if (y > 0 && pDiff[x * 4 + (y - 1) * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[x * 4 + (y - 1) * stride] = pOrig[x * 4 + (y - 1) * stride];
                                                                    pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[x * 4 + (y - 1) * stride + 1];
                                                                    pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[x * 4 + (y - 1) * stride + 2];
                                                                    pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[x * 4 + (y - 1) * stride] = pOrig[addr2 - 3];
                                                                    pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[x * 4 + (y - 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y - 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y - 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                                }
                                                            }
                                                            if (y < bOut.Height - 1 && pDiff[x * 4 + (y + 1) * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[x * 4 + (y + 1) * stride] = pOrig[x * 4 + (y + 1) * stride];
                                                                    pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[x * 4 + (y + 1) * stride + 1];
                                                                    pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[x * 4 + (y + 1) * stride + 2];
                                                                    pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[x * 4 + (y + 1) * stride] = pOrig[addr2 - 3];
                                                                    pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[x * 4 + (y + 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y + 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y + 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            p[addr] = 0;
                                                            p2[addr] = 0;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //set fixed alpha values
                                            List<BoundaryObject> objs = this.BoundaryObjects[j];

                                            for (int i = 0; i < objs.Count; i++) //coords
                                            {
                                                for (int l = 0; l < objs[i].OuterPixels.Count; l++)
                                                {
                                                    //reset AlphaBaseVals
                                                    int[] alphaVals = new int[objs[i].OuterPixels.Count];
                                                    for (int ll = objs[i].InnerPixels.Count, jj = 0; ll < objs[i].AlphaBaseValues.Length; ll++, jj++)
                                                        alphaVals[jj] = objs[i].AlphaBaseValues[ll];
                                                    objs[i].AlphaBaseValues = alphaVals;

                                                    if (objs[i].OuterPixels[l].X > 0 && objs[i].OuterPixels[l].X < bOut.Width && objs[i].OuterPixels[l].Y > 0 && objs[i].OuterPixels[l].Y < bOut.Height)
                                                    {
                                                        PointF pt = objs[i].NormalAmounts;

                                                        int addr = objs[i].OuterPixels[l].X * 4 + objs[i].OuterPixels[l].Y * stride + 3;
                                                        int addr2 = addr;
                                                        int addr4 = addr;

                                                        if (cs == ColorSource.Boundary)
                                                            //addr2 = objs[i].Location.X * 4 + objs[i].Location.Y * stride + 3;
                                                            //addr2 = (int)(objs[i].Location.X - pt.X) * 4 + (int)(objs[i].Location.Y - pt.Y) * stride + 3;
                                                            addr2 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                        else if (cs == ColorSource.Both)
                                                        {
                                                            addr2 = objs[i].OuterPixels[l].X * 4 + objs[i].OuterPixels[l].Y * stride + 3;
                                                            addr4 = (int)(objs[i].Location.X + pt.X) * 4 + (int)(objs[i].Location.Y + pt.Y) * stride + 3;
                                                        }

                                                        if (l < objs[i].AlphaBaseValues.Length)
                                                        {
                                                            //read color from orig
                                                            if (cs == ColorSource.OuterPixels)
                                                            {
                                                                p[addr - 3] = pOrig[addr - 3];
                                                                p[addr - 2] = pOrig[addr - 2];
                                                                p[addr - 1] = pOrig[addr - 1];
                                                            }
                                                            if (cs == ColorSource.Boundary)
                                                            {
                                                                p[addr - 3] = pOrig[addr2 - 3];
                                                                p[addr - 2] = pOrig[addr2 - 2];
                                                                p[addr - 1] = pOrig[addr2 - 1];
                                                            }
                                                            else if (cs == ColorSource.Both)
                                                            {
                                                                p[addr - 3] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 3] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                p[addr - 2] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 2] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                p[addr - 1] = (byte)Math.Max(Math.Min((double)pOrig[addr2 - 1] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                            }

                                                            p[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);
                                                            p2[addr] = (byte)Math.Max(Math.Min(objs[i].AlphaBaseValues[l] * objs[i].AlphaCorrectionColDist, 255), 0);

                                                            //set surrounding pixels
                                                            int x = (addr - 3) % stride / 4;
                                                            int y = (addr - 3) / stride;

                                                            if (x > 0 && pDiff[(x - 1) * 4 + y * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[(x - 1) * 4 + y * stride] = pOrig[(x - 1) * 4 + y * stride];
                                                                    pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[(x - 1) * 4 + y * stride + 1];
                                                                    pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[(x - 1) * 4 + y * stride + 2];
                                                                    pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[(x - 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                    pDiff[(x - 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[(x - 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[(x - 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[(x - 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[(x - 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x - 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[(x - 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                            }
                                                            if (x < bOut.Width - 1 && pDiff[(x + 1) * 4 + y * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[(x + 1) * 4 + y * stride] = pOrig[(x + 1) * 4 + y * stride];
                                                                    pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[(x + 1) * 4 + y * stride + 1];
                                                                    pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[(x + 1) * 4 + y * stride + 2];
                                                                    pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[(x + 1) * 4 + y * stride] = pOrig[addr2 - 3];
                                                                    pDiff[(x + 1) * 4 + y * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[(x + 1) * 4 + y * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[(x + 1) * 4 + y * stride] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[(x + 1) * 4 + y * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[(x + 1) * 4 + y * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[(x + 1) * 4 + y * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[(x + 1) * 4 + y * stride + 3] = 255;
                                                                }
                                                            }
                                                            if (y > 0 && pDiff[x * 4 + (y - 1) * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[x * 4 + (y - 1) * stride] = pOrig[x * 4 + (y - 1) * stride];
                                                                    pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[x * 4 + (y - 1) * stride + 1];
                                                                    pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[x * 4 + (y - 1) * stride + 2];
                                                                    pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[x * 4 + (y - 1) * stride] = pOrig[addr2 - 3];
                                                                    pDiff[x * 4 + (y - 1) * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[x * 4 + (y - 1) * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[x * 4 + (y - 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y - 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y - 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y - 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y - 1) * stride + 3] = 255;
                                                                }
                                                            }
                                                            if (y < bOut.Height - 1 && pDiff[x * 4 + (y + 1) * stride + 3] != 255)
                                                            {
                                                                if (cs == ColorSource.OuterPixels)
                                                                {
                                                                    pDiff[x * 4 + (y + 1) * stride] = pOrig[x * 4 + (y + 1) * stride];
                                                                    pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[x * 4 + (y + 1) * stride + 1];
                                                                    pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[x * 4 + (y + 1) * stride + 2];
                                                                    pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                                }
                                                                if (cs == ColorSource.Boundary)
                                                                {
                                                                    pDiff[x * 4 + (y + 1) * stride] = pOrig[addr2 - 3];
                                                                    pDiff[x * 4 + (y + 1) * stride + 1] = pOrig[addr2 - 2];
                                                                    pDiff[x * 4 + (y + 1) * stride + 2] = pOrig[addr2 - 1];
                                                                    pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                                }
                                                                else if (cs == ColorSource.Both)
                                                                {
                                                                    pDiff[x * 4 + (y + 1) * stride] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride] * numFactorOuterPx + (double)pOrig[addr4 - 3] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y + 1) * stride + 1] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 1] * numFactorOuterPx + (double)pOrig[addr4 - 2] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y + 1) * stride + 2] = (byte)Math.Max(Math.Min((double)pOrig[x * 4 + (y + 1) * stride + 2] * numFactorOuterPx + (double)pOrig[addr4 - 1] * boundaryVal, 255), 0);
                                                                    pDiff[x * 4 + (y + 1) * stride + 3] = 255;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            p[addr] = 0;
                                                            p2[addr] = 0;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    //interpolate in between the fixed alpha values, so this is a blur in the orbital direction
                                    for (int j = 0; j < newOutlines.Count; j++)
                                    {
                                        List<ChainCode> ol = newOutlines[j];
                                        for (int i = 0; i < ol.Count; i++)
                                        {
                                            List<Point> pts = ol[i].Coord;

                                            for (int l = 0; l < pts.Count; l++)
                                            {
                                                if (l > 0)
                                                {
                                                    int addr = pts[l].X * 4 + pts[l].Y * stride + 3;

                                                    if (p2[addr] < 255)
                                                    {
                                                        int z = (l + 1) % pts.Count;
                                                        int addr2 = pts[z].X * 4 + pts[z].Y * stride + 3;

                                                        while (p2[addr2] == 255)
                                                        {
                                                            z++;
                                                            if (z < pts.Count)
                                                                addr2 = pts[z].X * 4 + pts[z].Y * stride + 3;
                                                            else
                                                            {
                                                                int z2 = z % pts.Count;
                                                                addr2 = pts[z2].X * 4 + pts[z2].Y * stride + 3;
                                                            }
                                                        }

                                                        int dist = z - 1 - l;

                                                        if (dist != 0)
                                                        {
                                                            double step = ((double)p2[addr2] - (double)p2[addr]) / (double)dist;

                                                            for (int a = l + 1, w = 1; a < z; a++, w++)
                                                            {
                                                                if (a >= pts.Count)
                                                                {
                                                                    a -= pts.Count;
                                                                    z = z % pts.Count;
                                                                }

                                                                int val = (int)((double)p2[addr] + step * w);
                                                                int addr4 = pts[a].X * 4 + pts[a].Y * stride + 3;
                                                                p[addr4] = (byte)Math.Max(Math.Min(val, 255), 0);

                                                                //prevent endless loops, not needed since we change both a and z if a > pts.Count
                                                                if (w > pts.Count * 2)
                                                                    break;
                                                            }
                                                        }

                                                        if (z > l)
                                                            l = z - 1; //l will be incremented by one in the loop's head, so we use -1
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    //now check for orphan pixels (pixels that are nor touched by the previous operations)
                                    int wdth = bOut.Width;
                                    int hght = bOut.Height;

                                    Parallel.For(0, hght, y =>
                                    {
                                        for (int x = 0; x < wdth; x++)
                                        {
                                            if (pDiff[x * 4 + y * stride + 3] > 0 && p[x * 4 + y * stride + 3] == 255)
                                            {
                                                int sum = 0;
                                                int cnt = 0;

                                                if (x > 0 && p[(x - 1) * 4 + y * stride + 3] != 255)
                                                {
                                                    sum += p[(x - 1) * 4 + y * stride + 3];
                                                    cnt++;
                                                }
                                                if (x < wdth && p[(x + 1) * 4 + y * stride + 3] != 255)
                                                {
                                                    sum += p[(x + 1) * 4 + y * stride + 3];
                                                    cnt++;
                                                }
                                                if (y > 0 && p[x * 4 + (y - 1) * stride + 3] != 255)
                                                {
                                                    sum += p[x * 4 + (y - 1) * stride + 3];
                                                    cnt++;
                                                }
                                                if (y < hght && p[x * 4 + (y + 1) * stride + 3] != 255)
                                                {
                                                    sum += p[x * 4 + (y + 1) * stride + 3];
                                                    cnt++;
                                                }

                                                if (sum > 0)
                                                {
                                                    sum /= cnt;
                                                    p[x * 4 + y * stride + 3] = (byte)Math.Max(Math.Min(sum, 255), 0);
                                                }
                                            }

                                            if (pDiff[x * 4 + y * stride + 3] == 255 && p[x * 4 + y * stride + 3] == 0)
                                            {
                                                int sum = 0;
                                                int cnt = 0;

                                                if (x > 0 && p[(x - 1) * 4 + y * stride + 3] != 255)
                                                {
                                                    sum += p[(x - 1) * 4 + y * stride + 3];
                                                    cnt++;
                                                }
                                                if (x < wdth - 1 && p[(x + 1) * 4 + y * stride + 3] != 255)
                                                {
                                                    sum += p[(x + 1) * 4 + y * stride + 3];
                                                    cnt++;
                                                }
                                                if (y > 0 && p[x * 4 + (y - 1) * stride + 3] != 255)
                                                {
                                                    sum += p[x * 4 + (y - 1) * stride + 3];
                                                    cnt++;
                                                }
                                                if (y < hght - 1 && p[x * 4 + (y + 1) * stride + 3] != 255)
                                                {
                                                    sum += p[x * 4 + (y + 1) * stride + 3];
                                                    cnt++;
                                                }

                                                if (sum > 0)
                                                {
                                                    sum /= cnt;
                                                    p[x * 4 + y * stride] = pDiff[x * 4 + y * stride];
                                                    p[x * 4 + y * stride + 1] = pDiff[x * 4 + y * stride + 1];
                                                    p[x * 4 + y * stride + 2] = pDiff[x * 4 + y * stride + 2];
                                                    p[x * 4 + y * stride + 3] = (byte)Math.Max(Math.Min(sum, 255), 0);
                                                }
                                            }
                                        }
                                    });

                                    bDiff.UnlockBits(bmDiff);
                                    bDiff.Dispose();
                                    bDiff = null;
                                }

                                bOut.UnlockBits(bmD);
                                bTmp.UnlockBits(bmD2);
                                this.BmpOrig.UnlockBits(bmOrig);

                                //do a general blur with an average kernel and redraw the part that shouldn't be blurred
                                if (this._widthOutside > 0)
                                {
                                    if (blur != 0 && ((blur & 0x01) != 1))
                                        blur++;

                                    if (blur != 0 && blur < 3)
                                        blur = 3;

                                    fipbmp fip = new fipbmp();
                                    fip.ProgressPlus += Fip_ProgressPlus;

                                    if (blur > 0)
                                        fip.SmoothByAveragingA(bOut, blur, this.BGW);

                                    fip.ProgressPlus -= Fip_ProgressPlus;
                                }

                                using (Graphics gx = Graphics.FromImage(bOut))
                                {
                                    gx.CompositingMode = CompositingMode.SourceOver;
                                    gx.DrawImage(this.BmpWork, 0, 0);
                                }

                                CompAlphaMin(bOut, bOut2);
                            }
                        }
                    }
                }
            }

            return bOut;
        }

        private unsafe void CompAlphaMin(Bitmap bOut, Bitmap bOut2)
        {
            //we know, both are of the same size
            if (bOut != null && bOut2 != null)
            {
                int w = bOut.Width;
                int h = bOut.Height;
                BitmapData bmD = bOut.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmD2 = bOut2.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmD.Stride;

                Parallel.For(0, h, y =>
                {
                    byte* p = (byte*)bmD.Scan0;
                    byte* p2 = (byte*)bmD2.Scan0;

                    p += y * stride;
                    p2 += y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        if (p2[3] != 0)
                            p[3] = Math.Min(p[3], p2[3]);

                        p += 4;
                        p2 += 4;
                    }
                });

                bOut.UnlockBits(bmD);
                bOut2.UnlockBits(bmD2);
            }
        }

        public void Feather(Bitmap bmp, int width)
        {
            Bitmap bRet = null;

            BitmapBorderAction b = BitmapBorderAction.Feather;
            double[] saturation = null;
            double[] alpha = GetVector(width * 2 + 1);
            BitmapOutlineVariant bov = BitmapOutlineVariant.All;

            bRet = GetOutline(bmp, width, b, saturation, alpha, bov, false, null);

            using (Graphics gx = Graphics.FromImage(bmp))
            {
                gx.Clear(Color.Transparent);
                gx.DrawImage(bRet, 0, 0);
            }

            if (bRet != null)
                bRet.Dispose();
            bRet = null;
        }

        public void Feather(Bitmap bmp, int width, bool setLastCroppedBitmap, Bitmap lastCroppedPic)
        {
            Bitmap bRet = null;

            BitmapBorderAction b = BitmapBorderAction.Feather;
            double[] saturation = null;
            double[] alpha = GetVector(width * 2 + 1);
            BitmapOutlineVariant bov = BitmapOutlineVariant.All;

            bRet = GetOutline(bmp, width, b, saturation, alpha, bov, true, lastCroppedPic);

            using (Graphics gx = Graphics.FromImage(bmp))
            {
                gx.Clear(Color.Transparent);
                gx.DrawImage(bRet, 0, 0);
            }

            if (bRet != null)
                bRet.Dispose();
            bRet = null;
        }

        public void Feather(Bitmap bmp, double[] alpha)
        {
            Bitmap bRet = null;

            BitmapBorderAction b = BitmapBorderAction.Feather;
            double[] saturation = null;
            BitmapOutlineVariant bov = BitmapOutlineVariant.All;

            bRet = GetOutline(bmp, alpha.Length, b, saturation, alpha, bov, false, null);

            using (Graphics gx = Graphics.FromImage(bmp))
            {
                gx.Clear(Color.Transparent);
                gx.DrawImage(bRet, 0, 0);
            }

            if (bRet != null)
                bRet.Dispose();
            bRet = null;
        }

        public void Feather(Bitmap bmp, int width, int alphaStartValue, int innerWidth)
        {
            Bitmap bRet = null;

            BitmapBorderAction b = BitmapBorderAction.Feather;
            double[] saturation = null;
            double[] alpha = GetVector(width * 2 + 1, alphaStartValue, innerWidth);
            BitmapOutlineVariant bov = BitmapOutlineVariant.All;

            bRet = GetOutline(bmp, width, b, saturation, alpha, bov, false, null);

            using (Graphics gx = Graphics.FromImage(bmp))
            {
                gx.Clear(Color.Transparent);
                gx.DrawImage(bRet, 0, 0);
            }

            if (bRet != null)
                bRet.Dispose();
            bRet = null;
        }

        public void Feather(Bitmap bmp, double[] alpha, bool setLastCroppedBitmap, Bitmap lastCroppedPic)
        {
            Bitmap bRet = null;

            BitmapBorderAction b = BitmapBorderAction.Feather;
            double[] saturation = null;
            BitmapOutlineVariant bov = BitmapOutlineVariant.All;

            bRet = GetOutline(bmp, alpha.Length, b, saturation, alpha, bov, setLastCroppedBitmap, lastCroppedPic);

            using (Graphics gx = Graphics.FromImage(bmp))
            {
                gx.Clear(Color.Transparent);
                gx.DrawImage(bRet, 0, 0);
            }

            if (bRet != null)
                bRet.Dispose();
            bRet = null;
        }

        public Bitmap GetOutline(Bitmap bmp, int breite, BitmapBorderAction bba, double[] saturation, double[] alpha, BitmapOutlineVariant bov, bool setLastCroppedPic, Bitmap lastCroppedPic)
        {
            Bitmap b = null;
            Bitmap b2 = null;
            BitArray fbits = null;

            if (saturation == null && alpha == null)
                throw new Exception("No values defined.");

            if (saturation != null && saturation.Length < breite)
                throw new Exception("Values of wrong size defined");

            if (alpha != null && alpha.Length < breite)
                throw new Exception("Values of wrong size defined");

            try
            {
                b = (Bitmap)bmp.Clone();
                b2 = new Bitmap(bmp.Width, bmp.Height);
                int prg = 0;

                for (int i = 0; i < breite; i++)
                {
                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                        break;

                    prg = (int)((double)(i) * 100.0 / (breite - 1));
                    ChainFinder cf = new ChainFinder();
                    cf.AllowNullCells = true;

                    if (bov == BitmapOutlineVariant.All)
                    {
                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        if (bba == BitmapBorderAction.Desaturate)
                            DrawOutlineToBmp(b2, b, fList, bba, saturation[i], 0);

                        if (bba == BitmapBorderAction.Feather)
                            DrawOutlineToBmp(b2, b, fList, bba, 0, alpha[i]);

                        if (bba == BitmapBorderAction.DesaturateAndFeather)
                            DrawOutlineToBmp(b2, b, fList, bba, saturation[i], alpha[i]);

                        if (this.BGW != null && this.BGW.WorkerReportsProgress)
                            this.BGW.ReportProgress(prg);

                        RemoveOutline(b, fList);
                    }
                    else
                    {
                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        if (bba == BitmapBorderAction.Desaturate)
                            DrawOutlineToBmp(b2, b, fList, bba, saturation[i], 0);

                        if (bba == BitmapBorderAction.Feather)
                            DrawOutlineToBmp(b2, b, fList, bba, 0, alpha[i]);

                        if (bba == BitmapBorderAction.DesaturateAndFeather)
                            DrawOutlineToBmp(b2, b, fList, bba, saturation[i], alpha[i]);

                        if (this.BGW != null && this.BGW.WorkerReportsProgress)
                            this.BGW.ReportProgress(prg);

                        RemoveOutline(b, fList);
                    }
                }

                if (setLastCroppedPic && lastCroppedPic != null)
                {
                    using (Graphics g = Graphics.FromImage(lastCroppedPic))
                        g.DrawImage(b, 0, 0, b.Width, b.Height);
                }

                DrawRemainingBitmap(b, b2);

                if (b != null)
                    b.Dispose();

                b = null;

                return b2;
            }
            catch
            {
                if (fbits != null)
                    fbits = null;
            }

            if (b != null)
                b.Dispose();

            b = null;

            return null;
        }

        private void DrawRemainingBitmap(Bitmap b, Bitmap b2)
        {
            using (Graphics g = Graphics.FromImage(b2))
                g.DrawImage(b, 0, 0, b.Width, b.Height);
        }

        private void DrawOutlineToBmp(Bitmap b, Bitmap orig, List<ChainCode> fList, BitmapBorderAction bba, double saturation, double alpha)
        {
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                               ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            System.IntPtr Scan0 = bmData.Scan0;

            BitmapData bmDataOrig = orig.LockBits(new Rectangle(0, 0, orig.Width, orig.Height),
                   ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            System.IntPtr Scan0Orig = bmDataOrig.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                byte* pOrig = (byte*)(void*)Scan0Orig;

                int stride = bmData.Stride;

                if (fList != null && fList.Count > 0)
                {
                    foreach (ChainCode c in fList)
                    {
                        for (int i = 0; i < c.Coord.Count; i++)
                        {
                            int x = c.Coord[i].X;
                            int y = c.Coord[i].Y;

                            p[y * stride + x * 4] = pOrig[y * stride + x * 4];
                            p[y * stride + x * 4 + 1] = pOrig[y * stride + x * 4 + 1];
                            p[y * stride + x * 4 + 2] = pOrig[y * stride + x * 4 + 2];
                            p[y * stride + x * 4 + 3] = pOrig[y * stride + x * 4 + 3];

                            if (bba == BitmapBorderAction.Feather || bba == BitmapBorderAction.DesaturateAndFeather)
                                p[y * stride + x * 4 + 3] = (byte)((double)pOrig[y * stride + x * 4 + 3] * (alpha / 255.0));

                            if (bba == BitmapBorderAction.Desaturate || bba == BitmapBorderAction.DesaturateAndFeather)
                            {
                                ColorCurves.HSLData px = ColorCurves.fipbmp.RGBtoHSL((int)pOrig[y * stride + x * 4 + 2], (int)pOrig[y * stride + x * 4 + 1], (int)pOrig[y * stride + x * 4]);

                                double val = saturation / 255.0;

                                ColorCurves.PixelData nPixel = ColorCurves.fipbmp.HSLtoRGB(px.Hue, px.Saturation * val, px.Luminance);

                                p[y * stride + x * 4] = nPixel.blue;
                                p[y * stride + x * 4 + 1] = nPixel.green;
                                p[y * stride + x * 4 + 2] = nPixel.red;
                            }
                        }
                    }
                }
            }

            b.UnlockBits(bmData);
            orig.UnlockBits(bmDataOrig);
        }

        public void RemoveOutline(Bitmap b, List<ChainCode> fList)
        {
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                               ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            System.IntPtr Scan0 = bmData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                if (fList != null && fList.Count > 0)
                {
                    foreach (ChainCode c in fList)
                    {
                        for (int i = 0; i < c.Coord.Count; i++)
                        {
                            int x = c.Coord[i].X;
                            int y = c.Coord[i].Y;

                            p[y * stride + x * 4 + 3] = (byte)0;
                        }
                    }
                }
            }

            b.UnlockBits(bmData);
        }

        private unsafe Bitmap Subtract(Bitmap bTmp, Bitmap bTmp2)
        {
            int w = bTmp.Width;
            int h = bTmp.Height;

            BitmapData bmData = bTmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmData2 = bTmp2.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            Parallel.For(0, h, y =>
            {
                byte* p = (byte*)bmData.Scan0;
                byte* p2 = (byte*)bmData2.Scan0;

                p += y * stride;
                p2 += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (p[3] > 0 && p2[3] > 0)
                        p[3] = 0;

                    p += 4;
                    p2 += 4;
                }
            });

            bTmp.UnlockBits(bmData);
            bTmp2.UnlockBits(bmData2);

            return new Bitmap(bTmp);
        }

        private void Fip_ProgressPlus(object sender, ConvolutionLib.ProgressEventArgs e)
        {
            this.BGW.ReportProgress((int)e.CurrentProgress);
        }

        public Bitmap ExtOutline(Bitmap bmp, Bitmap bOrig, int breite, System.ComponentModel.BackgroundWorker bgw)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L))
            {
                Bitmap b = null;

                try
                {
                    b = (Bitmap)bmp.Clone();

                    if (breite == 0)
                        return b;

                    for (int i = 0; i < breite; i++)
                    {
                        if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                            break;

                        ChainFinder cf = new ChainFinder();
                        cf.AllowNullCells = true;

                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        cf.ExtendOutline(b, fList);
                    }

                    ChainFinder cf2 = new ChainFinder();
                    cf2.AllowNullCells = true;

                    List<ChainCode> fList2 = cf2.GetOutline(b, 0, false, 0, false);

                    fList2 = fList2.OrderByDescending(a => a.Coord.Count).ToList();

                    using (Graphics gx = Graphics.FromImage(b))
                    {
                        gx.Clear(Color.Transparent);

                        foreach (ChainCode c in fList2)
                        {
                            if (!ChainFinder.IsInnerOutline(c))
                                using (GraphicsPath gP = new GraphicsPath())
                                {
                                    gP.FillMode = FillMode.Winding;
                                    PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                    gP.AddLines(pts);
                                    gP.CloseAllFigures();

                                    using (TextureBrush tb = new TextureBrush(bOrig))
                                    {
                                        gx.FillPath(tb, gP);
                                        using (Pen p = new Pen(tb, 1))
                                            gx.DrawPath(p, gP);
                                    }
                                }
                        }
                    }

                    foreach (ChainCode cc in fList2)
                    {
                        if (ChainFinder.IsInnerOutline(cc))
                            using (GraphicsPath gP = new GraphicsPath())
                            {
                                try
                                {
                                    gP.StartFigure();
                                    PointF[] pts = cc.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                    gP.AddLines(pts);

                                    using (Graphics gx = Graphics.FromImage(bOrig))
                                    {
                                        gx.CompositingMode = CompositingMode.SourceCopy;
                                        gx.FillPath(Brushes.Transparent, gP);
                                    }
                                }
                                catch (Exception exc)
                                {
                                    Console.WriteLine(exc.ToString());
                                }
                            }
                    }

                    return b;
                }
                catch
                {
                    if (b != null)
                        b.Dispose();

                    b = null;
                }
            }

            return null;
        }

        private int GetAlphaBaseValue(int i, int cnt)
        {
            int rCnt = cnt + 1;
            return (int)(this._alphaStartValue / rCnt * (cnt - i - 1));
        }

        private List<ChainCode> GetBoundary(Bitmap bmp)
        {
            ChainFinder cf = new ChainFinder();
            cf.AllowNullCells = true;
            List<ChainCode> l = cf.GetOutline(bmp, 0, false, 0, false, 0, false);
            return l;
        }

        private List<ChainCode> GetBoundary(Bitmap upperImg, int minAlpha, bool grayScale)
        {
            List<ChainCode> l = null;
            if (upperImg != null)
            {
                Bitmap bmpTmp = null;
                try
                {
                    if (AvailMem.AvailMem.checkAvailRam(upperImg.Width * upperImg.Height * 4L))
                        bmpTmp = new Bitmap(upperImg);
                    else
                        throw new Exception("Not enough memory.");
                    int nWidth = bmpTmp.Width;
                    int nHeight = bmpTmp.Height;
                    ChainFinder cf = new ChainFinder();
                    lock (this._lockObject)
                        l = cf.GetOutline(bmpTmp, nWidth, nHeight, minAlpha, grayScale, 0, false, 0, false);
                }
                catch (Exception exc)
                {
                    OnBoundaryError(exc.Message);
                }
                finally
                {
                    if (bmpTmp != null)
                    {
                        bmpTmp.Dispose();
                        bmpTmp = null;
                    }
                }
            }
            return l;
        }

        private List<ChainCode> GetBoundary(Bitmap upperImg, ChainFinder cf, int minAlpha, bool grayScale)
        {
            List<ChainCode> l = null;
            cf.Reset();
            if (upperImg != null)
            {
                Bitmap bmpTmp = null;
                try
                {
                    if (AvailMem.AvailMem.checkAvailRam(upperImg.Width * upperImg.Height * 4L))
                        bmpTmp = new Bitmap(upperImg);
                    else
                        throw new Exception("Not enough memory.");
                    int nWidth = bmpTmp.Width;
                    int nHeight = bmpTmp.Height;
                    lock (this._lockObject)
                        l = cf.GetOutline(bmpTmp, nWidth, nHeight, minAlpha, grayScale, 0, false, 0, false);
                }
                catch (Exception exc)
                {
                    OnBoundaryError(exc.Message);
                }
                finally
                {
                    if (bmpTmp != null)
                    {
                        bmpTmp.Dispose();
                        bmpTmp = null;
                    }
                }
            }
            return l;
        }

        private void OnBoundaryError(string message)
        {
            BoundaryError?.Invoke(this, message);
        }

        private PointF GetNormal(List<Point> coord, int i, int lengthToCheck)
        {
            int st = (coord.Count + i - lengthToCheck) % coord.Count;
            int ed = (i + lengthToCheck) % coord.Count;

            if (st > -1 && ed > -1)
            {
                PointF ptL = coord[st];
                PointF ptR = coord[ed];

                double dx = ptR.X - ptL.X;
                double dy = ptR.Y - ptL.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist > 0)
                {
                    dx /= dist;
                    dy /= dist;
                }

                return new PointF((float)(dy), (float)(-dx)); // turn this way
            }

            return new PointF();
        }

        public void Dispose()
        {
            if (this.BmpWork != null)
            {
                this.BmpWork.Dispose();
                this.BmpWork = null;
            }
            if (this.BmpOrig != null)
            {
                this.BmpOrig.Dispose();
                this.BmpOrig = null;
            }
            if (this._lastCroppedPic != null)
            {
                this._lastCroppedPic.Dispose();
                this._lastCroppedPic = null;
            }
        }

        //I dont know, if this is of any practical use, I usually use Bayes-Matting for trimaps with a thin/narrow unknown region,
        //but I wasn't sure, if posting Bayes-Matting-Code is allowed. There's example code on GitHub though.
        //So I wrote some lines from an older Idea, to remove some outliers and get something like a matting effect.
        //Motto was: Just "do something" with the values and the mean and the stddev...
        //Maybe I'll add some real alpha-matting code, if I find the time to do so...
        public Bitmap ExperimentalOutlineProc(Bitmap trWork, int iW, int oW, int windowSize, double gamma, double gamma2, int normalDistToCheck, BackgroundWorker bgw)
        {
            Bitmap fg = this.BmpWork;
            Bitmap bOrig = this.BmpOrig;

            byte[,] distances = new byte[fg.Width, fg.Height];
            int distI = iW;
            int distO = oW;
            int distA = distI + distO;

            //distance of reference window center to the currently examined point in the trimap
            int windowShift = distA + 1;
            //field of elementary shifts ("normals")
            PointF[,] normalField = new PointF[fg.Width, fg.Height];

            //prepare outer parts
            Bitmap fgC = new Bitmap(fg);
            for (int i = 0; i < distO + 1; i++) //get a one pixel wide boundary around all drawn parts, therefore distO + 1
            {
                Bitmap bTmp4 = ExtOutline(fgC, bOrig, 1, bgw);

                Bitmap bOld2 = fgC;
                fgC = bTmp4;
                if (bOld2 != null)
                {
                    bOld2.Dispose();
                    bOld2 = null;
                }

                List<ChainCode> c = GetBoundary(fgC);

                foreach (ChainCode cc in c)
                {
                    List<Point> pts = cc.Coord;

                    int ii = 0;
                    foreach (Point pt in pts)
                    {
                        distances[pt.X, pt.Y] = (byte)(i + distI + 1);
                        PointF n = GetNormal(pts, ii, normalDistToCheck);

                        double f = 1.0;
                        if (Math.Abs(n.X) > Math.Abs(n.Y))
                            f = 1.0 / Math.Abs(n.X);
                        else
                            f = 1.0 / Math.Abs(n.Y);

                        normalField[pt.X, pt.Y] = new PointF((float)(n.X * f), (float)(n.Y * f));

                        ii++;
                    }
                }
            }

            fgC.Dispose();
            fgC = null;

            //prepare img outline
            List<ChainCode> c2 = GetBoundary(fg);

            foreach (ChainCode cc in c2)
            {
                List<Point> pts = cc.Coord;

                int ii = 0;
                foreach (Point pt in pts)
                {
                    distances[pt.X, pt.Y] = (byte)(distI + 1);

                    PointF n = GetNormal(pts, ii, normalDistToCheck);

                    double f = 1.0;
                    if (Math.Abs(n.X) > Math.Abs(n.Y))
                        f = 1.0 / Math.Abs(n.X);
                    else
                        f = 1.0 / Math.Abs(n.Y);

                    normalField[pt.X, pt.Y] = new PointF((float)(n.X * f), (float)(n.Y * f));

                    ii++;
                }
            }

            //prepare inner parts
            fgC = new Bitmap(fg);
            for (int i = 0; i < distI; i++)
            {
                Bitmap bTmp4 = RemOutline(fgC, 1, bgw);

                Bitmap bOld2 = fgC;
                fgC = bTmp4;
                if (bOld2 != null)
                {
                    bOld2.Dispose();
                    bOld2 = null;
                }

                List<ChainCode> c = GetBoundary(fgC);

                foreach (ChainCode cc in c)
                {
                    List<Point> pts = cc.Coord;

                    int ii = 0;
                    foreach (Point pt in pts)
                    {
                        distances[pt.X, pt.Y] = (byte)(distI - i);

                        PointF n = GetNormal(pts, ii, normalDistToCheck);

                        double f = 1.0;
                        if (Math.Abs(n.X) > Math.Abs(n.Y))
                            f = 1.0 / Math.Abs(n.X);
                        else
                            f = 1.0 / Math.Abs(n.Y);

                        normalField[pt.X, pt.Y] = new PointF((float)(n.X * f), (float)(n.Y * f));

                        ii++;
                    }
                }
            }

            fgC.Dispose();
            fgC = null;

            int w = bOrig.Width;
            int h = bOrig.Height;

            //check windowSize is odd
            if ((windowSize & 0x01) != 1)
                windowSize++;

            int h2 = windowSize / 2;

            List<double> alpha = GetVector(distA * 2 + 1).ToList();
            alpha.Insert(0, 0);
            alpha.Add(0);

            int alphaH = alpha.Count / 2;

            BitmapData bmData = bOrig.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmFG = fg.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmTr = trWork.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            unsafe
            {
                byte* p = (byte*)bmData.Scan0;
                byte* pFG = (byte*)bmFG.Scan0;
                byte* pTr = (byte*)bmTr.Scan0;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        //compute the shift distances
                        int winshiftX = (int)(normalField[x, y].X * windowShift);
                        int winshiftY = (int)(normalField[x, y].Y * windowShift);

                        if (pTr[x * 4 + y * stride] > 5 && pTr[x * 4 + y * stride] < 250)
                        {
                            //setup the arrays for each color channel
                            int[,] resFGB = new int[windowSize, windowSize];
                            int[,] resBGB = new int[windowSize, windowSize];
                            int[,] resFGG = new int[windowSize, windowSize];
                            int[,] resBGG = new int[windowSize, windowSize];
                            int[,] resFGR = new int[windowSize, windowSize];
                            int[,] resBGR = new int[windowSize, windowSize];

                            int maxFGB = 0;
                            int maxFGG = 0;
                            int maxFGR = 0;
                            int maxBGB = 0;
                            int maxBGG = 0;
                            int maxBGR = 0;

                            //fill the arrays
                            for (int yy = y - h2 + winshiftY, y4 = 0; yy < y + h2 + winshiftY; yy++, y4++)
                            {
                                if (yy >= 0 && yy < h && y4 >= 0 && y4 < windowSize)
                                    for (int xx = x - h2 + winshiftX, x4 = 0; xx < x + h2 + winshiftX; xx++, x4++)
                                        if (xx >= 0 && xx < w && x4 >= 0 && x4 < windowSize)
                                        {
                                            resFGB[x4, y4] = (int)p[xx * 4 + yy * stride];
                                            resFGG[x4, y4] = (int)p[xx * 4 + yy * stride + 1];
                                            resFGR[x4, y4] = (int)p[xx * 4 + yy * stride + 2];

                                            if (resFGB[x4, y4] > maxFGB)
                                                maxFGB = resFGB[x4, y4];
                                            if (resFGG[x4, y4] > maxFGG)
                                                maxFGG = resFGG[x4, y4];
                                            if (resFGR[x4, y4] > maxFGR)
                                                maxFGR = resFGR[x4, y4];
                                        }
                            }

                            for (int yy = y - h2 - winshiftY, y4 = 0; yy < y + h2 - winshiftY; yy++, y4++)
                            {
                                if (yy >= 0 && yy < h && y4 >= 0 && y4 < windowSize)
                                    for (int xx = x - h2 - winshiftX, x4 = 0; xx < x + h2 - winshiftX; xx++, x4++)
                                        if (xx >= 0 && xx < w && x4 >= 0 && x4 < windowSize)
                                        {
                                            resBGB[x4, y4] = (int)p[xx * 4 + yy * stride];
                                            resBGG[x4, y4] = (int)p[xx * 4 + yy * stride + 1];
                                            resBGR[x4, y4] = (int)p[xx * 4 + yy * stride + 2];

                                            if (resBGB[x4, y4] > maxBGB)
                                                maxBGB = resBGB[x4, y4];
                                            if (resBGG[x4, y4] > maxBGG)
                                                maxBGG = resBGG[x4, y4];
                                            if (resBGR[x4, y4] > maxBGR)
                                                maxBGR = resBGR[x4, y4];
                                        }
                            }

                            //get the means
                            double muFGB = resFGB.Cast<int>().Average();
                            double muFGG = resFGG.Cast<int>().Average();
                            double muFGR = resFGR.Cast<int>().Average();
                            double muBGB = resBGB.Cast<int>().Average();
                            double muBGG = resBGG.Cast<int>().Average();
                            double muBGR = resBGR.Cast<int>().Average();

                            //variances
                            double varFGB = 0;
                            double varFGG = 0;
                            double varFGR = 0;
                            double varBGB = 0;
                            double varBGG = 0;
                            double varBGR = 0;

                            int n = windowSize * windowSize;

                            //maybe check "value exists" by preiniting every dp with a significant number, and checking for divergence to it
                            for (int yy = 0; yy < windowSize; yy++)
                                for (int xx = 0; xx < windowSize; xx++)
                                {
                                    varFGB += Math.Pow(resFGB[xx, yy] - muFGB, 2);
                                    varFGG += Math.Pow(resFGG[xx, yy] - muFGG, 2);
                                    varFGR += Math.Pow(resFGR[xx, yy] - muFGR, 2);
                                    varBGB += Math.Pow(resBGB[xx, yy] - muBGB, 2);
                                    varBGG += Math.Pow(resBGG[xx, yy] - muBGG, 2);
                                    varBGR += Math.Pow(resBGR[xx, yy] - muBGR, 2);
                                }

                            varFGB /= n;
                            varFGG /= n;
                            varFGR /= n;
                            varBGB /= n;
                            varBGG /= n;
                            varBGR /= n;

                            //put all into an object (for probable later use, this algorithm will change a bit in next time)
                            WindowVal wv = new WindowVal()
                            {
                                MuFGB = muFGB,
                                MuFGG = muFGG,
                                MuFGR = muFGR,
                                MuBGB = muBGB,
                                MuBGG = muBGG,
                                MuBGR = muBGR,

                                VarFGB = varFGB,
                                VarFGG = varFGG,
                                VarFGR = varFGR,
                                VarBGB = varBGB,
                                VarBGG = varBGG,
                                VarBGR = varBGR,

                                StdFGB = Math.Sqrt(varFGB),
                                StdFGG = Math.Sqrt(varFGG),
                                StdFGR = Math.Sqrt(varFGR),
                                StdBGB = Math.Sqrt(varBGB),
                                StdBGG = Math.Sqrt(varBGG),
                                StdBGR = Math.Sqrt(varBGR),

                                Pt = new Point(x, y)
                            };

                            int blue = p[x * 4 + y * stride];
                            int green = p[x * 4 + y * stride + 1];
                            int red = p[x * 4 + y * stride + 2];

                            //get the ccolor distances and the fg dist to the averaged StdDeviations
                            double wvB = wv.MuFGB;
                            double wvG = wv.MuFGG;
                            double wvR = wv.MuFGR;

                            double dF = Math.Sqrt((blue - wvB) * (blue - wvB)
                                + (green - wvG) * (green - wvG)
                                + (red - wvR) * (red - wvR));

                            double dFMax = Math.Sqrt((blue - maxFGB) * (blue - maxFGB)
                                + (green - maxFGG) * (green - maxFGG)
                                + (red - maxFGR) * (red - maxFGR));

                            //double vF = Math.Abs(dF - ((wv.StdFGB + wv.StdFGG + wv.StdFGR) / 3));

                            double wvB2 = wv.MuBGB;
                            double wvG2 = wv.MuBGG;
                            double wvR2 = wv.MuBGR;

                            //double dB = Math.Sqrt((blue - wvB2) * (blue - wvB2)
                            //    + (green - wvG2) * (green - wvG2)
                            //    + (red - wvR2) * (red - wvR2));

                            double dBMax = Math.Sqrt((blue - maxBGB) * (blue - maxBGB)
                                + (green - maxBGG) * (green - maxBGG)
                                + (red - maxBGR) * (red - maxBGR));

                            //double vB = Math.Abs(dB - ((wv.StdBGB + wv.StdBGG + wv.StdBGR) / 3));

                            //double d = dF + dB;
                            double dMax = dFMax + dBMax;

                            //if (dF - ((wv.StdFGB + wv.StdFGG + wv.StdFGR) / 3) < 0)
                            //    vF = 0;

                            //get alpha
                            //double aDF2 = (441.67 - dF) / 441.67;
                            //double aDB = (441.67 - dB) / 441.67;
                            //double aDF = 1.0 - vF / d;

                            double aDF = 1.0 - Math.Pow((dFMax + dF) / (dMax * ((distA + 1) - distances[x, y])), gamma);
                            //aDF /= 2.0;
                            aDF *= Math.Pow((double)(distA - distances[x, y]) / (double)distA, gamma2);

                            pFG[x * 4 + y * stride] = p[x * 4 + y * stride];
                            pFG[x * 4 + y * stride + 1] = p[x * 4 + y * stride + 1];
                            pFG[x * 4 + y * stride + 2] = p[x * 4 + y * stride + 2];
                            pFG[x * 4 + y * stride + 3] = (byte)Math.Max(Math.Min(aDF * 255, 255), 0);

                            //todo: nachbearbeitung von innerpaths
                        }
                    }
                }

                //reshift some values to full opacity, gamma correct the rest
                //Parallel.For(0, h, y =>
                //{
                //    byte* pF = (byte*)bmFG.Scan0;
                //    pF += y * stride;

                //    for (int x = 0; x < w; x++)
                //    {
                //        if (pTr[x * 4 + y * stride] > 5 && pTr[x * 4 + y * stride] < 250)
                //        {
                //            if (pF[3] > alphaTh)
                //                pF[3] = 255;
                //            else
                //            {
                //                pF[3] = (byte)Math.Max(Math.Min(alphaTh * Math.Pow((double)pF[3] / alphaTh, 2.5), 255), 0);
                //            }
                //        }
                //        pF += 4;
                //    }
                //});
            }

            bOrig.UnlockBits(bmData);
            fg.UnlockBits(bmFG);
            trWork.UnlockBits(bmTr);

            //feather
            //Feather(fg, featherWidth /*Math.Max(distA / 4, 1)*/);

            //Form fff = new Form();
            //fff.BackgroundImage = fg;
            //fff.BackgroundImageLayout = ImageLayout.Zoom;
            //fff.ShowDialog();

            Bitmap bRes = new Bitmap(fg);

            fg.Dispose();
            bOrig.Dispose();
            trWork.Dispose();

            return bRes;
        }

        public Bitmap RemOutline(Bitmap bmp, int breite, System.ComponentModel.BackgroundWorker bgw)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L))
            {
                Bitmap b = null;
                BitArray fbits = null;

                try
                {
                    b = (Bitmap)bmp.Clone();

                    for (int i = 0; i < breite; i++)
                    {
                        if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                            break;

                        ChainFinder cf = new ChainFinder();
                        cf.AllowNullCells = true;

                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        cf.RemoveOutline(b, fList);

                        fbits = null;
                    }

                    return b;
                }
                catch
                {
                    if (fbits != null)
                        fbits = null;
                    if (b != null)
                        b.Dispose();

                    b = null;
                }
            }

            return null;
        }
    }
}
