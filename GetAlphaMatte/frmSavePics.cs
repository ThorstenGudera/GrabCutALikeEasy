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
    public partial class frmSavePics : Form
    {
        public Bitmap FBmp { get; private set; }
        public Bitmap CBmp { get; private set; }

        public frmSavePics(Bitmap fBMP, Bitmap cBMP)
        {
            InitializeComponent();

            this.FBmp = fBMP;
            this.CBmp = cBMP;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = this.saveFileDialog1.FileName;

                if (this.lstPics.SelectedIndex == 0)
                {
                    this.FBmp.Save(fileName);
                }
                else
                {
                    this.CBmp.Save(fileName);
                }
            }
        }

        private void lstPics_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.lstPics.SelectedIndex == 0)
            {
                this.pictureBox1.Image = this.FBmp;
            }
            else
            {
                this.pictureBox1.Image = this.CBmp;
            }
        }

        private void frmSavePics_Load(object sender, EventArgs e)
        {
            this.lstPics.SelectedIndex = 0;
        }
    }
}
