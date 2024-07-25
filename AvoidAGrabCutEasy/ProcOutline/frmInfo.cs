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
    public partial class frmInfo : Form
    {
        public frmInfo()
        {
            InitializeComponent();
        }

        public void Show(string message)
        {
            this.label1.Text = message;
            if (!this.IsDisposed)
                try
                {
                    this.CenterToScreen();
                    this.Show();
                }
                catch { }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
