using System.Drawing.Imaging;
using System.Drawing;
using System.Threading.Tasks;

namespace AvoidAGrabCutEasy
{
    internal class EdgeDetectionMethods
    {
        public static unsafe void ReplaceColors(Bitmap bmp, int nAlpha, int nRed, int nGreen, int nBlue, int tolerance, int zAlpha, int zRed, int zGreen, int zBlue)
        {
            BitmapData bmData = null;
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L))
            {
                try
                {
                    bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                    int stride = bmData.Stride;
                    System.IntPtr Scan0 = bmData.Scan0;

                    int nWidth = bmp.Width;
                    int nHeight = bmp.Height;

                    //byte* p = (byte*)bmData.Scan0;// new byte[(bmData.Stride * bmData.Height)];

                    Parallel.For(0, nHeight, y =>
                    {
                        byte* p = (byte*)bmData.Scan0;
                        // for (int y = 0; y < bmp.Height; y++)
                        int pos = 0;
                        pos += y * stride;

                        for (int x = 0; x <= nWidth - 1; x++)
                        {
                            if ((p[pos + 3] == 0 && zAlpha == 0) ||
                                ((p[pos + 3] > (zAlpha - tolerance)) && (p[pos + 3] < (zAlpha + tolerance)) &&
                                (p[pos + 2] > (zRed - tolerance)) && (p[pos + 2] < (zRed + tolerance)) &&
                                (p[pos + 1] > (zGreen - tolerance)) && (p[pos + 1] < (zGreen + tolerance)) &&
                                (p[pos] > (zBlue - tolerance)) && (p[pos] < (zBlue + tolerance))))
                            {
                                int value = nAlpha + System.Convert.ToInt32(p[pos + 3]) - zAlpha;
                                if (value < 0)
                                    value = 0;
                                if (value > 255)
                                    value = 255;
                                p[pos + 3] = System.Convert.ToByte(value);
                                int value2 = nRed + System.Convert.ToInt32(p[pos + 2]) - zRed;
                                if (value2 < 0)
                                    value2 = 0;
                                if (value2 > 255)
                                    value2 = 255;
                                p[pos + 2] = System.Convert.ToByte(value2);
                                int value3 = nGreen + System.Convert.ToInt32(p[pos + 1]) - zGreen;
                                if (value3 < 0)
                                    value3 = 0;
                                if (value3 > 255)
                                    value3 = 255;
                                p[pos + 1] = System.Convert.ToByte(value3);
                                int value4 = nBlue + System.Convert.ToInt32(p[pos]) - zBlue;
                                if (value4 < 0)
                                    value4 = 0;
                                if (value4 > 255)
                                    value4 = 255;
                                p[pos] = System.Convert.ToByte(value4);
                            }

                            pos += 4;
                        }
                    });

                    bmp.UnlockBits(bmData);
                }
                catch
                {
                    try
                    {
                        if (bmData != null)
                            bmp.UnlockBits(bmData);
                    }
                    catch
                    {
                    }
                }
            }
        }

    }
}