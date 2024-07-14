using System.Drawing;

namespace AvoidAGrabCutEasy
{
    public class StartNode
    {
        public int Indx { get; internal set; }
        public int Origin { get; internal set; }

        public StartNode(int indx, int origin)
        {
            this.Indx = indx;
            this.Origin = origin;
        }
    }
}