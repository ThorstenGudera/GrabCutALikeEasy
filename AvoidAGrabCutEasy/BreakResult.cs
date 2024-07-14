using System.Collections.Generic;

namespace AvoidAGrabCutEasy
{
    internal class BreakResult
    {
        internal int counter { get; private set; }
        internal int[] tree { get; private set; }

        public BreakResult(int counter, int[] tree)
        {
            this.counter = counter;
            this.tree = tree;
        }
    }
}