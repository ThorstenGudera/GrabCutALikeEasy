using AvoidAGrabCutEasy.ProcOutline;
using Cache;
using ChainCodeFinder;
using GetAlphaMatte;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvoidAGrabCutEasy
{
    public partial class frmProcOutline : Form
    {
        private bool _dontDoZoom;
        private bool _pic_changed;
        private bool _dontAskOnClosing;
        private Bitmap _bmpBU;
        private Bitmap _bmpOrig;
        private UndoOPCache _undoOPCache;
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
        private DefaultSmoothenOP _dsOP;
        private BoundaryMattingOP _bmOP;
        private int _maxWidth = 50;
        private int _oW = 0;
        private int _iW = 0;
        private Stopwatch _sw;
        private ClosedFormMatteOp _cfop;
        private int _lastRunNumber;
        private ClosedFormMatteOp[] _cfopArray;
        private frmInfo _frmInfo;
        private List<TrimapProblemInfo> _trimapProblemInfos = new List<TrimapProblemInfo>();
        private Bitmap _bmpRef;
        private GrabCutOp _gc;
        private object _lockObject = new object();

        public Bitmap FBitmap
        {
            get
            {
                return this.helplineRulerCtrl1.Bmp;
            }
        }

        public frmProcOutline(Bitmap bmp, Bitmap bmpOrig, string basePathAddition)
        {
            InitializeComponent();

            CachePathAddition = basePathAddition;

            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L))
            {
                this.helplineRulerCtrl1.Bmp = new Bitmap(bmp);
                _bmpBU = new Bitmap(bmp);
                this._bmpOrig = new Bitmap(bmpOrig);
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

            this._dontDoZoom = true;
            this.cmbZoom.SelectedIndex = 4;
            this._dontDoZoom = false;

            //while developing...
            AvailMem.AvailMem.NoMemCheck = true;
        }

        private void helplineRulerCtrl1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void helplineRulerCtrl1_MouseMove(object sender, MouseEventArgs e)
        {
            int ix = (int)((e.X - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.X) / (double)this.helplineRulerCtrl1.Zoom);
            int iy = (int)((e.Y - this.helplineRulerCtrl1.dbPanel1.AutoScrollPosition.Y) / (double)this.helplineRulerCtrl1.Zoom);

            if (ix >= this.helplineRulerCtrl1.Bmp.Width)
                ix = this.helplineRulerCtrl1.Bmp.Width - 1;
            if (iy >= this.helplineRulerCtrl1.Bmp.Height)
                iy = this.helplineRulerCtrl1.Bmp.Height - 1;

            if (ix >= 0 && ix < this.helplineRulerCtrl1.Bmp.Width && iy >= 0 && iy < this.helplineRulerCtrl1.Bmp.Height)
            {
                Color c = this.helplineRulerCtrl1.Bmp.GetPixel(ix, iy);
                this.toolStripStatusLabel1.Text = ix.ToString() + "; " + iy.ToString();
                this.toolStripStatusLabel2.BackColor = c;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
            }
        }

        private void helplineRulerCtrl1_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void helplineRulerCtrl1_Paint(object sender, PaintEventArgs e)
        {

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

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
            }
        }

        private void cbBGColor_CheckedChanged(object sender, EventArgs e)
        {
            if (cbBGColor.Checked)
                this.helplineRulerCtrl1.dbPanel1.BackColor = SystemColors.ControlDarkDark;
            else
                this.helplineRulerCtrl1.dbPanel1.BackColor = SystemColors.Control;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.saveFileDialog1.Filter = "Png-Images (*.png)|*.png";
                this.saveFileDialog1.FileName = "Bild1.png";

                try
                {
                    if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        this.helplineRulerCtrl1.Bmp.Save(this.saveFileDialog1.FileName, System.Drawing.Imaging.ImageFormat.Png);
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

                        this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                        this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                        this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                            (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                            (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                        this._pic_changed = false;

                        //this.helplineRulerCtrl1.CalculateZoom();

                        this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);

                        // SetHRControlVars();

                        this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom), System.Convert.ToInt32(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));
                        this.helplineRulerCtrl1.dbPanel1.Invalidate();

                        _undoOPCache.Reset(false);
                        //this.btnReset2.Enabled = false;

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

        private void button10_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The goal is to achieve results as good as with a GrabCut, but only with the first (GMM_probabilities) estimation, to reduce the memory footprint. So keep \"QuickEstimation\" checked and play around with the other parameters.");
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
                //this.btnReset2.PerformClick();
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

        private void frmProcOutline_FormClosing(object sender, FormClosingEventArgs e)
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
                    this.backgroundWorker1.CancelAsync();

                if (this.backgroundWorker2.IsBusy)
                    this.backgroundWorker2.CancelAsync();

                if (this.backgroundWorker3.IsBusy)
                    this.backgroundWorker3.CancelAsync();

                if (this.backgroundWorker4.IsBusy)
                    this.backgroundWorker4.CancelAsync();   
                
                if (this.backgroundWorker5.IsBusy)
                    this.backgroundWorker5.CancelAsync();

                if (this.backgroundWorker6.IsBusy)
                    this.backgroundWorker6.CancelAsync();

                if (this._bmpBU != null)
                    this._bmpBU.Dispose();
                if (this._bmpOrig != null)
                    this._bmpOrig.Dispose();
                if (this._bmpRef != null)
                    this._bmpRef.Dispose();

                if (this._cfop != null)
                {
                    this._cfop.ShowProgess -= Cfop_UpdateProgress;
                    this._cfop.ShowInfo -= _cfop_ShowInfo;
                    this._cfop.Dispose();
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this._dontAskOnClosing = true;
        }

        public void SetupCache()
        {
            _undoOPCache = new Cache.UndoOPCache(this.GetType(), CachePathAddition, "tAppBoundary");
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
                        //this.btnReset2.Enabled = false;

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

        private void btnJRem_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.Cursor = Cursors.WaitCursor;
                this.SetControls(false);

                Bitmap bWork = new Bitmap(this.helplineRulerCtrl1.Bmp);

                if (this.numJRem1.Value > 0)
                {
                    Bitmap bTmp = fipbmp.RemOutline(bWork, Math.Max((int)this.numJRem1.Value, 1), null);

                    Bitmap bOld = bWork;
                    bWork = bTmp;
                    if (bOld != null)
                    {
                        bOld.Dispose();
                        bOld = null;
                    }
                }

                if (this.numJRem2.Value > 0)
                {
                    Bitmap bTmp4 = fipbmp.ExtOutline(bWork, this._bmpOrig, Math.Max((int)this.numJRem2.Value, 0), null);

                    Bitmap bOld2 = bWork;
                    bWork = bTmp4;
                    if (bOld2 != null)
                    {
                        bOld2.Dispose();
                        bOld2 = null;
                    }
                }

                if (bWork != null)
                {
                    this.SetBitmap(this.helplineRulerCtrl1.Bmp, bWork, this.helplineRulerCtrl1, "Bmp");

                    Bitmap bC = new Bitmap(bWork);
                    this.SetBitmap(ref _bmpRef, ref bC);

                    this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                    this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                        (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                        (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                    _undoOPCache.Add(bWork);
                }

                this._pic_changed = true;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();

                this.Cursor = Cursors.Default;
                this.SetControls(true);

                this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());
            }
        }

        private void btnPPGo_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.Cursor = Cursors.WaitCursor;
                this.SetControls(false);
                this.btnPPGo.Text = "Cancel";
                this.btnPPGo.Enabled = true;
                this.toolStripProgressBar1.Value = 0;
                this.toolStripProgressBar1.Visible = true;

                this.backgroundWorker1.RunWorkerAsync();
            }
        }

        private double CheckWidthHeight(Bitmap bmp, bool fp, double maxSize)
        {
            double r = 1.0;
            if (fp)
            {
                r = (double)Math.Max(bmp.Width, bmp.Height) / maxSize;
                return r;
            }

            int res = 1;
            if (bmp.Width * bmp.Height > maxSize * maxSize * 256L)
            {
                res = 32;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 128L)
            {
                res = 24;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 64L)
            {
                res = 16;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 32L)
            {
                res = 12;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 16L)
            {
                res = 8;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 8L)
            {
                res = 6;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 4L)
            {
                res = 4;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize * 2L)
            {
                res = 3;
            }
            else if (bmp.Width * bmp.Height > maxSize * maxSize)
            {
                res = 2;
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

        private void btnAlphaV_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker2.IsBusy)
            {
                this.backgroundWorker2.CancelAsync();
                return;
            }
            if (this.backgroundWorker3.IsBusy)
            {
                this.backgroundWorker3.CancelAsync();
                return;
            }
            if (this.backgroundWorker4.IsBusy)
            {
                this.backgroundWorker4.CancelAsync();
                return;
            }
            if (this.backgroundWorker6.IsBusy)
            {
                this.backgroundWorker6.CancelAsync();
                return;
            }

            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.Cursor = Cursors.WaitCursor;
                this.SetControls(false);

                if (this.cbExpOutlProc.Checked)
                {
                    this.toolStripProgressBar1.Value = 0;
                    this.toolStripProgressBar1.Visible = true;

                    this.btnAlphaV.Text = "Cancel";
                    this.btnAlphaV.Enabled = true;

                    this.numSleep.Enabled = this.label2.Enabled = true;

                    if (_sw == null)
                        _sw = new Stopwatch();
                    _sw.Reset();
                    _sw.Start();

                    this.btnOK.Enabled = this.btnCancel.Enabled = false;

                    /*
                    int windowSize = (int)this.numWinSz.Value;
                    double gamma = (double)this.numGamma.Value;
                    int normalDistToCheck = 10;   
                    double gamma2 = (double)this.numGamma2.Value;

                    this.backgroundWorker3.RunWorkerAsync(new object[] { windowSize, gamma, normalDistToCheck, gamma2 });
                    */

                    if (this.helplineRulerCtrl1.Bmp != null && this._bmpOrig != null)
                    {
                        int innerW = this._iW;
                        int outerW = this._oW;

                        bool editTrimap = this.cbEditTrimap.Checked;
                        Bitmap bWork = new Bitmap(this._bmpOrig);

                        if (cbHalfSize.Checked)
                        {
                            Bitmap bWork2 = ResampleBmp(bWork, 2);

                            Bitmap bOld = bWork;
                            bWork = bWork2;
                            bOld.Dispose();
                            bOld = null;
                        }

                        //maybe put this before the halfSize code...
                        double res = CheckWidthHeight(bWork, true, (double)this.numMaxSize.Value);
                        this.toolStripStatusLabel1.Text = "resFactor: " + Math.Max(res, 1).ToString("N2");

                        if (res > 1)
                        {
                            Bitmap bOld = bWork;
                            bWork = ResampleDown(bWork, res);
                            if (bOld != null)
                            {
                                bOld.Dispose();
                                bOld = null;
                            }
                        }

                        //the question is: should we change innerW, outerW by the resampling factor.
                        double factor = this.cbHalfSize.Checked ? 2.0 : 1.0;
                        factor *= (res > 1) ? res : 1.0;

                        innerW = (int)Math.Max(Math.Ceiling(innerW / factor), 1);
                        outerW = (int)Math.Max(Math.Ceiling(outerW / factor), 1);

                        Bitmap bTrimap = new Bitmap(bWork.Width, bWork.Height);

                        Bitmap bW = new Bitmap(this.helplineRulerCtrl1.Bmp);
                        Bitmap bOld4 = bW;
                        bW = ResampleDown(bW, factor);
                        if (bOld4 != null)
                        {
                            bOld4.Dispose();
                            bOld4 = null;
                        }

                        using (Bitmap bForeground = RemoveOutlineEx(bW, innerW, true))
                        using (Bitmap bBackground = ExtendOutlineEx(bW, outerW, true, false))
                        {
                            using (Graphics gx = Graphics.FromImage(bTrimap))
                            {
                                gx.SmoothingMode = SmoothingMode.None;
                                gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                                gx.Clear(Color.Black);
                                gx.DrawImage(bBackground, 0, 0);
                                gx.DrawImage(bForeground, 0, 0);
                            }
                        }

                        bW.Dispose();
                        bW = null;

                        Bitmap trWork = bTrimap;

                        if (editTrimap)
                        {
                            using (frmEditTrimap frm = new frmEditTrimap(trWork, bWork, factor))
                            {
                                Bitmap bmp = null;

                                if (frm.ShowDialog() == DialogResult.OK)
                                {
                                    bmp = new Bitmap(frm.FBitmap);

                                    Bitmap bOld2 = trWork;
                                    trWork = bmp;
                                    if (bOld2 != null)
                                    {
                                        bOld2.Dispose();
                                        bOld2 = null;
                                    }
                                }
                            }
                        }

                        ClosedFormMatteOp cfop = new ClosedFormMatteOp(bWork, trWork);
                        BlendParameters bParam = new BlendParameters();
                        bParam.MaxIterations = 10000;
                        bParam.InnerIterations = 25;
                        bParam.DesiredMaxLinearError = (double)this.numError.Value;
                        bParam.Sleep = this.numSleep.Value > 0 ? true : false;
                        bParam.SleepAmount = (int)this.numSleep.Value;
                        bParam.BGW = this.backgroundWorker4;
                        cfop.BlendParameters = bParam;
                        this._cfop = cfop;

                        this._cfop.ShowProgess += Cfop_UpdateProgress;
                        this._cfop.ShowInfo += _cfop_ShowInfo;

                        bool scalesPics = false;
                        int scales = 0;
                        int overlap = 32;
                        bool interpolated = false;
                        bool forceSerial = false;
                        bool group = false;
                        int groupAmountX = 0;
                        int groupAmountY = 0;
                        int maxSize = bWork.Width * bWork.Height;
                        bool trySingleTile = this.cbHalfSize.Checked ? false : true;
                        bool verifyTrimaps = false;

                        this.backgroundWorker4.RunWorkerAsync(new object[] { 0, scalesPics, scales, overlap,
                            interpolated, forceSerial, group, groupAmountX, groupAmountY, maxSize, bWork, trWork,
                            trySingleTile, verifyTrimaps });
                    }
                }
                else
                {
                    MethodMode mm = (MethodMode)System.Enum.Parse(typeof(MethodMode), this.cmbMethodMode.SelectedItem.ToString());

                    if (mm == MethodMode.ModeFeather)
                    {
                        BlendType bt = (BlendType)System.Enum.Parse(typeof(BlendType), this.cmbBlendType.SelectedItem.ToString());
                        this.backgroundWorker2.RunWorkerAsync(new object[] { bt });
                    }
                    else
                    {
                        if (this.helplineRulerCtrl1.Bmp != null)
                        {
                            this.Cursor = Cursors.WaitCursor;
                            this.SetControls(false);

                            this.toolStripProgressBar1.Value = 0;
                            this.toolStripProgressBar1.Visible = true;

                            this.btnAlphaV.Text = "Cancel";
                            this.btnAlphaV.Enabled = true;

                            int innerW = this._iW;
                            int outerW = this._oW;

                            Bitmap bOrig = new Bitmap(this._bmpOrig);
                            Bitmap bWork = new Bitmap(this.helplineRulerCtrl1.Bmp);

                            int gmm_comp = 2;
                            double gamma = (double)50.0;
                            int numIters = 1;
                            bool rectMode = true;
                            Rectangle r = GetR(bWork, this._oW);
                            bool skipInit = false;
                            bool workOnPaths = false;
                            bool gammaChanged = false;
                            int intMult = 1;
                            bool quick = true;
                            bool useEightAdj = false;
                            bool useTh = true;
                            double th = (double)this.numTh.Value;
                            double resPic = CheckWidthHeight(bWork, true, 1200);
                            bool initWKpp = true;
                            bool multCapacitiesForTLinks = true;
                            double multTLinkCapacity = 2.0;
                            bool castTLInt = true;
                            bool getSourcePart = false;
                            ListSelectionMode selMode = (ListSelectionMode)0;
                            bool scribbleMode = false;
                            Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles = null;
                            double probMult1 = 1.0;
                            double kmInitW = 2.0;
                            double kmInitH = 2.0;
                            bool setPFGToFG = false;
                            bool cgWQE = false;
                            double numItems = 0;
                            double numCorrect = 0;
                            double numItems2 = 0;
                            double numCorrect2 = 0;
                            bool skipLearn = false;

                            Rectangle clipRect = new Rectangle(0, 0, this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);
                            bool dontFillPath = true;
                            bool drawNumComp = true;
                            int comp = 1000;

                            int blur = (int)this.numBlur.Value;
                            int alphaStartValue = (int)this.numAlphaStart.Value;

                            this.backgroundWorker6.RunWorkerAsync(new object[] { bWork, bOrig, innerW, outerW,
                                    gmm_comp, gamma, numIters, rectMode, r ,skipInit, workOnPaths,
                                    gammaChanged, intMult, quick, useEightAdj, useTh, th, resPic,
                                    initWKpp, multCapacitiesForTLinks, multTLinkCapacity, castTLInt,
                                    getSourcePart, selMode, scribbleMode, scribbles, probMult1,
                                    kmInitW, kmInitH, setPFGToFG, cgWQE, numItems, numCorrect,
                                    numItems2, numCorrect2, skipLearn, clipRect, dontFillPath,
                                    drawNumComp, comp, blur, alphaStartValue });
                        }
                    }
                }
            }
        }

        private void _cfop_ShowInfo(object sender, string e)
        {
            if (InvokeRequired)
                this.Invoke(new Action(() =>
                {
                    this.toolStripStatusLabel4.Text = e;
                    if (e.StartsWith("pic "))
                        this.toolStripStatusLabel1.Text = e;
                    //if (e.StartsWith("outer pic-amount"))
                    //    this.label13.Text = e;
                    if (e.StartsWith("picOuter "))
                        this.toolStripStatusLabel1.Text = e;
                }));
            else
            {
                this.toolStripStatusLabel4.Text = e;
                if (e.StartsWith("pic "))
                    this.toolStripStatusLabel1.Text = e;
                //if (e.StartsWith("outer pic-amount"))
                //    this.label13.Text = e;
                if (e.StartsWith("picOuter "))
                    this.toolStripStatusLabel1.Text = e;
            }
        }

        private void Cfop_UpdateProgress(object sender, GetAlphaMatte.ProgressEventArgs e)
        {
            this.backgroundWorker4.ReportProgress((int)(e.CurrentProgress / e.ImgWidthHeight * 100));
        }

        private void frmProcOutline_Load(object sender, EventArgs e)
        {
            foreach (string z in System.Enum.GetNames(typeof(BlendType)))
                this.cmbBlendType.Items.Add(z.ToString());

            this.cmbBlendType.SelectedIndex = 1;

            foreach (string z in System.Enum.GetNames(typeof(MethodMode)))
                this.cmbMethodMode.Items.Add(z.ToString());

            this.cmbMethodMode.SelectedIndex = 1;

            this.rbBoth.Checked = true;
            //this.cbBayes.Checked = true;

            this.cbBGColor_CheckedChanged(this.cbBGColor, new EventArgs());

            DisableBoundControls(this.cbExpOutlProc.Checked);
        }

        private void SetControls(bool e)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    foreach (Control ct in this.splitContainer1.Panel2.Controls)
                    {
                        if (ct.Name != "btnCancel" && !(ct is PictureBox))
                            ct.Enabled = e;
                        if (ct is GroupBox)
                        {
                            ct.Enabled = true;
                            GroupBox gb = ct as GroupBox;

                            foreach (Control c in gb.Controls)
                            {
                                if (!(c is Button))
                                    c.Enabled = e;

                                if (c is GroupBox)
                                {
                                    c.Enabled = true;
                                    GroupBox g = c as GroupBox;
                                    foreach (Control c1 in g.Controls)
                                    {
                                        if (!(c1 is Button) && !(c1.Name == "numSleep"))
                                            c1.Enabled = e;

                                        if (c1.Name == "numSleep")
                                            c1.Enabled = true;
                                    }
                                }
                            }
                        }
                    }

                    this.helplineRulerCtrl1.Enabled = this.helplineRulerCtrl1.Enabled = e;

                    this.Cursor = e ? Cursors.Default : Cursors.WaitCursor;
                }));
            }
            else
            {
                foreach (Control ct in this.splitContainer1.Panel2.Controls)
                {
                    if (ct.Name != "btnCancel" && !(ct is PictureBox))
                        ct.Enabled = e;
                    if (ct is GroupBox)
                    {
                        ct.Enabled = true;
                        GroupBox gb = ct as GroupBox;

                        foreach (Control c in gb.Controls)
                        {
                            if (!(c is Button))
                                c.Enabled = e;

                            if (c is GroupBox)
                            {
                                c.Enabled = true;
                                GroupBox g = c as GroupBox;
                                foreach (Control c1 in g.Controls)
                                {
                                    if (!(c1 is Button) && !(c1.Name == "numSleep"))
                                        c1.Enabled = e;

                                    if (c1.Name == "numSleep")
                                        c1.Enabled = true;
                                }
                            }
                        }
                    }
                }

                this.helplineRulerCtrl1.Enabled = this.helplineRulerCtrl1.Enabled = e;

                this.Cursor = e ? Cursors.Default : Cursors.WaitCursor;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap bmp = null;

            DefaultSmoothenOP dsOP = new DefaultSmoothenOP(this._bmpBU, this._bmpOrig);
            //dsOP.ShowInfo += _gc_ShowInfo;
            dsOP.BGW = this.backgroundWorker1;
            dsOP.Init((double)this.numPPEpsilon.Value, (double)this.numPPEpsilon2.Value, this.cbPPRemove.Checked, (int)this.numPPRemove.Value, (int)this.numPPRemove2.Value, this.cbApproxLines.Checked);
            if (this.cbPPCleanOutline.Checked)
                dsOP.CleanOutline((int)this.numPPPixelDepthOuter.Value, (int)this.numPPThresholdOuter.Value, (int)this.numPPPixelDepthInner.Value, (int)this.numPPThresholdInner.Value,
                    this.cbRemArea.Checked, (int)this.numPPMinAllowedArea.Value, (int)this.numPPCleanAmount.Value);
            else
            {
                if (this.cbRemArea.Checked)
                    dsOP.RemAreas((int)this.numPPMinAllowedArea.Value);
            }
            dsOP.ComputeOutlineInfo();
            bmp = dsOP.ComputePic(false, 0.0f, this.cbOutlinesAsCurves.Checked, (float)this.numTension.Value, false, 1.0f);
            if (bmp != null && this.cbReShift.Checked)
            {
                Bitmap bOld = bmp;
                bmp = dsOP.ReShiftShapes(bmp, (int)this.numShiftX.Value, (int)this.numShiftY.Value);
                if (bOld != null)
                {
                    bOld.Dispose();
                    bOld = null;
                }
            }
            this._dsOP = dsOP;

            e.Result = bmp;
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
            Bitmap bmp = null;

            if (e.Result != null)
                bmp = (Bitmap)e.Result;

            if (bmp != null)
            {
                this.SetBitmap(this.helplineRulerCtrl1.Bmp, bmp, this.helplineRulerCtrl1, "Bmp");

                Bitmap bC = new Bitmap(bmp);
                this.SetBitmap(ref _bmpRef, ref bC);

                this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                    (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                _undoOPCache.Add(bmp);
            }

            if (this._dsOP != null)
            {
                //this._dsOP.ShowInfo -= _gc_ShowInfo;
                this._dsOP.Dispose();
            }

            this.btnPPGo.Text = "Go";

            this.SetControls(true);
            this.Cursor = Cursors.Default;

            this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());

            this._pic_changed = true;

            this.helplineRulerCtrl1.dbPanel1.Invalidate();

            if (this.Timer3.Enabled)
                this.Timer3.Stop();

            this.Timer3.Start();

            this.backgroundWorker1.Dispose();
            this.backgroundWorker1 = new BackgroundWorker();
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.WorkerSupportsCancellation = true;
            this.backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            this.backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            this.backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
        }

        private void Timer3_Tick(object sender, EventArgs e)
        {
            this.Timer3.Stop();

            if (!this.toolStripProgressBar1.IsDisposed)
            {
                this.toolStripProgressBar1.Visible = false;
                this.toolStripProgressBar1.Value = 0;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap bmp = null;

            object[] o = (object[])e.Argument;
            BlendType bt = (BlendType)o[0];

            BoundaryMattingOP bmOP = new BoundaryMattingOP(this.helplineRulerCtrl1.Bmp, this._bmpOrig);
            //bmOP.ShowInfo += _gc_ShowInfo;
            bmOP.BGW = this.backgroundWorker2;

            bmOP.Init((int)this.numNormalDist.Value, (int)this.numBoundInner.Value, (int)this.numBoundOuter.Value,
                (float)Math.Min(this.numColDistDist.Value, this.numBoundOuter.Value), (double)this.numAlphaStart.Value, bt);

            ColorSource cs = ColorSource.OuterPixels;
            double numFactorOuterPx = (double)this.numFactorOuterPx.Value;
            int blur = (int)this.numBlur.Value;

            if (!this.cbBlur.Checked)
                blur = 0;

            if (this.rbBoundary.Checked)
                cs = ColorSource.Boundary;
            else if (this.rbBoth.Checked)
                cs = ColorSource.Both;

            bmp = bmOP.ProcessImage(cs, numFactorOuterPx, blur);

            this._bmOP = bmOP;

            e.Result = bmp;
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
            Bitmap bmp = null;

            if (e.Result != null)
                bmp = (Bitmap)e.Result;

            if (bmp != null)
            {
                this.SetBitmap(this.helplineRulerCtrl1.Bmp, bmp, this.helplineRulerCtrl1, "Bmp");

                Bitmap bC = new Bitmap(bmp);
                this.SetBitmap(ref _bmpRef, ref bC);

                this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                    (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                _undoOPCache.Add(bmp);
            }

            if (this._bmOP != null)
            {
                //this._bmOP.ShowInfo -= _gc_ShowInfo;
                this._bmOP.Dispose();
            }

            this.btnPPGo.Text = "Go";

            this.SetControls(true);
            this.Cursor = Cursors.Default;

            this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());

            this._pic_changed = true;

            this.helplineRulerCtrl1.dbPanel1.Invalidate();

            if (this.Timer3.Enabled)
                this.Timer3.Stop();

            this.Timer3.Start();

            this.backgroundWorker2.Dispose();
            this.backgroundWorker2 = new BackgroundWorker();
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.WorkerSupportsCancellation = true;
            this.backgroundWorker2.DoWork += backgroundWorker2_DoWork;
            this.backgroundWorker2.ProgressChanged += backgroundWorker2_ProgressChanged;
            this.backgroundWorker2.RunWorkerCompleted += backgroundWorker2_RunWorkerCompleted;
        }

        private void cbExpOutlProc_CheckedChanged(object sender, EventArgs e)
        {
            DisableBoundControls(this.cbExpOutlProc.Checked);
        }

        private void DisableBoundControls(bool ch)
        {
            for (int i = 0; i < this.groupBox4.Controls.Count; i++)
                if (!(this.groupBox4.Controls[i] is GroupBox) && !(this.groupBox4.Controls[i] is Button))
                    this.groupBox4.Controls[i].Enabled = !ch;

            this.label45.Enabled = this.label46.Enabled = this.numBoundOuter.Enabled = this.numBoundInner.Enabled = true;
            this.label52.Enabled = this.cbBlur.Enabled = this.numBlur.Enabled = !ch; //maybe this changes
            this.label54.Enabled = ch;
            this.cbHalfSize.Enabled = this.numError.Enabled = ch;
            this.label9.Enabled = this.numMaxSize.Enabled = ch;
            this.label4.Enabled = this.numTh.Enabled = !ch;

            cmbMethodMode_SelectedIndexChanged(this.cmbMethodMode, new EventArgs());

            int maxWidth = this._maxWidth;
            int oW = (int)this.numBoundOuter.Value;
            int iW = (int)this.numBoundInner.Value;

            if (oW + iW > maxWidth)
            {
                int diff = oW + iW - maxWidth;

                if (diff > 0)
                {
                    if (oW >= diff)
                        oW -= diff;
                    else
                    {
                        iW -= Math.Max(diff - oW, 0);
                        oW = 0;
                    }
                }
            }

            this._oW = oW;
            this._iW = iW;
        }

        private Bitmap RemoveOutlineEx(Bitmap bmp, int innerW, bool dontFill)
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
                                    gx.FillPath(Brushes.White, gp);
                                    using (Pen p = new Pen(Color.White, 1))
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

            return bOut;
        }

        private Bitmap ExtendOutlineEx(Bitmap bmp, int outerW, bool dontFill, bool drawPath)
        {
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
                                    gx.FillPath(Brushes.Gray, gp);
                                    gx.DrawPath(Pens.Gray, gp);

                                    if (drawPath && outerW > 0)
                                    {
                                        try
                                        {
                                            using (Pen pen = new Pen(Color.Gray, outerW))
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

        private Bitmap ExtendOutlineEx2(Bitmap bmp, int outerW, bool dontFill, bool drawPath)
        {
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

                                    using (TextureBrush tb = new TextureBrush(bmp))
                                    {
                                        gx.FillPath(tb, gp);

                                        using (Pen pen = new Pen(tb, 1))
                                            gx.DrawPath(Pens.Gray, gp);

                                        if (drawPath && outerW > 0)
                                        {
                                            try
                                            {
                                                using (Pen pen = new Pen(tb, outerW))
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
                                            if (drawPath)
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

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] o = (object[])e.Argument;
            int windowSize = (int)o[0];
            double gamma = (double)o[1];
            int normalDistToCheck = (int)o[2];  
            double gamma2 = (double)o[3];

            if (this.helplineRulerCtrl1.Bmp != null && this._bmpOrig != null)
            {
                int innerW = this._iW;
                int outerW = this._oW;

                Bitmap bTrimap = new Bitmap(this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);

                using (Bitmap bForeground = RemoveOutlineEx(this.helplineRulerCtrl1.Bmp, innerW, true))
                using (Bitmap bBackground = ExtendOutlineEx(this.helplineRulerCtrl1.Bmp, outerW, true, false))
                {
                    using (Graphics gx = Graphics.FromImage(bTrimap))
                    {
                        gx.SmoothingMode = SmoothingMode.None;
                        gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                        gx.Clear(Color.Black);
                        gx.DrawImage(bBackground, 0, 0);
                        gx.DrawImage(bForeground, 0, 0);
                    }
                }

                Bitmap bWork = new Bitmap(this._bmpOrig);
                Bitmap trWork = bTrimap;

                e.Result = ExperimentalOutlineProc(bWork, trWork, windowSize, gamma, gamma2, normalDistToCheck);
                return;
            }
        }

        private Bitmap ExperimentalOutlineProc(Bitmap bOrig, Bitmap trWork, int windowSize, double gamma,  double gamma2, int normalDistToCheck)
        {
            Bitmap fg = new Bitmap(this.helplineRulerCtrl1.Bmp);

            BoundaryMattingOP bMOP = new BoundaryMattingOP(fg, bOrig);
            Bitmap bRes = bMOP.ExperimentalOutlineProc(trWork, this._iW, this._oW, windowSize, gamma, gamma2, normalDistToCheck, this.backgroundWorker3);
            bMOP.Dispose();

            return bRes;
        }

        private void backgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!InvokeRequired)
            {
                this.Text = "frmProcOutline";
                this.Text += "        - ### -        " + TimeSpan.FromMilliseconds(this._sw.ElapsedMilliseconds).ToString();
                if (!this.IsDisposed && this.Visible && !this.toolStripProgressBar1.IsDisposed)
                    this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
            }
            else
                this.Invoke(new Action(() =>
                {
                    this.Text = "frmProcOutline";
                    if (!this.IsDisposed && this.Visible && !this.toolStripProgressBar1.IsDisposed)
                    {
                        this.Text += "        - ### -        " + TimeSpan.FromMilliseconds(this._sw.ElapsedMilliseconds).ToString();
                        this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
                    }
                }));
        }

        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                Bitmap bRes = (Bitmap)e.Result;

                this.SetBitmap(this.helplineRulerCtrl1.Bmp, bRes, this.helplineRulerCtrl1, "Bmp");

                Bitmap bC = new Bitmap(bRes);
                this.SetBitmap(ref _bmpRef, ref bC);

                this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                    (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                _undoOPCache.Add(bRes);

            }

            this.btnPPGo.Text = "Go";

            this.SetControls(true);
            this.Cursor = Cursors.Default;

            this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());

            this.btnAlphaV.Text = "Go";

            this._pic_changed = true;

            this.helplineRulerCtrl1.dbPanel1.Invalidate();

            if (this.Timer3.Enabled)
                this.Timer3.Stop();

            this.Timer3.Start();

            this.backgroundWorker3.Dispose();
            this.backgroundWorker3 = new BackgroundWorker();
            this.backgroundWorker3.WorkerReportsProgress = true;
            this.backgroundWorker3.WorkerSupportsCancellation = true;
            this.backgroundWorker3.DoWork += backgroundWorker3_DoWork;
            this.backgroundWorker3.ProgressChanged += backgroundWorker3_ProgressChanged;
            this.backgroundWorker3.RunWorkerCompleted += backgroundWorker3_RunWorkerCompleted;

            this._sw.Stop();
            this.Text = "frmProcOutline";
            this.Text += "        - ### -        " + TimeSpan.FromMilliseconds(this._sw.ElapsedMilliseconds).ToString();
        }

        private List<ChainCode> GetBoundary(Bitmap bmp)
        {
            ChainFinder cf = new ChainFinder();
            cf.AllowNullCells = true;
            List<ChainCode> l = cf.GetOutline(bmp, 0, false, 0, false, 0, false);
            return l;
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

        private void btmSetBU_Click(object sender, EventArgs e)
        {
            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.toolStripStatusLabel4.Text = "working...";
                this.statusStrip1.Refresh();
                Bitmap bC = new Bitmap(this.helplineRulerCtrl1.Bmp);
                this.SetBitmap(ref this._bmpBU, ref bC);
                this.toolStripStatusLabel4.Text = "done";
            }
        }

        private void numBoundOuter_ValueChanged(object sender, EventArgs e)
        {
            DisableBoundControls(this.cbExpOutlProc.Checked);
        }

        private void numBoundInner_ValueChanged(object sender, EventArgs e)
        {
            DisableBoundControls(this.cbExpOutlProc.Checked);
        }

        private void backgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            _cfop_ShowInfo(this, "outer pic-amount " + "...");
            if (this._cfop != null && e.Argument != null)
            {
                object[] o = (object[])e.Argument;
                int mode = (int)o[0];

                bool scalesPics = (bool)o[1];
                int scales = (int)o[2];

                int overlap = (int)o[3];
                bool interpolated = (bool)o[4];
                bool forceSerial = (bool)o[5];

                bool group = (bool)o[6];
                int groupAmountX = (int)o[7];
                int groupAmountY = (int)o[8];

                int maxSize = (int)o[9];

                Bitmap bWork = (Bitmap)o[10];
                Bitmap trWork = (Bitmap)o[11];

                bool trySingleTile = (bool)o[12];
                bool verifyTrimaps = (bool)o[13];

                string trimapProblemMessage = "In this configuration at least one trimap does not contain sufficient Information. " +
                    "Consider running the task again with a larger tileSize or less subtiles.\n\nYou could also rebuild the matte " +
                    "for selected rectangles by clicking on the \"RescanParts\" button.";
                int id = Environment.TickCount;
                this._lastRunNumber = id;

                if (!scalesPics)
                {
                    if (!AvailMem.AvailMem.checkAvailRam(bWork.Width * bWork.Height * 20L))
                        trySingleTile = false;

                    if (trySingleTile)
                    {
                        if (verifyTrimaps)
                        {
                            if (!CheckTrimap(trWork))
                            {
                                this.Invoke(new Action(() =>
                                {
                                    if (this._frmInfo == null || this._frmInfo.IsDisposed)
                                        this._frmInfo = new frmInfo();
                                    this._frmInfo.Show(trimapProblemMessage);
                                }));

                                this._trimapProblemInfos.Add(new TrimapProblemInfo(id, 1, 0, 0, trWork.Width, trWork.Height, 0));
                            }
                        }

                        _cfop_ShowInfo(this, "outer pic-amount " + 1.ToString());
                        this._cfop.GetMattingLaplacian(Math.Pow(10, -7));
                        Bitmap b = null;

                        if (this._cfop.BlendParameters.BGW != null && this._cfop.BlendParameters.BGW.WorkerSupportsCancellation && this._cfop.BlendParameters.BGW.CancellationPending)
                        {
                            e.Result = null;
                            return;
                        }

                        if (mode == 0)
                            b = this._cfop.SolveSystemGaussSeidel();
                        else if (mode == 1)
                            b = this._cfop.SolveSystemGMRES();

                        e.Result = b;
                    }
                    else
                    {
                        Bitmap result = new Bitmap(bWork.Width, bWork.Height);

                        int wh = bWork.Width * bWork.Height;
                        int n = 1;

                        while (wh > maxSize)
                        {
                            n += 1;
                            wh = bWork.Width / n * bWork.Height / n;
                        }
                        int n2 = n * n;

                        int h = bWork.Height / n;
                        int h2 = bWork.Height - h * (n - 1);

                        int w = bWork.Width / n;
                        int w2 = bWork.Width - w * (n - 1);

                        overlap = Math.Max(overlap, 1);

                        if (n2 == 1)
                            overlap = 0;

                        List<Bitmap> bmp = new List<Bitmap>();
                        List<Bitmap> bmp2 = new List<Bitmap>();

                        GetTiles(bWork, trWork, bmp, bmp2, w, w2, h, h2, overlap, n);

                        if (verifyTrimaps)
                            if (!CheckTrimaps(bmp2, w, h, n, id, overlap))
                                this.Invoke(new Action(() =>
                                {
                                    if (this._frmInfo == null || this._frmInfo.IsDisposed)
                                        this._frmInfo = new frmInfo();
                                    this._frmInfo.Show(trimapProblemMessage);
                                }));

                        Bitmap[] bmp4 = new Bitmap[bmp.Count];

                        this._cfopArray = new ClosedFormMatteOp[bmp.Count];

                        if (AvailMem.AvailMem.checkAvailRam(w * h * bmp.Count * 20L) && !forceSerial)
                            Parallel.For(0, bmp.Count, i =>
                            {
                                _cfop_ShowInfo(this, "pic " + (i + 1).ToString());
                                ClosedFormMatteOp cfop = new ClosedFormMatteOp(bmp[i], bmp2[i]);
                                BlendParameters bParam = new BlendParameters();
                                bParam.MaxIterations = _cfop.BlendParameters.MaxIterations;
                                bParam.InnerIterations = _cfop.BlendParameters.InnerIterations;
                                bParam.DesiredMaxLinearError = _cfop.BlendParameters.DesiredMaxLinearError;
                                bParam.Sleep = _cfop.BlendParameters.Sleep;
                                bParam.SleepAmount = _cfop.BlendParameters.SleepAmount;
                                bParam.BGW = this.backgroundWorker1;
                                cfop.BlendParameters = bParam;

                                cfop.ShowProgess += Cfop_UpdateProgress;
                                cfop.ShowInfo += _cfop_ShowInfo;

                                this._cfopArray[i] = cfop;

                                cfop.GetMattingLaplacian(Math.Pow(10, -7));
                                Bitmap b = null;

                                if (mode == 0)
                                    b = cfop.SolveSystemGaussSeidel();
                                else if (mode == 1)
                                    b = cfop.SolveSystemGMRES();

                                //save and draw out later serially
                                bmp4[i] = b;

                                cfop.ShowProgess -= Cfop_UpdateProgress;
                                cfop.ShowInfo -= _cfop_ShowInfo;
                                cfop.Dispose();
                            });
                        else
                        {
                            for (int i = 0; i < bmp.Count; i++)
                            {
                                _cfop_ShowInfo(this, "pic " + (i + 1).ToString());
                                ClosedFormMatteOp cfop = new ClosedFormMatteOp(bmp[i], bmp2[i]);
                                BlendParameters bParam = new BlendParameters();
                                bParam.MaxIterations = _cfop.BlendParameters.MaxIterations;
                                bParam.InnerIterations = _cfop.BlendParameters.InnerIterations;
                                bParam.DesiredMaxLinearError = _cfop.BlendParameters.DesiredMaxLinearError;
                                bParam.Sleep = _cfop.BlendParameters.Sleep;
                                bParam.SleepAmount = _cfop.BlendParameters.SleepAmount;
                                bParam.BGW = this.backgroundWorker1;
                                cfop.BlendParameters = bParam;

                                cfop.ShowProgess += Cfop_UpdateProgress;
                                cfop.ShowInfo += _cfop_ShowInfo;

                                this._cfopArray[i] = cfop;

                                cfop.GetMattingLaplacian(Math.Pow(10, -7));
                                Bitmap b = null;

                                if (mode == 0)
                                    b = cfop.SolveSystemGaussSeidel();
                                else if (mode == 1)
                                    b = cfop.SolveSystemGMRES();

                                //save and draw out later serially
                                bmp4[i] = b;

                                cfop.ShowProgess -= Cfop_UpdateProgress;
                                cfop.ShowInfo -= _cfop_ShowInfo;
                                cfop.Dispose();
                            }
                        }

                        if (this._cfop.BlendParameters.BGW != null && this._cfop.BlendParameters.BGW.WorkerSupportsCancellation && this._cfop.BlendParameters.BGW.CancellationPending)
                        {
                            for (int i = bmp.Count - 1; i >= 0; i--)
                            {
                                if (bmp[i] != null)
                                    bmp[i].Dispose();
                                if (bmp2[i] != null)
                                    bmp2[i].Dispose();
                                if (bmp4[i] != null)
                                    bmp4[i].Dispose();
                            }
                            e.Result = null;
                            return;
                        }

                        for (int i = 0; i < bmp.Count; i++)
                        {
                            int x = i % n;
                            int y = i / n;

                            using (Graphics gx = Graphics.FromImage(result))
                                gx.DrawImage(bmp4[i], x * w - (x == 0 ? 0 : overlap), y * h - (y == 0 ? 0 : overlap));
                        }

                        for (int i = bmp.Count - 1; i >= 0; i--)
                        {
                            bmp[i].Dispose();
                            bmp2[i].Dispose();
                            bmp4[i].Dispose();
                        }

                        e.Result = result;
                    }
                }
                else
                {
                    if (trySingleTile)
                    {
                        bool wgth = bWork.Width > bWork.Height;
                        int xP = 2;
                        int yP = 2;

                        if (scales == 8)
                        {
                            xP = wgth ? 4 : 2;
                            yP = wgth ? 2 : 4;
                        }
                        if (scales == 16)
                        {
                            xP = 4;
                            yP = 4;
                        }
                        if (scales == 32)
                        {
                            xP = wgth ? 8 : 4;
                            yP = wgth ? 4 : 8;
                        }
                        if (scales == 64)
                        {
                            xP = 8;
                            yP = 8;
                        }
                        if (scales == 128)
                        {
                            xP = wgth ? 16 : 8;
                            yP = wgth ? 8 : 16;
                        }
                        if (scales == 256)
                        {
                            xP = 16;
                            yP = 16;
                        }

                        int w = bWork.Width;
                        int h = bWork.Height;

                        Bitmap cfopBmp = bWork;
                        Bitmap cfopTrimap = trWork;

                        if (interpolated)
                        {
                            w = (int)(w * 1.41);
                            h = (int)(h * 1.41);

                            cfopBmp = new Bitmap(w, h);
                            cfopTrimap = new Bitmap(w, h);

                            using (Graphics gx = Graphics.FromImage(cfopBmp))
                            {
                                gx.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                gx.DrawImage(bWork, 0, 0, w, h);
                            }

                            using (Graphics gx = Graphics.FromImage(cfopTrimap))
                            {
                                gx.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                gx.DrawImage(trWork, 0, 0, w, h);
                            }
                        }

                        Bitmap result = new Bitmap(w, h);

                        List<Bitmap> bmp = new List<Bitmap>();
                        List<Bitmap> bmp2 = new List<Bitmap>();
                        List<Size> sizes = new List<Size>();

                        int ww = 0;
                        int hh = 0;

                        int www = result.Width / xP;
                        int hhh = result.Height / yP;
                        int xAdd2 = result.Width - www * xP;
                        int yAdd2 = result.Height - hhh * yP;

                        for (int y = 0; y < yP; y++)
                        {
                            for (int x = 0; x < xP; x++)
                            {
                                Size sz = new Size(result.Width / xP, result.Height / yP);
                                int xAdd = result.Width - sz.Width * xP;
                                int yAdd = result.Height - sz.Height * yP;

                                if (y < yP - 1)
                                {
                                    if (x < xP - 1)
                                        sizes.Add(new Size(sz.Width, sz.Height));
                                    else
                                        sizes.Add(new Size(sz.Width + xAdd, sz.Height));
                                }
                                else
                                {
                                    if (x < xP - 1)
                                        sizes.Add(new Size(sz.Width, sz.Height + yAdd));
                                    else
                                        sizes.Add(new Size(sz.Width + xAdd, sz.Height + yAdd));
                                }

                                int xx = sizes[sizes.Count - 1].Width / groupAmountX;
                                int yy = sizes[sizes.Count - 1].Height / groupAmountY;

                                ww = sizes[sizes.Count - 1].Width - (xx * groupAmountX);
                                hh = sizes[sizes.Count - 1].Height - (yy * groupAmountY);

                                Bitmap b1 = new Bitmap(sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height);
                                Bitmap b2 = new Bitmap(sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height);

                                int wdth = result.Width;
                                int hght = result.Height;

                                BitmapData bmD = cfopBmp.LockBits(new Rectangle(0, 0, wdth, hght), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                                BitmapData bmT = cfopTrimap.LockBits(new Rectangle(0, 0, wdth, hght), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                                BitmapData b1D = b1.LockBits(new Rectangle(0, 0, sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                                BitmapData b2D = b2.LockBits(new Rectangle(0, 0, sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                                int strideD = bmD.Stride;
                                int strideB = b1D.Stride;

                                unsafe
                                {
                                    byte* p = (byte*)bmD.Scan0;
                                    byte* pT = (byte*)bmT.Scan0;
                                    byte* p1 = (byte*)b1D.Scan0;
                                    byte* p2 = (byte*)b2D.Scan0;

                                    if (group)
                                    {
                                        for (int y2 = y * groupAmountY, y4 = 0; y2 < hght + yP; y2 += yP * groupAmountY, y4 += groupAmountY)
                                        {
                                            for (int x2 = x * groupAmountX, x4 = 0; x2 < wdth + xP; x2 += xP * groupAmountX, x4 += groupAmountX)
                                            {
                                                for (int y7 = y2, y41 = y4, cntY = 0; y7 <= y2 + groupAmountY; y7++, y41++)
                                                {
                                                    for (int x7 = x2, x41 = x4, cntX = 0; x7 <= x2 + groupAmountX; x7++, x41++)
                                                    {
                                                        if (x7 < wdth - ww * xP && y7 < hght - hh * yP && x41 < b1.Width && y41 < b1.Height)
                                                        {
                                                            p1[y41 * strideB + x41 * 4] = p[y7 * strideD + x7 * 4];
                                                            p1[y41 * strideB + x41 * 4 + 1] = p[y7 * strideD + x7 * 4 + 1];
                                                            p1[y41 * strideB + x41 * 4 + 2] = p[y7 * strideD + x7 * 4 + 2];
                                                            p1[y41 * strideB + x41 * 4 + 3] = p[y7 * strideD + x7 * 4 + 3];

                                                            p2[y41 * strideB + x41 * 4] = pT[y7 * strideD + x7 * 4];
                                                            p2[y41 * strideB + x41 * 4 + 1] = pT[y7 * strideD + x7 * 4 + 1];
                                                            p2[y41 * strideB + x41 * 4 + 2] = pT[y7 * strideD + x7 * 4 + 2];
                                                            p2[y41 * strideB + x41 * 4 + 3] = pT[y7 * strideD + x7 * 4 + 3];
                                                        }
                                                        else
                                                        {
                                                            if (x7 > wdth)
                                                            {
                                                                x7 = wdth - 1 - (ww * (xP - x)) + cntX - 1;
                                                                x41 = b1.Width - 1 - ww + cntX;
                                                                cntX++;
                                                            }
                                                            if (y7 >= hght)
                                                            {
                                                                y7 = hght - 1 - (hh * (yP - y)) + cntY - 1;
                                                                y41 = b1.Height - 1 - hh + cntY;
                                                                cntY++;
                                                            }

                                                            if (x7 < wdth && y7 < hght && x41 < b1.Width && y41 < b1.Height)
                                                            {
                                                                p1[y41 * strideB + x41 * 4] = p[y7 * strideD + x7 * 4];
                                                                p1[y41 * strideB + x41 * 4 + 1] = p[y7 * strideD + x7 * 4 + 1];
                                                                p1[y41 * strideB + x41 * 4 + 2] = p[y7 * strideD + x7 * 4 + 2];
                                                                p1[y41 * strideB + x41 * 4 + 3] = p[y7 * strideD + x7 * 4 + 3];

                                                                p2[y41 * strideB + x41 * 4] = pT[y7 * strideD + x7 * 4];
                                                                p2[y41 * strideB + x41 * 4 + 1] = pT[y7 * strideD + x7 * 4 + 1];
                                                                p2[y41 * strideB + x41 * 4 + 2] = pT[y7 * strideD + x7 * 4 + 2];
                                                                p2[y41 * strideB + x41 * 4 + 3] = pT[y7 * strideD + x7 * 4 + 3];
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int y2 = y, y4 = 0; y2 < hght + yP; y2 += yP, y4++)
                                        {
                                            for (int x2 = x, x4 = 0; x2 < wdth + xP; x2 += xP, x4++)
                                            {
                                                if (x4 < b1.Width && y4 < b1.Height && x2 < wdth && y2 < hght)
                                                {
                                                    p1[y4 * strideB + x4 * 4] = p[y2 * strideD + x2 * 4];
                                                    p1[y4 * strideB + x4 * 4 + 1] = p[y2 * strideD + x2 * 4 + 1];
                                                    p1[y4 * strideB + x4 * 4 + 2] = p[y2 * strideD + x2 * 4 + 2];
                                                    p1[y4 * strideB + x4 * 4 + 3] = p[y2 * strideD + x2 * 4 + 3];

                                                    p2[y4 * strideB + x4 * 4] = pT[y2 * strideD + x2 * 4];
                                                    p2[y4 * strideB + x4 * 4 + 1] = pT[y2 * strideD + x2 * 4 + 1];
                                                    p2[y4 * strideB + x4 * 4 + 2] = pT[y2 * strideD + x2 * 4 + 2];
                                                    p2[y4 * strideB + x4 * 4 + 3] = pT[y2 * strideD + x2 * 4 + 3];
                                                }
                                                else if (x4 < b1.Width && y4 < b1.Height && (x2 >= wdth || y2 >= hght))
                                                {
                                                    if (x2 >= wdth)
                                                        x2 -= xP - (xP - x);
                                                    if (y2 >= hght)
                                                        y2 -= yP - (yP - y);

                                                    p1[y4 * strideB + x4 * 4] = p[y2 * strideD + x2 * 4];
                                                    p1[y4 * strideB + x4 * 4 + 1] = p[y2 * strideD + x2 * 4 + 1];
                                                    p1[y4 * strideB + x4 * 4 + 2] = p[y2 * strideD + x2 * 4 + 2];
                                                    p1[y4 * strideB + x4 * 4 + 3] = p[y2 * strideD + x2 * 4 + 3];

                                                    p2[y4 * strideB + x4 * 4] = pT[y2 * strideD + x2 * 4];
                                                    p2[y4 * strideB + x4 * 4 + 1] = pT[y2 * strideD + x2 * 4 + 1];
                                                    p2[y4 * strideB + x4 * 4 + 2] = pT[y2 * strideD + x2 * 4 + 2];
                                                    p2[y4 * strideB + x4 * 4 + 3] = pT[y2 * strideD + x2 * 4 + 3];
                                                }
                                            }
                                        }
                                    }
                                }

                                b2.UnlockBits(b2D);
                                b1.UnlockBits(b1D);
                                cfopTrimap.UnlockBits(bmT);
                                cfopBmp.UnlockBits(bmD);

                                bmp.Add(b1);
                                bmp2.Add(b2);

                                //try
                                //{
                                //    Form fff = new Form();
                                //    fff.BackgroundImage = b1;
                                //    fff.BackgroundImageLayout = ImageLayout.Zoom;
                                //    fff.ShowDialog();
                                //    fff.BackgroundImage = b2;
                                //    fff.BackgroundImageLayout = ImageLayout.Zoom;
                                //    fff.ShowDialog();
                                //}
                                //catch (Exception exc)
                                //{
                                //    Console.WriteLine(exc.ToString());
                                //}
                            }
                        }

                        if (verifyTrimaps)
                            if (!CheckTrimaps(bmp2, www, hhh, xP, id, xAdd2, yAdd2))  //no overlap, we dont have an outerArray and all inner pics resemble the whole pic
                                this.Invoke(new Action(() =>
                                {
                                    if (this._frmInfo == null || this._frmInfo.IsDisposed)
                                        this._frmInfo = new frmInfo();
                                    this._frmInfo.Show(trimapProblemMessage);
                                }));

                        Bitmap[] bmp4 = new Bitmap[bmp.Count];

                        _cfop_ShowInfo(this, "outer pic-amount " + bmp.Count().ToString());

                        this._cfopArray = new ClosedFormMatteOp[bmp.Count];

                        if (AvailMem.AvailMem.checkAvailRam(w * h * bmp.Count * 10L) && !forceSerial)
                            Parallel.For(0, bmp.Count, i =>
                            //for(int i = 0; i < bmp.Count; i++)
                            {
                                ClosedFormMatteOp cfop = new ClosedFormMatteOp(bmp[i], bmp2[i]);
                                BlendParameters bParam = new BlendParameters();
                                bParam.MaxIterations = _cfop.BlendParameters.MaxIterations;
                                bParam.InnerIterations = _cfop.BlendParameters.InnerIterations;
                                bParam.DesiredMaxLinearError = _cfop.BlendParameters.DesiredMaxLinearError;
                                bParam.Sleep = _cfop.BlendParameters.Sleep;
                                bParam.SleepAmount = _cfop.BlendParameters.SleepAmount;
                                bParam.BGW = this.backgroundWorker1;
                                cfop.BlendParameters = bParam;

                                cfop.ShowProgess += Cfop_UpdateProgress;
                                cfop.ShowInfo += _cfop_ShowInfo;

                                this._cfopArray[i] = cfop;

                                cfop.GetMattingLaplacian(Math.Pow(10, -7));
                                Bitmap b = null;

                                if (mode == 0)
                                    b = cfop.SolveSystemGaussSeidel();
                                else if (mode == 1)
                                    b = cfop.SolveSystemGMRES();

                                //save and draw out later serially
                                bmp4[i] = b;

                                cfop.ShowProgess -= Cfop_UpdateProgress;
                                cfop.ShowInfo -= _cfop_ShowInfo;
                                cfop.Dispose();
                            });
                        else
                            for (int i = 0; i < bmp.Count; i++)
                            {
                                _cfop_ShowInfo(this, "pic " + (i + 1).ToString());
                                ClosedFormMatteOp cfop = new ClosedFormMatteOp(bmp[i], bmp2[i]);
                                BlendParameters bParam = new BlendParameters();
                                bParam.MaxIterations = _cfop.BlendParameters.MaxIterations;
                                bParam.InnerIterations = _cfop.BlendParameters.InnerIterations;
                                bParam.DesiredMaxLinearError = _cfop.BlendParameters.DesiredMaxLinearError;
                                bParam.Sleep = _cfop.BlendParameters.Sleep;
                                bParam.SleepAmount = _cfop.BlendParameters.SleepAmount;
                                bParam.BGW = this.backgroundWorker1;
                                cfop.BlendParameters = bParam;

                                cfop.ShowProgess += Cfop_UpdateProgress;
                                cfop.ShowInfo += _cfop_ShowInfo;

                                this._cfopArray[i] = cfop;

                                cfop.GetMattingLaplacian(Math.Pow(10, -7));
                                Bitmap b = null;

                                if (mode == 0)
                                    b = cfop.SolveSystemGaussSeidel();
                                else if (mode == 1)
                                    b = cfop.SolveSystemGMRES();

                                //save and draw out later serially
                                bmp4[i] = b;

                                cfop.ShowProgess -= Cfop_UpdateProgress;
                                cfop.ShowInfo -= _cfop_ShowInfo;
                                cfop.Dispose();
                            }

                        if (this._cfop.BlendParameters.BGW != null && this._cfop.BlendParameters.BGW.WorkerSupportsCancellation && this._cfop.BlendParameters.BGW.CancellationPending)
                        {
                            for (int i = bmp.Count - 1; i >= 0; i--)
                            {
                                if (bmp[i] != null)
                                    bmp[i].Dispose();
                                if (bmp2[i] != null)
                                    bmp2[i].Dispose();
                                if (bmp4[i] != null)
                                    bmp4[i].Dispose();
                            }
                            e.Result = null;
                            return;
                        }

                        for (int y = 0; y < yP; y++)
                        {
                            for (int x = 0; x < xP; x++)
                            {
                                int indx = y * xP + x;
                                int wdth = result.Width;
                                int hght = result.Height;

                                BitmapData bmR = result.LockBits(new Rectangle(0, 0, wdth, hght), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                                BitmapData b4D = bmp4[indx].LockBits(new Rectangle(0, 0, sizes[indx].Width, sizes[indx].Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                                int strideR = bmR.Stride;
                                int strideB = b4D.Stride;

                                unsafe
                                {
                                    byte* p = (byte*)bmR.Scan0;
                                    byte* pB = (byte*)b4D.Scan0;

                                    if (group)
                                    {
                                        for (int y2 = y * groupAmountY, y4 = 0; y2 < hght + yP * groupAmountY; y2 += yP * groupAmountY, y4 += groupAmountY)
                                        {
                                            for (int x2 = x * groupAmountX, x4 = 0; x2 < wdth + xP * groupAmountX; x2 += xP * groupAmountX, x4 += groupAmountX)
                                            {
                                                for (int y7 = y2, y41 = y4, cntY = 0; y7 < y2 + groupAmountY; y7++, y41++)
                                                {
                                                    for (int x7 = x2, x41 = x4, cntX = 0; x7 < x2 + groupAmountX; x7++, x41++)
                                                    {
                                                        if (x7 < wdth - ww * xP && y7 < hght - hh * yP && x41 < b4D.Width && y41 < b4D.Height)
                                                        {
                                                            p[y7 * strideR + x7 * 4] = pB[y41 * strideB + x41 * 4];
                                                            p[y7 * strideR + x7 * 4 + 1] = pB[y41 * strideB + x41 * 4 + 1];
                                                            p[y7 * strideR + x7 * 4 + 2] = pB[y41 * strideB + x41 * 4 + 2];
                                                            p[y7 * strideR + x7 * 4 + 3] = pB[y41 * strideB + x41 * 4 + 3];
                                                        }
                                                        else
                                                        {
                                                            if (x7 > wdth)
                                                            {
                                                                x7 = wdth - 1 - (ww * (xP - x)) + cntX - 1;
                                                                x41 = b4D.Width - 1 - ww + cntX;
                                                                cntX++;
                                                            }
                                                            if (y7 >= hght)
                                                            {
                                                                y7 = hght - 1 - (hh * (yP - y)) + cntY - 1;
                                                                y41 = b4D.Height - 1 - hh + cntY;
                                                                cntY++;
                                                            }

                                                            if (x7 < wdth && y7 < hght && x41 < b4D.Width && y41 < b4D.Height)
                                                            {
                                                                p[y7 * strideR + x7 * 4] = pB[y41 * strideB + x41 * 4];
                                                                p[y7 * strideR + x7 * 4 + 1] = pB[y41 * strideB + x41 * 4 + 1];
                                                                p[y7 * strideR + x7 * 4 + 2] = pB[y41 * strideB + x41 * 4 + 2];
                                                                p[y7 * strideR + x7 * 4 + 3] = pB[y41 * strideB + x41 * 4 + 3];
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (int y2 = y, y4 = 0; y2 < hght + yP; y2 += yP, y4++)
                                        {
                                            for (int x2 = x, x4 = 0; x2 < wdth + xP; x2 += xP, x4++)
                                            {
                                                if (x4 < b4D.Width && y4 < b4D.Height && x2 < wdth && y2 < hght)
                                                {
                                                    p[y2 * strideR + x2 * 4] = pB[y4 * strideB + x4 * 4];
                                                    p[y2 * strideR + x2 * 4 + 1] = pB[y4 * strideB + x4 * 4 + 1];
                                                    p[y2 * strideR + x2 * 4 + 2] = pB[y4 * strideB + x4 * 4 + 2];
                                                    p[y2 * strideR + x2 * 4 + 3] = pB[y4 * strideB + x4 * 4 + 3];
                                                }
                                                else if (x4 < b4D.Width && y4 < b4D.Height && (x2 >= wdth || y2 >= hght))
                                                {
                                                    if (x2 >= wdth)
                                                        x2 -= xP - (xP - x);
                                                    if (y2 >= hght)
                                                        y2 -= yP - (yP - y);

                                                    p[y2 * strideR + x2 * 4] = pB[y4 * strideB + x4 * 4];
                                                    p[y2 * strideR + x2 * 4 + 1] = pB[y4 * strideB + x4 * 4 + 1];
                                                    p[y2 * strideR + x2 * 4 + 2] = pB[y4 * strideB + x4 * 4 + 2];
                                                    p[y2 * strideR + x2 * 4 + 3] = pB[y4 * strideB + x4 * 4 + 3];
                                                }
                                            }
                                        }
                                    }
                                }

                                bmp4[indx].UnlockBits(b4D);
                                result.UnlockBits(bmR);

                                //Form fff = new Form();
                                //fff.BackgroundImage = result;
                                //fff.BackgroundImageLayout = ImageLayout.Zoom;
                                //fff.ShowDialog();
                            }
                        }

                        for (int i = bmp.Count - 1; i >= 0; i--)
                        {
                            bmp[i].Dispose();
                            bmp2[i].Dispose();
                            bmp4[i].Dispose();
                        }

                        if (interpolated)
                        {
                            using (Bitmap resOld = result)
                            {
                                result = new Bitmap(bWork.Width, bWork.Height);

                                using (Graphics gx = Graphics.FromImage(result))
                                {
                                    gx.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                    gx.DrawImage(resOld, 0, 0, result.Width, result.Height);
                                }
                            }

                            cfopBmp.Dispose();
                            cfopTrimap.Dispose();
                        }

                        e.Result = result;
                    }
                    else
                    {
                        int wh = bWork.Width * bWork.Height;
                        int n = 1;

                        while (wh > maxSize)
                        {
                            n += 1;
                            wh = bWork.Width / n * bWork.Height / n;
                        }
                        int n2 = n * n;

                        int hhh = bWork.Height / n;
                        int hhh2 = bWork.Height - hhh * (n - 1);

                        int www = bWork.Width / n;
                        int www2 = bWork.Width - www * (n - 1);

                        overlap = Math.Max(overlap, 1);

                        if (n2 == 1)
                            overlap = 0;

                        List<Bitmap> bmpF = new List<Bitmap>();
                        List<Bitmap> bmpF2 = new List<Bitmap>();

                        GetTiles(bWork, trWork, bmpF, bmpF2, www, www2, hhh, hhh2, overlap, n);

                        Bitmap[] bmpF4 = new Bitmap[bmpF.Count];

                        Bitmap bmpResult = new Bitmap(bWork.Width, bWork.Height);

                        for (int j = 0; j < bmpF.Count; j++)
                        {
                            bool wgth = bWork.Width > bWork.Height;
                            int xP = 2;
                            int yP = 2;

                            _cfop_ShowInfo(this, "picOuter " + (j + 1).ToString());

                            if (scales == 8)
                            {
                                xP = wgth ? 4 : 2;
                                yP = wgth ? 2 : 4;
                            }
                            if (scales == 16)
                            {
                                xP = 4;
                                yP = 4;
                            }
                            if (scales == 32)
                            {
                                xP = wgth ? 8 : 4;
                                yP = wgth ? 4 : 8;
                            }
                            if (scales == 64)
                            {
                                xP = 8;
                                yP = 8;
                            }
                            if (scales == 128)
                            {
                                xP = wgth ? 16 : 8;
                                yP = wgth ? 8 : 16;
                            }
                            if (scales == 256)
                            {
                                xP = 16;
                                yP = 16;
                            }

                            int w = bmpF[j].Width;
                            int h = bmpF[j].Height;

                            Bitmap cfopBmp = bmpF[j];
                            Bitmap cfopTrimap = bmpF2[j];

                            if (interpolated)
                            {
                                w = (int)(w * 1.41);
                                h = (int)(h * 1.41);

                                cfopBmp = new Bitmap(w, h);
                                cfopTrimap = new Bitmap(w, h);

                                using (Graphics gx = Graphics.FromImage(cfopBmp))
                                {
                                    gx.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                    gx.DrawImage(bmpF[j], 0, 0, w, h);
                                }

                                using (Graphics gx = Graphics.FromImage(cfopTrimap))
                                {
                                    gx.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                    gx.DrawImage(bmpF2[j], 0, 0, w, h);
                                }
                            }

                            Bitmap result = new Bitmap(w, h);

                            List<Bitmap> bmp = new List<Bitmap>();
                            List<Bitmap> bmp2 = new List<Bitmap>();
                            List<Size> sizes = new List<Size>();

                            int ww = 0;
                            int hh = 0;

                            for (int y = 0; y < yP; y++)
                            {
                                for (int x = 0; x < xP; x++)
                                {
                                    Size sz = new Size(result.Width / xP, result.Height / yP);
                                    int xAdd = result.Width - sz.Width * xP;
                                    int yAdd = result.Height - sz.Height * yP;

                                    if (y < yP - 1)
                                    {
                                        if (x < xP - 1)
                                            sizes.Add(new Size(sz.Width, sz.Height));
                                        else
                                            sizes.Add(new Size(sz.Width + xAdd, sz.Height));
                                    }
                                    else
                                    {
                                        if (x < xP - 1)
                                            sizes.Add(new Size(sz.Width, sz.Height + yAdd));
                                        else
                                            sizes.Add(new Size(sz.Width + xAdd, sz.Height + yAdd));
                                    }

                                    int xx = sizes[sizes.Count - 1].Width / groupAmountX;
                                    int yy = sizes[sizes.Count - 1].Height / groupAmountY;

                                    ww = sizes[sizes.Count - 1].Width - (xx * groupAmountX);
                                    hh = sizes[sizes.Count - 1].Height - (yy * groupAmountY);

                                    Bitmap b1 = new Bitmap(sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height);
                                    Bitmap b2 = new Bitmap(sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height);

                                    int wdth = result.Width;
                                    int hght = result.Height;

                                    BitmapData bmD = cfopBmp.LockBits(new Rectangle(0, 0, wdth, hght), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                                    BitmapData bmT = cfopTrimap.LockBits(new Rectangle(0, 0, wdth, hght), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                                    BitmapData b1D = b1.LockBits(new Rectangle(0, 0, sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                                    BitmapData b2D = b2.LockBits(new Rectangle(0, 0, sizes[sizes.Count - 1].Width, sizes[sizes.Count - 1].Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                                    int strideD = bmD.Stride;
                                    int strideB = b1D.Stride;

                                    unsafe
                                    {
                                        byte* p = (byte*)bmD.Scan0;
                                        byte* pT = (byte*)bmT.Scan0;
                                        byte* p1 = (byte*)b1D.Scan0;
                                        byte* p2 = (byte*)b2D.Scan0;

                                        if (group)
                                        {
                                            for (int y2 = y * groupAmountY, y4 = 0; y2 < hght + yP; y2 += yP * groupAmountY, y4 += groupAmountY)
                                            {
                                                for (int x2 = x * groupAmountX, x4 = 0; x2 < wdth + xP; x2 += xP * groupAmountX, x4 += groupAmountX)
                                                {
                                                    for (int y7 = y2, y41 = y4, cntY = 0; y7 <= y2 + groupAmountY; y7++, y41++)
                                                    {
                                                        for (int x7 = x2, x41 = x4, cntX = 0; x7 <= x2 + groupAmountX; x7++, x41++)
                                                        {
                                                            if (x7 < wdth - ww * xP && y7 < hght - hh * yP && x41 < b1.Width && y41 < b1.Height)
                                                            {
                                                                p1[y41 * strideB + x41 * 4] = p[y7 * strideD + x7 * 4];
                                                                p1[y41 * strideB + x41 * 4 + 1] = p[y7 * strideD + x7 * 4 + 1];
                                                                p1[y41 * strideB + x41 * 4 + 2] = p[y7 * strideD + x7 * 4 + 2];
                                                                p1[y41 * strideB + x41 * 4 + 3] = p[y7 * strideD + x7 * 4 + 3];

                                                                p2[y41 * strideB + x41 * 4] = pT[y7 * strideD + x7 * 4];
                                                                p2[y41 * strideB + x41 * 4 + 1] = pT[y7 * strideD + x7 * 4 + 1];
                                                                p2[y41 * strideB + x41 * 4 + 2] = pT[y7 * strideD + x7 * 4 + 2];
                                                                p2[y41 * strideB + x41 * 4 + 3] = pT[y7 * strideD + x7 * 4 + 3];
                                                            }
                                                            else
                                                            {
                                                                if (x7 > wdth)
                                                                {
                                                                    x7 = wdth - 1 - (ww * (xP - x)) + cntX - 1;
                                                                    x41 = b1.Width - 1 - ww + cntX;
                                                                    cntX++;
                                                                }
                                                                if (y7 >= hght)
                                                                {
                                                                    y7 = hght - 1 - (hh * (yP - y)) + cntY - 1;
                                                                    y41 = b1.Height - 1 - hh + cntY;
                                                                    cntY++;
                                                                }

                                                                if (x7 < wdth && y7 < hght && x41 < b1.Width && y41 < b1.Height)
                                                                {
                                                                    p1[y41 * strideB + x41 * 4] = p[y7 * strideD + x7 * 4];
                                                                    p1[y41 * strideB + x41 * 4 + 1] = p[y7 * strideD + x7 * 4 + 1];
                                                                    p1[y41 * strideB + x41 * 4 + 2] = p[y7 * strideD + x7 * 4 + 2];
                                                                    p1[y41 * strideB + x41 * 4 + 3] = p[y7 * strideD + x7 * 4 + 3];

                                                                    p2[y41 * strideB + x41 * 4] = pT[y7 * strideD + x7 * 4];
                                                                    p2[y41 * strideB + x41 * 4 + 1] = pT[y7 * strideD + x7 * 4 + 1];
                                                                    p2[y41 * strideB + x41 * 4 + 2] = pT[y7 * strideD + x7 * 4 + 2];
                                                                    p2[y41 * strideB + x41 * 4 + 3] = pT[y7 * strideD + x7 * 4 + 3];
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int y2 = y, y4 = 0; y2 < hght + yP; y2 += yP, y4++)
                                            {
                                                for (int x2 = x, x4 = 0; x2 < wdth + xP; x2 += xP, x4++)
                                                {
                                                    if (x4 < b1.Width && y4 < b1.Height && x2 < wdth && y2 < hght)
                                                    {
                                                        p1[y4 * strideB + x4 * 4] = p[y2 * strideD + x2 * 4];
                                                        p1[y4 * strideB + x4 * 4 + 1] = p[y2 * strideD + x2 * 4 + 1];
                                                        p1[y4 * strideB + x4 * 4 + 2] = p[y2 * strideD + x2 * 4 + 2];
                                                        p1[y4 * strideB + x4 * 4 + 3] = p[y2 * strideD + x2 * 4 + 3];

                                                        p2[y4 * strideB + x4 * 4] = pT[y2 * strideD + x2 * 4];
                                                        p2[y4 * strideB + x4 * 4 + 1] = pT[y2 * strideD + x2 * 4 + 1];
                                                        p2[y4 * strideB + x4 * 4 + 2] = pT[y2 * strideD + x2 * 4 + 2];
                                                        p2[y4 * strideB + x4 * 4 + 3] = pT[y2 * strideD + x2 * 4 + 3];
                                                    }
                                                    else if (x4 < b1.Width && y4 < b1.Height && (x2 >= wdth || y2 >= hght))
                                                    {
                                                        if (x2 >= wdth)
                                                            x2 -= xP - (xP - x);
                                                        if (y2 >= hght)
                                                            y2 -= yP - (yP - y);

                                                        p1[y4 * strideB + x4 * 4] = p[y2 * strideD + x2 * 4];
                                                        p1[y4 * strideB + x4 * 4 + 1] = p[y2 * strideD + x2 * 4 + 1];
                                                        p1[y4 * strideB + x4 * 4 + 2] = p[y2 * strideD + x2 * 4 + 2];
                                                        p1[y4 * strideB + x4 * 4 + 3] = p[y2 * strideD + x2 * 4 + 3];

                                                        p2[y4 * strideB + x4 * 4] = pT[y2 * strideD + x2 * 4];
                                                        p2[y4 * strideB + x4 * 4 + 1] = pT[y2 * strideD + x2 * 4 + 1];
                                                        p2[y4 * strideB + x4 * 4 + 2] = pT[y2 * strideD + x2 * 4 + 2];
                                                        p2[y4 * strideB + x4 * 4 + 3] = pT[y2 * strideD + x2 * 4 + 3];
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    b2.UnlockBits(b2D);
                                    b1.UnlockBits(b1D);
                                    cfopTrimap.UnlockBits(bmT);
                                    cfopBmp.UnlockBits(bmD);

                                    bmp.Add(b1);
                                    bmp2.Add(b2);

                                    //try
                                    //{
                                    //    Form fff = new Form();
                                    //    fff.BackgroundImage = b1;
                                    //    fff.BackgroundImageLayout = ImageLayout.Zoom;
                                    //    fff.ShowDialog();
                                    //    fff.BackgroundImage = b2;
                                    //    fff.BackgroundImageLayout = ImageLayout.Zoom;
                                    //    fff.ShowDialog();
                                    //}
                                    //catch (Exception exc)
                                    //{
                                    //    Console.WriteLine(exc.ToString());
                                    //}
                                }
                            }

                            if (verifyTrimaps)
                            {
                                if (!CheckTrimaps(bmp2))
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        if (this._frmInfo == null || this._frmInfo.IsDisposed)
                                            this._frmInfo = new frmInfo();
                                        this._frmInfo.Show(trimapProblemMessage);
                                    }));

                                    int x = j % n * www;
                                    int y = j / n * hhh;

                                    //int x = i % n;
                                    //x * www - (x == 0 ? 0 : overlap)

                                    if (x > 0)
                                        x -= overlap;

                                    if (y > 0)
                                        y -= overlap;

                                    this._trimapProblemInfos.Add(new TrimapProblemInfo(id, j, x, y, bmpF[j].Width, bmpF[j].Height, overlap));
                                }
                            }

                            Bitmap[] bmp4 = new Bitmap[bmp.Count];

                            this._cfopArray = new ClosedFormMatteOp[bmp.Count];

                            if (AvailMem.AvailMem.checkAvailRam(w * h * bmp.Count * 10L) && !forceSerial)
                                Parallel.For(0, bmp.Count, i =>
                                //for(int i = 0; i < bmp.Count; i++)
                                {
                                    ClosedFormMatteOp cfop = new ClosedFormMatteOp(bmp[i], bmp2[i]);
                                    BlendParameters bParam = new BlendParameters();
                                    bParam.MaxIterations = _cfop.BlendParameters.MaxIterations;
                                    bParam.InnerIterations = _cfop.BlendParameters.InnerIterations;
                                    bParam.DesiredMaxLinearError = _cfop.BlendParameters.DesiredMaxLinearError;
                                    bParam.Sleep = _cfop.BlendParameters.Sleep;
                                    bParam.SleepAmount = _cfop.BlendParameters.SleepAmount;
                                    bParam.BGW = this.backgroundWorker1;
                                    cfop.BlendParameters = bParam;

                                    cfop.ShowProgess += Cfop_UpdateProgress;
                                    cfop.ShowInfo += _cfop_ShowInfo;

                                    this._cfopArray[i] = cfop;

                                    cfop.GetMattingLaplacian(Math.Pow(10, -7));
                                    Bitmap b = null;

                                    if (mode == 0)
                                        b = cfop.SolveSystemGaussSeidel();
                                    else if (mode == 1)
                                        b = cfop.SolveSystemGMRES();

                                    //save and draw out later serially
                                    bmp4[i] = b;

                                    cfop.ShowProgess -= Cfop_UpdateProgress;
                                    cfop.ShowInfo -= _cfop_ShowInfo;
                                    cfop.Dispose();
                                });
                            else
                                for (int i = 0; i < bmp.Count; i++)
                                {
                                    _cfop_ShowInfo(this, "pic " + (i + 1).ToString());
                                    ClosedFormMatteOp cfop = new ClosedFormMatteOp(bmp[i], bmp2[i]);
                                    BlendParameters bParam = new BlendParameters();
                                    bParam.MaxIterations = _cfop.BlendParameters.MaxIterations;
                                    bParam.InnerIterations = _cfop.BlendParameters.InnerIterations;
                                    bParam.DesiredMaxLinearError = _cfop.BlendParameters.DesiredMaxLinearError;
                                    bParam.Sleep = _cfop.BlendParameters.Sleep;
                                    bParam.SleepAmount = _cfop.BlendParameters.SleepAmount;
                                    bParam.BGW = this.backgroundWorker1;
                                    cfop.BlendParameters = bParam;

                                    cfop.ShowProgess += Cfop_UpdateProgress;
                                    cfop.ShowInfo += _cfop_ShowInfo;

                                    this._cfopArray[i] = cfop;

                                    cfop.GetMattingLaplacian(Math.Pow(10, -7));
                                    Bitmap b = null;

                                    if (mode == 0)
                                        b = cfop.SolveSystemGaussSeidel();
                                    else if (mode == 1)
                                        b = cfop.SolveSystemGMRES();

                                    //save and draw out later serially
                                    bmp4[i] = b;

                                    cfop.ShowProgess -= Cfop_UpdateProgress;
                                    cfop.ShowInfo -= _cfop_ShowInfo;
                                    cfop.Dispose();
                                }

                            if (this._cfop.BlendParameters.BGW != null && this._cfop.BlendParameters.BGW.WorkerSupportsCancellation && this._cfop.BlendParameters.BGW.CancellationPending)
                            {
                                for (int i = bmp.Count - 1; i >= 0; i--)
                                {
                                    if (bmp[i] != null)
                                        bmp[i].Dispose();
                                    if (bmp2[i] != null)
                                        bmp2[i].Dispose();
                                    if (bmp4[i] != null)
                                        bmp4[i].Dispose();
                                }
                                e.Result = null;
                                return;
                            }

                            for (int y = 0; y < yP; y++)
                            {
                                for (int x = 0; x < xP; x++)
                                {
                                    int indx = y * xP + x;
                                    int wdth = result.Width;
                                    int hght = result.Height;

                                    BitmapData bmR = result.LockBits(new Rectangle(0, 0, wdth, hght), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                                    BitmapData b4D = bmp4[indx].LockBits(new Rectangle(0, 0, sizes[indx].Width, sizes[indx].Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                                    int strideR = bmR.Stride;
                                    int strideB = b4D.Stride;

                                    unsafe
                                    {
                                        byte* p = (byte*)bmR.Scan0;
                                        byte* pB = (byte*)b4D.Scan0;

                                        if (group)
                                        {
                                            for (int y2 = y * groupAmountY, y4 = 0; y2 < hght + yP * groupAmountY; y2 += yP * groupAmountY, y4 += groupAmountY)
                                            {
                                                for (int x2 = x * groupAmountX, x4 = 0; x2 < wdth + xP * groupAmountX; x2 += xP * groupAmountX, x4 += groupAmountX)
                                                {
                                                    for (int y7 = y2, y41 = y4, cntY = 0; y7 < y2 + groupAmountY; y7++, y41++)
                                                    {
                                                        for (int x7 = x2, x41 = x4, cntX = 0; x7 < x2 + groupAmountX; x7++, x41++)
                                                        {
                                                            if (x7 < wdth - ww * xP && y7 < hght - hh * yP && x41 < b4D.Width && y41 < b4D.Height)
                                                            {
                                                                p[y7 * strideR + x7 * 4] = pB[y41 * strideB + x41 * 4];
                                                                p[y7 * strideR + x7 * 4 + 1] = pB[y41 * strideB + x41 * 4 + 1];
                                                                p[y7 * strideR + x7 * 4 + 2] = pB[y41 * strideB + x41 * 4 + 2];
                                                                p[y7 * strideR + x7 * 4 + 3] = pB[y41 * strideB + x41 * 4 + 3];
                                                            }
                                                            else
                                                            {
                                                                if (x7 > wdth)
                                                                {
                                                                    x7 = wdth - 1 - (ww * (xP - x)) + cntX - 1;
                                                                    x41 = b4D.Width - 1 - ww + cntX;
                                                                    cntX++;
                                                                }
                                                                if (y7 >= hght)
                                                                {
                                                                    y7 = hght - 1 - (hh * (yP - y)) + cntY - 1;
                                                                    y41 = b4D.Height - 1 - hh + cntY;
                                                                    cntY++;
                                                                }

                                                                if (x7 < wdth && y7 < hght && x41 < b4D.Width && y41 < b4D.Height)
                                                                {
                                                                    p[y7 * strideR + x7 * 4] = pB[y41 * strideB + x41 * 4];
                                                                    p[y7 * strideR + x7 * 4 + 1] = pB[y41 * strideB + x41 * 4 + 1];
                                                                    p[y7 * strideR + x7 * 4 + 2] = pB[y41 * strideB + x41 * 4 + 2];
                                                                    p[y7 * strideR + x7 * 4 + 3] = pB[y41 * strideB + x41 * 4 + 3];
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int y2 = y, y4 = 0; y2 < hght + yP; y2 += yP, y4++)
                                            {
                                                for (int x2 = x, x4 = 0; x2 < wdth + xP; x2 += xP, x4++)
                                                {
                                                    if (x4 < b4D.Width && y4 < b4D.Height && x2 < wdth && y2 < hght)
                                                    {
                                                        p[y2 * strideR + x2 * 4] = pB[y4 * strideB + x4 * 4];
                                                        p[y2 * strideR + x2 * 4 + 1] = pB[y4 * strideB + x4 * 4 + 1];
                                                        p[y2 * strideR + x2 * 4 + 2] = pB[y4 * strideB + x4 * 4 + 2];
                                                        p[y2 * strideR + x2 * 4 + 3] = pB[y4 * strideB + x4 * 4 + 3];
                                                    }
                                                    else if (x4 < b4D.Width && y4 < b4D.Height && (x2 >= wdth || y2 >= hght))
                                                    {
                                                        if (x2 >= wdth)
                                                            x2 -= xP - (xP - x);
                                                        if (y2 >= hght)
                                                            y2 -= yP - (yP - y);

                                                        p[y2 * strideR + x2 * 4] = pB[y4 * strideB + x4 * 4];
                                                        p[y2 * strideR + x2 * 4 + 1] = pB[y4 * strideB + x4 * 4 + 1];
                                                        p[y2 * strideR + x2 * 4 + 2] = pB[y4 * strideB + x4 * 4 + 2];
                                                        p[y2 * strideR + x2 * 4 + 3] = pB[y4 * strideB + x4 * 4 + 3];
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    bmp4[indx].UnlockBits(b4D);
                                    result.UnlockBits(bmR);
                                }
                            }

                            for (int i = bmp.Count - 1; i >= 0; i--)
                            {
                                bmp[i].Dispose();
                                bmp2[i].Dispose();
                                bmp4[i].Dispose();
                            }

                            if (interpolated)
                            {
                                using (Bitmap resOld = result)
                                {
                                    result = new Bitmap(bmpF[j].Width, bmpF[j].Height);

                                    using (Graphics gx = Graphics.FromImage(result))
                                    {
                                        gx.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                        gx.DrawImage(resOld, 0, 0, result.Width, result.Height);
                                    }
                                }

                                cfopBmp.Dispose();
                                cfopTrimap.Dispose();
                            }

                            bmpF4[j] = result;
                        }

                        //draw to single pic
                        for (int i = 0; i < bmpF4.Length; i++)
                        {
                            int x = i % n;
                            int y = i / n;

                            using (Graphics gx = Graphics.FromImage(bmpResult))
                                gx.DrawImage(bmpF4[i], x * www - (x == 0 ? 0 : overlap), y * hhh - (y == 0 ? 0 : overlap));
                        }

                        for (int i = bmpF.Count - 1; i >= 0; i--)
                        {
                            if (bmpF[i] != null)
                                bmpF[i].Dispose();
                            if (bmpF2[i] != null)
                                bmpF2[i].Dispose();
                            if (bmpF4[i] != null)
                                bmpF4[i].Dispose();
                        }

                        e.Result = bmpResult;
                    }
                }
            }
            else
                e.Result = null;
        }

        private void backgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!this.toolStripProgressBar1.IsDisposed)
                if (InvokeRequired)
                {
                    try
                    {
                        this.toolStripProgressBar1.Value = e.ProgressPercentage;
                    }
                    catch
                    {

                    }
                }
                else
                {
                    try
                    {
                        this.toolStripProgressBar1.Value = e.ProgressPercentage;
                    }
                    catch
                    {

                    }
                }
        }

        private void backgroundWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!this.IsDisposed)
            {
                Bitmap bmp = null;

                if (e.Result != null)
                {
                    bmp = (Bitmap)e.Result;

                    Bitmap b2 = bmp;
                    bmp = ResampleBack(bmp);
                    b2.Dispose();
                    b2 = null;

                    frmEdgePic frm4 = new frmEdgePic(bmp);
                    frm4.Text = "Alpha Matte";
                    frm4.ShowDialog();

                    b2 = bmp;
                    bmp = GetAlphaBoundsPic(this._bmpOrig, bmp);
                    b2.Dispose();
                    b2 = null;
                }

                if (bmp != null)
                {
                    this.SetBitmap(this.helplineRulerCtrl1.Bmp, bmp, this.helplineRulerCtrl1, "Bmp");

                    Bitmap bC = new Bitmap(bmp);
                    this.SetBitmap(ref _bmpRef, ref bC);

                    this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                    this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                        (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                        (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                    _undoOPCache.Add(bmp);
                }

                if (this._cfop != null)
                {
                    this._cfop.ShowProgess -= Cfop_UpdateProgress;
                    this._cfop.ShowInfo -= _cfop_ShowInfo;
                    this._cfop.Dispose();
                }

                this.btnAlphaV.Text = "Go";

                this.SetControls(true);
                this.Cursor = Cursors.Default;

                this.btnOK.Enabled = this.btnCancel.Enabled = true;

                this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());

                this._pic_changed = true;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();

                if (this.Timer3.Enabled)
                    this.Timer3.Stop();

                this.Timer3.Start();

                this.backgroundWorker4.Dispose();
                this.backgroundWorker4 = new BackgroundWorker();
                this.backgroundWorker4.WorkerReportsProgress = true;
                this.backgroundWorker4.WorkerSupportsCancellation = true;
                this.backgroundWorker4.DoWork += backgroundWorker4_DoWork;
                this.backgroundWorker4.ProgressChanged += backgroundWorker4_ProgressChanged;
                this.backgroundWorker4.RunWorkerCompleted += backgroundWorker4_RunWorkerCompleted;
            }
        }

        private void GetTiles(Bitmap bWork, Bitmap trWork, List<Bitmap> bmp, List<Bitmap> bmp2,
            int w, int w2, int h, int h2, int overlap, int n)
        {
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    if (x < n - 1 && y < n - 1)
                    {
                        Bitmap b1 = new Bitmap(w + overlap * (x == 0 ? 1 : 2), h + overlap * (y == 0 ? 1 : 2));

                        using (Graphics gx = Graphics.FromImage(b1))
                            gx.DrawImage(bWork, -x * w + (x == 0 ? 0 : overlap), -y * h + (y == 0 ? 0 : overlap));

                        bmp.Add(b1);

                        Bitmap b3 = new Bitmap(w + overlap * (x == 0 ? 1 : 2), h + overlap * (y == 0 ? 1 : 2));

                        using (Graphics gx = Graphics.FromImage(b3))
                            gx.DrawImage(trWork, -x * w + (x == 0 ? 0 : overlap), -y * h + (y == 0 ? 0 : overlap));

                        bmp2.Add(b3);
                    }
                    else if (x == n - 1 && y < n - 1)
                    {
                        Bitmap b1 = new Bitmap(w2 + overlap, h + overlap * (y == 0 ? 1 : 2));

                        using (Graphics gx = Graphics.FromImage(b1))
                            gx.DrawImage(bWork, -x * w + (x == 0 ? 0 : overlap), -y * h + (y == 0 ? 0 : overlap));

                        bmp.Add(b1);

                        Bitmap b3 = new Bitmap(w2 + overlap, h + overlap * (y == 0 ? 1 : 2));

                        using (Graphics gx = Graphics.FromImage(b3))
                            gx.DrawImage(trWork, -x * w + (x == 0 ? 0 : overlap), -y * h + (y == 0 ? 0 : overlap));

                        bmp2.Add(b3);
                    }
                    else if (x < n - 1 && y == n - 1)
                    {
                        Bitmap b1 = new Bitmap(w + overlap * (x == 0 ? 1 : 2), h2 + overlap);

                        using (Graphics gx = Graphics.FromImage(b1))
                            gx.DrawImage(bWork, -x * w + (x == 0 ? 0 : overlap), -y * h + overlap);

                        bmp.Add(b1);

                        Bitmap b3 = new Bitmap(w + overlap * (x == 0 ? 1 : 2), h2 + overlap);

                        using (Graphics gx = Graphics.FromImage(b3))
                            gx.DrawImage(trWork, -x * w + (x == 0 ? 0 : overlap), -y * h + overlap);

                        bmp2.Add(b3);
                    }
                    else
                    {
                        Bitmap b1 = new Bitmap(w2 + overlap, h2 + overlap);

                        using (Graphics gx = Graphics.FromImage(b1))
                            gx.DrawImage(bWork, -x * w + overlap, -y * h + overlap);

                        bmp.Add(b1);

                        Bitmap b3 = new Bitmap(w2 + overlap, h2 + overlap);

                        using (Graphics gx = Graphics.FromImage(b3))
                            gx.DrawImage(trWork, -x * w + overlap, -y * h + overlap);

                        bmp2.Add(b3);
                    }
                }

                _cfop_ShowInfo(this, "outer pic-amount " + bmp.Count().ToString());
            }
        }

        private Bitmap ResampleBmp(Bitmap bmp, int n)
        {
            Bitmap bOut = new Bitmap(bmp.Width / n, bmp.Height / n);

            using (Graphics gx = Graphics.FromImage(bOut))
            {
                gx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                gx.DrawImage(bmp, 0, 0, bOut.Width, bOut.Height);
            }

            return bOut;
        }

        private Bitmap ResampleBack(Bitmap bmp)
        {
            if (this.helplineRulerCtrl1 != null && !this.IsDisposed && this.helplineRulerCtrl1.Bmp != null && bmp != null)
            {
                Bitmap bOut = new Bitmap(this.helplineRulerCtrl1.Bmp.Width, this.helplineRulerCtrl1.Bmp.Height);

                using (Graphics gx = Graphics.FromImage(bOut))
                {
                    gx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    gx.DrawImage(bmp, 0, 0, bOut.Width, bOut.Height);
                }

                return bOut;
            }

            return null;
        }

        private unsafe Bitmap GetAlphaBoundsPic(Bitmap bmpIn, Bitmap bmpAlpha)
        {
            Bitmap bmp = null;

            if (AvailMem.AvailMem.checkAvailRam(bmpAlpha.Width * bmpAlpha.Height * 16L))
            {
                int w = bmpAlpha.Width;
                int h = bmpAlpha.Height;

                bmp = new Bitmap(w, h);

                BitmapData bmD = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmIn = bmpIn.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData bmA = bmpAlpha.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmD.Stride;

                Parallel.For(0, h, y =>
                {
                    byte* p = (byte*)bmD.Scan0;
                    p += y * stride;

                    byte* pIn = (byte*)bmIn.Scan0;
                    pIn += y * stride;

                    byte* pA = (byte*)bmA.Scan0;
                    pA += y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        p[0] = pIn[0];
                        p[1] = pIn[1];
                        p[2] = pIn[2];

                        p[3] = pA[0];

                        p += 4;
                        pIn += 4;
                        pA += 4;
                    }
                });

                bmp.UnlockBits(bmD);
                bmpIn.UnlockBits(bmIn);
                bmpAlpha.UnlockBits(bmA);
            }

            return bmp;
        }

        private unsafe Bitmap GetAlphaBoundsPic(Bitmap bmpIn, Bitmap bmpAlpha, double gamma)
        {
            Bitmap bmp = null;

            if (AvailMem.AvailMem.checkAvailRam(bmpAlpha.Width * bmpAlpha.Height * 16L))
            {
                int w = bmpAlpha.Width;
                int h = bmpAlpha.Height;

                bmp = new Bitmap(w, h);

                BitmapData bmD = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmIn = bmpIn.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData bmA = bmpAlpha.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmD.Stride;

                Parallel.For(0, h, y =>
                {
                    byte* p = (byte*)bmD.Scan0;
                    p += y * stride;

                    byte* pIn = (byte*)bmIn.Scan0;
                    pIn += y * stride;

                    byte* pA = (byte*)bmA.Scan0;
                    pA += y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        p[0] = pIn[0];
                        p[1] = pIn[1];
                        p[2] = pIn[2];

                        p[3] = (byte)Math.Max(Math.Min(255.0 * Math.Pow((double)pA[0] / 255.0, gamma), 255), 0);

                        p += 4;
                        pIn += 4;
                        pA += 4;
                    }
                });

                bmp.UnlockBits(bmD);
                bmpIn.UnlockBits(bmIn);
                bmpAlpha.UnlockBits(bmA);
            }

            return bmp;
        }

        private unsafe Bitmap GetAlphaBoundsPic(Bitmap bmpAlpha, double gamma)
        {
            Bitmap bmp = null;

            if (AvailMem.AvailMem.checkAvailRam(bmpAlpha.Width * bmpAlpha.Height * 16L))
            {
                int w = bmpAlpha.Width;
                int h = bmpAlpha.Height;

                bmp = new Bitmap(w, h);

                BitmapData bmD = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                BitmapData bmA = bmpAlpha.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int stride = bmD.Stride;

                Parallel.For(0, h, y =>
                {
                    byte* p = (byte*)bmD.Scan0;
                    p += y * stride;

                    byte* pA = (byte*)bmA.Scan0;
                    pA += y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        p[0] = pA[0];
                        p[1] = pA[1];
                        p[2] = pA[2];

                        p[3] = (byte)Math.Max(Math.Min(255.0 * Math.Pow((double)pA[3] / 255.0, gamma), 255), 0);

                        p += 4;
                        pA += 4;
                    }
                });

                bmp.UnlockBits(bmD);
                bmpAlpha.UnlockBits(bmA);
            }

            return bmp;
        }

        private void numSleep_ValueChanged(object sender, EventArgs e)
        {
            if (this._cfop != null)
            {
                if ((int)this.numSleep.Value == 0)
                    this._cfop.BlendParameters.Sleep = false;
                else
                    this._cfop.BlendParameters.Sleep = true;

                if (this._cfop != null && this._cfop.BlendParameters != null)
                    this._cfop.BlendParameters.SleepAmount = (int)this.numSleep.Value;

                if (this._cfopArray != null)
                {
                    for (int i = 0; i < _cfopArray.Length; i++)
                        if (_cfopArray[i] != null)
                            try
                            {
                                if ((int)this.numSleep.Value == 0)
                                    _cfopArray[i].BlendParameters.Sleep = false;
                                else
                                    _cfopArray[i].BlendParameters.Sleep = true;

                                _cfopArray[i].BlendParameters.SleepAmount = (int)this.numSleep.Value;
                            }
                            catch
                            {

                            }
                }
            }
        }

        private bool CheckTrimaps(List<Bitmap> bmp2, int www, int hhh, int n, int id, int overlap)
        {
            bool result = true;
            if (bmp2 != null && bmp2.Count > 0)
            {
                for (int i = 0; i < bmp2.Count; i++)
                {
                    if (!CheckTrimap(bmp2[i]))
                    {
                        result = false;
                        //}
                        int x = i % n * www;
                        int y = i / n * hhh;

                        //int x = i % n;
                        //x * www - (x == 0 ? 0 : overlap)

                        if (x > 0)
                            x -= overlap;

                        if (y > 0)
                            y -= overlap;

                        this._trimapProblemInfos.Add(new TrimapProblemInfo(id, i, x, y, bmp2[i].Width, bmp2[i].Height, overlap));
                    }
                }
            }

            return result;
        }

        //no overlap, we dont have an outerArray and all inner pics resemble the whole pic
        private bool CheckTrimaps(List<Bitmap> bmp2, int www, int hhh, int n, int id, int xAdd, int yAdd)
        {
            bool result = true;
            if (bmp2 != null && bmp2.Count > 0)
            {
                for (int i = 0; i < bmp2.Count; i++)
                {
                    if (!CheckTrimap(bmp2[i]))
                    {
                        result = false;
                        //}
                        int x = i % n * www;
                        int y = i / n * hhh;

                        this._trimapProblemInfos.Add(new TrimapProblemInfo(id, i, x, y, bmp2[i].Width, bmp2[i].Height, 0));
                    }
                }
            }

            return result;
        }

        private bool CheckTrimaps(List<Bitmap> bmp2)
        {
            if (bmp2 != null && bmp2.Count > 0)
            {
                foreach (Bitmap b in bmp2)
                {
                    if (!CheckTrimap(b))
                        return false;
                }
            }

            return true;
        }

        private unsafe bool CheckTrimap(Bitmap b)
        {
            int w = b.Width;
            int h = b.Height;
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = bmData.Stride;

            //if (this._rnd == null)
            //    this._rnd = new Random();

            bool unknownFound = false;
            int bgCount = 0;
            int fgCount = 0;

            byte* p = (byte*)bmData.Scan0;

            //first check, if unknown pixels are present
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (p[0] > 25 && p[0] < 230)
                        unknownFound = true;

                    if (p[0] <= 25)
                        bgCount++;

                    if (p[0] >= 230)
                        fgCount++;

                    p += 4;
                }

            //p = (byte*)bmData.Scan0;

            //if (unknownFound && (bgCount == 0 || fgCount == 0))
            //{
            //    //just a test, if this idea works at this stage of dev
            //    int cnt = 0;
            //    int iterations = 0;

            //    //todo: check pixels in orig img for determining fg/bg, or get a region for writing bg/fg, write pixels in clusters

            //    while(cnt < 128 && iterations < 10000)
            //    {
            //        int x = _rnd.Next(w);
            //        int y = _rnd.Next(h);

            //        if (p[x * 4 + y * stride] > 25 && p[x * 4 + y * stride] < 230) // only change unknown pixels
            //        {
            //            if ((cnt & 0x01) == 1)
            //                p[x * 4 + y * stride] = p[x * 4 + y * stride + 1] = p[x * 4 + y * stride + 2] = 0;
            //            else
            //                p[x * 4 + y * stride] = p[x * 4 + y * stride + 1] = p[x * 4 + y * stride + 2] = 255;

            //            cnt++;
            //        }

            //        iterations++;
            //    }
            //}

            b.UnlockBits(bmData);

            return !(unknownFound && (bgCount == 0 || fgCount == 0));
        }

        private void btnSetGamma_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker5.IsBusy)
            {
                this.backgroundWorker5.CancelAsync();
                return;
            }

            if (this.helplineRulerCtrl1.Bmp != null && this._bmpRef != null)
            {
                this.Cursor = Cursors.WaitCursor;
                this.SetControls(false);

                this.btnSetGamma.Enabled = true;
                this.btnSetGamma.Text = "Cancel";

                double gamma = (double)this.numGamma.Value;

                this.backgroundWorker5.RunWorkerAsync(gamma);
            }
        }

        private void backgroundWorker5_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap bmp = new Bitmap(this._bmpRef);
            double gamma = (double)e.Argument;

            e.Result = GetAlphaBoundsPic(bmp, gamma);
        }

        private void backgroundWorker5_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!this.IsDisposed)
            {
                Bitmap bmp = null;

                if (e.Result != null)
                    bmp = (Bitmap)e.Result;

                if (bmp != null)
                {
                    this.SetBitmap(this.helplineRulerCtrl1.Bmp, bmp, this.helplineRulerCtrl1, "Bmp");

                    this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                    this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                    this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                        (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                        (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                    _undoOPCache.Add(bmp);
                }

                this.btnSetGamma.Text = "Go";

                this.SetControls(true);
                this.Cursor = Cursors.Default;

                this.btnOK.Enabled = this.btnCancel.Enabled = true;

                this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());

                this._pic_changed = true;

                this.helplineRulerCtrl1.dbPanel1.Invalidate();

                if (this.Timer3.Enabled)
                    this.Timer3.Stop();

                this.Timer3.Start();

                this.backgroundWorker5.Dispose();
                this.backgroundWorker5 = new BackgroundWorker();
                this.backgroundWorker5.WorkerReportsProgress = true;
                this.backgroundWorker5.WorkerSupportsCancellation = true;
                this.backgroundWorker5.DoWork += backgroundWorker5_DoWork;
                //this.backgroundWorker5.ProgressChanged += backgroundWorker5_ProgressChanged;
                this.backgroundWorker5.RunWorkerCompleted += backgroundWorker5_RunWorkerCompleted;
            }
        }

        private unsafe void backgroundWorker6_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] o = (object[])e.Argument;

            Bitmap bWork = (Bitmap)o[0];
            Bitmap bOrig = (Bitmap)o[1];
            int innerW = (int)o[2];
            int outerW = (int)o[3];

            int gmm_comp = (int)o[4];
            double gamma = (double)o[5];
            int numIters = (int)o[6];
            bool rectMode = (bool)o[7];
            Rectangle r = (Rectangle)o[8];
            bool skipInit = (bool)o[9];
            bool workOnPaths = (bool)o[10];
            bool gammaChanged = (bool)o[11];
            int intMult = (int)o[12];
            bool quick = (bool)o[13];
            bool useEightAdj = (bool)o[14];
            bool useTh = (bool)o[15];
            double th = (double)o[16];
            double resPic = (double)o[17];
            bool initWKpp = (bool)o[18];
            bool multCapacitiesForTLinks = (bool)o[19];
            double multTLinkCapacity = (double)o[20];
            bool castTLInt = (bool)o[21];
            bool getSourcePart = (bool)o[22];
            ListSelectionMode selMode = (ListSelectionMode)o[23];
            bool scribbleMode = (bool)o[24];
            Dictionary<int, Dictionary<int, List<List<Point>>>> scribbles = (Dictionary<int, Dictionary<int, List<List<Point>>>>)o[25];
            double probMult1 = (double)o[26];
            double kmInitW = (double)o[27];
            double kmInitH = (double)o[28];
            bool setPFGToFG = (bool)o[29];
            bool cgWQE = (bool)o[30];
            double numItems = (double)o[31];
            double numCorrect = (double)o[32];
            double numItems2 = (double)o[33];
            double numCorrect2 = (double)o[34];
            bool skipLearn = (bool)o[35];

            Rectangle clipRect = (Rectangle)o[36];
            bool dontFillPath = (bool)o[37];
            bool drawNumComp = (bool)o[38];
            int comp = (int)o[39];
            int blur = (int)o[40];
            int alphaStartValue = (int)o[41];

            //resize the input bmp
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

            Bitmap bTrimap = new Bitmap(bWork.Width, bWork.Height);
            Bitmap bInner = null;

            using (Bitmap bForeground = RemoveOutlineEx(bWork, innerW, true))
            using (Bitmap bBackground = ExtendOutlineEx2(bWork, outerW, true, false))
            {
                using (Graphics gx = Graphics.FromImage(bTrimap))
                {
                    gx.SmoothingMode = SmoothingMode.None;
                    gx.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gx.Clear(Color.Black);
                    gx.DrawImage(bBackground, 0, 0);
                    gx.DrawImage(bForeground, 0, 0);
                }

                bInner = GetInnerFGPic(bWork, bForeground);
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
                    BGW = this.backgroundWorker6,
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
                    CGwithQE = cgWQE,
                    NumItems = numItems,
                    NumCorrect = numCorrect,
                    NumItems2 = numItems2,
                    NumCorrect2 = numCorrect2
                };

                this._gc.ShowInfo += _gc_ShowInfo;
            }

            //now do the initialization
            //eg create the mask, preclassify the imagedata, compute the smootheness function and init the Gmms
            if (!skipInit)
            {
                int it = this._gc.InitWithTrimap(bTrimap);

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
                this._gc.CGwithQE = cgWQE;
                this._gc.NumItems = numItems;
                this._gc.NumCorrect = numCorrect;
                this._gc.NumItems2 = numItems2;
                this._gc.NumCorrect2 = numCorrect2;

                if (!workOnPaths && this._gc.ScribbleMode && this._gc.Scribbles != null && this._gc.Scribbles.Count > 0)
                {
                    if (!this._gc.RectMode)
                        r = new Rectangle(0, 0, bWork.Width, bWork.Height);
                    this._gc.ReInitScribbles();
                }
            }

            //now do the work ...
            int l = this._gc.RunBoundary();

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
            Bitmap bRes = new Bitmap(bWork.Width, bWork.Height);

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
                if (y > 0 && y < h)
                    for (int x = 0; x < w; x++)
                    {
                        if (x > 0 && x < w)
                            if (x < ww && y < hh && r.Contains(x, y) && (m[x, y] == 1 || m[x, y] == 3))
                            {
                                p[x * 4 + y * stride] = pWork[x * 4 + y * stride];
                                p[x * 4 + y * stride + 1] = pWork[x * 4 + y * stride + 1];
                                p[x * 4 + y * stride + 2] = pWork[x * 4 + y * stride + 2];
                                p[x * 4 + y * stride + 3] = pWork[x * 4 + y * stride + 3];
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
            //this.SetBitmap(ref this._bResCopy, ref bResCopy);

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
            //this.SetBitmap(ref this._bResCopyTransp, ref bCTransp);

            bTmp.Dispose();

            Bitmap bDiff = GetDiff(bRes, bInner);

            using (Graphics gx = Graphics.FromImage(bDiff))
                gx.DrawImage(bInner, 0, 0);

            fipbmp fip = new fipbmp();
            fip.SmoothByAveragingA(bDiff, blur, this.backgroundWorker6);

            BoundaryMattingOP bmOP = new BoundaryMattingOP();
            //bmOP.Feather(bDiff, (int)Math.Max(innerW * (resPic > 1 ? resPic : 1), 1), alphaStartValue, innerW);
            bmOP.Feather(bDiff, (int)Math.Max(innerW * (resPic > 1 ? resPic : 1), 1) + 2, alphaStartValue, innerW);
            bmOP.Dispose();

            using (Graphics gx = Graphics.FromImage(bDiff))
                gx.DrawImage(bInner, 0, 0);

            if (resPic > 1)
            {
                Bitmap bOld = bRes;

                bRes = ResampleUp(bDiff, resPic, bU2, dontFillPath, false);
                //bRes = ResampleUp(bRes, resPic, bU2, dontFillPath, false);
                //Bitmap bRCopy = ResampleUp(this._bResCopy, resPic, bU2, dontFillPath, false);
                //Bitmap bRCopy2 = ResampleUp(this._bResCopyTransp, resPic, bU2, true, true);

                //this.SetBitmap(ref this._bResCopy, ref bRCopy);
                //this.SetBitmap(ref this._bResCopyTransp, ref bRCopy2);

                //bU2.Dispose(); //--> is disposed in ResampleUp

                if (bOld != null)
                    bOld.Dispose();
            }
            else if (resPic <= 1)
            {
                Bitmap bOld = bRes;
                bRes = new Bitmap(bDiff);
                if (bOld != null)
                    bOld.Dispose();
            }
            //else if (resPic == 1)
            //{
            //    //set the list of all found paths [chains] to re_use later
            //    List<ChainCode> allChains = GetBoundary(this._bResCopyTransp);
            //    this._allChains = allChains;

            //    if (allChains != null && allChains.Count > 0)
            //    {
            //        int area = allChains.Sum(a => a.Area);
            //        int pxls = w * h;

            //        int fc = pxls / area;

            //        //if we have almost no output, maybe the initialization of the Gmms hasn't been good enough to receive a reasonable result
            //        //so restart with some different KMeans initialization, if wanted
            //        if (fc > 1000)
            //            if (MessageBox.Show("Amount pixels to segmented area ratio is " + fc.ToString() + "." +
            //                "Rerun with different Initialization of the Gmms?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            //            {
            //                this._restartDiffInit = true;
            //            }
            //    }
            //}

            bDiff.Dispose();
            bDiff = null;
            bInner.Dispose();
            bInner = null;
            bWork.Dispose();
            bWork = null;
            bOrig.Dispose();
            bOrig = null;
            bTrimap.Dispose();
            bTrimap = null;

            //our result pic
            e.Result = GetAlphaBoundsPic(bRes, 2); //bRes;

            bRes.Dispose();
            bRes = null;
        }

        private unsafe Bitmap GetDiff(Bitmap bRes, Bitmap bInner)
        {
            int w = bRes.Width;
            int h = bRes.Height;
            Bitmap bOut = new Bitmap(w, h);

            BitmapData bmData = bOut.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmWork = bRes.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmInner = bInner.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = bmData.Stride;

            Parallel.For(0, h, y =>
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;
                byte* pWork = (byte*)bmWork.Scan0;
                pWork += y * stride;
                byte* pInner = (byte*)bmInner.Scan0;
                pInner += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (pInner[3] > 0 || pWork[3] > 0)
                    {
                        p[0] = (byte)Math.Abs(((int)pInner[0] - (int)pWork[0]));
                        p[1] = (byte)Math.Abs(((int)pInner[1] - (int)pWork[1]));
                        p[2] = (byte)Math.Abs(((int)pInner[2] - (int)pWork[2]));
                        p[3] = Math.Max(pInner[3], pWork[3]);
                    }

                    p += 4;
                    pWork += 4;
                    pInner += 4;
                }
            });

            bRes.UnlockBits(bmWork);
            bInner.UnlockBits(bmInner);
            bOut.UnlockBits(bmData);

            return bOut;
        }

        private unsafe Bitmap GetInnerFGPic(Bitmap bWork, Bitmap bForeground)
        {
            int w = bWork.Width;
            int h = bWork.Height;
            Bitmap bRes = new Bitmap(w, h);

            BitmapData bmData = bRes.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData bmWork = bWork.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData bmFG = bForeground.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = bmData.Stride;

            Parallel.For(0, h, y =>
            {
                byte* p = (byte*)bmData.Scan0;
                p += y * stride;
                byte* pWork = (byte*)bmWork.Scan0;
                pWork += y * stride;
                byte* pFG = (byte*)bmFG.Scan0;
                pFG += y * stride;

                for (int x = 0; x < w; x++)
                {
                    if (pFG[3] > 0)
                    {
                        p[0] = pWork[0];
                        p[1] = pWork[1];
                        p[2] = pWork[2];
                        p[3] = pFG[3];
                    }

                    p += 4;
                    pWork += 4;
                    pFG += 4;
                }
            });

            bWork.UnlockBits(bmWork);
            bForeground.UnlockBits(bmFG);
            bRes.UnlockBits(bmData);

            return bRes;
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

        private void _gc_ShowInfo(object sender, string e)
        {
            if (InvokeRequired)
                this.Invoke(new Action(() => this.toolStripStatusLabel4.Text = e));
            else
                this.toolStripStatusLabel4.Text = e;
        }

        private void backgroundWorker6_ProgressChanged(object sender, ProgressChangedEventArgs e)
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

        private void backgroundWorker6_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                Bitmap bRes = (Bitmap)e.Result;

                this.SetBitmap(this.helplineRulerCtrl1.Bmp, bRes, this.helplineRulerCtrl1, "Bmp");

                Bitmap bC = new Bitmap(bRes);
                this.SetBitmap(ref _bmpRef, ref bC);

                this.helplineRulerCtrl1.SetZoom(this.helplineRulerCtrl1.Zoom.ToString());
                this.helplineRulerCtrl1.MakeBitmap(this.helplineRulerCtrl1.Bmp);
                this.helplineRulerCtrl1.dbPanel1.AutoScrollMinSize = new Size(
                    (int)(this.helplineRulerCtrl1.Bmp.Width * this.helplineRulerCtrl1.Zoom),
                    (int)(this.helplineRulerCtrl1.Bmp.Height * this.helplineRulerCtrl1.Zoom));

                _undoOPCache.Add(bRes);

            }

            this.btnPPGo.Text = "Go";

            this.SetControls(true);
            this.Cursor = Cursors.Default;

            this.cbExpOutlProc_CheckedChanged(this.cbExpOutlProc, new EventArgs());

            this.btnAlphaV.Text = "Go";

            this._pic_changed = true;

            if (this._gc != null)
            {
                this._gc.ShowInfo -= _gc_ShowInfo;
                this._gc.Dispose();
                this._gc = null;
            }

            this.helplineRulerCtrl1.dbPanel1.Invalidate();

            if (this.Timer3.Enabled)
                this.Timer3.Stop();

            this.Timer3.Start();

            this.backgroundWorker6.Dispose();
            this.backgroundWorker6 = new BackgroundWorker();
            this.backgroundWorker6.WorkerReportsProgress = true;
            this.backgroundWorker6.WorkerSupportsCancellation = true;
            this.backgroundWorker6.DoWork += backgroundWorker6_DoWork;
            this.backgroundWorker6.ProgressChanged += backgroundWorker6_ProgressChanged;
            this.backgroundWorker6.RunWorkerCompleted += backgroundWorker6_RunWorkerCompleted;
        }

        private Rectangle GetR(Bitmap bWork, int oW)
        {
            List<ChainCode> c = GetBoundary(bWork);
            using (GraphicsPath gP = new GraphicsPath())
            {
                for (int i = 0; i < c.Count; i++)
                {
                    gP.AddLines(c[i].Coord.Select(a => new PointF(a.X, a.Y)).ToArray());
                    gP.CloseFigure();
                }

                RectangleF rc = gP.GetBounds();
                rc.Inflate(oW, oW);

                return new Rectangle((int)Math.Floor(rc.X), (int)Math.Floor(rc.Y), (int)Math.Ceiling(rc.Width), (int)Math.Ceiling(rc.Height));
            }
        }

        private void cmbMethodMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.label4.Enabled = this.numTh.Enabled = (cmbMethodMode.SelectedIndex == 1 && !this.cbExpOutlProc.Checked);
            this.label48.Enabled = this.label47.Enabled = this.numNormalDist.Enabled = this.numColDistDist.Enabled =
                (cmbMethodMode.SelectedIndex == 0 && !this.cbExpOutlProc.Checked);
        }
    }
}
