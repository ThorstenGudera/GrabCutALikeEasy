using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LUBitmapDesigner
{
    public partial class PicInfoCtrl : UserControl
    {
        BitmapShape _curShape = null;
        private bool _dontRaise;

        public event EventHandler<BitmapShape> ShapeChanged;

        public PicInfoCtrl()
        {
            InitializeComponent();
        }

        public void SetValues(BitmapShape e)
        {
            if (e != null)
            {
                _curShape = e;

                RectangleF r = e.Bounds;
                float rot = e.Rotation;

                this.numX.Value = (decimal)r.X;
                this.numY.Value = (decimal)r.Y;
                this.numW.Value = (decimal)r.Width;
                this.numH.Value = (decimal)r.Height;
                this.numRot.Value = (decimal)rot;
            }
        }

        private void numX_ValueChanged(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                this._curShape.Bounds = new RectangleF((float)this.numX.Value, this._curShape.Bounds.Y, this._curShape.Bounds.Width, this._curShape.Bounds.Height);
                ShapeChanged?.Invoke(this, this._curShape);
            }
        }

        private void numY_ValueChanged(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                this._curShape.Bounds = new RectangleF(this._curShape.Bounds.X, (float)this.numY.Value, this._curShape.Bounds.Width, this._curShape.Bounds.Height);
                ShapeChanged?.Invoke(this, this._curShape);
            }
        }

        private void numW_ValueChanged(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                if (this.cbAspect.Checked)
                {
                    double a = (double)this._curShape.Bmp.Width / (double)this._curShape.Bmp.Height;
                    double h = (double)this.numW.Value / a;

                    this._dontRaise = true;
                    this._curShape.Bounds = new RectangleF(this._curShape.Bounds.X, this._curShape.Bounds.Y, (float)this.numW.Value, (float)h);
                    this._dontRaise = false;
                }
                else
                    this._curShape.Bounds = new RectangleF(this._curShape.Bounds.X, this._curShape.Bounds.Y, (float)this.numW.Value, this._curShape.Bounds.Height);

                if (!this._dontRaise)
                    ShapeChanged?.Invoke(this, this._curShape);
            }
        }

        private void numH_ValueChanged(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                if (this.cbAspect.Checked)
                {
                    double a = (double)this._curShape.Bmp.Width / (double)this._curShape.Bmp.Height;

                    double w = (double)this.numH.Value * a;

                    this._dontRaise = true;
                    this._curShape.Bounds = new RectangleF(this._curShape.Bounds.X, this._curShape.Bounds.Y, (float)w, (float)this.numH.Value);
                    this._dontRaise = false;
                }
                else
                    this._curShape.Bounds = new RectangleF(this._curShape.Bounds.X, this._curShape.Bounds.Y, this._curShape.Bounds.Width, (float)this.numH.Value);

                if (!this._dontRaise)
                    ShapeChanged?.Invoke(this, this._curShape);
            }
        }

        private void btnOrigSize_Click(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                this._curShape.Bounds = new RectangleF(this._curShape.Bounds.X, this._curShape.Bounds.Y, this._curShape.Bmp.Width, this._curShape.Bmp.Height);

                if (!this._dontRaise)
                    ShapeChanged?.Invoke(this, this._curShape);
            }
        }

        private void numRot_ValueChanged(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                this._curShape.Rotation = (float)this.numRot.Value; 

                if (!this._dontRaise)
                    ShapeChanged?.Invoke(this, this._curShape);
            }
        }

        private void numOpacity_ValueChanged(object sender, EventArgs e)
        {
            if (this._curShape != null)
            {
                this._curShape.Opacity = (float)this.numOpacity.Value; 

                if (!this._dontRaise)
                    ShapeChanged?.Invoke(this, this._curShape);
            }
        }
    }
}
