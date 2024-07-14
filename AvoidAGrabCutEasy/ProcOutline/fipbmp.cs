using ChainCodeFinder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using ConvolutionLib;

namespace AvoidAGrabCutEasy.ProcOutline
{
    public class fipbmp
    {
        public event EventHandler<ConvolutionLib.ProgressEventArgs> ProgressPlus;

        public static Bitmap RemOutline(Bitmap bmp, int breite, System.ComponentModel.BackgroundWorker bgw)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L))
            {
                Bitmap b = null;
                BitArray fbits = null;

                try
                {
                    b = (Bitmap)bmp.Clone();

                    for (int i = 0; i <= breite - 1; i++)
                    {
                        if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                            break;

                        ChainFinder cf = new ChainFinder();
                        cf.AllowNullCells = true;

                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        cf.RemoveOutline(b, fList);

                        fbits = null;
                    }

                    return b;
                }
                catch
                {
                    if (fbits != null)
                        fbits = null;
                    if (b != null)
                        b.Dispose();

                    b = null;
                }
            }

            return null;
        }

        public static Bitmap ExtOutline(Bitmap bmp, Bitmap bOrig, int breite, System.ComponentModel.BackgroundWorker bgw)
        {
            if (AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L))
            {
                Bitmap b = null;
                BitArray fbits = null;

                try
                {
                    b = (Bitmap)bmp.Clone();

                    for (int i = 0; i <= breite - 1; i++)
                    {
                        if (bgw != null && bgw.WorkerSupportsCancellation && bgw.CancellationPending)
                            break;

                        ChainFinder cf = new ChainFinder();
                        cf.AllowNullCells = true;

                        List<ChainCode> fList = cf.GetOutline(b, 0, false, 0, false);

                        cf.ExtendOutline(b, fList);

                        fbits = null;
                    }

                    ChainFinder cf2 = new ChainFinder();
                    cf2.AllowNullCells = true;

                    List<ChainCode> fList2 = cf2.GetOutline(b, 0, false, 0, false);

                    using (Graphics gx = Graphics.FromImage(b))
                    {
                        gx.Clear(Color.Transparent);

                        foreach (ChainCode c in fList2)
                        {
                            using (GraphicsPath gP = new GraphicsPath())
                            {
                                PointF[] pts = c.Coord.Select(a => new PointF(a.X, a.Y)).ToArray();
                                gP.AddLines(pts);
                                gP.CloseAllFigures();

                                using (TextureBrush tb = new TextureBrush(bOrig))
                                {
                                    gx.FillPath(tb, gP);
                                    using (Pen p = new Pen(tb, 1))
                                        gx.DrawPath(p, gP);
                                }
                            }
                        }
                    }

                    return b;
                }
                catch
                {
                    if (fbits != null)
                        fbits = null;
                    if (b != null)
                        b.Dispose();

                    b = null;
                }
            }

            return null;
        }

        public void SmoothByAveragingA(Bitmap bWork, int krnl, BackgroundWorker bgw)
        {
            double[,] KernelBB = new double[krnl - 1 + 1, krnl - 1 + 1];

            for (int y = 0; y <= KernelBB.GetLength(1) - 1; y++)
            {
                for (int x = 0; x <= KernelBB.GetLength(0) - 1; x++)
                    KernelBB[x, y] = 1.0;
            }

            Convolution conv = new Convolution();
            conv.ProgressPlus += conv_ProgressPlus;
            conv.CancelLoops = false;

            // for the edges of the image
            double[,] AddValsBB = conv.CalculateStandardAddVals(KernelBB);

            ConvolutionLib.ProgressEventArgs pe = new ConvolutionLib.ProgressEventArgs(bWork.Height, 20);
            // go
            bool b = conv.Convolve_par(bWork, KernelBB, AddValsBB, 0, 255, true, false, pe, bgw);
            pe = null;

            conv.ProgressPlus -= conv_ProgressPlus;
        }

        private void conv_ProgressPlus(object sender, ConvolutionLib.ProgressEventArgs e)
        {
            ProgressPlus?.Invoke(sender, e);
        }
    }
}
