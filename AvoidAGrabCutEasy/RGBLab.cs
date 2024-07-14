using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvoidAGrabCutEasy
{
    // ported from this recipe (with no copyright notice in that code/one that page, so assumed to be free): 
    // http://cookbooks.adobe.com/post_Useful_color_equations__RGB_to_LAB_converter-14227.html 
    public class RGBLab
    {
        // whiteD65
        const double REF_X = 95.047;
        const double REF_Y = 100.0;
        const double REF_Z = 108.883;

        // whiteD50
        const double REF_X2 = 96.52;
        const double REF_Y2 = 100.0;
        const double REF_Z2 = 82.49;

        public class XYZ
        {
            public double X
            {
                get
                {
                    return m_X;
                }
                set
                {
                    m_X = value;
                }
            }
            private double m_X;
            public double Y
            {
                get
                {
                    return m_Y;
                }
                set
                {
                    m_Y = value;
                }
            }
            private double m_Y;
            public double Z
            {
                get
                {
                    return m_Z;
                }
                set
                {
                    m_Z = value;
                }
            }
            private double m_Z;
        }

        public class LAB
        {
            public double L
            {
                get
                {
                    return m_L;
                }
                set
                {
                    m_L = value;
                }
            }
            private double m_L;
            public double A
            {
                get
                {
                    return m_A;
                }
                set
                {
                    m_A = value;
                }
            }
            private double m_A;
            public double B
            {
                get
                {
                    return m_B;
                }
                set
                {
                    m_B = value;
                }
            }
            private double m_B;
        }

        public class RGB
        {
            public int R
            {
                get
                {
                    return m_R;
                }
                set
                {
                    m_R = value;
                }
            }
            private int m_R;
            public int G
            {
                get
                {
                    return m_G;
                }
                set
                {
                    m_G = value;
                }
            }
            private int m_G;
            public int B
            {
                get
                {
                    return m_B;
                }
                set
                {
                    m_B = value;
                }
            }
            private int m_B;
        }

        public static XYZ RGB2XYZ(int r, int g, int b)
        {
            double rr = r / 255.0;
            double gg = g / 255.0;
            double bb = b / 255.0;

            if (rr > 0.04045)
                rr = Math.Pow((rr + 0.055) / 1.055, 2.4);
            else
                rr = rr / 12.92;
            if (gg > 0.04045)
                gg = Math.Pow((gg + 0.055) / 1.055, 2.4);
            else
                gg = gg / 12.92;
            if (bb > 0.04045)
                bb = Math.Pow((bb + 0.055) / 1.055, 2.4);
            else
                bb = bb / 12.92;
            rr = rr * 100.0;
            gg = gg * 100.0;
            bb = bb * 100.0;

            XYZ xyz = new XYZ();
            xyz.X = rr * 0.4124 + gg * 0.3576 + bb * 0.1805;
            xyz.Y = rr * 0.2126 + gg * 0.7152 + bb * 0.0722;
            xyz.Z = rr * 0.0193 + gg * 0.1192 + bb * 0.9505;

            return xyz;
        }

        public static XYZ RGB2XYZ(double r, double g, double b)
        {
            double rr = r / 255.0;
            double gg = g / 255.0;
            double bb = b / 255.0;

            if (rr > 0.04045)
                rr = Math.Pow((rr + 0.055) / 1.055, 2.4);
            else
                rr = rr / 12.92;
            if (gg > 0.04045)
                gg = Math.Pow((gg + 0.055) / 1.055, 2.4);
            else
                gg = gg / 12.92;
            if (bb > 0.04045)
                bb = Math.Pow((bb + 0.055) / 1.055, 2.4);
            else
                bb = bb / 12.92;
            rr = rr * 100.0;
            gg = gg * 100.0;
            bb = bb * 100.0;

            XYZ xyz = new XYZ();
            xyz.X = rr * 0.4124 + gg * 0.3576 + bb * 0.1805;
            xyz.Y = rr * 0.2126 + gg * 0.7152 + bb * 0.0722;
            xyz.Z = rr * 0.0193 + gg * 0.1192 + bb * 0.9505;

            return xyz;
        }

        public static LAB XYZ2LAB(double x, double y, double z)
        {
            double xx = x / REF_X;
            double yy = y / REF_Y;
            double zz = z / REF_Z;

            if (xx > 0.008856)
                xx = Math.Pow(xx, 1.0 / 3.0);
            else
                xx = (7.787 * xx) + (16.0 / 116.0);
            if (yy > 0.008856)
                yy = Math.Pow(yy, 1.0 / 3.0);
            else
                yy = (7.787 * yy) + (16.0 / 116.0);
            if (zz > 0.008856)
                zz = Math.Pow(zz, 1.0 / 3.0);
            else
                zz = (7.787 * zz) + (16.0 / 116.0);

            LAB lab = new LAB();
            lab.L = (116 * yy) - 16;
            lab.A = 500 * (xx - yy);
            lab.B = 200 * (yy - zz);

            return lab;
        }

        public static XYZ LAB2XYZ(double l, double a, double b)
        {
            double yy = (l + 16) / 116.0;
            double xx = a / 500 + yy;
            double zz = yy - b / 200;

            if (Math.Pow(yy, 3) > 0.008856)
                yy = Math.Pow(yy, 3);
            else
                yy = (yy - 16.0 / 116.0) / 7.787;
            if (Math.Pow(xx, 3) > 0.008856)
                xx = Math.Pow(xx, 3);
            else
                xx = (xx - 16.0 / 116.0) / 7.787;
            if (Math.Pow(zz, 3) > 0.008856)
                zz = Math.Pow(zz, 3);
            else
                zz = (zz - 16.0 / 116.0) / 7.787;

            XYZ xyz = new XYZ();
            xyz.X = REF_X * xx;
            xyz.Y = REF_Y * yy;
            xyz.Z = REF_Z * zz;

            return xyz;
        }

        public static RGB XYZ2RGB(double x, double y, double z)
        {
            if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z))
                return new RGB() { R = 0, G = 0, B = 0 };

            double xx = x / 100.0;
            double yy = y / 100.0;
            double zz = z / 100.0;

            double r = xx * 3.2406 + yy * -1.5372 + zz * -0.4986;
            double g = xx * -0.9689 + yy * 1.8758 + zz * 0.0415;
            double b = xx * 0.0557 + yy * -0.204 + zz * 1.057;

            if (r > 0.0031308)
                r = 1.055 * Math.Pow(r, (1 / 2.4)) - 0.055;
            else
                r = 12.92 * r;
            if (g > 0.0031308)
                g = 1.055 * Math.Pow(g, (1 / 2.4)) - 0.055;
            else
                g = 12.92 * g;
            if (b > 0.0031308)
                b = 1.055 * Math.Pow(b, (1 / 2.4)) - 0.055;
            else
                b = 12.92 * b;

            RGB rgb = new RGB();
            rgb.R = System.Convert.ToInt32(Math.Round(r * 255.0));
            rgb.G = System.Convert.ToInt32(Math.Round(g * 255.0));
            rgb.B = System.Convert.ToInt32(Math.Round(b * 255.0));
            return rgb;
        }

        public static LAB RGB2LAB(int r, int g, int b)
        {
            XYZ xyz = RGB2XYZ(r, g, b);
            return XYZ2LAB(xyz.X, xyz.Y, xyz.Z);
        }

        public static LAB RGB2LAB(double[] vals)
        {
            XYZ xyz = RGB2XYZ(vals[2], vals[1], vals[0]);
            return XYZ2LAB(xyz.X, xyz.Y, xyz.Z);
        }

        public static RGB LAB2RGB(LAB l)
        {
            XYZ xyz = LAB2XYZ(l.L, l.A, l.B);
            return XYZ2RGB(xyz.X, xyz.Y, xyz.Z);
        }
    }

}
