using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUBitmapDesigner
{
    public class ShapeList
    {
        internal List<BitmapShape> Shapes { get; set; }
        public int Count
        {
            get { return Shapes.Count; }
        }

        private const int _MAXSIZE = 2;

        internal ShapeList()
        {
            Shapes = new List<BitmapShape>();
        }

        public BitmapShape this[int index]
        {
            get
            {
                return this.Shapes[index];
            }
            set
            {
                this.Shapes[index] = value;
            }
        }

        internal void Add(BitmapShape b)
        {
            if (this.Shapes == null)
                this.Shapes = new List<BitmapShape>();

            if (this.Shapes.Count < ShapeList._MAXSIZE)
                this.Shapes.Add(b);
            else
                throw new Exception("This List already contains its maximum amount of Shapes");
        }

        internal void Remove(BitmapShape b)
        {
            if (this.Shapes == null)
                this.Shapes = new List<BitmapShape>();

            if (this.Shapes.Contains(b))
            {
                this.Shapes.Remove(b);
                b.Dispose();
                b = null;
            }
        }

        internal void RemoveAt(int index)
        {
            BitmapShape bitmapShape = this.Shapes[index];
            this.Shapes.RemoveAt(index);
            bitmapShape.Dispose();
            bitmapShape = null;
        }

        internal void Clear()
        {
            this.Shapes.Clear();
        }
    }
}
