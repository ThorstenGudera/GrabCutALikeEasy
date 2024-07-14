using System.Drawing;

namespace GetAlphaMatte
{
    public class ExtPoint
    {    
        public int X { get; }
        public int Y { get; }
        public int V { get; set; }
        public Point Point { get; private set; }

        public ExtPoint(int x, int y, int v)
        {
            X = x;
            Y = y;
            V = v;
            this.Point = new Point(x, y);
        }
    }
}