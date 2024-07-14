using System;
using System.Collections.Generic;

namespace AvoidAGrabCutEasy
{
    internal class InnerListObject
    {   
        public List<Tuple<int, int>> Edges { get; internal set; }
        public List<double> Capacities { get; internal set; }

        public InnerListObject()
        {
            this.Edges = new List<Tuple<int, int>>();
            this.Capacities = new List<double>();
        }
    }
}