using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUBitmapDesigner
{
    public class BitmapShape : Shape, IDisposable
    {
        public override RectangleF Bounds { get; set; }
        public override float Rotation { get; set; }
        public override float Zoom { get; set; } = 1.0f;
        public Bitmap Bmp { get; set; }
        public float Opacity { get; set; } = 1.0f;

        public override void Draw(Graphics gx)
        {
            RectangleF boundsZ = new RectangleF(this.Bounds.X * this.Zoom, this.Bounds.Y * this.Zoom, this.Bounds.Width * this.Zoom, this.Bounds.Height * this.Zoom);

            if (this.Rotation == 0)
                gx.SetClip(boundsZ);

            GraphicsContainer con = gx.BeginContainer();

            Matrix mx = gx.Transform;
            mx.RotateAt(this.Rotation, new PointF(this.Bounds.X * this.Zoom, this.Bounds.Y * this.Zoom));
            gx.Transform = mx;

            if (this.Zoom == 0f)
                this.Zoom = 1f;

            if (this.Opacity == 1.0f)
                gx.DrawImage(this.Bmp, this.Bounds.X * this.Zoom, this.Bounds.Y * this.Zoom, this.Bounds.Width * this.Zoom, this.Bounds.Height * this.Zoom);
            else
            {
                ColorMatrix cm = new ColorMatrix();
                cm.Matrix33 = this.Opacity;

                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(cm);
                    gx.DrawImage(this.Bmp,
                        new PointF[] { new PointF(this.Bounds.X * this.Zoom, this.Bounds.Y * this.Zoom),
                            new PointF((this.Bounds.X+this.Bounds.Width) * this.Zoom, this.Bounds.Y * this.Zoom),
                            new PointF(this.Bounds.X * this.Zoom,  (this.Bounds.Y +this.Bounds.Height) * this.Zoom) },
                            new RectangleF(0, 0,
                                this.Bounds.Width, this.Bounds.Height),
                                GraphicsUnit.Pixel, ia);
                }
            }
            gx.EndContainer(con);

            gx.ResetClip();
        }

        public override void Dispose()
        {
            if (this.Bmp != null)
            {
                this.Bmp.Dispose();
                this.Bmp = null;
            }
        }
    }
}
