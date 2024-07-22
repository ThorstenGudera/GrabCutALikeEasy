﻿using System.Collections.Generic;
using System.Drawing;

namespace AvoidAGrabCutEasy.ProcOutline
{
    public class BoundaryObject
    {
        public Point Location { get; set; }
        public PointF NormalAmounts { get; set; }
        public List<Point> InnerPixels { get; set; }
        public List<Point> OuterPixels { get; set; }
        public int[] AlphaBaseValues { get; internal set; }
        public double ColorDistance { get; internal set; }
        public double AlphaCorrectionColDist { get; internal set; }
    }
}