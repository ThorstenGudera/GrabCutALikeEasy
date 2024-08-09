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
    public partial class frmKMeansSettings : Form
    {
        public frmKMeansSettings()
        {
            InitializeComponent();     
            
            foreach (string z in System.Enum.GetNames(typeof(ListSelectionMode)))
                this.cmbSelMode.Items.Add(z.ToString());

            this.cmbSelMode.SelectedIndex = 0;
        }

        private void cbInitRnd_CheckedChanged(object sender, EventArgs e)
        {
            this.label2.Enabled = this.label3.Enabled = this.numInitW.Enabled = this.numInitH.Enabled = !this.cbInitRnd.Checked;
        }
    }
}
