using System.ComponentModel;

namespace GetAlphaMatte
{
    public class BlendParameters
    {
        public bool AutoSOR { get;  set; }
        public AutoSORMode AutoSORMode { get;  set; }
        public int MinPixelAmount { get;  set; } = 12;
        public int MaxIterations { get;  set; } = 15000;
        public double DesiredMaxLinearError { get;  set; } = 0.0001;

        public ProgressEventArgs PE {  get;  set; }
        public BackgroundWorker BGW {  get;  set; }
        public int InnerIterations { get;  set; }
        public bool Sleep { get;  set; }
        public int SleepAmount { get;  set; }
    }
}