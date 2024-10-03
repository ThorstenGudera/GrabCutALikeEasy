using Cache;
using LUBitmapDesigner;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    public partial class frmCompose : Form
    {
        private bool _pic_changed;
        private Bitmap _bmpBU;

        public Bitmap FBitmap { get; private set; }

        private UndoOPCache _undoOPCache = null;

        public string CachePathAddition
        {
            get
            {
                return m_CachePathAddition;
            }
            set
            {
                if (value != null)
                    m_CachePathAddition = value;
            }
        }

        private string m_CachePathAddition;

        public frmCompose(Bitmap bUpper, string basePathAddition)
        {
            InitializeComponent();

            CachePathAddition = basePathAddition;

            if (AvailMem.AvailMem.checkAvailRam(bUpper.Width * bUpper.Height * 16L))
            {
                this._bmpBU = new Bitmap(bUpper);
                this.luBitmapDesignerCtrl1.SetUpperImage(new Bitmap(bUpper), 0, 0);
            }
            else
                MessageBox.Show("Not enough memory.");

            this.luBitmapDesignerCtrl1.ShapeChanged += LuBitmapDesignerCtrl1_ShapeChanged;
            this.picInfoCtrl1.ShapeChanged += PicInfoCtrl1_ShapeChanged;
        }

        public void SetupCache()
        {
            _undoOPCache = new Cache.UndoOPCache(this.GetType(), CachePathAddition, "frmCompose");
            if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
                _undoOPCache.Add(this.luBitmapDesignerCtrl1.ShapeList[1].Bmp);
        }

        private void PicInfoCtrl1_ShapeChanged(object sender, BitmapShape e)
        {
            this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.Invalidate();
            e.IsLocked = this.picInfoCtrl1.cbLock.Checked;
        }

        private void LuBitmapDesignerCtrl1_ShapeChanged(object sender, BitmapShape e)
        {
            if (e != null)
            {
                this.picInfoCtrl1.SetValues(e);
            }
        }

        private void cbBGColor_CheckedChanged(object sender, EventArgs e)
        {
            if (cbBGColor.Checked)
                this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.BackColor = SystemColors.ControlDarkDark;
            else
                this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.BackColor = SystemColors.Control;
        }

        private void cmbZoom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Visible && this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp != null && cmbZoom.SelectedItem != null)
            {
                this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Enabled = false;
                this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Refresh();
                this.luBitmapDesignerCtrl1.SetZoom(cmbZoom.SelectedItem.ToString());
                this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Enabled = true;
                if (this.cmbZoom.SelectedIndex < 2)
                    this.luBitmapDesignerCtrl1.helplineRulerCtrl1.ZoomSetManually = true;

                this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.Invalidate();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp != null)
            {
                Bitmap bmp = DrawToBmp();

                this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
                this.saveFileDialog1.FileName = "Bild1.png";

                try
                {
                    if (bmp != null && this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        bmp.Save(this.saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        _pic_changed = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                bmp?.Dispose();
                bmp = null;
            }
        }

        private Bitmap DrawToBmp()
        {
            Bitmap bOut = null;

            if (this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp != null)
            {
                bOut = new Bitmap(this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp.Width, this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp.Height);
                using (Graphics gx = Graphics.FromImage(bOut))
                {
                    if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 0)
                        gx.DrawImage(this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp, 0, 0);

                    if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
                    {
                        float zBU = this.luBitmapDesignerCtrl1.ShapeList[0].Zoom;
                        this.luBitmapDesignerCtrl1.ShapeList[1].Zoom = 1.0f;
                        this.luBitmapDesignerCtrl1.ShapeList[1].Draw(gx);
                        this.luBitmapDesignerCtrl1.ShapeList[1].Zoom = zBU;
                    }
                }
            }

            return bOut;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp != null)
            {
                if (_pic_changed)
                {
                    DialogResult dlg = MessageBox.Show("Save image?", "Unsaved data", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                    if (dlg == DialogResult.Yes)
                        button2.PerformClick();
                    else if (dlg == DialogResult.No)
                        _pic_changed = false;
                }

                if (!_pic_changed && this._bmpBU != null)
                {
                    string f = this.Text.Split(new String[] { " - " }, StringSplitOptions.None)[0];
                    Bitmap b1 = null;

                    try
                    {
                        if (AvailMem.AvailMem.checkAvailRam(this._bmpBU.Width * this._bmpBU.Height * 12L))
                            b1 = (Bitmap)this._bmpBU.Clone();
                        else
                            throw new Exception();

                        this.SetBitmap(this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp, b1, this.luBitmapDesignerCtrl1.helplineRulerCtrl1, "Bmp");

                        this._pic_changed = false;

                        this.luBitmapDesignerCtrl1.helplineRulerCtrl1.CalculateZoom();

                        this.luBitmapDesignerCtrl1.helplineRulerCtrl1.MakeBitmap(this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp);

                        // SetHRControlVars();

                        this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp.Width * this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp.Height * this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Zoom));
                        this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.Invalidate();

                        //_undoOPCache.Reset(false);

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
                    pi?.SetValue(ct, bitmapToBeSet);
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

        private void frmCompose_Load(object sender, EventArgs e)
        {
            this.cmbZoom.SelectedIndex = 4;
            this.cbBGColor_CheckedChanged(this.cbBGColor, new EventArgs());
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmp = null;
                using (Image img = Image.FromFile(this.openFileDialog1.FileName))
                    bmp = new Bitmap(img);

                if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 0)
                {
                    BitmapShape b = this.luBitmapDesignerCtrl1.ShapeList[0];
                    BitmapShape bNew = new BitmapShape() { Bmp = bmp, Bounds = new RectangleF(0, 0, bmp.Width, bmp.Height), Rotation = 0, Zoom = this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Zoom };
                    this.luBitmapDesignerCtrl1.ShapeList[0] = bNew;
                    this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp = bNew.Bmp;

                    if (b != null)
                    {
                        b.Dispose();
                        b = null;
                    }

                    this.cmbZoom_SelectedIndexChanged(this.cmbZoom, new EventArgs());
                }
            }
        }

        private void frmCompose_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 0)
            {
                for (int i = this.luBitmapDesignerCtrl1.ShapeList.Count - 1; i >= 0; i--)
                    this.luBitmapDesignerCtrl1.ShapeList[i].Dispose();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.FBitmap = this.DrawToBmp();
        }

        private void btnAlphaZAndGain_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker1.IsBusy)
            {
                this.backgroundWorker1.CancelAsync();
                return;
            }

            if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
            {
                Bitmap b = this.luBitmapDesignerCtrl1.GetUpperImage();
                if (b != null)
                {
                    this.Cursor = Cursors.WaitCursor;
                    this.SetControls(false);

                    this.btnAlphaZAndGain.Enabled = true;
                    this.btnAlphaZAndGain.Text = "Cancel";

                    int alphaTh = (int)this.numAlphaZAndGain.Value;

                    this.backgroundWorker1.RunWorkerAsync(new object[] { b, alphaTh });
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument != null)
            {
                object[] o = (object[])e.Argument;
                Bitmap bmp = new Bitmap((Bitmap)o[0]);
                double alphaTh = (int)o[1];

                e.Result = GetAlphaZAndGainPic(bmp, alphaTh);
            }
        }

        private unsafe Bitmap GetAlphaZAndGainPic(Bitmap bmp, double alphaTh)
        {
            Bitmap bOut = null;

            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L))
            {
                int w = bmp.Width;
                int h = bmp.Height;

                bOut = new Bitmap(w, h);

                BitmapData bmD = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmA = bOut.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmD.Stride;

                double slope = 255.0 / (255.0 - alphaTh);

                Parallel.For(0, h, y =>
                {
                    byte* p = (byte*)bmD.Scan0;
                    p += y * stride;

                    byte* pA = (byte*)bmA.Scan0;
                    pA += y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        if (p[3] != 0)
                        {
                            pA[0] = p[0];
                            pA[1] = p[1];
                            pA[2] = p[2];

                            pA[3] = (byte)Math.Max(Math.Min(slope * ((double)p[3] - alphaTh), 255), 0);
                        }

                        p += 4;
                        pA += 4;
                    }
                });

                bmp.UnlockBits(bmD);
                bOut.UnlockBits(bmA);
            }

            return bOut;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!this.IsDisposed)
            {
                Bitmap bmp = null;

                if (e.Result != null)
                    bmp = (Bitmap)e.Result;

                if (bmp != null)
                {
                    float x = 0f;
                    float y = 0f;

                    if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
                    {
                        x = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.X;
                        y = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.Y;
                    }

                    this.luBitmapDesignerCtrl1.SetUpperImage(bmp, x, y);

                    this.cmbZoom_SelectedIndexChanged(this.cmbZoom, new EventArgs());

                    _undoOPCache?.Add(bmp);

                    this.btnUndo.Enabled = true;
                    this.CheckRedoButton();
                }

                this.btnAlphaZAndGain.Text = "Go";

                this.SetControls(true);
                this.Cursor = Cursors.Default;

                this.btnOK.Enabled = this.btnCancel.Enabled = true;

                this._pic_changed = true;

                this.backgroundWorker1.Dispose();
                this.backgroundWorker1 = new BackgroundWorker();
                this.backgroundWorker1.WorkerReportsProgress = true;
                this.backgroundWorker1.WorkerSupportsCancellation = true;
                this.backgroundWorker1.DoWork += backgroundWorker1_DoWork;
                //this.backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
                this.backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            }
        }

        private void CheckRedoButton()
        {
            _undoOPCache?.CheckRedoButton(this.btnRedo);
        }

        private void SetControls(bool e)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    foreach (Control ct in this.splitContainer1.Panel2.Controls)
                    {
                        if (ct.Name != "btnCancel")
                            ct.Enabled = e;
                    }
                    foreach (Control ct in this.splitContainer2.Panel2.Controls)
                    {
                        if (ct.Name != "btnCancel")
                            ct.Enabled = e;
                    }

                    this.luBitmapDesignerCtrl1.Enabled = this.luBitmapDesignerCtrl1.Enabled = e;

                    this.Cursor = e ? Cursors.Default : Cursors.WaitCursor;
                }));
            }
            else
            {
                foreach (Control ct in this.splitContainer1.Panel2.Controls)
                {
                    if (ct.Name != "btnCancel")
                        ct.Enabled = e;
                }
                foreach (Control ct in this.splitContainer2.Panel2.Controls)
                {
                    if (ct.Name != "btnCancel")
                        ct.Enabled = e;
                }

                this.luBitmapDesignerCtrl1.Enabled = this.luBitmapDesignerCtrl1.Enabled = e;

                this.Cursor = e ? Cursors.Default : Cursors.WaitCursor;
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument != null)
            {
                object[] o = (object[])e.Argument;
                Bitmap bmp = new Bitmap((Bitmap)o[0]);
                double gamma = (double)o[1];

                e.Result = GetAlphaPic(bmp, gamma);
            }
        }

        private unsafe Bitmap GetAlphaPic(Bitmap bmp, double gamma)
        {
            Bitmap bOut = null;

            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L))
            {
                int w = bmp.Width;
                int h = bmp.Height;

                bOut = new Bitmap(w, h);

                BitmapData bmD = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmA = bOut.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmD.Stride;

                Parallel.For(0, h, y =>
                {
                    byte* p = (byte*)bmD.Scan0;
                    p += y * stride;

                    byte* pA = (byte*)bmA.Scan0;
                    pA += y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        if (p[3] != 0)
                        {
                            pA[0] = p[0];
                            pA[1] = p[1];
                            pA[2] = p[2];

                            pA[3] = (byte)Math.Max(Math.Min(255.0 * Math.Pow((double)p[3] / 255.0, gamma), 255), 0);
                        }

                        p += 4;
                        pA += 4;
                    }
                });

                bmp.UnlockBits(bmD);
                bOut.UnlockBits(bmA);
            }

            return bOut;
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!this.IsDisposed)
            {
                Bitmap bmp = null;

                if (e.Result != null)
                    bmp = (Bitmap)e.Result;

                if (bmp != null)
                {
                    float x = 0f;
                    float y = 0f;

                    if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
                    {
                        x = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.X;
                        y = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.Y;
                    }

                    this.luBitmapDesignerCtrl1.SetUpperImage(bmp, x, y);

                    this.cmbZoom_SelectedIndexChanged(this.cmbZoom, new EventArgs());

                    _undoOPCache?.Add(bmp);

                    this.btnUndo.Enabled = true;
                    this.CheckRedoButton();
                }

                this.btnSetGamma.Text = "Go";

                this.SetControls(true);
                this.Cursor = Cursors.Default;

                this.btnOK.Enabled = this.btnCancel.Enabled = true;

                this._pic_changed = true;

                this.backgroundWorker2.Dispose();
                this.backgroundWorker2 = new BackgroundWorker();
                this.backgroundWorker2.WorkerReportsProgress = true;
                this.backgroundWorker2.WorkerSupportsCancellation = true;
                this.backgroundWorker2.DoWork += backgroundWorker2_DoWork;
                //this.backgroundWorker2.ProgressChanged += backgroundWorker2_ProgressChanged;
                this.backgroundWorker2.RunWorkerCompleted += backgroundWorker2_RunWorkerCompleted;
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            if (_undoOPCache != null && _undoOPCache.Processing == false)
            {
                Bitmap bOut = _undoOPCache.DoUndo();

                if (bOut != null)
                {
                    float x = 0f;
                    float y = 0f;

                    if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
                    {
                        x = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.X;
                        y = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.Y;
                    }

                    this.luBitmapDesignerCtrl1.SetUpperImage(bOut, x, y);

                    if (_undoOPCache.CurrentPosition < 1)
                    {
                        this.btnUndo.Enabled = false;
                        this._pic_changed = false;
                    }

                    this.cmbZoom_SelectedIndexChanged(this.cmbZoom, new EventArgs());

                    this.CheckRedoButton();
                }
                else
                    MessageBox.Show("Error while undoing.");
            }
        }

        private void btnRedo_Click(object sender, EventArgs e)
        {
            if (_undoOPCache != null && _undoOPCache.Processing == false)
            {
                Bitmap bOut = _undoOPCache.DoRedo();

                if (bOut != null)
                {
                    float x = 0f;
                    float y = 0f;

                    if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
                    {
                        x = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.X;
                        y = this.luBitmapDesignerCtrl1.ShapeList[1].Bounds.Y;
                    }

                    this.luBitmapDesignerCtrl1.SetUpperImage(bOut, x, y);
                    this._pic_changed = true;

                    this.cmbZoom_SelectedIndexChanged(this.cmbZoom, new EventArgs());

                    this.CheckRedoButton();

                    this.btnUndo.Enabled = true;
                }
                else
                    MessageBox.Show("Error while redoing.");
            }
        }

        private void btnSetGamma_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker2.IsBusy)
            {
                this.backgroundWorker2.CancelAsync();
                return;
            }

            if (this.luBitmapDesignerCtrl1.ShapeList != null && this.luBitmapDesignerCtrl1.ShapeList.Count > 1)
            {
                Bitmap b = this.luBitmapDesignerCtrl1.GetUpperImage();
                if (b != null)
                {
                    this.Cursor = Cursors.WaitCursor;
                    this.SetControls(false);

                    this.btnSetGamma.Enabled = true;
                    this.btnSetGamma.Text = "Cancel";

                    double gamma = (double)this.numGamma.Value;

                    this.backgroundWorker2.RunWorkerAsync(new object[] { b, gamma });
                }
            }
        }
    }
}
