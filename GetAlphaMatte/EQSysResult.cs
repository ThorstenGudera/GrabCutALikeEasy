namespace GetAlphaMatte
{
    internal class EQSysResult
    {
        public EQSysResult(double[] fMax, double[] bMax, double alphaMax)
        {
            FMax = fMax;
            BMax = bMax;
            AlphaMax = alphaMax;
        }

        public double[] FMax { get; }
        public double[] BMax { get; }
        public double AlphaMax { get; }
    }
}