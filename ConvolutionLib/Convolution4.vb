Option Strict On

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices

Partial Public Class Convolution
    Public Shared Function Conv3x3_Abs_IntoColorPlane(bmp As Bitmap, mx As ConvMatrix, Factor22 As Double, Factor222 As Double, eins As Integer, zwei As Integer,
        drei As Integer, vier As Integer, fuenf As Integer, sechs As Integer, sieben As Integer, acht As Integer,
        neun As Integer, planeRead As Integer, planeWrite As Integer) As Boolean

        If 0 = mx.Factor Then
            Return False
        End If
        If 0.0 = Factor22 Then
            Return False
        End If
        If 0.0 = Factor222 Then
            Return False
        End If

        If planeRead = planeWrite Then
            Return False
        End If
        If planeRead < 0 OrElse planeRead > 3 Then
            Return False
        End If
        If planeWrite < 0 OrElse planeWrite > 3 Then
            Return False
        End If

        Dim bmData As BitmapData = Nothing

        Try
            bmData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)

            Dim stride As Integer = bmData.Stride
            Dim stride2 As Integer = stride * 2
            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pos As Integer = 0

            Dim nOffset As Integer = stride - bmp.Width * 4
            Dim nWidth As Integer = bmp.Width - 2
            Dim nHeight As Integer = bmp.Height - 2

            Dim nPixel As Integer

            nPixel = CInt(Math.Abs((((p(pos + planeRead) * (mx.Pixel + eins)) +
                               (p(pos + 4 + planeRead) * mx.MidRight) +
                               (p(pos + stride + planeRead) * mx.BottomMid) +
                               (p(pos + 4 + stride + planeRead) * mx.BottomRight)) / CDbl(mx.Factor * Factor22)) + mx.Offset))

            If nPixel < 0 Then
                nPixel = 0
            End If
            If nPixel > 255 Then
                nPixel = 255
            End If
            p(pos + planeWrite) = CByte(nPixel)

            For x As Integer = 0 To nWidth - 1
                nPixel = CInt(Math.Abs((((p(pos + planeRead) * mx.MidLeft) +
                                   (p(pos + 4 + planeRead) * (mx.Pixel + zwei)) +
                                   (p(pos + 8 + planeRead) * mx.MidRight) +
                                   (p(pos + stride + planeRead) * mx.BottomLeft) +
                                   (p(pos + 4 + stride + planeRead) * mx.BottomMid) +
                                   (p(pos + 8 + stride + planeRead) * mx.BottomRight)) / CDbl(mx.Factor * Factor222)) + mx.Offset))

                If nPixel < 0 Then
                    nPixel = 0
                End If
                If nPixel > 255 Then
                    nPixel = 255
                End If
                p(pos + 4 + planeWrite) = CByte(nPixel)

                pos += 4
            Next

            nPixel = CInt(Math.Abs((((p(pos + planeRead) * mx.MidLeft) +
                               (p(pos + 4 + planeRead) * (mx.Pixel + drei)) +
                               (p(pos + stride + planeRead) * mx.BottomLeft) +
                               (p(pos + 4 + stride + planeRead) * mx.BottomMid)) / CDbl(mx.Factor * Factor22)) + mx.Offset))

            If nPixel < 0 Then
                nPixel = 0
            End If
            If nPixel > 255 Then
                nPixel = 255
            End If
            p(pos + 4 + planeWrite) = CByte(nPixel)

            pos = 0

            Dim nHeight1 As Integer = nHeight

            Parallel.For(0, nHeight1, Sub(y)
                                          'For y As Integer = 0 To nHeight - 1
                                          Dim nPixel2 As Integer = 0
                                          Dim pos2 As Integer = y * stride
                                          Dim mx2 As ConvMatrix = DirectCast(mx.Clone(), ConvMatrix)

                                          'first pixel
                                          nPixel2 = CInt(Math.Abs((((p(pos2 + planeRead) * mx2.TopMid) +
                                               (p(pos2 + 4 + planeRead) * mx2.TopRight) +
                                               (p(pos2 + stride + planeRead) * (mx2.Pixel + vier)) +
                                               (p(pos2 + 4 + stride + planeRead) * mx2.MidRight) +
                                               (p(pos2 + stride2 + planeRead) * mx2.BottomMid) +
                                               (p(pos2 + 4 + stride2 + planeRead) * mx2.BottomRight)) / CDbl(mx2.Factor * Factor222)) + mx2.Offset))

                                          If nPixel2 < 0 Then
                                              nPixel2 = 0
                                          End If
                                          If nPixel2 > 255 Then
                                              nPixel2 = 255
                                          End If
                                          p(pos2 + stride + planeWrite) = CByte(nPixel2)

                                          For x As Integer = 0 To nWidth - 1
                                              nPixel2 = CInt(Math.Abs((((p(pos2 + planeRead) * mx2.TopLeft) +
                                                   (p(pos2 + 4 + planeRead) * mx2.TopMid) +
                                                   (p(pos2 + 8 + planeRead) * mx2.TopRight) +
                                                   (p(pos2 + stride + planeRead) * mx2.MidLeft) +
                                                   (p(pos2 + 4 + stride + planeRead) * (mx2.Pixel + fuenf)) +
                                                   (p(pos2 + 8 + stride + planeRead) * mx2.MidRight) +
                                                   (p(pos2 + stride2 + planeRead) * mx2.BottomLeft) +
                                                   (p(pos2 + 4 + stride2 + planeRead) * mx2.BottomMid) +
                                                   (p(pos2 + 8 + stride2 + planeRead) * mx2.BottomRight)) / mx2.Factor) + mx2.Offset))

                                              If nPixel2 < 0 Then
                                                  nPixel2 = 0
                                              End If
                                              If nPixel2 > 255 Then
                                                  nPixel2 = 255
                                              End If
                                              p(pos2 + 4 + stride + planeWrite) = CByte(nPixel2)

                                              pos2 += 4
                                          Next

                                          'last pixel
                                          nPixel2 = CInt(Math.Abs((((p(pos2 + planeRead) * mx2.TopLeft) +
                                               (p(pos2 + 4 + planeRead) * mx2.TopMid) +
                                               (p(pos2 + stride + planeRead) * mx2.MidLeft) +
                                               (p(pos2 + 4 + stride + planeRead) * (mx2.Pixel + sechs)) +
                                               (p(pos2 + stride2 + planeRead) * mx2.BottomLeft) +
                                               (p(pos2 + 4 + stride2 + planeRead) * mx2.BottomMid)) / CDbl(mx2.Factor * Factor222)) + mx2.Offset))

                                          If nPixel2 < 0 Then
                                              nPixel2 = 0
                                          End If
                                          If nPixel2 > 255 Then
                                              nPixel2 = 255
                                          End If
                                          p(pos2 + 4 + stride + planeWrite) = CByte(nPixel2)

                                          pos2 += 8 + nOffset
                                          'Next
                                      End Sub)

            pos = p.Length - stride2

            'last line
            nPixel = CInt(Math.Abs((((p(pos + planeRead) * mx.TopMid) +
                 (p(pos + 4 + planeRead) * mx.TopRight) +
                 (p(pos + stride + planeRead) * (mx.Pixel + sieben)) +
                 (p(pos + 4 + stride + planeRead) * mx.MidRight)) / CDbl(mx.Factor * Factor22)) + mx.Offset))

            If nPixel < 0 Then
                nPixel = 0
            End If
            If nPixel > 255 Then
                nPixel = 255
            End If
            p(pos + stride + planeWrite) = CByte(nPixel)

            For x As Integer = 0 To nWidth - 1
                nPixel = CInt(Math.Abs((((p(pos + planeRead) * mx.TopLeft) +
                     (p(pos + 4 + planeRead) * mx.TopMid) +
                     (p(pos + 8 + planeRead) * mx.TopRight) +
                     (p(pos + stride + planeRead) * mx.MidLeft) +
                     (p(pos + 4 + stride + planeRead) * (mx.Pixel + acht)) +
                     (p(pos + 8 + stride + planeRead) * mx.MidRight)) / CDbl(mx.Factor * Factor222)) + mx.Offset))

                If nPixel < 0 Then
                    nPixel = 0
                End If
                If nPixel > 255 Then
                    nPixel = 255
                End If
                p(pos + 4 + stride + planeWrite) = CByte(nPixel)

                pos += 4
            Next

            nPixel = CInt(Math.Abs((((p(pos + planeRead) * mx.TopLeft) +
                 (p(pos + 4 + planeRead) * mx.TopMid) +
                 (p(pos + stride + planeRead) * mx.MidLeft) +
                 (p(pos + 4 + stride + planeRead) * (mx.Pixel + neun))) / CDbl(mx.Factor * Factor22)) + mx.Offset))

            If nPixel < 0 Then
                nPixel = 0
            End If
            If nPixel > 255 Then
                nPixel = 255
            End If

            p(pos + 4 + stride + planeWrite) = CByte(nPixel)

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            bmp.UnlockBits(bmData)

            p = Nothing

            Return True
        Catch
            Try
                bmp.UnlockBits(bmData)
            Catch

            End Try
        End Try

        Return False
    End Function
End Class
