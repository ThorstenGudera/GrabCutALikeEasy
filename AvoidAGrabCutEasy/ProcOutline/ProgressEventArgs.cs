using System;

namespace GetAlphaMatte
{
    public class ProgressEventArgs : EventArgs
    {
        private int _prgInterval = 1;
        public int ImgWidthHeight
        {
            get
            {
                return m_ImgWidthHeight;
            }
            set
            {
                m_ImgWidthHeight = value;
            }
        }
        private int m_ImgWidthHeight;
        public double CurrentProgress
        {
            get
            {
                return m_CurrentProgress;
            }
            set
            {
                m_CurrentProgress = value;
            }
        }
        private double m_CurrentProgress;
        public int PrgInterval
        {
            get
            {
                return _prgInterval;
            }
            set
            {
                _prgInterval = Math.Max(value, 1);
            }
        }

        public ProgressEventArgs(int ImageWidthHeight, int StartValue)
        {
            ImgWidthHeight = ImageWidthHeight;
            CurrentProgress = StartValue;
        }

        public ProgressEventArgs(int ImageWidthHeight, double StartValue, int Interval)
        {
            ImgWidthHeight = ImageWidthHeight;
            CurrentProgress = StartValue;
            PrgInterval = Interval;
        }
    }
}