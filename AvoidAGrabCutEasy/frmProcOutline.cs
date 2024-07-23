using AvoidAGrabCutEasy.ProcOutline;
using Cache;
using ChainCodeFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
        private int _maxWidth = 24;
        private int _oW = 0;
        private int _iW = 0;
        private Stopwatch _sw;

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
                {
                    this.backgroundWorker1.CancelAsync();
                    return;
                }

                if (this._bmpBU != null)
                    this._bmpBU.Dispose();
                if (this._bmpOrig != null)
                    this._bmpOrig.Dispose();
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

                this.cbSimpleMatting_CheckedChanged(this.cbSimpleMatting, new EventArgs());
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

        private void btnAlphaV_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker3.IsBusy)
            {
                this.backgroundWorker3.CancelAsync();
                return;
            }

            if (this.helplineRulerCtrl1.Bmp != null)
            {
                this.Cursor = Cursors.WaitCursor;
                this.SetControls(false);

                if (this.cbSimpleMatting.Checked)
                {
                    this.toolStripProgressBar1.Value = 0;
                    this.toolStripProgressBar1.Visible = true;

                    this.btnAlphaV.Text = "Cancel";
                    this.btnAlphaV.Enabled = true;

                    if (_sw == null)
                        _sw = new Stopwatch();
                    _sw.Reset();
                    _sw.Start();

                    int windowSize = (int)this.numWinSz.Value;
                    int alphaTh = (int)this.numAlphaTh.Value;
                    int normalDistToCheck = 10;
                    int featherWidth = (int)this.numFeatherWidth.Value;

                    this.backgroundWorker3.RunWorkerAsync(new object[] { windowSize, alphaTh, normalDistToCheck, featherWidth });
                }
                else
                {

                    BlendType bt = (BlendType)System.Enum.Parse(typeof(BlendType), this.cmbBlendType.SelectedItem.ToString());
                    this.backgroundWorker2.RunWorkerAsync(new object[] { bt });
                }
            }
        }

        private void frmProcOutline_Load(object sender, EventArgs e)
        {
            foreach (string z in System.Enum.GetNames(typeof(BlendType)))
                this.cmbBlendType.Items.Add(z.ToString());

            this.cmbBlendType.SelectedIndex = 1;

            this.rbBoth.Checked = true;
            //this.cbSimpleMatting.Checked = true;

            this.cbBGColor_CheckedChanged(this.cbBGColor, new EventArgs());
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

            this.cbSimpleMatting_CheckedChanged(this.cbSimpleMatting, new EventArgs());

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

            this.cbSimpleMatting_CheckedChanged(this.cbSimpleMatting, new EventArgs());

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

        private void cbSimpleMatting_CheckedChanged(object sender, EventArgs e)
        {
            DisableBoundControls(this.cbSimpleMatting.Checked);
        }

        private void DisableBoundControls(bool ch)
        {
            for (int i = 0; i < this.groupBox4.Controls.Count; i++)
                if (!(this.groupBox4.Controls[i] is GroupBox) && !(this.groupBox4.Controls[i] is Button))
                    this.groupBox4.Controls[i].Enabled = !ch;

            this.label45.Enabled = this.label46.Enabled = this.numBoundOuter.Enabled = this.numBoundInner.Enabled = true;
            this.label52.Enabled = this.cbBlur.Enabled = this.numBlur.Enabled = !ch; //maybe this changes
            this.label28.Enabled = this.label54.Enabled = this.label2.Enabled = ch;
            this.numWinSz.Enabled = this.numAlphaTh.Enabled = this.numFeatherWidth.Enabled = ch;

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

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] o = (object[])e.Argument;
            int windowSize = (int)o[0];
            int alphaTh = (int)o[1];
            int normalDistToCheck = (int)o[2];
            int featherWidth = (int)o[3];

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

                e.Result = DoSimpleMatting(bWork, trWork, windowSize, alphaTh, normalDistToCheck, featherWidth);
                return;
            }
        }

        private Bitmap DoSimpleMatting(Bitmap bOrig, Bitmap trWork, int windowSize, int alphaTh, int normalDistToCheck, int featherWidth)
        {
            Bitmap fg = new Bitmap(this.helplineRulerCtrl1.Bmp);

            BoundaryMattingOP bMOP = new BoundaryMattingOP(fg, bOrig);
            Bitmap bRes = bMOP.DoSimpleMatting(trWork, this._iW, this._oW, windowSize, alphaTh, normalDistToCheck, featherWidth, this.backgroundWorker3);
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
                        this.Text += "        - ### -        " + TimeSpan.FromMilliseconds(this._sw.ElapsedMilliseconds).ToString();
                    this.toolStripProgressBar1.Value = Math.Max(Math.Min(e.ProgressPercentage, this.toolStripProgressBar1.Maximum), 0);
                }));
        }

        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                Bitmap bRes = (Bitmap)e.Result;

                this.SetBitmap(this.helplineRulerCtrl1.Bmp, bRes, this.helplineRulerCtrl1, "Bmp");

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

            this.cbSimpleMatting_CheckedChanged(this.cbSimpleMatting, new EventArgs());

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
            DisableBoundControls(this.cbSimpleMatting.Checked);
        }

        private void numBoundInner_ValueChanged(object sender, EventArgs e)
        {
            DisableBoundControls(this.cbSimpleMatting.Checked);
        }
    }
}
