using System.ComponentModel;

namespace GetAlphaMatte
{
    public class BlendParameters
    {
        public bool AutoSOR { get; internal set; }
        public AutoSORMode AutoSORMode { get; internal set; }
        public int MinPixelAmount { get; internal set; } = 12;
        public int MaxIterations { get; internal set; } = 15000;
        public double DesiredMaxLinearError { get; internal set; } = 0.0001;

        public ProgressEventArgs PE {  get; internal set; }
        public BackgroundWorker BGW {  get; internal set; }
        public int InnerIterations { get; internal set; }
        public bool Sleep { get; internal set; }
        public int SleepAmount { get; internal set; }
    }
}