using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LUBitmapDesigner
{
    public partial class LUBitmapDesignerCtrl : UserControl
    {
        private int _ix;
        private int _iy;
        private float _rotationStart;

        public ShapeList ShapeList { get; set; }
        public BitmapShape SelectedShape { get; private set; }

        public event EventHandler<BitmapShape> ShapeChanged;

        public LUBitmapDesignerCtrl()
        {
            InitializeComponent();
        }

        public void SetBlankLowerImage()
        {
            Bitmap bLower = new Bitmap(this.Width, this.Height);
            if (this.ShapeList == null)
                this.ShapeList = new ShapeList();
            BitmapShape b = new BitmapShape() { Bmp = bLower, Bounds = new RectangleF(0, 0, bLower.Width, bLower.Height), Rotation = 0, Zoom = 1f };
            this.ShapeList.Add(b);

            this.helplineRulerCtrl1.Bmp = bLower;
        }

        public Bitmap GetUpperImage()
        {
            Bitmap bOut = null;
            if (this.ShapeList == null)
                this.ShapeList = new ShapeList();
            if (this.ShapeList.Count == 0)
                SetBlankLowerImage();
            if (this.ShapeList.Count > 1)
                bOut = this.ShapeList[1].Bmp;

            return bOut;
        }

        //public void SetUpperImage(Bitmap bmp, float x, float y)
        //{
        //    if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L))
        //    {
        //        if (this.ShapeList == null)
        //            this.ShapeList = new ShapeList();
        //        if (this.ShapeList.Count == 0)
        //            SetBlankLowerImage();
        //        if (bmp != null && this.ShapeList.Count > 0)
        //        {
        //            for (int i = 2; i < this.ShapeList.Count; i++)
        //                this.ShapeList.RemoveAt(i);

        //            int cnt = this.ShapeList.Count;

        //            BitmapShape b = null;
        //            if (this.ShapeList.Count > 1)
        //                b = this.ShapeList[1];

        //            RectangleF rc = new RectangleF(x, y, bmp.Width, bmp.Height);

        //            this.ShapeList.AllowAdding = true;
        //            this.ShapeList.Add(new BitmapShape() { Bmp = bmp, Bounds = rc, Rotation = 0, Zoom = 1f });
        //            if (cnt >= 2)
        //                this.ShapeList.RemoveAt(1);

        //            if (b != null)
        //            {
        //                b.Dispose();
        //                b = null;
        //            }
        //        }

        //    }
        //    else
        //    {
        //        MessageBox.Show("Not enough Memory");
        //        return;
        //    }

        //    this.helplineRulerCtrl1.Bmp = this.ShapeList[0].Bmp;

        //    double faktor = System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Height);
        //    double multiplier = System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Height);
        //    if (multiplier >= faktor)
        //        this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Width));
        //    else
        //        this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Height) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Height));

        //    this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp?.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp?.Height * this.helplineRulerCtrl1.Zoom));
        //    this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
        //}

        public void SetUpperImage(Bitmap bmp, float x, float y)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L))
            {
                if (this.ShapeList == null)
                    this.ShapeList = new ShapeList();
                if (this.ShapeList.Count == 0)
                    SetBlankLowerImage();
                if (bmp != null && this.ShapeList.Count > 0)
                {
                    for (int i = 2; i < this.ShapeList.Count; i++)
                        this.ShapeList.RemoveAt(i);

                    int cnt = this.ShapeList.Count;

                    BitmapShape b = null;
                    if (this.ShapeList.Count > 1)
                        b = this.ShapeList[1];

                    RectangleF? r = null;
                    float? rot = null;
                    //old loaction and size
                    if (b != null)
                    {
                        r = b.Bounds;
                        rot = b.Rotation;
                    }

                    RectangleF rc = new RectangleF(x, y, bmp.Width, bmp.Height);

                    this.ShapeList.AllowAdding = true;
                    this.ShapeList.Add(new BitmapShape() { Bmp = bmp, Bounds = rc, Rotation = 0, Zoom = 1f });
                    if (r != null && rot != null)
                    {
                        this.ShapeList[this.ShapeList.Count - 1].Bounds = r.Value;
                        this.ShapeList[this.ShapeList.Count - 1].Rotation = rot.Value;
                    }

                    if (cnt >= 2)
                        this.ShapeList.RemoveAt(1);

                    if (b != null)
                    {
                        b.Dispose();
                        b = null;
                    }
                }

            }
            else
            {
                MessageBox.Show("Not enough Memory");
                return;
            }

            this.helplineRulerCtrl1.Bmp = this.ShapeList[0].Bmp;

            double faktor = System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Height);
            double multiplier = System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Height);
            if (multiplier >= faktor)
                this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Width));
            else
                this.helplineRulerCtrl1.Zoom = System.Convert.ToSingle(System.Convert.ToDouble(helplineRulerCtrl1.dbPanel1.Height) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp?.Height));

            this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp?.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp?.Height * this.helplineRulerCtrl1.Zoom));
            this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
        }

        private void helplineRulerCtrl1_Load(object sender, EventArgs e)
        {
            if (!this.DesignMode)
            {
                this.helplineRulerCtrl1.AddDefaultHelplines();
                this.helplineRulerCtrl1.ResetAllHelpLineLabelsColor();

                this.helplineRulerCtrl1.dbPanel1.MouseDown += helplineRulerCtrl1_MouseDown;
                this.helplineRulerCtrl1.dbPanel1.MouseMove += helplineRulerCtrl1_MouseMove;
                this.helplineRulerCtrl1.dbPanel1.MouseUp += helplineRulerCtrl1_MouseUp;

                this.helplineRulerCtrl1.PostPaint += helplineRulerCtrl1_Paint;
            }
        }

        private void helplineRulerCtrl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.ShapeList?.Count > 1 && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
            {
                int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
                int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

                if (this.ShapeList.Count > 1 && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
                {
                    if (HitTest(ix, iy))
                    {
                        this._ix = ix;
                        this._iy = iy;

                        if (e.Button == MouseButtons.Right && this.SelectedShape?.IsLocked == false)
                        {
                            BitmapShape b = this.ShapeList[1];

                            float xP1 = ix - b.Bounds.X;
                            float yP1 = iy - b.Bounds.Y;

                            _rotationStart = (float)(Math.Atan2(yP1, xP1) / (Math.PI / 180.0)) - b.Rotation;
                        }
                    }
                }
            }
        }

        private void helplineRulerCtrl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.ShapeList?.Count > 1 && e.Button == MouseButtons.Left && this.SelectedShape?.IsLocked == false)
            {
                int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
                int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

                //int eX = e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X;
                //int eY = e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y;

                if (ix >= 0 && ix < this.helplineRulerCtrl1.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl1.Bmp.Height)
                {
                    if (HitTest(ix, iy))
                    {
                        BitmapShape b = this.ShapeList[1];
                        b.Bounds = new RectangleF(b.Bounds.X + (ix - _ix), b.Bounds.Y + (iy - _iy), b.Bounds.Width, b.Bounds.Height);
                        this._ix = ix;
                        this._iy = iy;
                        this.helplineRulerCtrl1.dbPanel1.Invalidate();
                    }
                }
            }
            if (this.ShapeList?.Count > 1 && e.Button == MouseButtons.Right && this.SelectedShape?.IsLocked == false)
            {
                int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
                int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

                if (ix >= 0 && ix < this.helplineRulerCtrl1.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl1.Bmp.Height)
                {
                    if (HitTest(ix, iy))
                    {
                        BitmapShape b = this.ShapeList[1];

                        PointF f = b.Bounds.Location;

                        float pX = ix - f.X;
                        float pY = iy - f.Y;

                        b.Rotation = (float)(Math.Atan2(pY, pX) / (Math.PI / 180.0)) - _rotationStart;

                        if (b.Rotation > 360.0F)
                            b.Rotation -= 360;

                        if (b.Rotation < -360.0F)
                            b.Rotation += 360;

                        this.helplineRulerCtrl1.dbPanel1.Invalidate();
                    }
                }
            }
        }

        private bool HitTest(int ix, int iy)
        {
            // check, if a DrawnRectangle is at the current point
            if (this.ShapeList != null && this.ShapeList.Count > 1)
            {
                this.SelectedShape = null;
                // we use a GraphicsPath for testing, because it is easy to handle when obects are rotated
                // then, we simply rotate the path
                using (GraphicsPath gP = new GraphicsPath())
                {
                    gP.FillMode = FillMode.Winding;

                    using (Matrix m = new Matrix(1, 0, 0, 1, 0, 0))
                    {
                        gP.AddRectangle(this.ShapeList[1].Bounds);

                        // setup the matrix
                        if (this.ShapeList[1].Rotation != 0)
                            m.RotateAt(this.ShapeList[1].Rotation, this.ShapeList[1].Bounds.Location, MatrixOrder.Prepend);

                        // and transform (here: rotate) the path
                        gP.Transform(m);
                    }

                    PointF p = new PointF(ix, iy);

                    // with the method "IsVisible" we check, if the current point is inside the path
                    if (gP.IsVisible(p))
                    {
                        ShapeChanged?.Invoke(this, this.ShapeList[1]);
                        gP.Dispose();
                        this.SelectedShape = this.ShapeList[1];
                        return true;
                    }
                }
            }

            return false;
        }

        private void helplineRulerCtrl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.ShapeList?.Count > 1 && e.Button == MouseButtons.Left && this.SelectedShape?.IsLocked == false)
            {
                BitmapShape b = this.ShapeList[1];
            }
        }

        private void helplineRulerCtrl1_Paint(object sender, PaintEventArgs e)
        {
            if (this.ShapeList?.Count > 1)
            {
                e.Graphics.TranslateTransform(this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X,
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y);
                this.ShapeList[1].Draw(e.Graphics);
                e.Graphics.ResetTransform();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (this.helplineRulerCtrl1.dbPanel1 != null)
            {
                if (keyData == Keys.Down)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y + 4);
                    return true;
                }
                if (keyData == Keys.Up)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y - 4);
                    return true;
                }
                if (keyData == Keys.Left)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X - 4, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y);
                    return true;
                }
                if (keyData == Keys.Right)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X + 4, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y);
                    return true;
                }

                if (keyData == Keys.PageDown)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y + this.helplineRulerCtrl1.dbPanel1.ClientSize.Height);
                    return true;
                }
                if (keyData == Keys.PageUp)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y - this.helplineRulerCtrl1.dbPanel1.ClientSize.Height);
                    return true;
                }
                if (keyData == Keys.Home)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-Int16.MaxValue, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y);
                    return true;
                }
                if (keyData == Keys.End)
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(Int16.MaxValue, -this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y);
                    return true;
                }

                if (keyData == (Keys.Home | Keys.Control))
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(-Int16.MaxValue, -Int16.MaxValue);
                    return true;
                }
                if (keyData == (Keys.End | Keys.Control))
                {
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition = new Point(Int16.MaxValue, Int16.MaxValue);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        public void SetZoom(string zoomString)
        {
            if (this.helplineRulerCtrl1.Bmp != null && zoomString != null)
            {
                float zoom = this.helplineRulerCtrl1.Zoom;

                if (zoomString == "1")
                    zoom = 1.0F;
                else if (zoomString.ToLower() == "fit_width")
                    zoom = System.Convert.ToSingle(System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.ClientSize.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width));
                else if (zoomString.ToLower() == "fit")
                {
                    double faktor = System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Height);
                    double multiplier = System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Height);
                    if (multiplier >= faktor)
                        zoom = System.Convert.ToSingle(System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Width) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Width));
                    else
                        zoom = System.Convert.ToSingle(System.Convert.ToDouble(this.helplineRulerCtrl1.dbPanel1.Height) / System.Convert.ToDouble(this.helplineRulerCtrl1.Bmp.Height));
                }
                else
                {
                    float j = 0;

                    if (float.TryParse(zoomString, out j))
                    {
                        this.Cursor = Cursors.WaitCursor;
                        zoom = j;
                    }
                }

                this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * zoom));
                this.helplineRulerCtrl1.Zoom = zoom;

                this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
                this.Cursor = Cursors.Default;

                if (this.ShapeList != null)
                    for (int i = 0; i < this.ShapeList.Count; i++)
                        this.ShapeList[i].Zoom = zoom;
            }
        }


    }
}
