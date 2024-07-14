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
    public partial class frmRect : Form
    {
        public frmRect()
        {
            InitializeComponent();
        }

        public frmRect(Rectangle rc)
        {
            InitializeComponent();

            this.numX.Value = (decimal)rc.X;
            this.numY.Value = (decimal)rc.Y;
            this.numW.Value = (decimal)rc.Width;
            this.numH.Value = (decimal)rc.Height;
        }
    }
}
