using LUBitmapDesigner;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        public frmCompose(Bitmap bUpper)
        {
            InitializeComponent();

            if (AvailMem.AvailMem.checkAvailRam(bUpper.Width * bUpper.Height * 16L))
            {
                this._bmpBU = new Bitmap(bUpper);
                this.luBitmapDesignerCtrl1.SetUpperImage(new Bitmap(bUpper));
            }
            else
                MessageBox.Show("Not enough memory.");

            this.luBitmapDesignerCtrl1.ShapeChanged += LuBitmapDesignerCtrl1_ShapeChanged;
            this.picInfoCtrl1.ShapeChanged += PicInfoCtrl1_ShapeChanged;
        }

        private void PicInfoCtrl1_ShapeChanged(object sender, BitmapShape e)
        {
            this.luBitmapDesignerCtrl1.helplineRulerCtrl1.dbPanel1.Invalidate();
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
            if (this.Visible && this.luBitmapDesignerCtrl1.helplineRulerCtrl1.Bmp != null /*&& !this._dontDoZoom*/)
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

                bmp.Dispose();
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
    }
}
