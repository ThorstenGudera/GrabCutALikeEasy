using System.Drawing;

namespace AvoidAGrabCutEasy
{
    public class Centroid
    {
        public Point Pt { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public int Group { get; set; }

        public Centroid(Point pt)
        {
            this.Pt = pt;
        }
    }
}