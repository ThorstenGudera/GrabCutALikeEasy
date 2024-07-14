Option Strict On

Imports System.Runtime.InteropServices
Imports System.Drawing.Imaging
Imports System.Drawing
Imports System.Threading.Tasks

Public Class fipbmp
    Shared Sub GradColors(bmp As Bitmap, redValues As Byte(), greenValues As Byte(), blueValues As Byte())
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
            Dim bmData As BitmapData = Nothing

            Try
                bmData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)

                Dim stride As Integer = bmData.Stride
                Dim Scan0 As System.IntPtr = bmData.Scan0

                Dim nWidth As Integer = bmp.Width
                Dim nHeight As Integer = bmp.Height

                Dim p((bmData.Stride * bmData.Height) - 1) As Byte
                Marshal.Copy(bmData.Scan0, p, 0, p.Length)

                Parallel.[For](0, nHeight, Sub(y)
                                               'For y As Integer = 0 To nHeight - 1
                                               Dim pos As Integer = y * stride

                                               For x As Integer = 0 To nWidth - 1
                                                   Dim value As Integer = redValues(p(pos + 2))
                                                   If value < 0 Then
                                                       value = 0
                                                   End If
                                                   If value > 255 Then
                                                       value = 255
                                                   End If
                                                   p(pos + 2) = CByte(value)

                                                   Dim value2 As Integer = greenValues(p(pos + 1))
                                                   If value2 < 0 Then
                                                       value2 = 0
                                                   End If
                                                   If value2 > 255 Then
                                                       value2 = 255
                                                   End If
                                                   p(pos + 1) = CByte(value2)

                                                   Dim value3 As Integer = blueValues(p(pos))
                                                   If value3 < 0 Then
                                                       value3 = 0
                                                   End If
                                                   If value3 > 255 Then
                                                       value3 = 255
                                                   End If
                                                   p(pos) = CByte(value3)

                                                   pos += 4
                                               Next
                                               'Next
                                           End Sub)

                Marshal.Copy(p, 0, bmData.Scan0, p.Length)
                bmp.UnlockBits(bmData)

                p = Nothing
            Catch
                Try
                    bmp.UnlockBits(bmData)
                Catch
                End Try
            End Try
        End If
    End Sub

    Shared Sub GradColors(bmp As Bitmap, redValues As Byte(), greenValues As Byte(), blueValues As Byte(), fColor As Color, tolerance As Integer)
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
            Dim bmData As BitmapData = Nothing

            Try
                bmData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)

                Dim stride As Integer = bmData.Stride
                Dim Scan0 As System.IntPtr = bmData.Scan0

                Dim nWidth As Integer = bmp.Width
                Dim nHeight As Integer = bmp.Height

                Dim p((bmData.Stride * bmData.Height) - 1) As Byte
                Marshal.Copy(bmData.Scan0, p, 0, p.Length)

                Parallel.[For](0, nHeight, Sub(y)
                                               'For y As Integer = 0 To nHeight - 1
                                               Dim pos As Integer = y * stride

                                               For x As Integer = 0 To nWidth - 1
                                                   If (p(pos + 3) = 0 AndAlso fColor.A = 0) OrElse
                                                       ((p(pos + 3) >= (CInt(fColor.A) - tolerance)) AndAlso (CInt(p(pos + 3)) <= (CInt(fColor.A) + tolerance)) _
                                                        AndAlso (CInt(p(pos + 2)) >= (CInt(fColor.R) - tolerance)) AndAlso (CInt(p(pos + 2)) <= (CInt(fColor.R) + tolerance)) _
                                                        AndAlso (CInt(p(pos + 1)) >= (CInt(fColor.G) - tolerance)) AndAlso (CInt(p(pos + 1)) <= (CInt(fColor.G) + tolerance)) _
                                                        AndAlso (CInt(p(pos)) >= (CInt(fColor.B) - tolerance)) AndAlso (CInt(p(pos)) <= (CInt(fColor.B) + tolerance))) Then

                                                       Dim value As Integer = redValues(p(pos + 2))
                                                       If value < 0 Then
                                                           value = 0
                                                       End If
                                                       If value > 255 Then
                                                           value = 255
                                                       End If
                                                       p(pos + 2) = CByte(value)

                                                       Dim value2 As Integer = greenValues(p(pos + 1))
                                                       If value2 < 0 Then
                                                           value2 = 0
                                                       End If
                                                       If value2 > 255 Then
                                                           value2 = 255
                                                       End If
                                                       p(pos + 1) = CByte(value2)

                                                       Dim value3 As Integer = blueValues(p(pos))
                                                       If value3 < 0 Then
                                                           value3 = 0
                                                       End If
                                                       If value3 > 255 Then
                                                           value3 = 255
                                                       End If
                                                       p(pos) = CByte(value3)
                                                   End If
                                                   pos += 4

                                               Next
                                               'Next
                                           End Sub)

                Marshal.Copy(p, 0, bmData.Scan0, p.Length)
                bmp.UnlockBits(bmData)

                p = Nothing
            Catch
                Try
                    bmp.UnlockBits(bmData)
                Catch
                End Try
            End Try
        End If
    End Sub


    Public Shared Sub gradColors(bmp As Bitmap, lumValues As Integer(), Multiplicator As Integer)
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
            Dim bmData As BitmapData = Nothing

            Try
                bmData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)

                Dim stride As Integer = bmData.Stride
                Dim Scan0 As System.IntPtr = bmData.Scan0

                Dim nWidth As Integer = bmp.Width
                Dim nHeight As Integer = bmp.Height

                Dim p((bmData.Stride * bmData.Height) - 1) As Byte
                Marshal.Copy(bmData.Scan0, p, 0, p.Length)

                Parallel.[For](0, nHeight, Sub(y)
                                               'For y As Integer = 0 To nHeight - 1
                                               Dim pos As Integer = y * stride

                                               For x As Integer = 0 To nWidth - 1
                                                   Dim hsl As HSLData = RGBtoHSL(CInt(p(pos + 2)), CInt(p(pos + 1)), CInt(p(pos)))

                                                   Dim lValue As Double = hsl.Luminance * Multiplicator
                                                   Dim rest As Double = lValue - CInt(lValue)

                                                   Dim lTmpValue As Integer = CInt(lValue * (CDbl(lumValues.Length - 1) / CDbl(Multiplicator)))

                                                   If lTmpValue < 0 Then
                                                       lTmpValue = 0
                                                   End If
                                                   If lTmpValue > lumValues.Length - 1 Then
                                                       lTmpValue = lumValues.Length - 1
                                                   End If
                                                   lTmpValue = lumValues(lTmpValue)

                                                   Dim val_luminance As Single = CSng(CDbl(lTmpValue) * (CDbl(Multiplicator) / CDbl(lumValues.Length)) + rest)

                                                   val_luminance /= Multiplicator

                                                   If val_luminance > 1.0F Then
                                                       val_luminance = 1.0F
                                                   End If

                                                   If val_luminance < 0.0F Then
                                                       val_luminance = 0.0F
                                                   End If

                                                   Dim nPixel As PixelData = HSLtoRGB(hsl.Hue, hsl.Saturation, val_luminance)

                                                   Dim value As Integer = nPixel.red
                                                   If value < 0 Then
                                                       value = 0
                                                   End If
                                                   If value > 255 Then
                                                       value = 255
                                                   End If
                                                   p(pos + 2) = CByte(value)
                                                   Dim value2 As Integer = nPixel.green
                                                   If value2 < 0 Then
                                                       value2 = 0
                                                   End If
                                                   If value2 > 255 Then
                                                       value2 = 255
                                                   End If
                                                   p(pos + 1) = CByte(value2)
                                                   Dim value3 As Integer = nPixel.blue
                                                   If value3 < 0 Then
                                                       value3 = 0
                                                   End If
                                                   If value3 > 255 Then
                                                       value3 = 255
                                                   End If
                                                   p(pos) = CByte(value3)
                                                   pos += 4
                                               Next
                                               'Next
                                           End Sub)

                Marshal.Copy(p, 0, bmData.Scan0, p.Length)
                bmp.UnlockBits(bmData)

                p = Nothing
            Catch
                Try
                    bmp.UnlockBits(bmData)
                Catch
                End Try
            End Try
        End If
    End Sub

    Public Shared Function RGBtoHSL(Red As Integer, Green As Integer, Blue As Integer) As HSLData
        Dim hsl As New HSLData()

        Dim c As Color = Color.FromArgb(255, Red, Green, Blue)
        hsl.Hue = c.GetHue()
        hsl.Saturation = c.GetSaturation()
        hsl.Luminance = c.GetBrightness()

        Return hsl
    End Function

    'see http://www.mpa-garching.mpg.de/MPA-GRAPHICS/hsl-rgb.html
    'or http://130.113.54.154/~monger/hsl-rgb.html
    Public Shared Function HSLtoRGB(H As Double, S As Double, L As Double) As PixelData
        Dim Temp1 As Double = 0.0, Temp2 As Double = 0.0
        Dim r As Double = 0.0, g As Double = 0.0, b As Double = 0.0

        If S = 0 Then
            r = L
            g = L
            b = L
        Else
            If L < 0.5 Then
                Temp2 = L * (1.0 + S)
            Else
                Temp2 = (L + S) - (S * L)
            End If

            Temp1 = 2.0 * L - Temp2

            'bischen Spaghetti hier, evtl. in eigene Funktion auslagern

            Dim hTmp As Double = H / 360.0
            Dim rTmp As Double, gTmp As Double, bTmp As Double

            rTmp = hTmp + (1.0 / 3.0)
            gTmp = hTmp
            bTmp = hTmp - (1.0 / 3.0)

            If rTmp < 0.0 Then
                rTmp += 1.0
            End If
            If gTmp < 0.0 Then
                gTmp += 1.0
            End If
            If bTmp < 0.0 Then
                bTmp += 1.0
            End If

            If rTmp > 1.0 Then
                rTmp -= 1.0
            End If
            If gTmp > 1.0 Then
                gTmp -= 1.0
            End If
            If bTmp > 1.0 Then
                bTmp -= 1.0
            End If

            If 6.0 * rTmp < 1.0 Then
                r = Temp1 + (Temp2 - Temp1) * 6.0 * rTmp
            ElseIf 2.0 * rTmp < 1.0 Then
                r = Temp2
            ElseIf 3.0 * rTmp < 2.0 Then
                r = Temp1 + (Temp2 - Temp1) * ((2.0 / 3.0) - rTmp) * 6.0
            Else
                r = Temp1
            End If

            If 6.0 * gTmp < 1.0 Then
                g = Temp1 + (Temp2 - Temp1) * 6.0 * gTmp
            ElseIf 2.0 * gTmp < 1.0 Then
                g = Temp2
            ElseIf 3.0 * gTmp < 2.0 Then
                g = Temp1 + (Temp2 - Temp1) * ((2.0 / 3.0) - gTmp) * 6.0
            Else
                g = Temp1
            End If

            If 6.0 * bTmp < 1.0 Then
                b = Temp1 + (Temp2 - Temp1) * 6.0 * bTmp
            ElseIf 2.0 * bTmp < 1.0 Then
                b = Temp2
            ElseIf 3.0 * bTmp < 2.0 Then
                b = Temp1 + (Temp2 - Temp1) * ((2.0 / 3.0) - bTmp) * 6.0
            Else
                b = Temp1
            End If
        End If

        Dim RGB As New PixelData()

        r *= 255.0
        g *= 255.0
        b *= 255.0

        RGB.red = CByte(CInt(r))
        RGB.green = CByte(CInt(g))
        RGB.blue = CByte(CInt(b))

        Return RGB
    End Function

    Public Shared Function ScrollToPic(bmp As Bitmap) As Point
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
            ' GDI+ still lies to us - the return format is BGR, NOT RGB.
            Dim bmData As BitmapData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)

            Dim scanline As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim lefttop As New Point()
            Dim complete As Boolean = False

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pos As Integer = 0

            For y As Integer = 0 To bmp.Height - 1
                For x As Integer = 0 To bmp.Width - 1
                    If p(pos + 3) <> 0 Then
                        lefttop = New Point(x, y)
                        complete = True
                        Exit For
                    End If

                    pos += 4
                Next
                If complete Then
                    Exit For
                End If
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            bmp.UnlockBits(bmData)

            p = Nothing

            Return lefttop
        End If

        Return New Point(0, 0)
    End Function
End Class
