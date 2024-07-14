using System;

namespace GetAlphaMatte
{
    public class PicturesEventHandler : EventArgs
    {  
        public int Mode { get; private set; }

        public PicturesEventHandler(int mode) 
        { 
            this.Mode = mode;
        }
    }
}