using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GetAlphaMatte
{
    public partial class frmSavePics2 : Form
    {
        public Bitmap PB1Bmp { get; private set; }
        public Bitmap OBmp { get; private set; }
        public Bitmap FGBmp { get; private set; }
        public Bitmap TRBmp { get; private set; }
        public Bitmap mBmp { get; private set; }
        public Bitmap cBmp { get; private set; }
        public Bitmap FBitmap { get; internal set; }

        public frmSavePics2(Bitmap pb1BMP, Bitmap oBMP, Bitmap fgBMP, Bitmap trBMP, Bitmap mBMP, Bitmap cBMP)
        {
            InitializeComponent();

            this.PB1Bmp = pb1BMP;
            this.OBmp = oBMP;
            this.FGBmp = fgBMP;
            this.TRBmp = trBMP;
            this.mBmp = mBMP;
            this.cBmp = cBMP;

            if (cBMP == null)
                this.lstPics.Items.RemoveAt(5);
            if (mBMP == null)
                this.lstPics.Items.RemoveAt(4);
            if (trBMP == null)
                this.lstPics.Items.RemoveAt(3);
            if (fgBMP == null)
                this.lstPics.Items.RemoveAt(2);
            if (oBMP == null)
                this.lstPics.Items.RemoveAt(1);
            if (pb1BMP == null)
                this.lstPics.Items.RemoveAt(0);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (this.lstPics.Items.Count > 0)
            {
                if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string fileName = this.saveFileDialog1.FileName;

                    if (this.lstPics.SelectedItem.ToString() == "MainBitmap")
                        this.PB1Bmp.Save(fileName);
                    else if (this.lstPics.SelectedItem.ToString() == "2nd Bitmap")
                        this.OBmp.Save(fileName);
                    else if (this.lstPics.SelectedItem.ToString() == "3rd Bitmap")
                        this.FGBmp.Save(fileName);
                    else if (this.lstPics.SelectedItem.ToString() == "4th Bitmap")
                        this.TRBmp.Save(fileName);
                    else if (this.lstPics.SelectedItem.ToString() == "5th Bitmap")
                        this.mBmp.Save(fileName);
                    else if (this.lstPics.SelectedItem.ToString() == "6th Bitmap")
                        this.cBmp.Save(fileName);
                }
            }
            else
                this.btnSave.Enabled = false;
        }

        private void lstPics_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.lstPics.SelectedItem.ToString() == "MainBitmap")
                this.pictureBox1.Image = this.PB1Bmp;
            else if (this.lstPics.SelectedItem.ToString() == "2nd Bitmap")
                this.pictureBox1.Image = this.OBmp;
            else if (this.lstPics.SelectedItem.ToString() == "3rd Bitmap")
                this.pictureBox1.Image = this.FGBmp;
            else if (this.lstPics.SelectedItem.ToString() == "4th Bitmap")
                this.pictureBox1.Image = this.TRBmp;
            else if (this.lstPics.SelectedItem.ToString() == "5th Bitmap")
                this.pictureBox1.Image = this.mBmp;
            else if (this.lstPics.SelectedItem.ToString() == "6th Bitmap")
                this.pictureBox1.Image = this.cBmp;

            this.FBitmap = this.pictureBox1.Image as Bitmap;
        }

        private void frmSavePics_Load(object sender, EventArgs e)
        {
            if (this.lstPics.Items.Count > 0)
                this.lstPics.SelectedIndex = 0;
            else
                this.btnSave.Enabled = this.btnOK.Enabled = false;
        }
    }
}
