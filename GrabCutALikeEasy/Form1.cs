using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrabCutALikeEasy
{
    public partial class Form1 : Form
    {
        private string _basePathAddition = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Thorsten_Gudera");

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Bitmap bmp = null;
            if (this.OpenFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (Image img = Image.FromFile(this.OpenFileDialog1.FileName))
                    bmp = new Bitmap(img);

                Image iOld = this.pictureBox1.Image;
                this.pictureBox1.Image = bmp;
                if (iOld != null)
                    iOld.Dispose();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.pictureBox1.Image != null)
            {
                AvoidAGrabCutEasy.frmAvoidAGrabCutEasy frm = new AvoidAGrabCutEasy.frmAvoidAGrabCutEasy(new Bitmap((Bitmap)this.pictureBox1.Image), _basePathAddition);
                frm.SetupCache();
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    Image iOld = this.pictureBox1.Image;
                    this.pictureBox1.Image = new Bitmap(frm.FBitmap);
                    if (iOld != null)
                        iOld.Dispose();
                }
            }
        }
    }
}
