using ChainCodeFinder;
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
    public class DefaultSmoothenOP : IDisposable
    {
        public Bitmap Bmp { get; private set; }
        public Bitmap BmpWork { get; private set; }
        public Bitmap BmpOrig { get; private set; }
        public bool cbPPRemoveChecked { get; set; }
        public double numPPEpsilonValue { get; set; }
        public int numPPRemove2Value { get; set; }

        private bool cbApproxLinesChecked;

        public int numPPRemoveValue { get; set; }
        public double numPPEpsilon2Value { get; set; }
        public BackgroundWorker BGW { get; set; }

        public event EventHandler<string> ShowInfo;
        public event EventHandler<string> BoundaryError;

        private object _lockObject = new object();

        public DefaultSmoothenOP(Bitmap bmp, Bitmap bmpOrig)
        {
            this.Bmp = new Bitmap(bmp);
            this.BmpWork = new Bitmap(bmp);
            this.BmpOrig = new Bitmap(bmpOrig);
        }
        public DefaultSmoothenOP(Bitmap bmp)
        {
            this.Bmp = new Bitmap(bmp);
            this.BmpWork = new Bitmap(bmp);
            this.BmpOrig = new Bitmap(bmp);
        }

        public void Init(double numPPEpsilonValue, double numPPEpsilon2Value, bool cbPPRemoveChecked,
            int numPPRemoveValue, int numPPRemove2Value, bool approxLines)
        {
            this.numPPEpsilonValue = numPPEpsilonValue;
            this.numPPEpsilon2Value = numPPEpsilon2Value;
            this.cbPPRemoveChecked = cbPPRemoveChecked;
            this.numPPRemoveValue = numPPRemoveValue;
            this.numPPRemove2Value = numPPRemove2Value;
            this.cbApproxLinesChecked = approxLines;
        }

        public unsafe void ComputeOutlineInfo()
        {
            if (this.Bmp != null && this.BmpWork != null)
            {
                ShowInfo?.Invoke(this, "Computing Outline Info...");

                List<ChainCode> c = GetBoundary(this.Bmp, 0, false);
                c = c.OrderByDescending(x => x.Coord.Count).ToList();

                List<Point> outerPoints = new List<Point>();

                int w = this.Bmp.Width;
                int h = this.Bmp.Height;

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(20);

                //part 1: get points around outline
                BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmData.Stride;

                int[] add = new int[4] { 4, -4, stride, -stride };

                byte* p = (byte*)bmData.Scan0;

                foreach (ChainCode cc in c)
                {
                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                        break;

                    if (cc.Coord.Count >= 4)
                    {
                        List<Point> pts = cc.Coord;

                        for (int i = 0; i < pts.Count; i++)
                        {
                            int addr = pts[i].X * 4 + pts[i].Y * stride;

                            for (int j = 0; j < add.Length; j++)
                            {
                                int x = (addr + add[j]) % stride / 4;
                                int y = (addr + add[j]) / stride;

                                if (x >= 0 && x < w && y >= 0 && y < h)
                                {
                                    if (p[addr + add[j] + 3] == 0)
                                        outerPoints.Add(new Point(x, y)); //Get Points unsorted
                                }
                            }
                        }
                    }
                }

                outerPoints = outerPoints.Distinct().ToList();
                Dictionary<int, List<Point>> definingPts = new Dictionary<int, List<Point>>();

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(40);

                //part 2: Get Points to draw to
                p = (byte*)bmData.Scan0;

                for (int i = 0; i < outerPoints.Count; i++)
                {
                    int addr = outerPoints[i].X * 4 + outerPoints[i].Y * stride;

                    for (int j = 0; j < add.Length; j++)
                    {
                        int x = (addr + add[j]) % stride / 4;
                        int y = (addr + add[j]) / stride;

                        if (x >= 0 && x < w && y >= 0 && y < h)
                        {
                            if (p[addr + add[j] + 3] > 0)
                            {
                                if (definingPts.ContainsKey(addr))
                                    definingPts[addr].Add(new Point(x, y));
                                else
                                {
                                    definingPts.Add(addr, new List<Point>());
                                    definingPts[addr].Add(new Point(x, y));
                                }
                            }
                        }
                    }
                }

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(60);

                //part 3: Write colors to bmpWork
                BitmapData bmData2 = this.BmpWork.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                int stride2 = bmData.Stride;

                byte* p2 = (byte*)bmData2.Scan0;

                foreach (int address in definingPts.Keys)
                {
                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                        break;

                    for (int i = 0; i < definingPts[address].Count; i++)
                    {
                        if (definingPts[address] != null && definingPts[address].Count > 0)
                        {
                            int[] values = new int[4];
                            int addr = definingPts[address][i].X * 4 + definingPts[address][i].Y * stride2;

                            for (int j = 0; j < values.Length; j++)
                                values[j] += p[addr + j];

                            p2[address] = (byte)(values[0] / definingPts[address].Count);
                            p2[address + 1] = (byte)(values[1] / definingPts[address].Count);
                            p2[address + 2] = (byte)(values[2] / definingPts[address].Count);
                            p2[address + 3] = (byte)(values[3] / definingPts[address].Count);
                        }
                    }
                }

                this.Bmp.UnlockBits(bmData);
                this.BmpWork.UnlockBits(bmData2);

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);
            }
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

        public Bitmap ComputePic(bool redrawShifted, float redrawShift, bool drawCurves, float tension, bool useYNeg = false, float penSize = 0.5f)
        {
            if (this.Bmp != null && this.BmpWork != null)
            {
                ShowInfo?.Invoke(this, "Computing Pic... part1");

                Bitmap bOut = new Bitmap(this.Bmp.Width, this.Bmp.Height);
                Bitmap bWork = new Bitmap(this.Bmp);

                ChainFinder cf = new ChainFinder();
                List<ChainCode> c = GetBoundary(this.Bmp, cf, 0, false);
                c = c.OrderByDescending(x => x.Coord.Count).ToList();

                if (this.cbPPRemoveChecked)
                {
                    //removeOuntline
                    Bitmap bTmp = RemoveOutline(bWork, this.Bmp, Math.Max((int)this.numPPRemoveValue, 1), true, true);

                    Bitmap bOld = bWork;
                    bWork = bTmp;
                    if (bOld != null)
                    {
                        bOld.Dispose();
                        bOld = null;
                    }

                    c = GetBoundary(bWork, 0, false);
                    c = c.OrderByDescending(x => x.Coord.Count).ToList();
                }

                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    return null;

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(20);

                double epsilon = (double)this.numPPEpsilonValue;

                if (redrawShifted)
                {
                    ShowInfo?.Invoke(this, "Computing Pic... part2");
                    using (Bitmap bTmpRS = new Bitmap(this.BmpWork.Width, this.BmpWork.Height))
                    {
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            using (Graphics gx = Graphics.FromImage(bTmpRS))
                            {
                                gx.SmoothingMode = SmoothingMode.None;
                                gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                gx.PixelOffsetMode = PixelOffsetMode.None;

                                foreach (ChainCode cc in c)
                                {
                                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                                        break;

                                    bool isInner = ChainFinder.IsInnerOutline(cc);

                                    if (!isInner)
                                    {
                                        if (cc.Coord.Count > 4 && Math.Abs(cc.Area) > 1)
                                        {
                                            gP.StartFigure();
                                            gP.AddLines(cc.Coord.ToArray());
                                            gx.FillPath(Brushes.Black, gP);
                                        }
                                        else
                                            gx.FillRectangle(Brushes.Black, new Rectangle(cc.Coord[0].X, cc.Coord[0].Y, 1, 1));
                                    }
                                }
                            }

                            Invert(bTmpRS);

                            using (Graphics gx = Graphics.FromImage(bTmpRS))
                                gx.DrawImage(bTmpRS, redrawShift, redrawShift);

                            Invert(bTmpRS);

                            ShowInfo?.Invoke(this, "Computing Pic... part3");

                            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                                this.BGW.ReportProgress(40);

                            //get all inner outlines from (removed px) orig and reset transp
                            using (Bitmap bmpResTrnsp = new Bitmap(bWork.Width, bWork.Height))
                            {
                                using (Graphics gx = Graphics.FromImage(bmpResTrnsp))
                                {
                                    gx.SmoothingMode = SmoothingMode.None;
                                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                    gx.PixelOffsetMode = PixelOffsetMode.None;

                                    using (GraphicsPath gP2 = new GraphicsPath())
                                    {
                                        foreach (ChainCode cc in c)
                                        {
                                            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                                                break;

                                            bool isInner = ChainFinder.IsInnerOutline(cc);

                                            if (isInner)
                                            {
                                                gP2.StartFigure();
                                                gP2.AddLines(cc.Coord.ToArray());
                                                gx.FillPath(Brushes.Lime, gP2);
                                            }
                                        }

                                        ResetTransp(bTmpRS, bWork, bmpResTrnsp);
                                    }
                                }
                            }

                            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                                this.BGW.ReportProgress(60);

                            c = GetBoundary(bTmpRS, cf, 0, false);
                            c = c.OrderByDescending(x => x.Coord.Count).ToList();

                            ShowInfo?.Invoke(this, "Computing Pic... part4");

                            foreach (ChainCode cc in c)
                            {
                                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                                    break;

                                bool isInner = ChainFinder.IsInnerOutline(cc);
                                List<Point> pts = cc.Coord;

                                if (!isInner)
                                {
                                    if (this.cbApproxLinesChecked)
                                    {
                                        pts = cf.RemoveColinearity(pts, true, 4);
                                        pts = cf.ApproximateLines(pts, epsilon);
                                        pts = cf.RemoveColinearity(pts, true, 4);

                                        cc.Coord = pts;
                                    }

                                    using (Graphics gx = Graphics.FromImage(bOut))
                                    {
                                        gx.CompositingQuality = CompositingQuality.HighQuality;
                                        gx.SmoothingMode = SmoothingMode.AntiAlias;
                                        gx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                        gx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                                        using (TextureBrush tb = new TextureBrush(this.BmpOrig))
                                        {
                                            if (cc.Coord.Count > 4 && Math.Abs(cc.Area) > 1)
                                            {
                                                using (GraphicsPath gP2 = new GraphicsPath())
                                                {
                                                    if (drawCurves)
                                                        gP2.AddClosedCurve(cc.Coord.ToArray(), tension);
                                                    else
                                                        gP2.AddLines(cc.Coord.ToArray());

                                                    gx.FillPath(tb, gP2);
                                                }
                                            }
                                            else
                                                gx.FillRectangle(tb, new Rectangle(cc.Coord[0].X, cc.Coord[0].Y, 1, 1));
                                        }
                                    }
                                }
                            }

                            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                                this.BGW.ReportProgress(80);
                        }
                    }
                }
                else
                {
                    ShowInfo?.Invoke(this, "Computing Pic... part2");

                    if (this.BGW != null && this.BGW.WorkerReportsProgress)
                        this.BGW.ReportProgress(20);

                    foreach (ChainCode cc in c)
                    {
                        if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                            break;

                        bool isInner = ChainFinder.IsInnerOutline(cc);
                        List<Point> pts = cc.Coord;

                        if (!isInner)
                        {
                            if (this.cbApproxLinesChecked)
                            {
                                pts = cf.RemoveColinearity(pts, true, 4);
                                pts = cf.ApproximateLines(pts, epsilon);
                                pts = cf.RemoveColinearity(pts, true, 4);

                                cc.Coord = pts;
                            }

                            using (Graphics gx = Graphics.FromImage(bOut))
                            {
                                gx.CompositingQuality = CompositingQuality.HighQuality;
                                gx.SmoothingMode = SmoothingMode.AntiAlias;
                                gx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                gx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                                using (TextureBrush tb = new TextureBrush(this.BmpOrig))
                                {
                                    if (cc.Coord.Count > 4 && Math.Abs(cc.Area) > 1)
                                    {
                                        using (GraphicsPath gP = new GraphicsPath())
                                        {
                                            if (drawCurves)
                                                gP.AddClosedCurve(cc.Coord.ToArray(), tension);
                                            else
                                                gP.AddLines(cc.Coord.ToArray());

                                            gx.FillPath(tb, gP);
                                        }
                                    }
                                    else
                                        gx.FillRectangle(tb, new Rectangle(cc.Coord[0].X, cc.Coord[0].Y, 1, 1));
                                }
                            }
                        }
                    }

                    if (this.BGW != null && this.BGW.WorkerReportsProgress)
                        this.BGW.ReportProgress(80);
                }

                ShowInfo?.Invoke(this, "Computing Pic... part5");

                Bitmap bTmp2 = new Bitmap(this.BmpWork.Width, this.BmpWork.Height);
                int innerCount = 0;
                foreach (ChainCode cc in c)
                {
                    if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                        break;

                    bool isInner = ChainFinder.IsInnerOutline(cc);
                    List<Point> pts = cc.Coord;

                    if (isInner)
                    {
                        innerCount++;
                        using (Graphics gx = Graphics.FromImage(bTmp2))
                        {
                            gx.SmoothingMode = SmoothingMode.None;
                            gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                            gx.PixelOffsetMode = PixelOffsetMode.None;

                            if (cc.Coord.Count > 4 && Math.Abs(cc.Area) > 1)
                            {
                                using (GraphicsPath gP = new GraphicsPath())
                                {
                                    gP.FillMode = FillMode.Winding;
                                    gP.AddLines(cc.Coord.ToArray());
                                    gx.FillPath(Brushes.Black, gP);
                                }
                            }
                            else
                                gx.FillRectangle(Brushes.Black, new Rectangle(cc.Coord[0].X, cc.Coord[0].Y, 1, 1));
                        }
                    }
                }

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(90);

                ShowInfo?.Invoke(this, "Computing Pic... part6");

                if (innerCount > 0)
                {
                    Bitmap bTmp4 = (this.cbPPRemoveChecked) ? ExtendOutline(bTmp2, Math.Max((int)this.numPPRemove2Value, 0), true) : new Bitmap(bTmp2);

                    Bitmap bOld2 = bTmp2;
                    bTmp2 = bTmp4;
                    if (bOld2 != null)
                    {
                        bOld2.Dispose();
                        bOld2 = null;
                    }

                    ChainFinder cf2 = new ChainFinder();
                    List<ChainCode> c2 = GetBoundary(bTmp2, cf2, 0, false);
                    c2 = c2.OrderByDescending(x => x.Coord.Count).ToList();

                    double epsilon2 = (double)this.numPPEpsilon2Value;

                    using (Bitmap bOut2 = new Bitmap(this.BmpWork.Width, this.BmpWork.Height))
                    {
                        foreach (ChainCode cc in c2)
                        {
                            if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                                break;

                            bool isInner = ChainFinder.IsInnerOutline(cc);
                            List<Point> pts = cc.Coord;

                            if (!isInner)
                            {
                                if (this.cbApproxLinesChecked)
                                {
                                    pts = cf2.RemoveColinearity(pts, true, 4);
                                    pts = cf2.ApproximateLines(pts, epsilon2);
                                    pts = cf2.RemoveColinearity(pts, true, 4);

                                    cc.Coord = pts;
                                }

                                using (Graphics gx = Graphics.FromImage(bOut2))
                                {
                                    gx.CompositingQuality = CompositingQuality.HighQuality;
                                    gx.SmoothingMode = SmoothingMode.AntiAlias;
                                    gx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    gx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                                    using (TextureBrush tb = new TextureBrush(this.BmpOrig))
                                    {
                                        if (pts.Count > 4)
                                        {
                                            using (GraphicsPath gP = new GraphicsPath())
                                            {
                                                gP.FillMode = FillMode.Winding;

                                                if (drawCurves)
                                                    gP.AddClosedCurve(cc.Coord.ToArray(), tension);
                                                else
                                                    gP.AddLines(cc.Coord.ToArray());

                                                //gx.FillPath(tb, gP); //produces difference pic, if all works alright
                                                gx.FillPath(Brushes.Black, gP);

                                                //since chainfinder.approximatelines creates lines "to the left and top" of the points, we redraw shifted
                                                //I perhaps will write an overload of chainfinder.approximatelines that will create lines in outset for inner chains and inset for outer chains.
                                                if (redrawShifted)
                                                {
                                                    using (Matrix mx = new Matrix(1, 0, 0, 1, redrawShift, useYNeg ? -redrawShift : redrawShift))
                                                        gP.Transform(mx);

                                                    gx.FillPath(Brushes.Black, gP);

                                                    using (Matrix mx = new Matrix(1, 0, 0, 1, -redrawShift, useYNeg ? redrawShift : -redrawShift))
                                                        gP.Transform(mx);
                                                }

                                                if (penSize > 0.0f)
                                                    using (Pen p = new Pen(Color.Black, penSize))
                                                    {
                                                        try
                                                        {
                                                            p.LineJoin = LineJoin.Round;

                                                            //gP.Widen(p); //dont widen, it takes away too much of the picture

                                                            gx.DrawPath(p, gP);

                                                            if (redrawShifted)
                                                            {
                                                                using (Matrix mx = new Matrix(1, 0, 0, 1, redrawShift, useYNeg ? -redrawShift : redrawShift))
                                                                    gP.Transform(mx);

                                                                gx.DrawPath(p, gP);
                                                            }
                                                        }
                                                        catch { }
                                                    }
                                            }
                                        }
                                        else
                                            gx.FillRectangle(Brushes.Black, new Rectangle(cc.Coord[0].X, cc.Coord[0].Y, 1, 1));
                                    }
                                }
                            }
                        }

                        CompAlphaBased(bOut, bOut2);
                    }

                    bTmp2.Dispose();
                    bTmp2 = null;
                }

                if (this.BGW != null && this.BGW.WorkerReportsProgress)
                    this.BGW.ReportProgress(100);

                ShowInfo?.Invoke(this, "done");

                return bOut;
            }

            return null;
        }

        public Bitmap ComputePic2(bool redrawShifted, float redrawShift, bool drawCurves, float tension)
        {
            if (this.Bmp != null && this.BmpWork != null)
            {
                Bitmap bOut = new Bitmap(this.Bmp.Width, this.Bmp.Height);
                Bitmap bWork = new Bitmap(this.Bmp);

                ChainFinder cf = new ChainFinder();
                List<ChainCode> c = GetBoundary(this.Bmp, cf, 0, false);
                c = c.OrderByDescending(x => x.Coord.Count).ToList();

                if (this.cbPPRemoveChecked)
                {
                    //removeOuntline
                    Bitmap bTmp = RemoveOutline(bWork, this.Bmp, Math.Max((int)this.numPPRemoveValue, 1), true, true);

                    Bitmap bOld = bWork;
                    bWork = bTmp;
                    if (bOld != null)
                    {
                        bOld.Dispose();
                        bOld = null;
                    }

                    c = GetBoundary(bWork, 0, false);
                    c = c.OrderByDescending(x => x.Coord.Count).ToList();
                }

                if (redrawShifted)
                {
                    using (Bitmap bTmpRS = new Bitmap(this.BmpWork.Width, this.BmpWork.Height))
                    {
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            gP.FillMode = FillMode.Winding;

                            foreach (ChainCode cc in c)
                            {
                                bool isInner = ChainFinder.IsInnerOutline(cc);

                                if (isInner)
                                {
                                    if (cc.Coord.Count > 4 && Math.Abs(cc.Area) > 1)
                                    {
                                        gP.StartFigure();

                                        if (drawCurves)
                                            gP.AddClosedCurve(cc.Coord.ToArray(), tension);
                                        else
                                            gP.AddLines(cc.Coord.ToArray());
                                    }
                                    else
                                        gP.AddRectangle(new Rectangle(cc.Coord[0].X, cc.Coord[0].Y, 1, 1));
                                }
                            }

                            using (Graphics gx = Graphics.FromImage(bTmpRS))
                            {
                                gx.SmoothingMode = SmoothingMode.None;
                                gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                gx.PixelOffsetMode = PixelOffsetMode.None;

                                gx.FillPath(Brushes.Black, gP);

                                using (Pen p = new Pen(Color.Black, redrawShift))
                                {
                                    //p.LineJoin = LineJoin.Round;
                                    gP.Widen(p);
                                    gx.DrawPath(p, gP);
                                }
                            }

                            Invert(bTmpRS);

                            //Form fff = new Form();
                            //fff.BackgroundImage = bTmpRS;
                            //fff.BackgroundImageLayout = ImageLayout.Zoom;
                            //fff.ShowDialog();

                            ChainFinder cf2 = new ChainFinder();
                            List<ChainCode> c2 = GetBoundary(bTmpRS, cf2, 0, false);
                            c2 = c2.OrderByDescending(x => x.Coord.Count).ToList();

                            for (int i = c.Count - 1; i >= 0; i--)
                            {
                                bool isInner = ChainFinder.IsInnerOutline(c[i]);
                                if (isInner)
                                    c.RemoveAt(i);
                            }

                            foreach (ChainCode cc in c2)
                            {
                                bool isInner = ChainFinder.IsInnerOutline(cc);
                                if (isInner)
                                    c.Add(cc);
                            }

                            c = c.OrderByDescending(x => x.Coord.Count).ToList();
                        }
                    }
                }

                using (GraphicsPath gP = new GraphicsPath())
                {
                    foreach (ChainCode cc in c)
                    {
                        double epsilon = (double)this.numPPEpsilonValue;
                        bool isInner = ChainFinder.IsInnerOutline(cc);
                        if (isInner)
                            epsilon = (double)this.numPPEpsilon2Value;

                        List<Point> pts = cc.Coord;

                        if (this.cbApproxLinesChecked)
                        {
                            pts = cf.RemoveColinearity(pts, true, 4);
                            pts = cf.ApproximateLines(pts, epsilon);
                            pts = cf.RemoveColinearity(pts, true, 4);

                            cc.Coord = pts;
                        }

                        gP.StartFigure();

                        if (cc.Coord.Count > 4 && Math.Abs(cc.Area) > 1)
                        {
                            if (drawCurves)
                                gP.AddClosedCurve(cc.Coord.ToArray(), tension);
                            else
                                gP.AddLines(cc.Coord.ToArray());
                        }
                    }

                    using (Graphics gx = Graphics.FromImage(bOut))
                    {
                        gx.CompositingQuality = CompositingQuality.HighQuality;
                        gx.SmoothingMode = SmoothingMode.AntiAlias;
                        gx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gx.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        using (TextureBrush tb = new TextureBrush(this.BmpWork))
                            gx.FillPath(tb, gP);
                    }
                }

                return bOut;
            }

            return null;
        }

        private unsafe void ResetTransp(Bitmap bOut, Bitmap bOut2, Bitmap bResTransp)
        {
            int w = bOut.Width;
            int h = bOut.Height;

            BitmapData bmData = bOut.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmSrc = bOut2.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmRef = bResTransp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            for (int y = 0; y < h; y++)
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;
                byte* p2 = (byte*)bmSrc.Scan0;
                p2 += y * stride;
                byte* p4 = (byte*)bmRef.Scan0;
                p4 += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (p4[0] == 0 && p4[1] == 255 && p4[2] == 0)
                        p[3] = p2[3];

                    p += 4;
                    p2 += 4;
                    p4 += 4;
                }
            }

            bOut.UnlockBits(bmData);
            bOut2.UnlockBits(bmSrc);
            bResTransp.UnlockBits(bmRef);
        }

        private unsafe void Invert(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            Parallel.For(0, h, y =>
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;

                for (int x = 0; x < w; x++)
                {
                    p[3] = (byte)(255 - p[3]);

                    p += 4;
                }
            });

            bmp.UnlockBits(bmData);
        }

        private unsafe void CompAlphaBased(Bitmap bOut, Bitmap bOut2)
        {
            int w = bOut.Width;
            int h = bOut.Height;

            BitmapData bmData = bOut.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmSrc = bOut2.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            Parallel.For(0, h, y =>
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;
                byte* p2 = (byte*)bmSrc.Scan0;
                p2 += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (p2[3] != 0)
                        p[3] = (byte)(255 - p2[3]);

                    p += 4;
                    p2 += 4;
                }
            });

            bOut.UnlockBits(bmData);
            bOut2.UnlockBits(bmSrc);
        }

        private Bitmap RemoveOutline(Bitmap bmp, Bitmap bOrig, int innerW, bool dontFill)
        {
            Bitmap bOut = new Bitmap(bmp.Width, bmp.Height);

            using (Bitmap b = RemOutline(bmp, innerW, null))
            {
                using (Graphics gx = Graphics.FromImage(bOut))
                {
                    gx.SmoothingMode = SmoothingMode.None;
                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gx.DrawImage(b, 0, 0);
                }
            }

            List<ChainCode> lInner = GetBoundary(bOut);

            if (lInner.Count > 0)
            {
                lInner = lInner.OrderByDescending(a => a.Coord.Count).ToList();

                for (int i = 0; i < lInner.Count; i++)
                {
                    List<PointF> pts = lInner[i].Coord.Select(a => new PointF(a.X, a.Y)).ToList();

                    if (pts.Count > 2)
                    {
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            gp.AddLines(pts.ToArray());

                            if (gp.PointCount > 0)
                            {
                                using (Graphics gx = Graphics.FromImage(bOut))
                                {
                                    gx.SmoothingMode = SmoothingMode.None;
                                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                    using (TextureBrush tb = new TextureBrush(bOrig))
                                    {
                                        gx.FillPath(tb, gp);

                                        using (Pen p = new Pen(tb, 1))
                                            gx.DrawPath(p, gp);
                                    }
                                }
                            }
                        }
                    }
                }

                if (dontFill)
                    for (int i = 0; i < lInner.Count; i++)
                    {
                        if (ChainFinder.IsInnerOutline(lInner[i]))
                        {
                            List<PointF> pts = lInner[i].Coord.Select(a => new PointF(a.X, a.Y)).ToList();

                            if (pts.Count > 2)
                            {
                                using (GraphicsPath gp = new GraphicsPath())
                                {
                                    gp.AddLines(pts.ToArray());

                                    if (gp.PointCount > 0)
                                    {
                                        using (Graphics gx = Graphics.FromImage(bOut))
                                        {
                                            gx.SmoothingMode = SmoothingMode.None;
                                            gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                            gx.CompositingMode = CompositingMode.SourceCopy;
                                            gx.FillPath(Brushes.Transparent, gp);
                                            gx.DrawPath(Pens.Transparent, gp);
                                        }
                                    }
                                }
                            }
                        }
                    }
            }

            return bOut;
        }

        private Bitmap RemoveOutline(Bitmap bmp, Bitmap bOrig, int innerW, bool dontFill, bool outerOnly)
        {
            Bitmap bOut = new Bitmap(bmp.Width, bmp.Height);

            using (Bitmap b = RemOutline(bmp, innerW, outerOnly, null))
            {
                using (Graphics gx = Graphics.FromImage(bOut))
                {
                    gx.SmoothingMode = SmoothingMode.None;
                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gx.DrawImage(b, 0, 0);
                }
            }

            if (outerOnly)
                return bOut;

            List<ChainCode> lInner = GetBoundary(bOut);

            if (lInner.Count > 0)
            {
                lInner = lInner.OrderByDescending(a => a.Coord.Count).ToList();

                for (int i = 0; i < lInner.Count; i++)
                {
                    List<PointF> pts = lInner[i].Coord.Select(a => new PointF(a.X, a.Y)).ToList();

                    if (pts.Count > 2)
                    {
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            gp.AddLines(pts.ToArray());

                            if (gp.PointCount > 0)
                            {
                                using (Graphics gx = Graphics.FromImage(bOut))
                                {
                                    gx.SmoothingMode = SmoothingMode.None;
                                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                    using (TextureBrush tb = new TextureBrush(bOrig))
                                    {
                                        gx.FillPath(tb, gp);

                                        using (Pen p = new Pen(tb, 1))
                                            gx.DrawPath(p, gp);
                                    }
                                }
                            }
                        }
                    }
                }

                if (dontFill && !outerOnly)
                    for (int i = 0; i < lInner.Count; i++)
                    {
                        if (ChainFinder.IsInnerOutline(lInner[i]))
                        {
                            List<PointF> pts = lInner[i].Coord.Select(a => new PointF(a.X, a.Y)).ToList();

                            if (pts.Count > 2)
                            {
                                using (GraphicsPath gp = new GraphicsPath())
                                {
                                    gp.AddLines(pts.ToArray());

                                    if (gp.PointCount > 0)
                                    {
                                        using (Graphics gx = Graphics.FromImage(bOut))
                                        {
                                            gx.SmoothingMode = SmoothingMode.None;
                                            gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                            gx.CompositingMode = CompositingMode.SourceCopy;
                                            gx.FillPath(Brushes.Transparent, gp);
                                            gx.DrawPath(Pens.Transparent, gp);
                                        }
                                    }
                                }
                            }
                        }
                    }
            }

            return bOut;
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

                    for (int i = 0; i <= breite - 1; i++)
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

        public Bitmap RemOutline(Bitmap bmp, int breite, bool outerOnly, System.ComponentModel.BackgroundWorker bgw)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L))
            {
                Bitmap b = null;
                BitArray fbits = null;

                try
                {
                    b = (Bitmap)bmp.Clone();

                    for (int i = 0; i <= breite - 1; i++)
                    {
                        if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                            break;

                        ChainFinder cf = new ChainFinder();
                        cf.AllowNullCells = true;

                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        cf.RemoveOutline(b, fList, outerOnly);

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

        private Bitmap ExtendOutline(Bitmap bmp, int outerW, bool dontFill)
        {
            if (outerW == 0)
                return new Bitmap(bmp);

            Bitmap bOut = new Bitmap(bmp.Width, bmp.Height);

            List<ChainCode> lInner = GetBoundary(bmp);

            if (lInner.Count > 0)
            {
                lInner = lInner.OrderByDescending(a => a.Coord.Count).ToList();

                for (int i = 0; i < lInner.Count; i++)
                {
                    List<PointF> pts = lInner[i].Coord.Select(a => new PointF(a.X, a.Y)).ToList();

                    if (pts.Count > 2)
                    {
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            gp.AddLines(pts.ToArray());

                            if (gp.PointCount > 0)
                            {
                                using (Graphics gx = Graphics.FromImage(bOut))
                                {
                                    gx.SmoothingMode = SmoothingMode.None;
                                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                    gx.FillPath(Brushes.Black, gp);
                                    gx.DrawPath(Pens.Black, gp);

                                    try
                                    {
                                        using (Pen pen = new Pen(Color.Black, outerW))
                                        {
                                            pen.LineJoin = LineJoin.Round;
                                            gp.Widen(pen);
                                            gx.DrawPath(pen, gp);
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        Console.WriteLine(exc.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (dontFill)
                if (lInner.Count > 0)
                {
                    for (int i = 0; i < lInner.Count; i++)
                    {
                        if (ChainFinder.IsInnerOutline(lInner[i]))
                        {
                            List<PointF> pts = lInner[i].Coord.Select(a => new PointF(a.X, a.Y)).ToList();

                            if (pts.Count > 2)
                            {
                                using (GraphicsPath gp = new GraphicsPath())
                                {
                                    gp.AddLines(pts.ToArray());

                                    if (gp.PointCount > 0)
                                    {
                                        using (Graphics gx = Graphics.FromImage(bOut))
                                        {
                                            gx.SmoothingMode = SmoothingMode.None;
                                            gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                            gx.CompositingMode = CompositingMode.SourceCopy;
                                            gx.FillPath(Brushes.Transparent, gp);
                                            gx.DrawPath(Pens.Transparent, gp);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            return bOut;
        }

        public Bitmap ExtOutline(Bitmap bmp, int breite, System.ComponentModel.BackgroundWorker bgw)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L))
            {
                Bitmap b = null;
                BitArray fbits = null;

                try
                {
                    b = (Bitmap)bmp.Clone();

                    for (int i = 0; i <= breite - 1; i++)
                    {
                        if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                            break;

                        ChainFinder cf = new ChainFinder();
                        cf.AllowNullCells = true;

                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        cf.ExtendOutline(b, fList);

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

        public unsafe void CleanOutline(int pixelDepthOuter = 2, int thresholdOuter = 45, int pixelDepthInner = 2, int thresholdInner = 75,
            bool removeSinglePixels = false, int minAllowedArea = 200, int numCleanAmount = 1)
        {
            ChainFinder cf = new ChainFinder();
            List<ChainCode> c = GetBoundary(this.Bmp, cf, 0, false);
            c = c.OrderByDescending(x => x.Coord.Count).ToList();

            ShowInfo?.Invoke(this, "Cleaning Outline...");

            int w = this.Bmp.Width;
            int h = this.Bmp.Height;

            BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(20);

            foreach (ChainCode cc in c)
            {
                if (this.BGW != null && this.BGW.WorkerSupportsCancellation && this.BGW.CancellationPending)
                    break;

                if (cc.Coord.Count > 4 || Math.Abs(cc.Area) > 1)
                {
                    List<Point> pts = cc.Coord;
                    List<int> dir = cc.Chain;

                    //if (ChainFinder.IsInnerOutline(cc))
                    //{
                    for (int i = 0; i < pts.Count; i++)
                    {
                        Point[] ptsToDo = new Point[pixelDepthInner];
                        int dirCur = dir[i];
                        int xR = 0;
                        int yR = 0;

                        //get normal direction
                        switch (dirCur)
                        {
                            case 0:
                                xR = 0;
                                yR = -1;
                                break;
                            case 1:
                                xR = 1;
                                yR = 0;
                                break;
                            case 2:
                                xR = 0;
                                yR = 1;
                                break;
                            case 3:
                                xR = -1;
                                yR = 0;
                                break;
                            default:
                                break;
                        }

                        for (int j = 0; j < pixelDepthInner; j++)
                            ptsToDo[j] = new Point(pts[i].X + (j + 1) * xR, pts[i].Y + (j + 1) * yR);

                        int b = p[pts[i].X * 4 + pts[i].Y * stride];
                        int g = p[pts[i].X * 4 + pts[i].Y * stride + 1];
                        int r = p[pts[i].X * 4 + pts[i].Y * stride + 2];
                        int a = p[pts[i].X * 4 + pts[i].Y * stride + 3];

                        List<Color> cols = new List<Color>();
                        cols.Add(Color.FromArgb(a, r, g, b));

                        for (int j = 0; j < ptsToDo.Length; j++)
                        {
                            int x = ptsToDo[j].X;
                            int y = ptsToDo[j].Y;

                            if (x >= 0 && y >= 0 && x < this.Bmp.Width && y < this.Bmp.Height)
                            {
                                //step out on transparency, we left the shape
                                if (p[x * 4 + y * stride + 3] == 0)
                                    break;

                                int bb = p[x * 4 + y * stride];
                                int gg = p[x * 4 + y * stride + 1];
                                int rr = p[x * 4 + y * stride + 2];
                                int aa = p[x * 4 + y * stride + 3];

                                cols.Add(Color.FromArgb(aa, rr, gg, bb));
                            }
                        }

                        List<double> distances = new List<double>();

                        for (int j = 1; j < cols.Count; j++)
                        {
                            double dist = GetColorDist(cols[j - 1], cols[j]);
                            distances.Add(dist);
                        }

                        if (DeletePoint(distances, thresholdInner))
                            p[pts[i].X * 4 + pts[i].Y * stride] = p[pts[i].X * 4 + pts[i].Y * stride + 1] =
                                p[pts[i].X * 4 + pts[i].Y * stride + 2] = p[pts[i].X * 4 + pts[i].Y * stride + 3] = 0;


                    }
                    //}
                    //else
                    {
                        for (int i = 0; i < pts.Count; i++)
                        {
                            Point[] ptsToDo = new Point[pixelDepthOuter];
                            int dirCur = dir[i];
                            int xR = 0;
                            int yR = 0;

                            //get normal direction
                            switch (dirCur)
                            {
                                case 0:
                                    xR = 0;
                                    yR = -1;
                                    break;
                                case 1:
                                    xR = 1;
                                    yR = 0;
                                    break;
                                case 2:
                                    xR = 0;
                                    yR = 1;
                                    break;
                                case 3:
                                    xR = -1;
                                    yR = 0;
                                    break;
                                default:
                                    break;
                            }

                            for (int j = 0; j < pixelDepthOuter; j++)
                                ptsToDo[j] = new Point(pts[i].X + (j + 1) * xR, pts[i].Y + (j + 1) * yR);

                            int b = p[pts[i].X * 4 + pts[i].Y * stride];
                            int g = p[pts[i].X * 4 + pts[i].Y * stride + 1];
                            int r = p[pts[i].X * 4 + pts[i].Y * stride + 2];
                            int a = p[pts[i].X * 4 + pts[i].Y * stride + 3];

                            List<Color> cols = new List<Color>();
                            cols.Add(Color.FromArgb(a, r, g, b));

                            for (int j = 0; j < ptsToDo.Length; j++)
                            {
                                int x = ptsToDo[j].X;
                                int y = ptsToDo[j].Y;

                                if (x >= 0 && y >= 0 && x < this.Bmp.Width && y < this.Bmp.Height)
                                {
                                    //step out on transparency, we left the shape
                                    if (p[x * 4 + y * stride + 3] == 0)
                                        break;

                                    int bb = p[x * 4 + y * stride];
                                    int gg = p[x * 4 + y * stride + 1];
                                    int rr = p[x * 4 + y * stride + 2];
                                    int aa = p[x * 4 + y * stride + 3];

                                    cols.Add(Color.FromArgb(aa, rr, gg, bb));
                                }
                            }

                            List<double> distances = new List<double>();

                            for (int j = 1; j < cols.Count; j++)
                            {
                                double dist = GetColorDist(cols[j - 1], cols[j]);
                                distances.Add(dist);
                            }

                            if (DeletePoint(distances, thresholdOuter))
                                p[pts[i].X * 4 + pts[i].Y * stride] = p[pts[i].X * 4 + pts[i].Y * stride + 1] =
                                    p[pts[i].X * 4 + pts[i].Y * stride + 2] = p[pts[i].X * 4 + pts[i].Y * stride + 3] = 0;
                        }
                    }
                }
            }

            this.Bmp.UnlockBits(bmData);

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(80);

            if (removeSinglePixels)
                for (int i = 0; i < numCleanAmount; i++)
                    RemoveSinglePixels(minAllowedArea);

            if (this.BGW != null && this.BGW.WorkerReportsProgress)
                this.BGW.ReportProgress(100);

            //Bitmap bOld = this.BmpWork;
            //this.BmpWork = new Bitmap(this.Bmp);
            //if (bOld != null)
            //{
            //    bOld.Dispose();
            //    bOld = null;
            //}
        }

        private unsafe void RemoveSinglePixels(int minAllowedArea)
        {
            ChainFinder cf = new ChainFinder();
            List<ChainCode> c = GetBoundary(this.Bmp, cf, 0, false);
            c = c.OrderByDescending(x => x.Coord.Count).ToList();

            int w = this.Bmp.Width;
            int h = this.Bmp.Height;

            //BitmapData bmData = this.Bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //int stride = bmData.Stride;

            //byte* p = (byte*)bmData.Scan0;

            //foreach (ChainCode cc in c)
            //{
            //    if (cc.Coord.Count < 4 || cc.Area < minAllowedArea)
            //    {
            //        List<Point> pts = cc.Coord;

            //        for (int i = 0; i < pts.Count; i++)
            //        {
            //            p[pts[i].X * 4 + pts[i].Y * stride] = p[pts[i].X * 4 + pts[i].Y * stride + 1] =
            //                p[pts[i].X * 4 + pts[i].Y * stride + 2] = p[pts[i].X * 4 + pts[i].Y * stride + 3] = 0;
            //        }
            //    }
            //}

            //this.Bmp.UnlockBits(bmData);

            using (Graphics gx = Graphics.FromImage(this.Bmp))
            {
                gx.Clear(Color.Transparent);
                foreach (ChainCode cc in c)
                {
                    if (!ChainFinder.IsInnerOutline(cc) && cc.Coord.Count >= 4 && Math.Abs(cc.Area) >= minAllowedArea)
                    {
                        List<Point> pts = cc.Coord;
                        for (int i = 0; i < pts.Count; i++)
                            pts[i] = new Point(pts[i].X, pts[i].Y);
                        cc.Coord = pts;

                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            gP.StartFigure();
                            gP.AddLines(cc.Coord.ToArray());
                            using (TextureBrush tb = new TextureBrush(this.BmpOrig))
                            {
                                gx.FillPath(tb, gP);
                                using (Pen pen = new Pen(tb, 1))
                                    gx.DrawPath(pen, gP);
                            }
                        }
                    }
                }
            }

            foreach (ChainCode cc in c)
            {
                if (ChainFinder.IsInnerOutline(cc) && cc.Coord.Count >= 4 && Math.Abs(cc.Area) >= minAllowedArea)
                {
                    List<Point> pts = cc.Coord;
                    for (int i = 0; i < pts.Count; i++)
                        pts[i] = new Point(pts[i].X, pts[i].Y);
                    cc.Coord = pts;

                    using (Graphics gx = Graphics.FromImage(this.Bmp))
                    using (GraphicsPath gP = new GraphicsPath())
                    {
                        gx.SmoothingMode = SmoothingMode.None;
                        gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                        gx.CompositingMode = CompositingMode.SourceCopy;
                        gP.StartFigure();
                        gP.AddLines(cc.Coord.ToArray());
                        gx.FillPath(Brushes.Transparent, gP);
                        gx.DrawPath(Pens.Transparent, gP);

                    }
                }
            }
        }

        public void RemAreas(int minAllowedArea)
        {
            RemoveSinglePixels(minAllowedArea);
        }

        private bool DeletePoint(List<double> distances, int threshold)
        {
            for (int i = 0; i < distances.Count; i++)
                if (distances[i] >= threshold)
                    return true;

            return false;
        }

        private double GetColorDist(Color c1, Color c2)
        {
            double d = 0;

            double r1 = c1.R * c1.A / 255.0;
            double r2 = c2.R * c2.A / 255.0;

            double g1 = c1.G * c1.A / 255.0;
            double g2 = c2.G * c2.A / 255.0;

            double b1 = c2.B * c1.A / 255.0;
            double b2 = c2.B * c2.A / 255.0;

            double dR = c2.R - c1.R;
            double dG = c2.G - c1.G;
            double dB = c2.B - c1.B;

            d = Math.Sqrt((dR * dR) + (dG * dG) + (dB * dB)); //0 - 441.67

            return d;
        }

        private void OnBoundaryError(string message)
        {
            BoundaryError?.Invoke(this, message);
        }

        public void Dispose()
        {
            if (this.Bmp != null)
            {
                this.Bmp.Dispose();
                this.Bmp = null;
            }
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
        }

        public Bitmap ReShiftShapes(Bitmap bmp, int x, int y)
        {
            if (this.BmpOrig != null)
            {
                Bitmap bOut = new Bitmap(bmp.Width, bmp.Height);

                List<ChainCode> c = GetBoundary(bmp, 0, false);
                c = c.OrderByDescending(a => a.Coord.Count).ToList();

                foreach (ChainCode cc in c)
                {
                    if (!ChainFinder.IsInnerOutline(cc))
                    {
                        List<Point> pts = cc.Coord;
                        for (int i = 0; i < pts.Count; i++)
                            pts[i] = new Point(pts[i].X + x, pts[i].Y + y);
                        cc.Coord = pts;

                        using (Graphics gx = Graphics.FromImage(bOut))
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            gP.StartFigure();
                            gP.AddLines(cc.Coord.ToArray());
                            using (TextureBrush tb = new TextureBrush(this.BmpOrig))
                            {
                                gx.FillPath(tb, gP);
                                using (Pen p = new Pen(tb, 1))
                                    gx.DrawPath(p, gP);
                            }
                        }
                    }
                }

                foreach (ChainCode cc in c)
                {
                    if (ChainFinder.IsInnerOutline(cc))
                    {
                        List<Point> pts = cc.Coord;
                        for (int i = 0; i < pts.Count; i++)
                            pts[i] = new Point(pts[i].X + x, pts[i].Y + y);
                        cc.Coord = pts;

                        using (Graphics gx = Graphics.FromImage(bOut))
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            gx.SmoothingMode = SmoothingMode.None;
                            gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                            gx.CompositingMode = CompositingMode.SourceCopy;
                            gP.StartFigure();
                            gP.AddLines(cc.Coord.ToArray());
                            gx.FillPath(Brushes.Transparent, gP);
                            gx.DrawPath(Pens.Transparent, gP);

                        }
                    }
                }

                return bOut;
            }
            else
                return null;
        }
    }
}
