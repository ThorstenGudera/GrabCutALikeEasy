using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUBitmapDesigner
{
    public abstract class Shape : IDisposable
    {
        public abstract RectangleF Bounds { get; set; }
        public abstract float Rotation { get; set; }
        public abstract float Zoom { get; set; }

        public abstract void Draw(Graphics gx);
        public abstract void Dispose();
    }
}
