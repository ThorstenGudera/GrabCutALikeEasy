namespace GetAlphaMatte
{
    public class TrimapProblemInfo
    {
        public int RunNumber { get; private set; }
        public int TileNumber { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Overlap { get; private set; }

        public TrimapProblemInfo(int id, int tileNo, int x, int y, int width, int height, int overlap)
        {
            this.RunNumber = id;
            this.TileNumber = tileNo;
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.Overlap = overlap;
        }
    }
}