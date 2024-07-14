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
    public partial class frmDrawNumComp : Form
    {     
        public int CompCount { get; private set; }
        public frmDrawNumComp(int compCount)
        {
            InitializeComponent();

            this.CompCount = compCount;

            this.label1.Text = "Total number of components is: " + compCount.ToString();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.numericUpDown1.Enabled = this.checkBox1.Checked;
        }
    }
}
