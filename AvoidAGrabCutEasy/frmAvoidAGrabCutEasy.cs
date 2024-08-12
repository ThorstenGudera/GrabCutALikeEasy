using Cache;
using ChainCodeFinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    public partial class frmAvoidAGrabCutEasy : Form
    {
        private UndoOPCache _undoOPCache = null;

        private bool _dontAskOnClosing;

        public string CachePathAddition
        {
            get
            {
                return m_CachePathAddition;
            }
            set
            {
                m_CachePathAddition = value;
            }
        }

        private string m_CachePathAddition;

        public Bitmap FBitmap
        {
            get
            {
                return this.helplineRulerCtrl2.Bmp;
            }
        }

        private Bitmap _bmpBU = null;
        private bool _pic_changed = false;

        private int _openPanelHeight;
        private bool _isHoveringVertically;
        private bool _doubleClicked;
        private bool _dontDoZoom;
        private bool _tracking;
        private bool _tracking4;
        private int _rX;
        private int _rY;
        private int _eX;
        private int _eY;
        private int _rW;
        private int _rH;
        private Rectangle _rect;
        private int _eW;
        private int _eH;

        private GrabCutOp _gc;
        private bool _tracking2;
        private List<Point> _points = new List<Point>();
        private Dictionary<int, GraphicsPath> _pathList;
        private List<Point> _points2 = new List<Point>();
        private int _currentDraw;
        private Color[] _colors = new Color[4];
        //private Dictionary<int, List<Tuple<List<Point>, int>>> _allPoints2;
        private Dictionary<int, List<Tuple<List<Point>, int>>> _allPoints;
        private double _oldGamma;
        private double _oldXi;
        //private double _oldBeta;
        private object _lockObject = new object();
        private bool _drawImgOverlay;
        private int _eX2f;
        private int _eY2f;
        private List<int> _lastDraw;
        private int _eX2;
        private int _eY2;

        private Dictionary<int, Dictionary<int, List<List<Point>>>> _scribblesBU;
        private Dictionary<int, Dictionary<int, List<List<Point>>>> _scribbles;
        private int _maxSize = 1200;
        private int _algMaxIter = Int32.MaxValue / 3;
        private int _algQATH = 1000000;

        private double _items = 10;
        private double _correctItems = 5;
        private double _items2 = 10;
        private double _correctItems2 = 2;
        private List<ChainCode> _allChains;
        private Bitmap _bResCopy;
        private Bitmap _bResCopyTransp;
        private bool _restartDiffInit;
        private Bitmap _b4Copy;
        private double _KMeansInitW = 2;
        private double _KMeansInitH = 2;
        private ListSelectionMode _KMeansSelMode = 0;
        private int _KMeansInitIters = 10;
        private bool _kMInitRnd = false;
        private int _KMeansIters = 0;
        private int _numShiftX;
        private int _numShiftY;

        public event EventHandler<string> ShowInfo;
        //public event EventHandler<string> BoundaryError;

        public frmAvoidAGrabCutEasy()
        {
            InitializeComponent();
        }

        public frmAvoidAGrabCutEasy(Bitmap bmp, string basePathAddition)
        {
            InitializeComponent();

            CachePathAddition = basePathAddition;

            //HelperFunctions.HelperFunctions.SetFormSizeBig(this);
            this.CenterToScreen();

            // Me.helplineRulerCtrl1.SetZoomOnlyByMethodCall = True

            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L))
            {
                this.helplineRulerCtrl1.Bmp = new Bitmap(bmp);
                _bmpBU = new Bitmap(bmp);
            }
            else
            {
                MessageBox.Show("Not enough Memory");
                return;
            }

            double faktor = System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Height);
            double multiplier = System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Height);
            if (multiplier >= faktor)
                this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width));
            else
                this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Height) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Height));

            this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));
            this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

            this.helplineRulerCtrl1.AddDefaultHelplines();
            this.helplineRulerCtrl1.ResetAllHelpLineLabelsColor();

            this.helplineRulerCtrl1.dbPanel1.DragOver += bitmappanel1_DragOver;
            this.helplineRulerCtrl1.dbPanel1.DragDrop += bitmappanel1_DragDrop;

            this.helplineRulerCtrl1.dbPanel1.MouseDown += helplineRulerCtrl1_MouseDown;
            this.helplineRulerCtrl1.dbPanel1.MouseMove += helplineRulerCtrl1_MouseMove;
            this.helplineRulerCtrl1.dbPanel1.MouseUp += helplineRulerCtrl1_MouseUp;

            this.helplineRulerCtrl1.PostPaint += helplineRulerCtrl1_Paint;

            this.helplineRulerCtrl2.dbPanel1.MouseDown += helplineRulerCtrl2_MouseDown;
            this.helplineRulerCtrl2.dbPanel1.MouseMove += helplineRulerCtrl2_MouseMove;
            this.helplineRulerCtrl2.dbPanel1.MouseUp += helplineRulerCtrl2_MouseUp;

            this.helplineRulerCtrl2.PostPaint += helplineRulerCtrl2_Paint;

            this._dontDoZoom = true;
            this.cmbZoom.SelectedIndex = 4;
            this._dontDoZoom = false;

            this._colors[0] = Color.Black;
            this._colors[1] = Color.White;
            this._colors[2] = Color.Black;
            this._colors[3] = Color.Yellow;

            this._currentDraw = 0;

            this.helplineRulerCtrl2.AddDefaultHelplines();
            this.helplineRulerCtrl2.ResetAllHelpLineLabelsColor();

            //while developing...
            AvailMem.AvailMem.NoMemCheck = true;
        }

        private void helplineRulerCtrl2_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.helplineRulerCtrl2.Bmp != null)
            {
                int ix = (int)((e.X - this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl2.Zoom);
                int iy = (int)((e.Y - this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl2.Zoom);

                if (this.cbDraw.Checked && ix >= 0 && ix < this.helplineRulerCtrl2.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl2.Bmp.Height)
                {
                    this._points.Add(new Point(ix, iy));
                    this._tracking2 = true;
                }
            }
        }

        private void helplineRulerCtrl2_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.helplineRulerCtrl2.Bmp != null)
            {
                int ix = (int)((e.X - this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl2.Zoom);
                int iy = (int)((e.Y - this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl2.Zoom);

                if (ix >= 0 && ix < this.helplineRulerCtrl2.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl2.Bmp.Height)
                {
                    if (this._tracking2)
                    {
                        this._points.Add(new Point(ix, iy));
                        this.helplineRulerCtrl2.dbPanel1.Invalidate();
                    }

                    Color c = this.helplineRulerCtrl2.Bmp.GetPixel(ix, iy);
                    this.toolStripStatusLabel4.Text = ix.ToString() + "; " + iy.ToString();
                    this.toolStripStatusLabel3.BackColor = c;

                    this.helplineRulerCtrl2.dbPanel1.Invalidate();
                }
            }

            if (this._drawImgOverlay)
            {
                this._eX2f = e.X;
                this._eY2f = e.Y;
            }
        }

        private void helplineRulerCtrl2_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.helplineRulerCtrl2.Bmp != null)
            {
                int ix = (int)((e.X - this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl2.Zoom);
                int iy = (int)((e.Y - this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl2.Zoom);

                if (ix >= 0 && ix < this.helplineRulerCtrl2.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl2.Bmp.Height)
                {
                    if (this._tracking2)
                    {
                        if (this._points[this._points.Count - 1].X != ix || this._points[this._points.Count - 1].Y != iy)
                            this._points.Add(new Point(ix, iy));

                        AddPointsToPath();
                        this._points.Clear();
                        this.helplineRulerCtrl2.dbPanel1.Invalidate();
                    }
                }
            }

            this._tracking2 = false;
        }

        private void AddPointsToPath()
        {
            if (this._pathList == null)
                this._pathList = new Dictionary<int, GraphicsPath>();

            if (!this._pathList.ContainsKey(this._currentDraw))
                this._pathList.Add(this._currentDraw, new GraphicsPath());

            if (this._allPoints == null)
                this._allPoints = new Dictionary<int, List<Tuple<List<Point>, int>>>();

            if (!this._allPoints.ContainsKey(this._currentDraw))
                this._allPoints.Add(this._currentDraw, new List<Tuple<List<Point>, int>>());

            GraphicsPath gp = this._pathList[this._currentDraw];
            using (GraphicsPath gpTmp = new GraphicsPath())
            {
                PointF[] z = this._points.Select(a => new PointF(a.X, a.Y)).ToArray();

                if (z.Length > 1)
                {
                    gpTmp.AddLines(z);
                    gp.AddPath((GraphicsPath)gpTmp.Clone(), false);

                    List<Point> ll = new List<Point>();
                    ll.AddRange(this._points.ToArray());
                    this._allPoints[this._currentDraw].Add(Tuple.Create(ll, (int)this.numWH.Value));
                }
                else if (z.Length == 1)
                {
                    Point pt = this._points[0];
                    Rectangle r = new Rectangle(pt.X - (int)this.numWH.Value / 2,
                        pt.Y - (int)this.numWH.Value / 2,
                        (int)this.numWH.Value,
                        (int)this.numWH.Value);

                    gpTmp.AddRectangle(r);
                    gp.AddPath((GraphicsPath)gpTmp.Clone(), false);

                    List<Point> ll = new List<Point>();
                    ll.AddRange(this._points.ToArray());
                    this._allPoints[this._currentDraw].Add(Tuple.Create(ll, (int)this.numWH.Value));
                }
            }

            if (this._lastDraw == null)
                this._lastDraw = new List<int>();

            this._lastDraw.Add(this._currentDraw);
        }

        private void helplineRulerCtrl2_Paint(object sender, PaintEventArgs e)
        {
            if (this.helplineRulerCtrl2.Bmp != null)
            {
                if (this._drawImgOverlay)
                {
                    HelplineRulerControl.DBPanel pz = this.helplineRulerCtrl2.dbPanel1;

                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix33 = 0.25F;

                    using (ImageAttributes ia = new ImageAttributes())
                    {
                        ia.SetColorMatrix(cm);
                        e.Graphics.DrawImage(this.helplineRulerCtrl1.Bmp,
                            new Rectangle(0, 0, pz.ClientRectangle.Width, pz.ClientRectangle.Height),
                            -pz.AutoScrollPosition.X / this.helplineRulerCtrl2.Zoom,
                                -pz.AutoScrollPosition.Y / this.helplineRulerCtrl2.Zoom,
                                pz.ClientRectangle.Width / this.helplineRulerCtrl2.Zoom,
                                pz.ClientRectangle.Height / this.helplineRulerCtrl2.Zoom, GraphicsUnit.Pixel, ia);
                    }

                    int wh2 = (int)this.numWH.Value;

                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(95, 0, 255, 0)))
                        e.Graphics.FillRectangle(sb, new RectangleF(
                            this._eX2f - (float)(wh2 / 2f * this.helplineRulerCtrl2.Zoom) - 1f,
                            this._eY2f - (float)(wh2 / 2f * this.helplineRulerCtrl2.Zoom) - 1f,
                            (float)(wh2 * this.helplineRulerCtrl2.Zoom),
                            (float)(wh2 * this.helplineRulerCtrl2.Zoom)));
                }

                if (this._allPoints != null)
                {
                    foreach (int j in this._allPoints.Keys)
                    {
                        Color c = this._colors[j];
                        List<Tuple<List<Point>, int>> ll = this._allPoints[j];

                        for (int i = 0; i < ll.Count; i++)
                        {
                            bool doRect = ll[i].Item1.Count == 1;

                            if (!doRect)
                            {
                                foreach (Point pt in ll[i].Item1)
                                {
                                    using (SolidBrush sb = new SolidBrush(c))
                                        e.Graphics.FillRectangle(sb, new Rectangle(
                                            (int)((int)(pt.X - ll[i].Item2 / 2) * this.helplineRulerCtrl2.Zoom) + this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.X,
                                            (int)((int)(pt.Y - ll[i].Item2 / 2) * this.helplineRulerCtrl2.Zoom) + this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.Y,
                                            (int)(ll[i].Item2 * this.helplineRulerCtrl2.Zoom),
                                            (int)(ll[i].Item2 * this.helplineRulerCtrl2.Zoom)));
                                }
                            }
                            else
                            {
                                Point pt = ll[i].Item1[0];
                                using (SolidBrush sb = new SolidBrush(c))
                                    e.Graphics.FillRectangle(sb, new Rectangle(
                                        (int)((int)(pt.X - ll[i].Item2 / 2) * this.helplineRulerCtrl2.Zoom) + this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.X,
                                        (int)((int)(pt.Y - ll[i].Item2 / 2) * this.helplineRulerCtrl2.Zoom) + this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.Y,
                                        (int)(ll[i].Item2 * this.helplineRulerCtrl2.Zoom),
                                        (int)(ll[i].Item2 * this.helplineRulerCtrl2.Zoom)));
                            }
                        }
                    }
                }

                if (this._points.Count > 0)
                {
                    Color c = this._colors[this._currentDraw];

                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        if (this._points.Count > 1)
                        {
                            foreach (Point pt in this._points)
                            {
                                using (SolidBrush sb = new SolidBrush(c))
                                    e.Graphics.FillRectangle(sb, new Rectangle(
                                        (int)((int)(pt.X - (int)this.numWH.Value / 2) * this.helplineRulerCtrl2.Zoom) + this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.X,
                                        (int)((int)(pt.Y - (int)this.numWH.Value / 2) * this.helplineRulerCtrl2.Zoom) + this.helplineRulerCtrl2.dbPanel1.AutoScrollPosition.Y,
                                        (int)((int)this.numWH.Value * this.helplineRulerCtrl2.Zoom),
                                        (int)((int)this.numWH.Value * this.helplineRulerCtrl2.Zoom)));
                            }
                        }
                    }
                }
            }
        }

        private void helplineRulerCtrl1_MouseDown(object sender, MouseEventArgs e)
        {
            int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
            int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

            int eX = e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X;
            int eY = e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y;

            if (ix >= 0 && ix < this.helplineRulerCtrl1.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl1.Bmp.Height)
            {
                this._rX = ix;
                this._rY = iy;

                this._eX = eX;
                this._eY = eY;

                if (this.cbScribbleMode.Checked)
                {
                    this._points2.Add(new Point(ix, iy));
                    this._tracking4 = true;
                }

                this._tracking = true;
            }
        }

        private void helplineRulerCtrl1_MouseMove(object sender, MouseEventArgs e)
        {
            int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
            int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

            this._eX2 = e.X;
            this._eY2 = e.Y;

            if (ix >= this.helplineRulerCtrl1.Bmp.Width)
                ix = this.helplineRulerCtrl1.Bmp.Width - 1;
            if (iy >= this.helplineRulerCtrl1.Bmp.Height)
                iy = this.helplineRulerCtrl1.Bmp.Height - 1;

            if (ix >= 0 && ix < this.helplineRulerCtrl1.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl1.Bmp.Height)
            {
                if (this._tracking)
                {
                    this._rW = Math.Abs(ix - this._rX);
                    this._rH = Math.Abs(iy - this._rY);

                    this._eW = Math.Abs(e.X - this._eX - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X);
                    this._eH = Math.Abs(e.Y - this._eY - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y);
                }

                if (this._tracking4)
                    this._points2.Add(new Point(ix, iy));

                Color c = this.helplineRulerCtrl1.Bmp.GetPixel(ix, iy);
                this.toolStripStatusLabel1.Text = ix.ToString() + "; " + iy.ToString();
                this.ToolStripStatusLabel2.BackColor = c;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
            }
        }

        private void helplineRulerCtrl1_MouseUp(object sender, MouseEventArgs e)
        {
            int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
            int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

            int eX = e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X;
            int eY = e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y;

            if (ix >= this.helplineRulerCtrl1.Bmp.Width)
                ix = this.helplineRulerCtrl1.Bmp.Width - 1;
            if (iy >= this.helplineRulerCtrl1.Bmp.Height)
                iy = this.helplineRulerCtrl1.Bmp.Height - 1;

            if (ix >= 0 && ix < this.helplineRulerCtrl1.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl1.Bmp.Height)
            {
                this._rW = Math.Abs(ix - this._rX);
                this._rH = Math.Abs(iy - this._rY);

                this._rect = new Rectangle(this._rX, this._rY, this._rW, this._rH);

                if (this._tracking4)
                {
                    AddPointsToScribblePath();
                    this._points2.Clear();
                }

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
            }

            this._tracking = false;
            this._tracking4 = false;
        }

        private void AddPointsToScribblePath()
        {
            int fgbg = this.rbFG.Checked ? 1 : 0;
            int wh = (int)this.numWHScribbles.Value;

            if (!this._scribbles.ContainsKey(fgbg))
                this._scribbles.Add(fgbg, new Dictionary<int, List<List<Point>>>());

            if (!this._scribbles[fgbg].ContainsKey(wh))
                this._scribbles[fgbg].Add(wh, new List<List<Point>>());

            List<List<Point>> whPts = this._scribbles[fgbg][wh];
            whPts.Add(new List<Point>());
            whPts[whPts.Count - 1].AddRange(this._points2.ToArray());
        }

        private void helplineRulerCtrl1_Paint(object sender, PaintEventArgs e)
        {
            if (this.cbRectMode.Checked)
            {
                using (Pen pen = new Pen(new SolidBrush(Color.Lime), 2))
                    e.Graphics.DrawRectangle(pen, new Rectangle(this._eX + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                        this._eY + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y, this._eW, this._eH));
            }

            if (this.cbScribbleMode.Checked)
            {
                if (this._scribbles == null)
                    this._scribbles = new Dictionary<int, Dictionary<int, List<List<Point>>>>();

                if (this._scribbles.ContainsKey(0)) //BG
                {
                    if (this._scribbles[0] != null)
                    {
                        foreach (int wh in this._scribbles[0].Keys)
                        {
                            List<List<Point>> whPts = this._scribbles[0][wh];

                            foreach (List<Point> pts in whPts)
                            {
                                bool doRect = pts.Count > 1;
                                Color c = Color.Black;

                                if (doRect)
                                {
                                    foreach (Point pt in pts)
                                    {
                                        using (SolidBrush sb = new SolidBrush(c))
                                            e.Graphics.FillRectangle(sb, new Rectangle(
                                                (int)((int)(pt.X - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                                                (int)((int)(pt.Y - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y,
                                                (int)(wh * this.helplineRulerCtrl1.Zoom),
                                                (int)(wh * this.helplineRulerCtrl1.Zoom)));
                                    }
                                }
                                else
                                {
                                    Point pt = pts[0];
                                    using (SolidBrush sb = new SolidBrush(c))
                                        e.Graphics.FillRectangle(sb, new Rectangle(
                                            (int)((int)(pt.X - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                                            (int)((int)(pt.Y - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y,
                                            (int)(wh * this.helplineRulerCtrl1.Zoom),
                                            (int)(wh * this.helplineRulerCtrl1.Zoom)));
                                }
                            }
                        }
                    }
                }

                if (this._scribbles.ContainsKey(1)) //FG
                {
                    if (this._scribbles[1] != null)
                    {
                        foreach (int wh in this._scribbles[1].Keys)
                        {
                            List<List<Point>> whPts = this._scribbles[1][wh];

                            foreach (List<Point> pts in whPts)
                            {
                                bool doRect = pts.Count > 1;
                                Color c = Color.White;

                                if (doRect)
                                {
                                    foreach (Point pt in pts)
                                    {
                                        using (SolidBrush sb = new SolidBrush(c))
                                            e.Graphics.FillRectangle(sb, new Rectangle(
                                                (int)((int)(pt.X - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                                                (int)((int)(pt.Y - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y,
                                                (int)(wh * this.helplineRulerCtrl1.Zoom),
                                                (int)(wh * this.helplineRulerCtrl1.Zoom)));
                                    }
                                }
                                else
                                {
                                    Point pt = pts[0];
                                    using (SolidBrush sb = new SolidBrush(c))
                                        e.Graphics.FillRectangle(sb, new Rectangle(
                                            (int)((int)(pt.X - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                                            (int)((int)(pt.Y - wh / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y,
                                            (int)(wh * this.helplineRulerCtrl1.Zoom),
                                            (int)(wh * this.helplineRulerCtrl1.Zoom)));
                                }
                            }
                        }
                    }
                }

                if (this._points2.Count > 0)
                {
                    Color c = this.rbFG.Checked ? Color.White : Color.Black;

                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        if (this._points2.Count > 1)
                        {
                            foreach (Point pt in this._points2)
                            {
                                using (SolidBrush sb = new SolidBrush(c))
                                    e.Graphics.FillRectangle(sb, new Rectangle(
                                        (int)((int)(pt.X - (int)this.numWHScribbles.Value / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                                        (int)((int)(pt.Y - (int)this.numWHScribbles.Value / 2) * this.helplineRulerCtrl1.Zoom) + this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y,
                                        (int)((int)this.numWHScribbles.Value * this.helplineRulerCtrl1.Zoom),
                                        (int)((int)this.numWHScribbles.Value * this.helplineRulerCtrl1.Zoom)));
                            }
                        }
                    }
                }

                int wh2 = (int)this.numWHScribbles.Value;

                using (SolidBrush sb = new SolidBrush(Color.FromArgb(95, 255, 0, 0)))
                    e.Graphics.FillRectangle(sb, new RectangleF(
                        this._eX2 - (float)(wh2 / 2f * this.helplineRulerCtrl1.Zoom) - 1f,
                        this._eY2 - (float)(wh2 / 2f * this.helplineRulerCtrl1.Zoom) - 1f,
                        (float)(wh2 * this.helplineRulerCtrl1.Zoom),
                        (float)(wh2 * this.helplineRulerCtrl1.Zoom)));
            }
        }

        public void SetupCache()
        {
            _undoOPCache = new Cache.UndoOPCache(this.GetType(), CachePathAddition, "testApp");
            if (this.helplineRulerCtrl1.Bmp != null)
                _undoOPCache.Add(this.helplineRulerCtrl1.Bmp);
        }

        private void bitmappanel1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void bitmappanel1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Effect == DragDropEffects.Copy)
                {
                    Bitmap b1 = null;
                    Bitmap b2 = null;

                    try
                    {
                        if (_pic_changed)
                        {
                            DialogResult dlg = MessageBox.Show("Save image?", "Unsaved data", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                            if (dlg == DialogResult.Yes)
                                button2.PerformClick();
                            else if (dlg == DialogResult.No)
                                _pic_changed = false;
                        }

                        String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
                        using (Image img = Image.FromFile(files[0]))
                        {
                            if (AvailMem.AvailMem.checkAvailRam(img.Width * img.Height * 16L))
                            {
                                b1 = new Bitmap((Bitmap)img.Clone());
                                this.SetBitmap(this.helplineRulerCtrl1.Bmp, b1, this.helplineRulerCtrl1, "Bmp");
                                b2 = new Bitmap((Bitmap)img.Clone());
                                this.SetBitmap(ref this._bmpBU, ref b2);
                            }
                            else
                                throw new Exception();
                        }

                        _undoOPCache.Clear(this.helplineRulerCtrl1.Bmp);

                        _pic_changed = false;

                        double faktor = System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Height);
                        double multiplier = System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Height);
                        if (multiplier >= faktor)
                            this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width));
                        else
                            this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Height) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Height));

                        this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));
                        this.btnReset2.Enabled = false;

                        this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

                        this.helplineRulerCtrl1.dbPanel1.Invalidate();

                        this.Text = files[0] + " - frmQuickExtract";
                    }
                    catch
                    {
                        MessageBox.Show("Fehler beim Laden des Bildes");

                        if (b1 != null)
                            b1.Dispose();

                        if (b2 != null)
                            b2.Dispose();
                    }
                }
            }
        }

        private void CheckRedoButton()
        {
            //_undoOPCache.CheckRedoButton(this.btnRedo);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The goal is to achieve results as good as with a GrabCut, but only with the first (GMM_probabilities) estimation, to reduce the memory footprint. So keep \"QuickEstimation\" checked and play around with the other parameters.");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (_undoOPCache != null && _undoOPCache.Processing == false)
            {
                this.btnReset2.Enabled = false;
                this.btnReset2.Refresh();

                Bitmap bOut = _undoOPCache.DoUndoAll();

                if (bOut != null)
                {
                    this.SetBitmap(this.helplineRulerCtrl1.Bmp, bOut, this.helplineRulerCtrl1, "Bmp");
                    this._pic_changed = false;
                    this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

                    this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));
                    this.helplineRulerCtrl1.dbPanel1.Invalidate();
                }
                else
                    MessageBox.Show("Error while resetting.");
            }

            this.cbRectMode.Enabled = true;
            this.cbRectMode.Checked = false;
            this.cbScribbleMode.Enabled = true;
            this.cbScribbleMode.Checked = false;
            this.btnReset2.Enabled = true;
            this.cbDraw.Checked = false;
            this.numMaxSize.Enabled = this.numGmmComp.Enabled = true;

            this.cbQuickEst_CheckedChanged(this.cbQuickEst, new EventArgs());
            this.cbDraw_CheckedChanged(this.cbDraw, new EventArgs());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //    if (_undoOPCache != null)
            //    {
            //        this.btnRedo.Enabled = false;
            //        this.btnRedo.Refresh();

            //        Bitmap bOut = _undoOPCache.DoRedo();

            //        if (bOut != null)
            //        {
            //            this.SetBitmap(this.helplineRulerCtrl1.Bmp, bOut, this.helplineRulerCtrl1, "Bmp");

            //            this._pic_changed = true;
            //            // Me.helplineRulerCtrl1.CalculateZoom()

            //            this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

            //            this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));
            //            // Me.helplineRulerCtrl1.dbPanel1.Invalidate()

            //            if (_undoOPCache.CurrentPosition > 1)
            //            {
            //                this.btnReset.Enabled = true;
            //                this.cbRectMode.Enabled = false;
            //            }
            //            else
            //                this.btnReset.Enabled = false;

            //            if (_undoOPCache.CurrentPosition < _undoOPCache.Count)
            //                this.btnRedo.Enabled = true;
            //            else
            //                this.btnRedo.Enabled = false;
            //        }
            //        this.helplineRulerCtrl1.dbPanel1.Invalidate();
            //    }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
                this.saveFileDialog1.FileName = "Bild1.png";

                try
                {
                    if (this.helplineRulerCtrl2.Bmp != null && this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        this.helplineRulerCtrl2.Bmp.Save(this.saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        _pic_changed = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null)
            {
                if (_pic_changed)
                {
                    DialogResult dlg = MessageBox.Show("Save image?", "Unsaved data", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                    if (dlg == DialogResult.Yes)
                        button2.PerformClick();
                    else if (dlg == DialogResult.No)
                        _pic_changed = false;
                }

                if (!_pic_changed)
                {
                    string f = this.Text.Split(new String[] { " - " }, StringSplitOptions.None)[0];
                    Bitmap b1 = null;

                    try
                    {
                        if (AvailMem.AvailMem.checkAvailRam(this._bmpBU.Width * this._bmpBU.Height * 12L))
                            b1 = (Bitmap)this._bmpBU.Clone();
                        else
                            throw new Exception();

                        this.SetBitmap(this.helplineRulerCtrl1.Bmp, b1, this.helplineRulerCtrl1, "Bmp");

                        this._pic_changed = false;

                        this.helplineRulerCtrl1.CalculateZoom();

                        this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

                        // SetHRControlVars();

                        this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));
                        this.helplineRulerCtrl1.dbPanel1.Invalidate();

                        _undoOPCache.Reset(false);
                        this.btnReset2.Enabled = false;

                        //if (_undoOPCache.Count > 1)
                        //    this.btnRedo.Enabled = true;
                        //else
                        //    this.btnRedo.Enabled = false;
                    }
                    catch
                    {
                        if (b1 != null)
                            b1.Dispose();
                    }
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.I | Keys.Control | Keys.Shift))
            {
                if (this.helplineRulerCtrl1.Bmp != null)
                    MessageBox.Show(this.helplineRulerCtrl1.Bmp.Size.ToString());

                return true;
            }

            if (keyData == Keys.Enter)
            {
                this.helplineRulerCtrl1._contentPanel_MouseDoubleClick(this.helplineRulerCtrl1.dbPanel1, new MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 2, 0, 0, 0));

                return true;
            }

            if (keyData == (Keys.Z | Keys.Control))
            {
                this.btnReset2.PerformClick();
                return true;
            }

            if (keyData == (Keys.Y | Keys.Control))
            {
                //this.btnRedo.PerformClick();
                return true;
            }

            //if (keyData == Keys.F3)
            //{
            //    if (this.FrmSetRamFreeable != null && this.FrmSetRamFreeable.Visible)
            //    {
            //        this.FrmSetRamFreeable.Show();
            //        this.FrmSetRamFreeable.BringToFront();
            //    }
            //    else
            //    {
            //        this.FrmSetRamFreeable = new frmSetRamFreeableAmount(this);
            //        this.FrmSetRamFreeable.Show();
            //    }
            //}

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void panel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.helplineRulerCtrl1.dbPanel1.SuspendLayout();
                this.SuspendLayout();

                if (!this._doubleClicked)
                {
                    this._openPanelHeight = this.panel1.Height;
                    this._doubleClicked = true;
                    this.panel1.Dock = DockStyle.None;
                    this.panel3.Dock = DockStyle.Top;

                    this._isHoveringVertically = true;

                    this.panel3.SendToBack();
                    this.splitContainer1.BringToFront();
                    this.statusStrip1.SendToBack();

                    this.panel1.BringToFront();

                    this.panel1.AutoScroll = false;
                }
                else
                {
                    this._doubleClicked = false;
                    this.panel3.Dock = DockStyle.None;
                    this.panel1.Dock = DockStyle.Top;

                    this.panel3.Width = this.splitContainer1.Width = this.panel1.Width = this.ClientSize.Width;

                    this._isHoveringVertically = false;
                    this.panel1.Height = this._openPanelHeight;

                    this.statusStrip1.SendToBack();
                    this.panel1.SendToBack();
                    this.splitContainer1.BringToFront();

                    this.panel1.AutoScroll = true;
                    this.panel1.BringToFront();
                    this.splitContainer1.BringToFront();

                    this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);
                }

                this.ResumeLayout();
                this.helplineRulerCtrl1.dbPanel1.ResumeLayout(true);
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this._doubleClicked && e.Button == MouseButtons.Right)
            {
                this.SuspendLayout();

                if (this._isHoveringVertically)
                {
                    this.panel1.Dock = DockStyle.Top;
                    this.panel3.Dock = DockStyle.None;
                    this._isHoveringVertically = false;
                    this.panel3.SendToBack();
                    this.splitContainer1.BringToFront();
                    this.statusStrip1.SendToBack();
                    this.panel1.Height = 24;
                }
                else
                {
                    this.panel3.Dock = DockStyle.Top;
                    this.panel1.Dock = DockStyle.None;
                    this._isHoveringVertically = true;
                    this.panel3.SendToBack();
                    this.splitContainer1.BringToFront();
                    this.statusStrip1.SendToBack();

                    this.panel1.BringToFront();
                    this.panel1.Height = this._openPanelHeight;
                }

                this.ResumeLayout(true);
            }
        }

        private void helplineRulerCtrl1_DBPanelDblClicked(object sender, HelplineRulerControl.ZoomEventArgs e)
        {
            this._dontDoZoom = true;

            if (e.Zoom == 1.0F)
                this.cmbZoom.SelectedIndex = 2;
            else if (e.ZoomWidth)
                this.cmbZoom.SelectedIndex = 3;
            else
                this.cmbZoom.SelectedIndex = 4;

            this.helplineRulerCtrl1.dbPanel1.Invalidate();

            this._dontDoZoom = false;
        }

        private void cbBGColor_CheckedChanged(object sender, EventArgs e)
        {
            if (cbBGColor.Checked)
                this.helplineRulerCtrl2.dbPanel1.BackColor = this.helplineRulerCtrl1.dbPanel1.BackColor = SystemColors.ControlDarkDark;
            else
                this.helplineRulerCtrl2.dbPanel1.BackColor = this.helplineRulerCtrl1.dbPanel1.BackColor = SystemColors.Control;
        }

        private void Button28_Click(object sender, EventArgs e)
        {
            this._dontAskOnClosing = true;
        }

        private void SetControls(bool e)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    foreach (Control ct in this.panel1.Controls)
                    {
                        if (ct.Name != "btnCancel" && !(ct is PictureBox))
                            ct.Enabled = e;
                    }

                    this.helplineRulerCtrl1.Enabled = this.helplineRulerCtrl2.Enabled = e;

                    this.Cursor = e ? Cursors.Default : Cursors.WaitCursor;
                }));
            }
            else
            {
                foreach (Control ct in this.panel1.Controls)
                {
                    if (ct.Name != "btnCancel" && !(ct is PictureBox))
                        ct.Enabled = e;
                }

                this.helplineRulerCtrl1.Enabled = this.helplineRulerCtrl2.Enabled = e;

                this.Cursor = e ? Cursors.Default : Cursors.WaitCursor;
            }
        }

        private void SetBitmap(Bitmap bitmapToSet, Bitmap bitmapToBeSet, Control ct, string property)
        {
            Bitmap bOld = bitmapToSet;

            bitmapToSet = bitmapToBeSet;

            if (bOld != null && bOld.Equals(bitmapToBeSet) == false)
                bOld.Dispose();

            if (ct != null)
            {
                if (ct.GetType().GetProperties().Where((a) => a.Name == property).Count() > 0)
                {
                    System.Reflection.PropertyInfo pi = ct.GetType().GetProperty(property);
                    pi.SetValue(ct, bitmapToBeSet);
                }
            }
        }

        private void SetBitmap(ref Bitmap bitmapToSet, ref Bitmap bitmapToBeSet)
        {
            Bitmap bOld = bitmapToSet;

            bitmapToSet = bitmapToBeSet;

            if (bOld != null && bOld.Equals(bitmapToBeSet) == false)
                bOld.Dispose();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_pic_changed & !_dontAskOnClosing)
            {
                DialogResult dlg = MessageBox.Show("Save image?", "Unsaved data", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                if (dlg == DialogResult.Yes)
                {
                    button2.PerformClick();
                    e.Cancel = true;
                }
                else if (dlg == DialogResult.Cancel)
                    e.Cancel = true;
            }

            if (!e.Cancel)
            {
                if (this.backgroundWorker1.IsBusy)
                {
                    this.backgroundWorker1.CancelAsync();
                    return;
                }

                if (this._gc != null)
                {
                    this._gc.ShowInfo -= _gc_ShowInfo;
                    this._gc.Dispose();
                }

                if (this._bmpBU != null)
                    this._bmpBU.Dispose();
                if (this._b4Copy != null)
                    this._b4Copy.Dispose();
                if (this._bResCopy != null)
                    this._bResCopy.Dispose();
                if (this._bResCopyTransp != null)
                    this._bResCopyTransp.Dispose();
                if (this._undoOPCache != null)
                    this._undoOPCache.Dispose();

                if (this._pathList != null)
                {
                    foreach (int j in this._pathList.Keys)
                        this._pathList[j].Dispose();
                }
            }
        }

        private void _gc_ShowInfo(object sender, string e)
        {
            if (InvokeRequired)
                this.Invoke(new Action(() => this.toolStripStatusLabel4.Text = e));
            else
                this.toolStripStatusLabel4.Text = e;
        }

        private void frmGrabCut_Load(object sender, EventArgs e)
        {
            this.cbBGColor_CheckedChanged(this.cbBGColor, new EventArgs());
            this.cmbCurrentColor.SelectedIndex = 0;

            this.cbQuickEst_CheckedChanged(this.cbQuickEst, new EventArgs());
            this.cbDraw_CheckedChanged(this.cbDraw, new EventArgs());

            this.numAlgMaxIter.Maximum = Int32.MaxValue;
            this.numAlgMaxIter.Value = (decimal)this._algMaxIter;
            this.numQATH.Maximum = Int32.MaxValue;
            this.numQATH.Value = (decimal)this._algQATH;
        }

        private double CheckWidthHeight(Bitmap bmp, bool fp)
        {
            double r = 1.0;
            if (fp)
            {
                r = (double)Math.Max(bmp.Width, bmp.Height) / (double)_maxSize;
                return r;
            }

            int res = 1;
            if (bmp.Width * bmp.Height > _maxSize * _maxSize * 256L)
            {
                res = 32;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 128L)
            {
                res = 24;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 64L)
            {
                res = 16;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 32L)
            {
                res = 12;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 16L)
            {
                res = 8;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 8L)
            {
                res = 6;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 4L)
            {
                res = 4;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize * 2L)
            {
                res = 3;
            }
            else if (bmp.Width * bmp.Height > _maxSize * _maxSize)
            {
                res = 2;
            }

            return res;
        }

        private void cbQuickEst_CheckedChanged(object sender, EventArgs e)
        {        
            this.btnMinCut.Enabled = !this.cbQuickEst.Checked;
        }

        private void btnRect_Click(object sender, EventArgs e)
        {
            Rectangle rc = new Rectangle(this._rX, this._rY, this._rW, this._rH);
            using (frmRect frm = new frmRect(rc))
            {
                frm.TopMost = true;

                frm.numX.ValueChanged += NumX_ValueChanged;
                frm.numY.ValueChanged += NumY_ValueChanged;
                frm.numW.ValueChanged += NumW_ValueChanged;
                frm.numH.ValueChanged += NumH_ValueChanged;

                frm.ShowDialog();
            }
        }

        private void NumH_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            this._rH = (int)num.Value;
            this._eH = (int)((double)num.Value * (double)this.helplineRulerCtrl1.Zoom);

            this.helplineRulerCtrl1.dbPanel1.Invalidate();
        }

        private void NumW_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            this._rW = (int)num.Value;
            this._eW = (int)((double)num.Value * (double)this.helplineRulerCtrl1.Zoom);

            this.helplineRulerCtrl1.dbPanel1.Invalidate();
        }

        private void NumY_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            this._rY = (int)num.Value;
            this._eY = (int)((double)num.Value * (double)this.helplineRulerCtrl1.Zoom);

            this.helplineRulerCtrl1.dbPanel1.Invalidate();
        }

        private void NumX_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            this._rX = (int)num.Value;
            this._eX = (int)((double)num.Value * (double)this.helplineRulerCtrl1.Zoom);

            this.helplineRulerCtrl1.dbPanel1.Invalidate();
        }

        private void numAlgMaxIter_ValueChanged(object sender, EventArgs e)
        {
            this._algMaxIter = (int)this.numAlgMaxIter.Value;

            if (this._gc != null && this._gc.Alg != null)
                this._gc.MaxIter = this._gc.Alg.MaxIter = (int)this._algMaxIter;

            if (this._gc != null && this._gc.AlgBK != null)
                this._gc.MaxIter = this._gc.AlgBK.MaxIter = (int)this._algMaxIter;
        }

        private void numQATH_ValueChanged(object sender, EventArgs e)
        {
            this._algQATH = (int)this.numQATH.Value;

            if (this._gc != null && this._gc.Alg != null)
                if (this._gc.Alg is IQATHAlg)
                    ((IQATHAlg)this._gc.Alg).QATH = (int)this._algQATH;
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            //reset state
            if (this.backgroundWorker1.IsBusy || this.backgroundWorker2.IsBusy)
            {
                if (this.backgroundWorker1.IsBusy)
                    this.backgroundWorker1.CancelAsync();

                if (this.backgroundWorker2.IsBusy)
                    this.backgroundWorker2.CancelAsync();

                return;
            }

            //reset variable
            this._maxSize = (int)this.numMaxSize.Value;

            if (!this.backgroundWorker1.IsBusy && this.helplineRulerCtrl1.Bmp != null)
            {
                //get the resizeFactor
                double res = CheckWidthHeight(this.helplineRulerCtrl1.Bmp, true);
                //this.lblResPic.Text = res.ToString();

                //this test is not corresponding to the real amount of RAM being used by the algorithms. It's more or less just an indicator, if to start at all.
                if (AvailMem.AvailMem.checkAvailRam(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Bmp.Height * 100L))
                {
                    //reset
                    if (this.cbRectMode.Enabled && this._gc != null)
                    {
                        this._gc.ShowInfo -= _gc_ShowInfo;
                        this._gc.Dispose();
                        this._gc = null;
                    }

                    //ensure gammaChanged = false on reset
                    if (this.cbRectMode.Checked || this.cbScribbleMode.Checked)
                    {
                        this._oldGamma = (double)this.numGamma.Value;
                        this._oldXi = 1.0;
                    }

                    //now get the needed parameters from the controls
                    //get some of them before we disabel the UI-Controls
                    int intMult = 1;
                    double gamma = (double)this.numGamma.Value;
                    double xi = 1.0;
                    bool setPFGToFG = this.cbSetPFGToFG.Checked;

                    bool skipLearn = this.cbSkipLearn.Enabled && this.cbSkipLearn.Checked;

                    this.SetControls(false);
                    this.btnGo.Text = "Cancel";
                    this.btnGo.Enabled = true;
                    this.toolStripProgressBar1.Value = 0;
                    this.toolStripProgressBar1.Visible = true;
                    this.label39.Enabled = this.numAlgMaxIter.Enabled = true;
                    this.label40.Enabled = this.numQATH.Enabled = true;

                    this.toolStripStatusLabel1.Text = "resFactor: " + Math.Max(res, 1).ToString("N2");

                    int gmm_comp = (int)this.numGmmComp.Value;

                    int num_Iters = 1;
                    bool rectMode = this.cbRectMode.Checked;
                    bool scribbleMode = this.cbScribbleMode.Checked;
                    if (this._rW == 0)
                        this._rW = this.helplineRulerCtrl1.Bmp.Width;
                    if (this._rH == 0)
                        this._rH = this.helplineRulerCtrl1.Bmp.Height;
                    Rectangle r = new Rectangle(this._rX, this._rY, this._rW, this._rH);
                    bool autoBias = false;
                    bool skipInit = !(rectMode || scribbleMode);
                    bool workOnPaths = !(rectMode || scribbleMode) && this.cbDraw.Checked;
                    int wh = (int)this.numWH.Value;
                    Bitmap bWork = new Bitmap(this.helplineRulerCtrl1.Bmp);
                    bool gammaChanged = gamma - this._oldGamma != 0;
                    gammaChanged |= xi - this._oldXi != 0;
                    this._oldGamma = gamma;
                    this._oldXi = xi;
                    bool useEightAdj = this.cbEightAdj.Checked;
                    bool useTh = this.cbUseTh.Checked;
                    double th = useTh ? (double)this.numDblMult.Value : 0;
                    bool initWKpp = true;

                    bool multCapacitiesForTLinks = this.cbMultTLCap.Checked;
                    double multTLinkCapacity = (double)this.numMultTLCap.Value;
                    bool castTLInt = cbCastTLInt.Checked;
                    bool getSourcePart = false;

                    ListSelectionMode selMode = this._KMeansSelMode;

                    double probMult1 = (double)this.numProbMult1.Value;

                    double numItems = this._items;
                    double numCorrect = this._correctItems;
                    double numItems2 = this._items2;
                    double numCorrect2 = this._correctItems2;

                    double kmInitW = this._KMeansInitW;
                    double kmInitH = this._KMeansInitH;

                    int KMeansInitIters = this._KMeansInitIters;
                    bool kMInitRnd = this._kMInitRnd;
                    int KMeansIters = this._KMeansIters;

                    int comp = (int)this.numMaxComponents.Value;
                    bool autoThreshold = this.cbAutoThreshold.Checked;

                    //now start the work
                    this.backgroundWorker1.RunWorkerAsync(new object[] { bWork, gmm_comp, gamma, num_Iters,
                        rectMode, r, autoBias, skipInit, workOnPaths,
                        wh, gammaChanged, intMult, useEightAdj, useTh, th, xi, res, initWKpp,
                        multCapacitiesForTLinks, multTLinkCapacity, castTLInt, getSourcePart, selMode,
                        scribbleMode, this._scribbles, probMult1, kmInitW, kmInitH, setPFGToFG,
                        numItems, numCorrect, numItems2, numCorrect2, skipLearn, comp, autoThreshold,
                        KMeansInitIters, kMInitRnd, KMeansIters});
                }
            }
        }

        private unsafe void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] o = (object[])e.Argument;

            Bitmap bRes = null;
            int gmm_comp = (int)o[1];
            double gamma = (double)o[2];
            int numIters = (int)o[3];
            bool rectMode = (bool)o[4];
            Rectangle r = (Rectangle)o[5];
            bool autoBias = (bool)o[6];
            bool skipInit = (bool)o[7];
            bool workOnPaths = (bool)o[8];
            int wh = (int)o[9];
            bool gammaChanged = (bool)o[10];
            int intMult = (int)o[11];
            bool quick = true;
            bool useEightAdj = (bool)o[12];
            bool useTh = (bool)o[13];
            double th = (double)o[14];
            double xi = (double)o[15];
            double resPic = (double)o[16];
            bool initWKpp = (bool)o[17];
            bool multCapacitiesForTLinks = (bool)o[18];
            double multTLinkCapacity = (double)o[19];
            bool castTLInt = (bool)o[20];
            bool getSourcePart = (bool)o[21];
            ListSelectionMode selMode = (ListSelectionMode)o[22];
            bool scribbleMode = (bool)o[23];
            Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles = (Dictionary<int, Dictionary<int, List<List<Point>>>>)o[24];
            double probMult1 = (double)o[25];
            double kmInitW = (double)o[26];
            double kmInitH = (double)o[27];
            bool setPFGToFG = (bool)o[28];
            double numItems = (double)o[29];
            double numCorrect = (double)o[30];
            double numItems2 = (double)o[31];
            double numCorrect2 = (double)o[32];
            bool skipLearn = (bool)o[33];

            if (scribbleMode && !rectMode)
                r = new Rectangle(0, 0, this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);

            Rectangle clipRect = new Rectangle(0, 0, this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);
            bool dontFillPath = true;
            bool drawNumComp = true;
            int comp = (int)o[34];
            bool autoThreshold = (bool)o[35];
            int KMeansInitIters = (int)o[36];
            bool kMInitRnd = (bool)o[37];
            int KMeansIters = (int)o[38];

            //if we have a large pic that will be resized during these OPs
            //we need to resize also the scribbles, if present
            if (scribbleMode && resPic > 1)
            {
                if (this._scribbles != null)
                {
                    Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles2 = ResizeAllScribbles(this._scribbles, resPic);
                    this._scribblesBU = this._scribbles;
                    this._scribbles = scribbles2;
                    scribbles = this._scribbles;
                }
            }

            //resize the input bmp
            Bitmap bWork = (Bitmap)o[0];
            Bitmap bU2 = null;
            if (resPic > 1)
            {
                Bitmap bOld = bWork;
                bU2 = new Bitmap(bWork);
                bWork = ResampleDown(bWork, ref r, ref clipRect, resPic, scribbleMode, rectMode);
                if (bOld != null)
                {
                    bOld.Dispose();
                    bOld = null;
                }
            }

            //do a check to ensure a correct initialisation of the GC_OP
            Bitmap bTmp = new Bitmap(bWork);
            if (r.Width == 0 && r.Height == 0)
            {
                this.Invoke(new Action(() => { MessageBox.Show("No Image passed to function. Cancelled operation."); }));
                if (bU2 != null)
                    bU2.Dispose();
                e.Result = new Bitmap(bTmp);
                return;
            }

            //create the operator for the GrabcutALike methods
            //if we have already a gc prrsent, we set its params later
            if (this._gc == null)
            {
                this._gc = new GrabCutOp()
                {
                    Bmp = bWork,
                    Gmm_comp = gmm_comp,
                    Gamma = gamma,
                    NumIters = numIters,
                    RectMode = rectMode,
                    ScribbleMode = scribbleMode,
                    Scribbles = scribbles,
                    Rc = r,
                    BGW = this.backgroundWorker1,
                    QuickEstimation = quick,
                    EightAdj = useEightAdj,
                    UseThreshold = useTh,
                    Threshold = th,
                    MultCapacitiesForTLinks = multCapacitiesForTLinks,
                    MultTLinkCapacity = multTLinkCapacity,
                    CastIntCapacitiesForTLinks = castTLInt,
                    SelectionMode = selMode,
                    ProbMult1 = probMult1,
                    KMInitW = kmInitW,
                    KMInitH = kmInitH,
                    NumItems = numItems,
                    NumCorrect = numCorrect,
                    NumItems2 = numItems2,
                    NumCorrect2 = numCorrect2,
                    MaxIter = this._algMaxIter,
                    QATH = this._algQATH,
                    AutoThreshold = autoThreshold,
                    KMeansInitIters = KMeansInitIters,
                    kMInitRnd = kMInitRnd,
                    KMeansIters = KMeansIters
                };

                this._gc.ShowInfo += _gc_ShowInfo;
            }

            //now do the initialisation
            //eg create the mask, preclassify the imagedata, compute the smootheness function and init the Gmms
            if (!skipInit)
            {
                int it = this._gc.Init();

                if (this._gc.BGW != null && this._gc.BGW.WorkerSupportsCancellation && this._gc.BGW.CancellationPending)
                    it = -4;

                if (it != 0)
                {
                    if (bU2 != null)
                        bU2.Dispose();

                    switch (it)
                    {
                        case -1:
                            this.Invoke(new Action(() => { MessageBox.Show("No BGPixels found. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -2:
                            this.Invoke(new Action(() => { MessageBox.Show("No FGPixels found. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -3:
                            this.Invoke(new Action(() => { MessageBox.Show("No Image passed to function. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -4:
                            this.Invoke(new Action(() => { MessageBox.Show("Operation cancelled."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -5:
                            this.Invoke(new Action(() => { MessageBox.Show("Mask is null. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                    }
                }
            }
            else
            {
                this._gc.Gamma = gamma;
                this._gc.GammaChanged = gammaChanged;
                this._gc.NumIters = numIters;
                this._gc.Rc = r;
                this._gc.QuickEstimation = quick;
                this._gc.EightAdj = useEightAdj;
                this._gc.UseThreshold = useTh;
                this._gc.Threshold = th;
                this._gc.MultCapacitiesForTLinks = multCapacitiesForTLinks;
                this._gc.MultTLinkCapacity = multTLinkCapacity;
                this._gc.CastIntCapacitiesForTLinks = castTLInt;
                this._gc.SelectionMode = selMode;
                this._gc.ProbMult1 = probMult1;
                this._gc.KMInitW = kmInitW;
                this._gc.KMInitH = kmInitH;
                this._gc.NumItems = numItems;
                this._gc.NumCorrect = numCorrect;
                this._gc.NumItems2 = numItems2;
                this._gc.NumCorrect2 = numCorrect2;
                this._gc.AutoThreshold = autoThreshold;
                this._gc.KMeansInitIters = KMeansInitIters;
                this._gc.kMInitRnd = kMInitRnd;
                this._gc.KMeansIters = KMeansIters;

                if (!workOnPaths && this._gc.ScribbleMode && this._gc.Scribbles != null && this._gc.Scribbles.Count > 0)
                {
                    if (!this._gc.RectMode)
                        r = new Rectangle(0, 0, bWork.Width, bWork.Height);
                    this._gc.ReInitScribbles();
                }
            }

            //if we use scribbles on the previously computed result
            if (workOnPaths)
            {
                if (this._allPoints != null && this._allPoints.Count > 0)
                {
                    this._gc.GammaChanged = gammaChanged;

                    Dictionary<int, List<Tuple<List<Point>, int>>> allPts2 = this._allPoints;

                    if (resPic > 1)
                        allPts2 = ResizeAllPoints(this._allPoints, resPic);

                    Dictionary<int, List<Tuple<List<Point>, int>>> allPts4 = allPts2;

                    this._gc.SetAllPointsInMask(allPts4, setPFGToFG);

                    this._gc.SkipLearn = skipLearn;
                }
            }

            //now do the work ...
            int l = this._gc.Run();

            if (l != 0)
            {
                if (bU2 != null)
                    bU2.Dispose();

                switch (l)
                {
                    case -1:
                        this.Invoke(new Action(() => { MessageBox.Show("Arrays-Length, or Graph-Length failed test. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case -25:
                        this.Invoke(new Action(() => { MessageBox.Show("Graph-Construction failed. Maybe the threshold is too big. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 100:
                        this.Invoke(new Action(() => { MessageBox.Show("Bmp_width or Bmp_height = 0. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 101:
                        this.Invoke(new Action(() => { MessageBox.Show("Operation cancelled."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 102:
                        this.Invoke(new Action(() => { MessageBox.Show("At least one GMM is null. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 103:
                        this.Invoke(new Action(() => { MessageBox.Show("This Mode only makes sense with RectMode."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 104:
                        this.Invoke(new Action(() => { MessageBox.Show("This Mode only makes sense with ScribbleMode."); }));
                        e.Result = new Bitmap(bTmp);
                        return;
                }
            }

            //... and get the result ...
            List<int> res = this._gc.Result;

            //... and the result image
            bRes = new Bitmap(bWork.Width, bWork.Height);

            int[,] m = this._gc.Mask;

            if ((scribbleMode && !rectMode) || workOnPaths)
                r = new Rectangle(0, 0, bWork.Width, bWork.Height);

            //lock the bmps for fast processing
            BitmapData bmData = bRes.LockBits(new Rectangle(0, 0, bRes.Width, bRes.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmWork = bTmp.LockBits(new Rectangle(0, 0, bTmp.Width, bTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int w = bTmp.Width;
            int h = bTmp.Height;
            int stride = bmData.Stride;

            //get the references to the pointer addresses
            byte* p = (byte*)bmData.Scan0;
            byte* pWork = (byte*)bmWork.Scan0;

            //for (int i = 0; i < res.Count(); i++)
            //{
            //    int j = res[i];
            //    int x = j % w;
            //    int y = j / w;

            //    p[x * 4 + y * stride] = pWork[x * 4 + y * stride];
            //    p[x * 4 + y * stride + 1] = pWork[x * 4 + y * stride + 1];
            //    p[x * 4 + y * stride + 2] = pWork[x * 4 + y * stride + 2];
            //    p[x * 4 + y * stride + 3] = pWork[x * 4 + y * stride + 3];
            //}

            //write the data
            int ww = m.GetLength(0);
            int hh = m.GetLength(1);

            for (int y = 0; y < h; y++)
            {
                if (y + this._numShiftY > 0 && y + this._numShiftY < h)
                    for (int x = 0; x < w; x++)
                    {
                        if (x + this._numShiftX > 0 && x + this._numShiftX < w)
                            if (x < ww && y < hh && r.Contains(x, y) && (m[x, y] == 1 || m[x, y] == 3))
                            {
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride] = pWork[x * 4 + y * stride];
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride + 1] = pWork[x * 4 + y * stride + 1];
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride + 2] = pWork[x * 4 + y * stride + 2];
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride + 3] = pWork[x * 4 + y * stride + 3];
                            }
                    }
            }

            //and unlock the bmps
            bTmp.UnlockBits(bmWork);
            bRes.UnlockBits(bmData);

            //now do some analysis of the result, to be able to redraw the resultpic with a different set of components
            Bitmap bResCopy = new Bitmap(bRes);
            Bitmap bCTransp = new Bitmap(bRes);

            //BU of the original result
            this.SetBitmap(ref this._bResCopy, ref bResCopy);

            //use a ChainCode [that works on the - invisible - "cracks" between the pixels, because it is very fast aand reliable]
            List<ChainCode> c = GetBoundary(bRes, 0, false);
            c = c.OrderByDescending(x => x.Coord.Count).ToList();

            int comp2 = c.Count;

            if (c.Count > 0)
            {
                //if we have a very lot of components, allow the user to restrict the amount
                if (c.Count > 1000 && (comp > 1000 || !drawNumComp))
                {
                    using (frmDrawNumComp frm = new frmDrawNumComp(c.Count))
                    {
                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            if (frm.checkBox1.Checked)
                            {
                                drawNumComp = true;
                                comp = comp2 = (int)frm.numericUpDown1.Value;
                            }
                        }
                    }
                }

                //now begin to redraw each component
                using (Graphics gx = Graphics.FromImage(bRes))
                {
                    gx.Clear(Color.Transparent);

                    int amnt = (!drawNumComp) ? c.Count : Math.Min(comp, c.Count);

                    for (int i = 0; i < amnt; i++)
                    {
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            PointF[] pts = c[i].Coord.Select(pt => new PointF(pt.X, pt.Y)).ToArray();
                            //make sure, each path is treated as an "outer-outline"
                            gp.FillMode = FillMode.Winding;
                            gp.AddLines(pts);
                            gp.CloseAllFigures();

                            //tmp try...catch
                            try
                            {
                                using (TextureBrush tb = new TextureBrush(bTmp))
                                    gx.FillPath(tb, gp);

                                //using (Pen pen = new Pen(Color.Red, 2))
                                //    gx.DrawPath(pen, gp);
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(exc.ToString());
                            }
                        }
                    }
                }
            }

            //now redraw the inner outlines and "transparent" components
            //we can do this in the easy way with setting graphics.CompositionMode to sourceCopy
            //because the whole operations are full_pixel_wise (at least for the standard 4-connectivity)
            if (dontFillPath && c.Count > 0)
            {
                int amnt = (!drawNumComp) ? c.Count : Math.Min(comp, c.Count);

                for (int i = 0; i < amnt; i++)
                {
                    ChainCode cc = c[i];
                    if (ChainFinder.IsInnerOutline(cc))
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            try
                            {
                                gP.StartFigure();
                                PointF[] pts = cc.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                gP.AddLines(pts);

                                using (Graphics gx = Graphics.FromImage(bRes))
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
            }

            //get a backup - for later use - of this bmp with the transparent paths
            this.SetBitmap(ref this._bResCopyTransp, ref bCTransp);

            bTmp.Dispose();

            if (resPic > 1)
            {
                Bitmap bOld = bRes;

                bRes = ResampleUp(bRes, resPic, bU2, dontFillPath, false);
                Bitmap bRCopy = ResampleUp(this._bResCopy, resPic, bU2, dontFillPath, false);
                Bitmap bRCopy2 = ResampleUp(this._bResCopyTransp, resPic, bU2, true, true);

                this.SetBitmap(ref this._bResCopy, ref bRCopy);
                this.SetBitmap(ref this._bResCopyTransp, ref bRCopy2);

                //bU2.Dispose();

                if (bOld != null)
                    bOld.Dispose();
            }
            else if (resPic == 1)
            {
                //set the list of all found paths [chains] to re_use later
                List<ChainCode> allChains = GetBoundary(this._bResCopyTransp);
                this._allChains = allChains;

                if (allChains != null && allChains.Count > 0)
                {
                    int area = allChains.Sum(a => a.Area);
                    int pxls = w * h;

                    int fc = pxls / area;

                    //if we have almost no output, maybe the initialization of the Gmms hasn't been good enough to receive a reasonable result
                    //so restart with some different KMeans initialization, if wanted
                    if (fc > 1000)
                        if (MessageBox.Show("Amount pixels to segmented area ratio is " + fc.ToString() + "." +
                            "Rerun with different Initialization of the Gmms?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                        {
                            this._restartDiffInit = true;
                        }
                }
            }

            //reset the scriblles, if resized
            if (scribbleMode && resPic > 1)
                if (this._scribblesBU != null)
                    this._scribbles = this._scribblesBU;

            //our result pic
            e.Result = bRes;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!InvokeRequired)
            {
                if (!this.IsDisposed && this.Visible && !this.toolStripProgressBar1.IsDisposed)
                    this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
            }
            else
                this.Invoke(new Action(() =>
                {
                    if (!this.IsDisposed && this.Visible && !this.toolStripProgressBar1.IsDisposed)
                        this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
                }));
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //if we have a result pic, display it
            if (e.Result != null)
            {
                Bitmap b = (Bitmap)e.Result;

                this.SetBitmap(this.helplineRulerCtrl2.Bmp, b, this.helplineRulerCtrl2, "Bmp");

                Bitmap bC = new Bitmap(this.helplineRulerCtrl2.Bmp);
                this.SetBitmap(ref this._b4Copy, ref bC);

                this.helplineRulerCtrl2.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl2.MakeBitmap(this.helplineRulerCtrl2.Bmp);
                this.helplineRulerCtrl2.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl2.Bmp.Width * this.helplineRulerCtrl2.Zoom),
                    (int)(this.helplineRulerCtrl2.Bmp.Height * this.helplineRulerCtrl2.Zoom));

                _undoOPCache.Add(b);

                //if (this._resWC > 1)
                //    ResampleAndTranslateBack();

                //if (this._bResCopyTransp != null)
                //{
                //    List<ChainCode> c2 = GetBoundary(this._bResCopyTransp, 0, false);
                //    c2 = c2.OrderByDescending(x => x.Coord.Count).ToList();

                //    //this.numRedrawComponents.Value = this.numRedrawComponents.Maximum = c2.Count;
                //}
            }

            //re enable the UI
            this.SetControls(true);

            btnReset2.Enabled = true;
            this._pic_changed = true;

            //reset the UI-drawn lists
            if (this._allPoints != null)
                this._allPoints.Clear();
            if (this._pathList != null)
                this._pathList.Clear();
            if (this._points != null)
                this._points.Clear();

            this.helplineRulerCtrl2.dbPanel1.Invalidate();

            if (this.Timer3.Enabled)
                this.Timer3.Stop();

            this.Timer3.Start();
            this.btnGo.Text = "Go";

            //re init the bgw
            this.backgroundWorker1.Dispose();
            this.backgroundWorker1 = new BackgroundWorker();
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            this.backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            this.backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;

            if (this._gc != null)
                this._gc.BGW = this.backgroundWorker1;

            //do some UI settings
            this.cbQuickEst_CheckedChanged(this.cbQuickEst, new EventArgs());
            this.cbDraw_CheckedChanged(this.cbDraw, new EventArgs());

            //if we didnt have a good result and set the auto restart with different KMeans settings
            if (this._restartDiffInit)
            {
                this._restartDiffInit = false;
                this._KMeansInitW += 1;
                this._KMeansInitH += 1;

                bool bR = this.cbRectMode.Checked;
                bool bS = this.cbScribbleMode.Checked;

                this.button4_Click(this.btnReset2, new EventArgs());

                if (bR)
                    this.cbRectMode.Checked = true;
                if (bS)
                    this.cbScribbleMode.Checked = true;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();

                this.btnGo_Click(this.btnGo, new EventArgs());
            }

            //do some UI settings
            this.cbRectMode.Checked = false;
            this.cbRectMode.Enabled = false;
            this.cbScribbleMode.Checked = false;
            this.cbScribbleMode.Enabled = false;

            this.numMaxSize.Enabled = this.numGmmComp.Enabled = false;

            this.toolStripStatusLabel4.Text = "done";
        }

        private Dictionary<int, Dictionary<int, List<List<Point>>>> ResizeAllScribbles(Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles, double resWC)
        {
            Dictionary<int, Dictionary<int, List<List<Point>>>> res = new Dictionary<int, Dictionary<int, List<List<Point>>>>();

            foreach (int j in scribbles.Keys)
            {
                res.Add(j, new Dictionary<int, List<List<Point>>>());
                Dictionary<int, List<List<Point>>> ptsWH = scribbles[j];

                foreach (int wh in ptsWH.Keys)
                {
                    int newWH = (int)Math.Max(wh / resWC, 1);

                    if (res[j].ContainsKey(newWH))
                    {
                        List<List<Point>> ja = ptsWH[wh];

                        foreach (List<Point> pts in ja)
                        {
                            List<Point> newPts = new List<Point>();
                            Point[] transPts = pts.Select(a => new Point((int)(a.X / resWC), (int)(a.Y / resWC))).ToArray();
                            newPts.AddRange(transPts);

                            res[j][newWH].Add(newPts);
                        }
                    }
                    else
                    {
                        res[j].Add(newWH, new List<List<Point>>());

                        List<List<Point>> ja = ptsWH[wh];

                        foreach (List<Point> pts in ja)
                        {
                            List<Point> newPts = new List<Point>();
                            Point[] transPts = pts.Select(a => new Point((int)(a.X / resWC), (int)(a.Y / resWC))).ToArray();
                            newPts.AddRange(transPts);

                            res[j][newWH].Add(newPts);
                        }
                    }
                }
            }

            return res;
        }

        private Bitmap ResampleDown(Bitmap bWork, double resPic)
        {
            Bitmap bOut = new Bitmap((int)Math.Ceiling(bWork.Width / resPic), (int)Math.Ceiling(bWork.Height / resPic));

            using (Graphics gx = Graphics.FromImage(bOut))
                gx.DrawImage(bWork, 0, 0, bOut.Width, bOut.Height);

            return bOut;
        }

        private Bitmap ResampleDown(Bitmap bWork, ref Rectangle r, ref Rectangle r2, double resPic, bool scribbleMode, bool rectMode)
        {
            Bitmap bOut = new Bitmap((int)Math.Ceiling(bWork.Width / resPic), (int)Math.Ceiling(bWork.Height / resPic));
            if (!scribbleMode || rectMode)
            {
                r.X = (int)(r.X / resPic);
                r.Y = (int)(r.Y / resPic);
                r.Width = (int)(r.Width / resPic);
                r.Height = (int)(r.Height / resPic);
            }
            r2.X = (int)(r2.X / resPic);
            r2.Y = (int)(r2.Y / resPic);
            r2.Width = (int)(r2.Width / resPic);
            r2.Height = (int)(r2.Height / resPic);
            using (Graphics gx = Graphics.FromImage(bOut))
                gx.DrawImage(bWork, 0, 0, bOut.Width, bOut.Height);

            return bOut;
        }

        private Bitmap ResampleUp(Bitmap bRes, double resPic, Bitmap bOrig, bool dontFillPath, bool disposebOrig)
        {
            //take orig image,
            //get chains from result pic
            //"cut" (crop) orig image with chains as Mask

            Bitmap bOut = new Bitmap(bOrig.Width, bOrig.Height);

            using (Bitmap bTmp = new Bitmap(bOrig.Width, bOrig.Height))
            {
                using (Graphics gx = Graphics.FromImage(bTmp))
                    gx.DrawImage(bRes, 0, 0, bOut.Width, bOut.Height);

                List<ChainCode> allChains = GetBoundary(bTmp);
                this._allChains = allChains;

                using (TextureBrush tb = new TextureBrush(bOrig))
                {
                    foreach (ChainCode c in allChains)
                    {
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            try
                            {
                                gP.StartFigure();
                                PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                gP.AddLines(pts);

                                using (Graphics gx = Graphics.FromImage(bOut))
                                    gx.FillPath(tb, gP);
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(exc.ToString());
                            }
                        }
                    }
                }

                if (dontFillPath)
                    foreach (ChainCode c in allChains)
                    {
                        if (ChainFinder.IsInnerOutline(c))
                            using (GraphicsPath gP = new GraphicsPath())
                            {
                                try
                                {
                                    gP.StartFigure();
                                    PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                    gP.AddLines(pts);

                                    using (Graphics gx = Graphics.FromImage(bOut))
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
            }

            if (disposebOrig)
                bOrig.Dispose();

            return bOut;
        }

        private List<ChainCode> GetBoundary(Bitmap bmp)
        {
            ChainFinder cf = new ChainFinder();
            cf.AllowNullCells = true;
            List<ChainCode> l = cf.GetOutline(bmp, 0, false, 0, false, 0, false);
            return l;
        }

        private Dictionary<int, List<Tuple<List<Point>, int>>> ShiftAllPoints(Dictionary<int, List<Tuple<List<Point>, int>>> allPoints, Rectangle clipRect)
        {
            Dictionary<int, List<Tuple<List<Point>, int>>> res = new Dictionary<int, List<Tuple<List<Point>, int>>>();
            foreach (int j in allPoints.Keys)
            {
                res.Add(j, new List<Tuple<List<Point>, int>>());

                List<Tuple<List<Point>, int>> points = allPoints[j];

                for (int i = 0; i < points.Count; i++)
                {
                    List<Point> pt = points[i].Item1;
                    int wh = points[i].Item2;
                    Tuple<List<Point>, int> ptRes = Tuple.Create(new List<Point>(), wh);

                    foreach (Point p in pt)
                        ptRes.Item1.Add(new Point(p.X - clipRect.X, p.Y - clipRect.Y));

                    res[j].Add(ptRes);
                }

            }
            return res;
        }

        private Dictionary<int, List<Tuple<List<Point>, int>>> ResizeAllPoints(Dictionary<int, List<Tuple<List<Point>, int>>> allPoints, double resPic)
        {
            Dictionary<int, List<Tuple<List<Point>, int>>> res = new Dictionary<int, List<Tuple<List<Point>, int>>>();
            foreach (int j in allPoints.Keys)
            {
                res.Add(j, new List<Tuple<List<Point>, int>>());

                List<Tuple<List<Point>, int>> points = allPoints[j];

                for (int i = 0; i < points.Count; i++)
                {
                    List<Point> pt = points[i].Item1;
                    int wh = (int)Math.Max(points[i].Item2 / resPic, 1);
                    Tuple<List<Point>, int> ptRes = Tuple.Create(new List<Point>(), wh);

                    foreach (Point p in pt)
                        ptRes.Item1.Add(new Point((int)(p.X / resPic), (int)(p.Y / resPic)));

                    res[j].Add(ptRes);
                }

            }
            return res;
        }


        private List<ChainCode> GetBoundary(Bitmap upperImg, int minAlpha, bool grayScale)
        {
            List<ChainCode> l = null;
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
                Console.WriteLine(exc.Message);
            }
            finally
            {
                if (bmpTmp != null)
                {
                    bmpTmp.Dispose();
                    bmpTmp = null;
                }
            }
            return l;
        }

        private void cbRectMode_CheckedChanged(object sender, EventArgs e)
        {
            if (this.cbRectMode.Checked)
                this.cbScribbleMode.Checked = false;

            SetRectOrScribblesValues(this.helplineRulerCtrl1.Zoom);
        }

        private void SetRectOrScribblesValues(float zoom)
        {
            if (this.cbRectMode.Checked)
            {
                Rectangle r = new Rectangle(this._rX, this._rY, this._rW, this._rH);
                this._eX = (int)(r.X * zoom);
                this._eY = (int)(r.Y * zoom);
                this._eW = (int)(r.Width * zoom);
                this._eH = (int)(r.Height * zoom);
            }

            //this._crX = 0;
            //this._crY = 0;
            //this._crW = this.helplineRulerCtrl1.Bmp.Width;
            //this._crH = this.helplineRulerCtrl1.Bmp.Height;

            this.btnRect.Enabled = this.cbRectMode.Checked;
        }

        private void cbScribbleMode_CheckedChanged(object sender, EventArgs e)
        {
            SetRectOrScribblesValues(this.helplineRulerCtrl1.Zoom);

            if (this.cbScribbleMode.Checked)
                this.cbRectMode.Checked = false;

            this.btnLoadScribbles.Enabled = this.btnSaveScribbles.Enabled = this.cbScribbleMode.Checked;
        }

        private void cbMultTLCap_CheckedChanged(object sender, EventArgs e)
        {
            this.cbCastTLInt.Enabled = this.cbMultTLCap.Checked;
        }

        private void btnRemLastScribbles_Click(object sender, EventArgs e)
        {
            this._points2.Clear();

            int fg = this.rbFG.Checked ? 1 : 0;
            int wh = (int)this.numWHScribbles.Value;

            if (this._scribbles != null && this._scribbles.ContainsKey(fg) && this._scribbles[fg].ContainsKey(wh))
            {
                List<List<Point>> j = this._scribbles[fg][wh];

                if (j != null && j.Count > 0)
                    j.RemoveAt(j.Count - 1);
            }

            this.helplineRulerCtrl1.dbPanel1.Invalidate();
        }

        private void btnClearScribbles_Click(object sender, EventArgs e)
        {
            this._points2.Clear();
            if (this._scribbles != null)
                this._scribbles.Clear();
            this.helplineRulerCtrl1.dbPanel1.Invalidate();
        }

        private void cbDraw_CheckedChanged(object sender, EventArgs e)
        {
            this._drawImgOverlay = cbDraw.Checked;
            this.cbSkipLearn.Enabled = cbDraw.Checked;
            this.cbSkipLearn.Checked = cbDraw.Checked;
            this.helplineRulerCtrl2.dbPanel1.Invalidate();
        }

        private void cmbCurrentColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            this._currentDraw = this.cmbCurrentColor.SelectedIndex;
        }

        private void btnRemStroke_Click(object sender, EventArgs e)
        {
            if (this._pathList == null)
                this._pathList = new Dictionary<int, GraphicsPath>();

            if (this._allPoints == null)
                this._allPoints = new Dictionary<int, List<Tuple<List<Point>, int>>>();

            if (this._allPoints.Count > 0 && this._lastDraw.Count > 0 && this._allPoints.ContainsKey(this._lastDraw[this._lastDraw.Count - 1]) && this._allPoints[this._lastDraw[this._lastDraw.Count - 1]].Count > 0)
                this._allPoints[this._lastDraw[this._lastDraw.Count - 1]].RemoveAt(this._allPoints[this._lastDraw[this._lastDraw.Count - 1]].Count - 1);

            if (this._lastDraw.Count > 0 && this._pathList.ContainsKey(this._lastDraw[this._lastDraw.Count - 1]))
            {
                GraphicsPath gp = this._pathList[this._lastDraw[this._lastDraw.Count - 1]];
                gp.Reset();

                if (this._allPoints.ContainsKey(this._lastDraw[this._lastDraw.Count - 1]))
                {
                    foreach (Tuple<List<Point>, int> l in this._allPoints[this._lastDraw[this._lastDraw.Count - 1]])
                    {
                        PointF[] z = l.Item1.Select(a => new PointF(a.X, a.Y)).ToArray();
                        gp.AddLines(z);
                    }
                }

                this._pathList[this._lastDraw[this._lastDraw.Count - 1]] = gp;

                if (this._lastDraw.Count > 0)
                    this._lastDraw.RemoveAt(this._lastDraw.Count - 1);
            }

            this.helplineRulerCtrl2.dbPanel1.Invalidate();
        }

        private void btnRecut_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null && this._allChains != null)
            {
                Bitmap bOut = new Bitmap(this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);
                Bitmap bC = new Bitmap(bOut);
                Bitmap bCT = new Bitmap(bOut);

                List<ChainCode> allChains = this._allChains;
                float wFactor = 1.0f; // ((float)bOut.Width + (float)this.numRCX.Value) / (float)bOut.Width;
                float hFactor = 1.0f; // ((float)bOut.Height + (float)this.numRCY.Value) / (float)bOut.Height;

                using (TextureBrush tb = new TextureBrush(this.helplineRulerCtrl1.Bmp))
                {
                    foreach (ChainCode c in allChains)
                    {
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            try
                            {
                                gP.StartFigure();
                                PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                gP.AddLines(pts);

                                using (Matrix mx = new Matrix(wFactor, 0, 0, hFactor, 0, 0))
                                    gP.Transform(mx);

                                using (Graphics gx = Graphics.FromImage(bOut), gx2 = Graphics.FromImage(bC), gx4 = Graphics.FromImage(bCT))
                                {
                                    gx.FillPath(tb, gP);
                                    gx2.FillPath(tb, gP);
                                    gx4.FillPath(tb, gP);
                                }
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(exc.ToString());
                            }
                        }
                    }

                    foreach (ChainCode c in allChains)
                    {
                        if (ChainFinder.IsInnerOutline(c))
                            using (GraphicsPath gP = new GraphicsPath())
                            {
                                try
                                {
                                    gP.StartFigure();
                                    PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                    gP.AddLines(pts);

                                    using (Matrix mx = new Matrix(wFactor, 0, 0, hFactor, 0, 0))
                                        gP.Transform(mx);

                                    using (Graphics gx = Graphics.FromImage(bOut))
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

                    foreach (ChainCode c in allChains)
                    {
                        if (ChainFinder.IsInnerOutline(c))
                            using (GraphicsPath gP = new GraphicsPath())
                            {
                                try
                                {
                                    gP.StartFigure();
                                    PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                    gP.AddLines(pts);

                                    using (Matrix mx = new Matrix(wFactor, 0, 0, hFactor, 0, 0))
                                        gP.Transform(mx);

                                    using (Graphics gx = Graphics.FromImage(bCT))
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
                }

                OnShowInfo("done");

                this.SetBitmap(this.helplineRulerCtrl2.Bmp, bOut, this.helplineRulerCtrl2, "Bmp");
                this.SetBitmap(ref this._bResCopy, ref bC);
                this.SetBitmap(ref this._bResCopyTransp, ref bCT);

                _undoOPCache.Add(bOut);

                this._pic_changed = true;

                this.helplineRulerCtrl2.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl2.MakeBitmap(this.helplineRulerCtrl2.Bmp);
                this.helplineRulerCtrl2.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl2.Bmp.Width * this.helplineRulerCtrl2.Zoom),
                    (int)(this.helplineRulerCtrl2.Bmp.Height * this.helplineRulerCtrl2.Zoom));
                this.helplineRulerCtrl2.dbPanel1.Invalidate();
            }
        }

        private void OnShowInfo(string message)
        {
            ShowInfo?.Invoke(this, message);
        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            this.Timer3.Stop();

            if (!this.toolStripProgressBar1.IsDisposed)
            {
                this.toolStripProgressBar1.Visible = false;
                this.toolStripProgressBar1.Value = 0;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
                this.helplineRulerCtrl2.dbPanel1.Invalidate();
            }
        }

        private void cmbZoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Visible && this.helplineRulerCtrl1.Bmp != null && !this._dontDoZoom)
            {
                this.helplineRulerCtrl1.Enabled = false;
                this.helplineRulerCtrl1.Refresh();
                this.helplineRulerCtrl1.SetZoom(cmbZoom.SelectedItem.ToString());
                this.helplineRulerCtrl1.Enabled = true;
                if (this.cmbZoom.SelectedIndex < 2)
                    this.helplineRulerCtrl1.ZoomSetManually = true;

                SetRectOrScribblesValues(this.helplineRulerCtrl1.Zoom);

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
            }
            if (this.Visible && this.helplineRulerCtrl2.Bmp != null && !this._dontDoZoom)
            {
                this.helplineRulerCtrl2.Enabled = false;
                this.helplineRulerCtrl2.Refresh();
                this.helplineRulerCtrl2.SetZoom(cmbZoom.SelectedItem.ToString());
                this.helplineRulerCtrl2.Enabled = true;
                if (this.cmbZoom.SelectedIndex < 2)
                    this.helplineRulerCtrl2.ZoomSetManually = true;

                this.helplineRulerCtrl2.dbPanel1.Invalidate();
            }
        }

        private void btnResQATH_Click(object sender, EventArgs e)
        {
            this.numQATH.Value = 1000000;
        }

        private void btnResMaxIter_Click(object sender, EventArgs e)
        {
            this.numAlgMaxIter.Value = Int32.MaxValue / 3;
        }

        private void btnMinCut_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker1.IsBusy || this.backgroundWorker2.IsBusy)
            {
                if (this.backgroundWorker1.IsBusy)
                    this.backgroundWorker1.CancelAsync();

                if (this.backgroundWorker2.IsBusy)
                    this.backgroundWorker2.CancelAsync();

                return;
            }

            this._maxSize = 300; //dont increase too much, running time grows exponentially

            if (MessageBox.Show("Reset Controls?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                this.cbUseTh.Checked = this.cbMultTLCap.Checked = false;
                this.numProbMult1.Value = (decimal)1.0;
            }

            if (!this.backgroundWorker2.IsBusy && this.helplineRulerCtrl1.Bmp != null)
            {
                double res = CheckWidthHeight(this.helplineRulerCtrl1.Bmp, true);
                //this.lblResPic.Text = res.ToString();

                //this test is not corresponding to the real amount of RAM being used by the algorithms. It's more or less just an indicator, if to start at all.
                if (AvailMem.AvailMem.checkAvailRam(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Bmp.Height * 100L))
                {
                    if (this.cbRectMode.Enabled && this._gc != null)
                    {
                        this._gc.ShowInfo -= _gc_ShowInfo;
                        this._gc.Dispose();
                        this._gc = null;
                    }

                    //ensure gammaChanged = false on reset
                    if (this.cbRectMode.Checked || this.cbScribbleMode.Checked)
                    {
                        this._oldGamma = (double)this.numGamma.Value;
                        this._oldXi = 1.0;
                    }

                    int intMult = 1;
                    double gamma = (double)this.numGamma.Value;
                    double xi = 1.0;
                    bool setPFGToFG = true;
                    bool skipLearn = this.cbSkipLearn.Enabled && this.cbSkipLearn.Checked;

                    this.SetControls(false);
                    this.btnMinCut.Text = "Cancel";
                    this.btnMinCut.Enabled = true;
                    this.toolStripProgressBar1.Value = 0;
                    this.toolStripProgressBar1.Visible = true;
                    this.label39.Enabled = this.numAlgMaxIter.Enabled = true;
                    this.label40.Enabled = this.numQATH.Enabled = true;

                    this.toolStripStatusLabel1.Text = "resFactor: " + Math.Max(res, 1).ToString("N2");

                    int gmm_comp = 5;

                    int num_Iters = 1;
                    bool rectMode = this.cbRectMode.Checked;
                    bool scribbleMode = this.cbScribbleMode.Checked;
                    Rectangle r = new Rectangle(this._rX, this._rY, this._rW, this._rH);
                    bool autoBias = false;
                    bool skipInit = !(rectMode || scribbleMode);
                    bool workOnPaths = !(rectMode || scribbleMode) && this.cbDraw.Checked;
                    int wh = (int)this.numWH.Value;
                    Bitmap bWork = new Bitmap(this.helplineRulerCtrl1.Bmp);
                    bool gammaChanged = gamma - this._oldGamma != 0;
                    gammaChanged |= xi - this._oldXi != 0;
                    this._oldGamma = gamma;
                    this._oldXi = xi;
                    bool useEightAdj = this.cbEightAdj.Checked;
                    bool useTh = this.cbUseTh.Checked;
                    double th = useTh ? (double)this.numDblMult.Value : 0;
                    bool initWKpp = true;

                    bool multCapacitiesForTLinks = this.cbMultTLCap.Checked;
                    double multTLinkCapacity = (double)this.numMultTLCap.Value;
                    bool castTLInt = cbCastTLInt.Checked;
                    bool getSourcePart = false;

                    ListSelectionMode selMode = this._KMeansSelMode;

                    double probMult1 = (double)this.numProbMult1.Value;



                    double numItems = this._items;
                    double numCorrect = this._correctItems;
                    double numItems2 = this._items2;
                    double numCorrect2 = this._correctItems2;

                    double kmInitW = this._KMeansInitW;
                    double kmInitH = this._KMeansInitH;

                    int KMeansInitIters = this._KMeansInitIters;
                    bool kMInitRnd = this._kMInitRnd;
                    int KMeansIters = this._KMeansIters;

                    int comp = (int)this.numMaxComponents.Value;
                    bool autoThreshold = this.cbAutoThreshold.Checked;

                    this.backgroundWorker2.RunWorkerAsync(new object[] { bWork, gmm_comp, gamma, num_Iters,
                        rectMode, r, autoBias, skipInit, workOnPaths,
                        wh, gammaChanged, intMult, useEightAdj, useTh, th, xi, res, initWKpp,
                        multCapacitiesForTLinks, multTLinkCapacity, castTLInt, getSourcePart, selMode,
                        scribbleMode, this._scribbles, probMult1, kmInitW, kmInitH, setPFGToFG,
                        numItems, numCorrect, numItems2, numCorrect2, skipLearn, comp, autoThreshold,
                        KMeansInitIters, kMInitRnd, KMeansIters});
                }
            }
        }

        private unsafe void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] o = (object[])e.Argument;

            Bitmap bRes = null;
            int gmm_comp = (int)o[1];
            double gamma = (double)o[2];
            int numIters = (int)o[3];
            bool rectMode = (bool)o[4];
            Rectangle r = (Rectangle)o[5];
            bool autoBias = (bool)o[6];
            bool skipInit = (bool)o[7];
            bool workOnPaths = (bool)o[8];
            int wh = (int)o[9];
            bool gammaChanged = (bool)o[10];
            int intMult = (int)o[11];
            bool quick = false;
            bool useEightAdj = (bool)o[12];
            bool useTh = (bool)o[13];
            double th = (double)o[14];
            double xi = (double)o[15];
            double resPic = (double)o[16];
            bool initWKpp = (bool)o[17];
            bool multCapacitiesForTLinks = (bool)o[18];
            double multTLinkCapacity = (double)o[19];
            bool castTLInt = (bool)o[20];
            bool getSourcePart = (bool)o[21];
            ListSelectionMode selMode = (ListSelectionMode)o[22];
            bool scribbleMode = (bool)o[23];
            Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles = (Dictionary<int, Dictionary<int, List<List<Point>>>>)o[24];
            double probMult1 = (double)o[25];
            double kmInitW = (double)o[26];
            double kmInitH = (double)o[27];
            bool setPFGToFG = (bool)o[28];
            double numItems = (double)o[29];
            double numCorrect = (double)o[30];
            double numItems2 = (double)o[31];
            double numCorrect2 = (double)o[32];
            bool skipLearn = (bool)o[33];
            int comp = (int)o[34];
            bool autoThreshold = (bool)o[35];
            int KMeansInitIters = (int)o[36];
            bool kMInitRnd = (bool)o[37];
            int KMeansIters = (int)o[38];

            if (scribbleMode && !rectMode)
                r = new Rectangle(0, 0, this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);

            Rectangle clipRect = new Rectangle(0, 0, this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);
            bool dontFillPath = true;
            bool drawNumComp = true;

            if (scribbleMode && resPic > 1)
            {
                if (this._scribbles != null)
                {
                    Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles2 = ResizeAllScribbles(this._scribbles, resPic);
                    this._scribblesBU = this._scribbles;
                    this._scribbles = scribbles2;
                    scribbles = this._scribbles;
                }
            }

            Bitmap bWork = (Bitmap)o[0];
            Bitmap bU2 = null;
            if (resPic > 1)
            {
                Bitmap bOld = bWork;
                bU2 = new Bitmap(bWork);
                bWork = ResampleDown(bWork, ref r, ref clipRect, resPic, scribbleMode, rectMode);
                if (bOld != null)
                {
                    bOld.Dispose();
                    bOld = null;
                }
            }

            Bitmap bTmp = new Bitmap(bWork);
            if (r.Width == 0 && r.Height == 0)
            {
                this.Invoke(new Action(() => { MessageBox.Show("No Image passed to function. Cancelled operation."); }));
                if (bU2 != null)
                    bU2.Dispose();
                e.Result = new Bitmap(bTmp);
                return;
            }

            if (this._gc == null)
            {
                this._gc = new GrabCutOp()
                {
                    Bmp = bWork,
                    Gmm_comp = gmm_comp,
                    Gamma = gamma,
                    NumIters = numIters,
                    RectMode = rectMode,
                    ScribbleMode = scribbleMode,
                    Scribbles = scribbles,
                    Rc = r,
                    BGW = this.backgroundWorker2,
                    QuickEstimation = quick,
                    EightAdj = useEightAdj,
                    UseThreshold = useTh,
                    Threshold = th,
                    MultCapacitiesForTLinks = multCapacitiesForTLinks,
                    MultTLinkCapacity = multTLinkCapacity,
                    CastIntCapacitiesForTLinks = castTLInt,
                    SelectionMode = selMode,
                    ProbMult1 = probMult1,
                    KMInitW = kmInitW,
                    KMInitH = kmInitH,
                    NumItems = numItems,
                    NumCorrect = numCorrect,
                    NumItems2 = numItems2,
                    NumCorrect2 = numCorrect2,
                    MaxIter = this._algMaxIter,
                    QATH = this._algQATH,
                    AutoThreshold = autoThreshold,
                    KMeansInitIters = KMeansInitIters,
                    kMInitRnd = kMInitRnd,
                    KMeansIters = KMeansIters
                };

                this._gc.ShowInfo += _gc_ShowInfo;
            }

            if (!skipInit)
            {
                int it = this._gc.Init();

                if (this._gc.BGW != null && this._gc.BGW.WorkerSupportsCancellation && this._gc.BGW.CancellationPending)
                    it = -4;

                if (it != 0)
                {
                    if (bU2 != null)
                        bU2.Dispose();

                    switch (it)
                    {
                        case -1:
                            this.Invoke(new Action(() => { MessageBox.Show("No BGPixels found. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -2:
                            this.Invoke(new Action(() => { MessageBox.Show("No FGPixels found. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -3:
                            this.Invoke(new Action(() => { MessageBox.Show("No Image passed to function. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -4:
                            this.Invoke(new Action(() => { MessageBox.Show("Operation cancelled."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                        case -5:
                            this.Invoke(new Action(() => { MessageBox.Show("Mask is null. Cancelled operation."); }));
                            e.Result = new Bitmap(bTmp);
                            return;
                    }
                }
            }
            else
            {
                this._gc.Gamma = gamma;
                this._gc.GammaChanged = gammaChanged;
                this._gc.NumIters = numIters;
                this._gc.Rc = r;
                this._gc.QuickEstimation = quick;
                this._gc.EightAdj = useEightAdj;
                this._gc.UseThreshold = useTh;
                this._gc.Threshold = th;
                this._gc.MultCapacitiesForTLinks = multCapacitiesForTLinks;
                this._gc.MultTLinkCapacity = multTLinkCapacity;
                this._gc.CastIntCapacitiesForTLinks = castTLInt;
                this._gc.SelectionMode = selMode;
                this._gc.ProbMult1 = probMult1;
                this._gc.KMInitW = kmInitW;
                this._gc.KMInitH = kmInitH;
                this._gc.NumItems = numItems;
                this._gc.NumCorrect = numCorrect;
                this._gc.NumItems2 = numItems2;
                this._gc.NumCorrect2 = numCorrect2;
                this._gc.AutoThreshold = autoThreshold;
                this._gc.KMeansInitIters = KMeansInitIters;
                this._gc.kMInitRnd = kMInitRnd;
                this._gc.KMeansIters = KMeansIters;

                if (!workOnPaths && this._gc.ScribbleMode && this._gc.Scribbles != null && this._gc.Scribbles.Count > 0)
                {
                    if (!this._gc.RectMode)
                        r = new Rectangle(0, 0, bWork.Width, bWork.Height);
                    this._gc.ReInitScribbles();
                }
            }

            if (workOnPaths)
            {
                if (this._allPoints != null && this._allPoints.Count > 0)
                {
                    this._gc.GammaChanged = gammaChanged;

                    Dictionary<int, List<Tuple<List<Point>, int>>> allPts2 = this._allPoints;

                    if (resPic > 1)
                        allPts2 = ResizeAllPoints(this._allPoints, resPic);

                    Dictionary<int, List<Tuple<List<Point>, int>>> allPts4 = allPts2;

                    this._gc.SetAllPointsInMask(allPts4, setPFGToFG);

                    this._gc.SkipLearn = skipLearn;
                }
            }

            int l = this._gc.RunMinCut();

            if (l != 0)
            {
                if (bU2 != null)
                    bU2.Dispose();

                switch (l)
                {
                    case -1:
                        this.Invoke(new Action(() => { MessageBox.Show("Arrays-Length, or Graph-Length failed test. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case -25:
                        this.Invoke(new Action(() => { MessageBox.Show("Graph-Construction failed. Maybe the threshold is too big. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 100:
                        this.Invoke(new Action(() => { MessageBox.Show("Bmp_width or Bmp_height = 0. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 101:
                        this.Invoke(new Action(() => { MessageBox.Show("Operation cancelled."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 102:
                        this.Invoke(new Action(() => { MessageBox.Show("At least one GMM is null. Cancelled operation."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 103:
                        this.Invoke(new Action(() => { MessageBox.Show("This Mode only makes sense with RectMode."); }));
                        e.Result = new Bitmap(bTmp);
                        return;

                    case 104:
                        this.Invoke(new Action(() => { MessageBox.Show("This Mode only makes sense with ScribbleMode."); }));
                        e.Result = new Bitmap(bTmp);
                        return;
                }
            }

            List<int> res = this._gc.Result;

            bRes = new Bitmap(bWork.Width, bWork.Height);

            int[,] m = this._gc.Mask;

            if ((scribbleMode && !rectMode) || workOnPaths)
                r = new Rectangle(0, 0, bWork.Width, bWork.Height);

            BitmapData bmData = bRes.LockBits(new Rectangle(0, 0, bRes.Width, bRes.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmWork = bTmp.LockBits(new Rectangle(0, 0, bTmp.Width, bTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int w = bTmp.Width;
            int h = bTmp.Height;
            int stride = bmData.Stride;

            byte* p = (byte*)bmData.Scan0;
            byte* pWork = (byte*)bmWork.Scan0;

            //for (int i = 0; i < res.Count(); i++)
            //{
            //    int j = res[i];
            //    int x = j % w;
            //    int y = j / w;

            //    p[x * 4 + y * stride] = pWork[x * 4 + y * stride];
            //    p[x * 4 + y * stride + 1] = pWork[x * 4 + y * stride + 1];
            //    p[x * 4 + y * stride + 2] = pWork[x * 4 + y * stride + 2];
            //    p[x * 4 + y * stride + 3] = pWork[x * 4 + y * stride + 3];
            //}

            int ww = m.GetLength(0);
            int hh = m.GetLength(1);

            for (int y = 0; y < h; y++)
            {
                if (y + this._numShiftY > 0 && y + this._numShiftY < h)
                    for (int x = 0; x < w; x++)
                    {
                        if (x + this._numShiftX > 0 && x + this._numShiftX < w)
                            if (x < ww && y < hh && r.Contains(x, y) && (m[x, y] == 1 || m[x, y] == 3))
                            {
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride] = pWork[x * 4 + y * stride];
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride + 1] = pWork[x * 4 + y * stride + 1];
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride + 2] = pWork[x * 4 + y * stride + 2];
                                p[(x + this._numShiftX) * 4 + (y + this._numShiftY) * stride + 3] = pWork[x * 4 + y * stride + 3];
                            }
                    }
            }

            bTmp.UnlockBits(bmWork);
            bRes.UnlockBits(bmData);

            Bitmap bResCopy = new Bitmap(bRes);
            Bitmap bCTransp = new Bitmap(bRes);

            this.SetBitmap(ref this._bResCopy, ref bResCopy);

            List<ChainCode> c = GetBoundary(bRes, 0, false);
            c = c.OrderByDescending(x => x.Coord.Count).ToList();

            int comp2 = c.Count;

            if (/*!dontFillPath &&*/ c.Count > 0)
            {
                if (c.Count > 1000 && (comp > 1000 || !drawNumComp))
                {
                    using (frmDrawNumComp frm = new frmDrawNumComp(c.Count))
                    {
                        if (frm.ShowDialog() == DialogResult.OK)
                        {
                            if (frm.checkBox1.Checked)
                            {
                                drawNumComp = true;
                                comp = comp2 = (int)frm.numericUpDown1.Value;
                            }
                        }
                    }
                }

                using (Graphics gx = Graphics.FromImage(bRes))
                {
                    gx.Clear(Color.Transparent);

                    int amnt = (!drawNumComp) ? c.Count : Math.Min(comp, c.Count);

                    for (int i = 0; i < amnt; i++)
                    {
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            PointF[] pts = c[i].Coord.Select(pt => new PointF(pt.X, pt.Y)).ToArray();
                            gp.FillMode = FillMode.Winding;
                            gp.AddLines(pts);
                            gp.CloseAllFigures();

                            //tmp try...catch
                            try
                            {
                                using (TextureBrush tb = new TextureBrush(bTmp))
                                    gx.FillPath(tb, gp);

                                //using (Pen pen = new Pen(Color.Red, 2))
                                //    gx.DrawPath(pen, gp);
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(exc.ToString());
                            }
                        }
                    }
                }
            }

            if (dontFillPath && c.Count > 0)
            {
                int amnt = (!drawNumComp) ? c.Count : Math.Min(comp, c.Count);

                for (int i = 0; i < amnt; i++)
                {
                    ChainCode cc = c[i];
                    if (ChainFinder.IsInnerOutline(cc))
                        using (GraphicsPath gP = new GraphicsPath())
                        {
                            try
                            {
                                gP.StartFigure();
                                PointF[] pts = cc.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                gP.AddLines(pts);

                                using (Graphics gx = Graphics.FromImage(bRes))
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
            }

            this.SetBitmap(ref this._bResCopyTransp, ref bCTransp);

            bTmp.Dispose();

            if (resPic > 1)
            {
                Bitmap bOld = bRes;

                bRes = ResampleUp(bRes, resPic, bU2, dontFillPath, false);
                Bitmap bRCopy = ResampleUp(this._bResCopy, resPic, bU2, dontFillPath, false);
                Bitmap bRCopy2 = ResampleUp(this._bResCopyTransp, resPic, bU2, true, true);

                this.SetBitmap(ref this._bResCopy, ref bRCopy);
                this.SetBitmap(ref this._bResCopyTransp, ref bRCopy2);

                //bU2.Dispose();

                if (bOld != null)
                    bOld.Dispose();
            }
            else if (resPic == 1)
            {
                List<ChainCode> allChains = GetBoundary(this._bResCopyTransp);
                this._allChains = allChains;

                if (allChains != null && allChains.Count > 0)
                {
                    int area = allChains.Sum(a => a.Area);
                    int pxls = w * h;

                    int fc = pxls / area;

                    if (fc > 1000)
                        if (MessageBox.Show("Amount pixels to segmented area ratio is " + fc.ToString() + "." +
                            "Rerun with different Initialization of the Gmms?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                        {
                            this._restartDiffInit = true;
                        }
                }
            }

            if (scribbleMode && resPic > 1)
                if (this._scribblesBU != null)
                    this._scribbles = this._scribblesBU;

            e.Result = bRes;

        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!InvokeRequired)
            {
                if (!this.IsDisposed && this.Visible && !this.toolStripProgressBar1.IsDisposed)
                    this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
            }
            else
                this.Invoke(new Action(() =>
                {
                    if (!this.IsDisposed && this.Visible && !this.toolStripProgressBar1.IsDisposed)
                        this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
                }));
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                Bitmap b = (Bitmap)e.Result;

                this.SetBitmap(this.helplineRulerCtrl2.Bmp, b, this.helplineRulerCtrl2, "Bmp");

                Bitmap bC = new Bitmap(this.helplineRulerCtrl2.Bmp);
                this.SetBitmap(ref this._b4Copy, ref bC);

                this.helplineRulerCtrl2.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl2.MakeBitmap(this.helplineRulerCtrl2.Bmp);
                this.helplineRulerCtrl2.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl2.Bmp.Width * this.helplineRulerCtrl2.Zoom),
                    (int)(this.helplineRulerCtrl2.Bmp.Height * this.helplineRulerCtrl2.Zoom));

                _undoOPCache.Add(b);

                //if (this._resWC > 1)
                //    ResampleAndTranslateBack();

                if (this._bResCopyTransp != null)
                {
                    List<ChainCode> c2 = GetBoundary(this._bResCopyTransp, 0, false);
                    c2 = c2.OrderByDescending(x => x.Coord.Count).ToList();

                    //this.numRedrawComponents.Value = this.numRedrawComponents.Maximum = c2.Count;
                }
            }

            this.SetControls(true);

            btnReset2.Enabled = true;
            this._pic_changed = true;

            if (this._allPoints != null)
                this._allPoints.Clear();
            if (this._pathList != null)
                this._pathList.Clear();
            if (this._points != null)
                this._points.Clear();

            this.helplineRulerCtrl2.dbPanel1.Invalidate();

            if (this.Timer3.Enabled)
                this.Timer3.Stop();

            this.Timer3.Start();
            this.btnMinCut.Text = "Go";

            this.backgroundWorker2.Dispose();
            this.backgroundWorker2 = new BackgroundWorker();
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.WorkerSupportsCancellation = true;
            this.backgroundWorker2.DoWork += backgroundWorker2_DoWork;
            this.backgroundWorker2.ProgressChanged += backgroundWorker2_ProgressChanged;
            this.backgroundWorker2.RunWorkerCompleted += backgroundWorker2_RunWorkerCompleted;

            if (this._gc != null)
                this._gc.BGW = this.backgroundWorker2;

            this.cbQuickEst_CheckedChanged(this.cbQuickEst, new EventArgs());
            this.cbDraw_CheckedChanged(this.cbDraw, new EventArgs());

            if (this._restartDiffInit)
            {
                this._restartDiffInit = false;
                this._KMeansInitW += 1;
                this._KMeansInitH += 1;

                bool bR = this.cbRectMode.Checked;
                bool bS = this.cbScribbleMode.Checked;

                this.button4_Click(this.btnReset2, new EventArgs());

                if (bR)
                    this.cbRectMode.Checked = true;
                if (bS)
                    this.cbScribbleMode.Checked = true;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();

                this.btnGo_Click(this.btnGo, new EventArgs());
            }

            this.cbRectMode.Checked = false;
            this.cbRectMode.Enabled = false;
            this.cbScribbleMode.Checked = false;
            this.cbScribbleMode.Enabled = false;

            this.toolStripStatusLabel4.Text = "done";
        }

        private void btnLoadScribbles_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.Filter = "Scribbles-Files (*.tgScribbles)|*.tgScribbles";
            this.openFileDialog1.FileName = "File1.tgScribbles";

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SavedScribbles f = DeserializeF(this.openFileDialog1.FileName);

                if (f != null)
                {
                    this._scribbles = f.ToDictionary();
                    //this._scribblesChanged = true;

                    if (this.cbScribbleMode.Enabled)
                    {
                        this.cbRectMode.Checked = false;
                        this.cbScribbleMode.Checked = true;
                    }

                    if (f.Bmp != null && this.cbLSBmp.Checked)
                    {
                        Bitmap b = new Bitmap(f.Bmp);

                        this.SetBitmap(this.helplineRulerCtrl1.Bmp, b, this.helplineRulerCtrl1, "Bmp");
                        _undoOPCache.Add(b);

                        btnReset2.Enabled = true;
                        this._pic_changed = true;

                        this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                        this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                            (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                            (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                        this.cmbZoom_SelectedIndexChanged(this.cmbZoom, new EventArgs());
                    }

                    this.helplineRulerCtrl1.dbPanel1.Invalidate();

                    f.Dispose();
                }
            }
        }

        private void btnSaveScribbles_Click(object sender, EventArgs e)
        {
            if (this._scribbles != null)
            {
                SavedScribbles f = new SavedScribbles();

                if (this._scribbles.ContainsKey(0)) //BG
                {
                    if (this._scribbles[0] != null)
                    {
                        f.BGSizes = this._scribbles[0].Keys.ToArray();
                        f.BGPoints = new Point[f.BGSizes.Length][][];
                        int i = 0;

                        foreach (int wh in this._scribbles[0].Keys)
                        {
                            f.BGPoints[i] = new Point[this._scribbles[0][wh].Count][];

                            List<List<Point>> whPts = this._scribbles[0][wh];
                            int j = 0;

                            foreach (List<Point> pts in whPts)
                            {
                                f.BGPoints[i][j] = pts.ToArray();
                                j++;
                            }

                            i++;
                        }
                    }
                }

                if (this._scribbles.ContainsKey(1)) //FG
                {
                    if (this._scribbles[1] != null)
                    {
                        f.FGSizes = this._scribbles[1].Keys.ToArray();
                        f.FGPoints = new Point[f.FGSizes.Length][][];
                        int i = 0;

                        foreach (int wh in this._scribbles[1].Keys)
                        {
                            f.FGPoints[i] = new Point[this._scribbles[1][wh].Count][];

                            List<List<Point>> whPts = this._scribbles[1][wh];
                            int j = 0;

                            foreach (List<Point> pts in whPts)
                            {
                                f.FGPoints[i][j] = pts.ToArray();
                                j++;
                            }

                            i++;
                        }
                    }
                }

                if (this.helplineRulerCtrl1.Bmp != null && this.cbLSBmp.Checked)
                    f.Bmp = new Bitmap(this.helplineRulerCtrl1.Bmp);

                WriteToFile(f);

                f.Dispose();
            }
        }

        private void WriteToFile(SavedScribbles f)
        {
            this.saveFileDialog1.Filter = "Scribbles-Files (*.tgScribbles)|*.tgScribbles";
            this.saveFileDialog1.FileName = "File1.tgScribbles";

            if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                SerializeF(f, this.saveFileDialog1.FileName);
        }

        private bool SerializeF(SavedScribbles f, string FileName)
        {
            //serialize data to binary
            IFormatter formatter = new BinaryFormatter();
            Stream stream = null;

            bool bError = false;

            try
            {
                //write the data to the file
                stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, f);
            }
            catch (Exception ex)
            {
                bError = true;
                Console.WriteLine(DateTime.Now.ToString() + " " + ex.ToString());
            }

            try
            {
                //stream.Close()
                stream.Dispose();
                stream = null;
            }
            catch
            { }

            return !bError;
        }

        private SavedScribbles DeserializeF(string fileName)
        {
            //deserialize from binary
            IFormatter formatter = new BinaryFormatter();
            Stream stream = null;
            SavedScribbles f = null;

            try
            {
                //write the data to the file
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                f = (SavedScribbles)formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " " + ex.ToString());
            }

            try
            {
                //stream.Close()
                stream.Dispose();
                stream = null;
            }
            catch
            { }

            return f;
        }

        //if you want to upgrade to .net 8.0
        //if you want to use these in this .net-framework version, install the
        //System.Text.Json NuGet package
        //private bool SerializeF(SavedScribbles f, string FileName)
        //{
        //    bool bError = false;

        //    try
        //    {
        //        JsonSerializerOptions options = new()
        //        {
        //            NumberHandling =
        //                JsonNumberHandling.AllowReadingFromString |
        //                JsonNumberHandling.WriteAsString,
        //            WriteIndented = true,
        //            ReadCommentHandling = JsonCommentHandling.Skip,
        //            AllowTrailingCommas = true,
        //        };

        //        using FileStream createStream = File.Create(FileName);
        //        JsonSerializer.Serialize(createStream, f, options);
        //    }
        //    catch (Exception ex)
        //    {
        //        bError = true;
        //        Console.WriteLine(DateTime.Now.ToString() + " " + ex.ToString());
        //    }

        //    return !bError;
        //}

        //private SavedScribbles? DeserializeF(string fileName)
        //{
        //    Stream? stream = null;
        //    SavedScribbles? f = null;

        //    try
        //    {
        //        JsonSerializerOptions options = new()
        //        {
        //            NumberHandling =
        //                JsonNumberHandling.AllowReadingFromString |
        //                JsonNumberHandling.WriteAsString,
        //            WriteIndented = true,
        //            ReadCommentHandling = JsonCommentHandling.Skip,
        //            AllowTrailingCommas = true,
        //        };

        //        stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        //        f = JsonSerializer.Deserialize<SavedScribbles>(stream, options);


        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(DateTime.Now.ToString() + " " + ex.ToString());
        //    }

        //    try
        //    {
        //        //stream.Close()
        //        stream?.Dispose();
        //        stream = null;
        //    }
        //    catch
        //    { }

        //    return f;
        //}

        private void numShiftX_ValueChanged(object sender, EventArgs e)
        {
            this._numShiftX = (int)this.numShiftX.Value;
        }

        private void numShiftY_ValueChanged(object sender, EventArgs e)
        {
            this._numShiftY = (int)this.numShiftY.Value;
        }

        private void btnOutline_Click(object sender, EventArgs e)
        {
            if (this._b4Copy != null)
            {
                using (frmProcOutline frm = new frmProcOutline(this.helplineRulerCtrl2.Bmp, this._bmpBU, this.CachePathAddition))
                {
                    frm.SetupCache();

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        Bitmap b = new Bitmap(frm.FBitmap);

                        this.SetBitmap(this.helplineRulerCtrl2.Bmp, b, this.helplineRulerCtrl2, "Bmp");

                        //Bitmap bC = new Bitmap(this.helplineRulerCtrl2.Bmp);
                        //this.SetBitmap(ref this._b4Copy, ref bC);

                        this.helplineRulerCtrl2.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                        this.helplineRulerCtrl2.MakeBitmap(this.helplineRulerCtrl2.Bmp);
                        this.helplineRulerCtrl2.dbPanel1.AutoScrollMinSize = new Size(
                            (int)(this.helplineRulerCtrl2.Bmp.Width * this.helplineRulerCtrl2.Zoom),
                            (int)(this.helplineRulerCtrl2.Bmp.Height * this.helplineRulerCtrl2.Zoom));

                        _undoOPCache.Add(b);
                    }
                }
            }
        }

        private void btnCache_Click(object sender, EventArgs e)
        {
            if (_undoOPCache != null && _undoOPCache.Cache.IsActive)
            {
                string folder = _undoOPCache.GetCachePath();

                using (frmCachedPictures frm = new frmCachedPictures(folder))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        if (frm.cbLoadTo.Checked)
                        {
                            bool hlc1 = frm.rbHlc1.Checked || frm.rbHlc12.Checked;
                            bool hlc2 = frm.rbHlc2.Checked || frm.rbHlc12.Checked;

                            if (hlc1)
                            {
                                Bitmap bmp = null;
                                FileInfo fi = new FileInfo(Path.Combine(folder, frm.listBox1.SelectedItem.ToString()));

                                using (Image img = Image.FromFile(fi.FullName))
                                    bmp = new Bitmap(img);

                                this.SetBitmap(this.helplineRulerCtrl1.Bmp, bmp, this.helplineRulerCtrl1, "Bmp");

                                this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                                this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                                this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                                    (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                                    (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                                Bitmap bC = new Bitmap(bmp);
                                this.SetBitmap(ref this._bmpBU, ref bC);
                            }

                            if (hlc2)
                            {
                                Bitmap bmp = null;
                                FileInfo fi = new FileInfo(Path.Combine(folder, frm.listBox1.SelectedItem.ToString()));

                                using (Image img = Image.FromFile(fi.FullName))
                                    bmp = new Bitmap(img);

                                this.SetBitmap(this.helplineRulerCtrl2.Bmp, bmp, this.helplineRulerCtrl2, "Bmp");

                                this.helplineRulerCtrl2.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                                this.helplineRulerCtrl2.MakeBitmap(this.helplineRulerCtrl2.Bmp);
                                this.helplineRulerCtrl2.dbPanel1.AutoScrollMinSize = new Size(
                                    (int)(this.helplineRulerCtrl2.Bmp.Width * this.helplineRulerCtrl2.Zoom),
                                    (int)(this.helplineRulerCtrl2.Bmp.Height * this.helplineRulerCtrl2.Zoom));

                                //Bitmap bC = new Bitmap(bmp);
                                //this.SetBitmap(ref this._b4Copy, ref bC);
                            }
                        }
                    }
                }
            }
        }

        private void btnCompose_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl2.Bmp != null)
            {
                using (frmCompose frm = new frmCompose(this.helplineRulerCtrl2.Bmp))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        Bitmap bmp = new Bitmap(frm.FBitmap);

                        this.SetBitmap(this.helplineRulerCtrl2.Bmp, bmp, this.helplineRulerCtrl2, "Bmp");
                        _undoOPCache.Add(bmp);

                        this.helplineRulerCtrl2.SetZoom(this.helplineRulerCtrl2.Zoom.ToString());
                        this.helplineRulerCtrl2.MakeBitmap(this.helplineRulerCtrl2.Bmp);
                        this.helplineRulerCtrl2.dbPanel1.AutoScrollMinSize = new Size(
                            (int)(this.helplineRulerCtrl2.Bmp.Width * this.helplineRulerCtrl2.Zoom),
                            (int)(this.helplineRulerCtrl2.Bmp.Height * this.helplineRulerCtrl2.Zoom));

                        Bitmap bC = new Bitmap(bmp);
                        this.SetBitmap(ref this._bmpBU, ref bC);
                    }
                }
            }
        }

        private void btnInitSettings_Click(object sender, EventArgs e)
        {
            using (frmKMeansSettings frm = new frmKMeansSettings())
            {
                frm.cmbSelMode.SelectedIndex = (int)this._KMeansSelMode;
                frm.numKMeansIters.Value = (decimal)this._KMeansIters;
                frm.numInitIters.Value = (decimal)this._KMeansInitIters;
                frm.numInitW.Value = (decimal)this._KMeansInitW;
                frm.numInitH.Value = (decimal)this._KMeansInitH;
                frm.cbInitRnd.Checked = this._kMInitRnd;

                if (frm.ShowDialog() == DialogResult.OK)
                {
                    ListSelectionMode selMode = (ListSelectionMode)System.Enum.Parse(typeof(ListSelectionMode), frm.cmbSelMode.SelectedItem.ToString());
                    this._KMeansSelMode = selMode;
                    this._KMeansIters = (int)frm.numKMeansIters.Value;
                    this._KMeansInitIters = (int)frm.numInitIters.Value;
                    this._KMeansInitW = (double)frm.numInitW.Value;
                    this._KMeansInitH = (double)frm.numInitH.Value;
                    this._kMInitRnd = frm.cbInitRnd.Checked;
                }
            }
        }
    }
}
