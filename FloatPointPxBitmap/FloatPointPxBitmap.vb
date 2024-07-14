Option Strict On

Imports System.IO
Imports System.Drawing.Imaging
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Windows.Forms

'A representation of a bitmap (pixels) as an array with floatpoint precision,
'so you have to do only one pass on the real image when doing combined algorithms that need eg. rotated images as input
<Serializable()>
Public Class FloatPointPxBitmap
    Implements ICloneable, IDisposable
    Dim lockObject As New Object()

    Public Property Pixels() As FloatPointF(,)
        Get
            Return m_Pixels
        End Get
        Set(value As FloatPointF(,))
            m_Pixels = value
        End Set
    End Property
    Private m_Pixels As FloatPointF(,)
    Public Property OrigWidth() As Integer
        Get
            Return m_OrigWidth
        End Get
        Set(value As Integer)
            m_OrigWidth = value
        End Set
    End Property
    Private m_OrigWidth As Integer
    Public Property OrigHeight() As Integer
        Get
            Return m_OrigHeight
        End Get
        Set(value As Integer)
            m_OrigHeight = value
        End Set
    End Property
    Private m_OrigHeight As Integer

    Public Property RotatedWidth() As Integer
        Get
            Return m_RotatedWidth
        End Get
        Set(value As Integer)
            m_RotatedWidth = value
        End Set
    End Property
    Private m_RotatedWidth As Integer
    Public Property RotatedHeight() As Integer
        Get
            Return m_RotatedHeight
        End Get
        Set(value As Integer)
            m_RotatedHeight = value
        End Set
    End Property
    Private m_RotatedHeight As Integer

    Public Property DistX() As Single
        Get
            Return m_DistX
        End Get
        Set(value As Single)
            m_DistX = value
        End Set
    End Property
    Private m_DistX As Single
    Public Property DistY() As Single
        Get
            Return m_DistY
        End Get
        Set(value As Single)
            m_DistY = value
        End Set
    End Property
    Private m_DistY As Single

    Public Property RotatedFixPoint() As PointF
        Get
            Return m_RotatedFixPoint
        End Get
        Set(value As PointF)
            m_RotatedFixPoint = value
        End Set
    End Property
    Private m_RotatedFixPoint As PointF

    Public Delegate Sub ProgressPlusEventHandler(sender As Object, e As ProgressEventArgs)
    Public Event ProgressPlus As ProgressPlusEventHandler

    Public Sub New()
    End Sub

    Public Sub New(bmp As Bitmap)
        Dim w As Integer = bmp.Width
        Dim h As Integer = bmp.Height
        Try
            If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
                Me.Pixels = New FloatPointF(w - 1, h - 1) {}
            Else
                MessageBox.Show("Not enough Memory")
                Return
            End If

            Parallel.[For](0, h, Sub(y)
                                     For x As Integer = 0 To w - 1
                                         Me.Pixels(x, y).X = x
                                         Me.Pixels(x, y).Y = y
                                     Next
                                 End Sub)

            Me.OrigWidth = w
            Me.OrigHeight = h
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Public Sub New(fp As FloatPointF(,))
        Me.Pixels = fp

        Me.OrigWidth = fp.GetLength(0)
        Me.OrigHeight = fp.GetLength(1)
    End Sub

    Public Sub New(width As Integer, height As Integer)
        If AvailMem.AvailMem.checkAvailRam(width * height * 4L) Then
            Me.Pixels = New FloatPointF(width - 1, height - 1) {}
        Else
            MessageBox.Show("Not enough Memory")
            Return
        End If
        Me.OrigWidth = width
        Me.OrigHeight = height
    End Sub

#Region "not used here"
    'some methods to rotate the whole thing, not used in this demo
    Public Function Rotate(deg As Double) As Boolean
        Dim fp As FloatPointF(,) = Nothing

        Try
            Dim width As Integer = Me.Pixels.GetLength(0)
            Dim height As Integer = Me.Pixels.GetLength(1)

            Dim w As Double = -deg / 180.0 * Math.PI

            Dim newWidth As Double = Math.Abs(Math.Cos(w) * width) + Math.Abs(Math.Sin(w) * height)
            Dim newHeight As Double = Math.Abs(Math.Sin(w) * width) + Math.Abs(Math.Cos(w) * height)

            Dim w2 As Integer = CInt(Math.Ceiling(newWidth)) - 1
            Dim h2 As Integer = CInt(Math.Ceiling(newHeight)) - 1

            If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 4L) Then
                fp = New FloatPointF(w2, h2) {}
            End If

            Dim newW As Integer = InlineAssignHelper(Me.RotatedWidth, fp.GetLength(0))
            Dim newH As Integer = InlineAssignHelper(Me.RotatedHeight, fp.GetLength(1))

            Parallel.[For](0, newH, Sub(y)
                                        For x As Integer = 0 To newW - 1
                                            fp(x, y).X = Single.MinValue
                                            fp(x, y).Y = Single.MinValue
                                        Next
                                    End Sub)

            Dim newCX As Single = newW / 2.0F
            Dim newCY As Single = newH / 2.0F

            Dim distX As Single = InlineAssignHelper(Me.DistX, (newW - width) / 2.0F)
            Dim distY As Single = InlineAssignHelper(Me.DistY, (newH - height) / 2.0F)

            Parallel.[For](0, newH, Sub(y)
                                        'for (int y = 0; y < newH; y++)
                                        For x As Integer = 0 To newW - 1
                                            Dim realX As Double = x - newCX
                                            Dim realY As Double = y - newCY

                                            Dim newX As Double = Math.Cos(w) * realX - Math.Sin(w) * realY
                                            Dim newY As Double = Math.Sin(w) * realX + Math.Cos(w) * realY

                                            fp(x, y).X = CSng(newCX + newX - distX)
                                            fp(x, y).Y = CSng(newCY + newY - distY)

                                            If fp(x, y).X <= 1 AndAlso fp(x, y).X >= -1 Then
                                                fp(x, y).X = Fix(fp(x, y).X)
                                            End If
                                            '0;
                                            If fp(x, y).Y <= 1 AndAlso fp(x, y).Y >= -1 Then
                                                fp(x, y).Y = Fix(fp(x, y).Y)
                                            End If
                                            '0;
                                            If fp(x, y).X >= width - 1 AndAlso fp(x, y).X <= width + 1 Then
                                                fp(x, y).X = Fix(fp(x, y).X)
                                            End If
                                            'width - 1;
                                            If fp(x, y).Y >= height - 1 AndAlso fp(x, y).Y <= height + 1 Then
                                                fp(x, y).Y = Fix(fp(x, y).Y)
                                                'height - 1;
                                            End If
                                        Next
                                    End Sub)

            OffsetArray(Me.Pixels, fp, True)
        Catch
            fp = Nothing
            Return False
        End Try

        Me.Pixels = fp

        Return True
    End Function

    Public Function RotateAndSetAFixPointOffset(deg As Double, fixPoint As Point, pt3Angle As Double) As Boolean
        Dim fp As FloatPointF(,) = Nothing

        Try
            Dim width As Integer = Me.Pixels.GetLength(0)
            Dim height As Integer = Me.Pixels.GetLength(1)

            Dim w As Double = -deg / 180.0 * Math.PI

            Dim newWidth As Double = Math.Abs(Math.Cos(w) * width) + Math.Abs(Math.Sin(w) * height)
            Dim newHeight As Double = Math.Abs(Math.Sin(w) * width) + Math.Abs(Math.Cos(w) * height)

            Dim w2 As Integer = CInt(Math.Ceiling(newWidth)) - 1
            Dim h2 As Integer = CInt(Math.Ceiling(newHeight)) - 1

            If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 4L) Then
                fp = New FloatPointF(CInt(Math.Ceiling(newWidth)) - 1, CInt(Math.Ceiling(newHeight)) - 1) {}
            End If

            Dim newW As Integer = InlineAssignHelper(Me.RotatedWidth, fp.GetLength(0))
            Dim newH As Integer = InlineAssignHelper(Me.RotatedHeight, fp.GetLength(1))

            Parallel.[For](0, newH, Sub(y)
                                        For x As Integer = 0 To newW - 1
                                            fp(x, y).X = Single.MinValue
                                            fp(x, y).Y = Single.MinValue
                                        Next
                                    End Sub)

            Dim newCX As Single = newW / 2.0F
            Dim newCY As Single = newH / 2.0F

            Dim distX As Single = InlineAssignHelper(Me.DistX, (newW - width) / 2.0F)
            Dim distY As Single = InlineAssignHelper(Me.DistY, (newH - height) / 2.0F)

            Parallel.[For](0, newH, Sub(y)
                                        'for (int y = 0; y < newH; y++)
                                        For x As Integer = 0 To newW - 1
                                            Dim realX As Double = x - newCX
                                            Dim realY As Double = y - newCY

                                            Dim newX As Double = Math.Cos(w) * realX - Math.Sin(w) * realY
                                            Dim newY As Double = Math.Sin(w) * realX + Math.Cos(w) * realY

                                            fp(x, y).X = CSng(newCX + newX - distX)
                                            fp(x, y).Y = CSng(newCY + newY - distY)

                                            If fp(x, y).X <= 1 AndAlso fp(x, y).X >= -1 Then
                                                fp(x, y).X = Fix(fp(x, y).X)
                                            End If
                                            '0;
                                            If fp(x, y).Y <= 1 AndAlso fp(x, y).Y >= -1 Then
                                                fp(x, y).Y = Fix(fp(x, y).Y)
                                            End If
                                            '0;
                                            If fp(x, y).X >= width - 1 AndAlso fp(x, y).X <= width + 1 Then
                                                fp(x, y).X = Fix(fp(x, y).X)
                                            End If
                                            'width - 1;
                                            If fp(x, y).Y >= height - 1 AndAlso fp(x, y).Y <= height + 1 Then
                                                fp(x, y).Y = Fix(fp(x, y).Y)
                                            End If
                                            'height - 1;
                                            'maybe use the angle evaluated while cropping; I hadnt got enough time to test yet... (set in ptp: pt3Angle)
                                            If x = fixPoint.X AndAlso y = fixPoint.Y Then
                                                Dim cX As Double = Me.Pixels.GetLength(0) / 2.0F
                                                Dim cY As Double = Me.Pixels.GetLength(1) / 2.0F
                                                Dim rX As Double = x - cX
                                                Dim rY As Double = y - cY
                                                Dim nX As Double = Math.Cos(-w) * rX - Math.Sin(-w) * rY
                                                Dim nY As Double = Math.Sin(-w) * rX + Math.Cos(-w) * rY
                                                Me.RotatedFixPoint = New PointF(CSng(newCX + nX), CSng(newCY + nY))
                                            End If
                                        Next
                                    End Sub)

            OffsetArray(Me.Pixels, fp, True)
        Catch
            fp = Nothing
            Return False
        End Try

        Me.Pixels = fp

        Return True
    End Function

    Public Function Rotate(deg As Double, newSize As Size) As Boolean
        Dim fp As FloatPointF(,) = Nothing

        Try
            Dim width As Integer = Me.Pixels.GetLength(0)
            Dim height As Integer = Me.Pixels.GetLength(1)

            Dim w As Double = -deg / 180.0 * Math.PI

            Dim newWidth As Double = newSize.Width
            Dim newHeight As Double = newSize.Height

            Dim w2 As Integer = CInt(Math.Ceiling(newWidth)) - 1
            Dim h2 As Integer = CInt(Math.Ceiling(newHeight)) - 1

            If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 4L) Then
                fp = New FloatPointF(CInt(Math.Ceiling(newWidth)) - 1, CInt(Math.Ceiling(newHeight)) - 1) {}
            End If

            Dim newW As Integer = InlineAssignHelper(Me.RotatedWidth, fp.GetLength(0))
            Dim newH As Integer = InlineAssignHelper(Me.RotatedHeight, fp.GetLength(1))

            Parallel.[For](0, newH, Sub(y)
                                        For x As Integer = 0 To newW - 1
                                            fp(x, y).X = Single.MinValue
                                            fp(x, y).Y = Single.MinValue
                                        Next
                                    End Sub)

            Dim newCX As Single = newW / 2.0F
            Dim newCY As Single = newH / 2.0F

            Dim distX As Single = InlineAssignHelper(Me.DistX, (newW - width) / 2.0F)
            Dim distY As Single = InlineAssignHelper(Me.DistY, (newH - height) / 2.0F)

            Parallel.[For](0, newH, Sub(y)
                                        'for (int y = 0; y < newH; y++)
                                        For x As Integer = 0 To newW - 1
                                            Dim realX As Double = x - newCX
                                            Dim realY As Double = y - newCY

                                            Dim newX As Double = Math.Cos(w) * realX - Math.Sin(w) * realY
                                            Dim newY As Double = Math.Sin(w) * realX + Math.Cos(w) * realY

                                            fp(x, y).X = CSng(newCX + newX - distX)
                                            fp(x, y).Y = CSng(newCY + newY - distY)
                                        Next
                                    End Sub)

            OffsetArray(Me.Pixels, fp, True)
        Catch
            fp = Nothing
            Return False
        End Try

        Me.Pixels = fp

        Return True
    End Function

    Public Function RotateSameSize(deg As Double) As Boolean
        Dim fp As FloatPointF(,) = Nothing

        Try
            Dim width As Integer = InlineAssignHelper(Me.RotatedWidth, Me.Pixels.GetLength(0))
            Dim height As Integer = InlineAssignHelper(Me.RotatedHeight, Me.Pixels.GetLength(1))

            Dim w As Double = -deg / 180.0 * Math.PI

            If AvailMem.AvailMem.checkAvailRam(width * height * 4L) Then
                fp = New FloatPointF(width - 1, height - 1) {}
            End If

            Parallel.[For](0, height, Sub(y)
                                          For x As Integer = 0 To width - 1
                                              fp(x, y).X = Single.MinValue
                                              fp(x, y).Y = Single.MinValue
                                          Next
                                      End Sub)

            Dim newCX As Single = width / 2.0F
            Dim newCY As Single = height / 2.0F

            Parallel.[For](0, height, Sub(y)
                                          'for (int y = 0; y < height; y++)
                                          For x As Integer = 0 To width - 1
                                              Dim realX As Double = x - newCX
                                              Dim realY As Double = y - newCY

                                              Dim newX As Double = Math.Cos(w) * realX - Math.Sin(w) * realY
                                              Dim newY As Double = Math.Sin(w) * realX + Math.Cos(w) * realY

                                              fp(x, y).X = CSng(newCX + newX)
                                              fp(x, y).Y = CSng(newCY + newY)
                                          Next
                                      End Sub)

            OffsetArray(Me.Pixels, fp, True)
        Catch
            fp = Nothing
            Return False
        End Try

        Me.Pixels = fp

        Return True
    End Function

    Public Sub OffsetArray(pixels As FloatPointF(,), fp As FloatPointF(,), setPxOutTransp As Boolean)
        Dim nWidth As Integer = pixels.GetLength(0)
        Dim nHeight As Integer = pixels.GetLength(1)

        Dim nWNew As Integer = fp.GetLength(0)
        Dim nHNew As Integer = fp.GetLength(1)

        Dim distX As Double = (nWNew - nWidth) / 2.0
        Dim distY As Double = (nHNew - nHeight) / 2.0

        Dim strtX As Integer = 0
        ' (int)Math.Max(distX, 0);
        Dim endX As Integer = nWNew
        'Math.Min(nWNew - (int)distX, nWNew);
        Dim strtY As Integer = 0
        ' (int)Math.Max(distY, 0);
        Dim endY As Integer = nHNew
        ' Math.Min(nHNew - (int)distY, nHNew);
        Parallel.[For](strtY, endY, Sub(y)
                                        For x As Integer = strtX To endX - 1
                                            Dim ceil_x As Integer, ceil_y As Integer
                                            Dim xOffset As Double = fp(x, y).X
                                            Dim yOffset As Double = fp(x, y).Y

                                            If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then
                                                Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                If floor_x = nWidth - 1 Then
                                                    floor_x -= 1
                                                End If

                                                If floor_y = nHeight - 1 Then
                                                    floor_y -= 1
                                                End If

                                                If floor_x <> Single.MinValue Then
                                                    ceil_x = floor_x + 1
                                                Else
                                                    ceil_x = floor_x
                                                End If

                                                If floor_y <> Single.MinValue Then
                                                    ceil_y = floor_y + 1
                                                Else
                                                    ceil_y = floor_y
                                                End If

                                                Dim fraction_x As Double = xOffset - floor_x
                                                Dim fraction_y As Double = yOffset - floor_y
                                                Dim one_minus_x As Double = 1.0 - fraction_x
                                                Dim one_minus_y As Double = 1.0 - fraction_y

                                                If floor_y >= 0 AndAlso ceil_y < nHeight AndAlso floor_x >= 0 AndAlso ceil_x < nWidth Then
                                                    Dim x1 As Double = fraction_x * pixels(ceil_x, floor_y).X + one_minus_x * pixels(floor_x, floor_y).X
                                                    Dim x2 As Double = fraction_x * pixels(ceil_x, ceil_y).X + one_minus_x * pixels(floor_x, ceil_y).X
                                                    Dim xx As Double = fraction_y * x2 + one_minus_y * x1

                                                    Dim y1 As Double = fraction_x * pixels(ceil_x, floor_y).Y + one_minus_x * pixels(floor_x, floor_y).Y
                                                    Dim y2 As Double = fraction_x * pixels(ceil_x, ceil_y).Y + one_minus_x * pixels(floor_x, ceil_y).Y
                                                    Dim yy As Double = fraction_y * y2 + one_minus_y * y1

                                                    fp(x, y).X = CSng(xx)
                                                    fp(x, y).Y = CSng(yy)
                                                ElseIf setPxOutTransp Then
                                                    fp(x, y).X = InlineAssignHelper(fp(x, y).Y, Single.MinValue)
                                                End If
                                            End If
                                        Next
                                    End Sub)
    End Sub
#End Region

    'methods for bicubiq resampling, see: http://de.wikipedia.org/wiki/Mitchell-Netravali-Filter
    'B = 0, C = 0.5, d = fraction
    Public Function ResampleBiQCR(P0 As Double, P1 As Double, P2 As Double, P3 As Double, d As Double) As Double
        Return (((-0.5 * P0) + (1.5 * P1) - (1.5 * P2) + (0.5 * P3)) * d * d * d) +
            ((P0 - (2.5 * P1) + (2 * P2) - (0.5 * P3)) * d * d) +
            (((-0.5 * P0) + (0.5 * P2)) * d) + P1
    End Function

    'B = 0, C = 1, d = fraction //most sharpness, possible ringing artifacts
    Public Function ResampleBiQ(P0 As Double, P1 As Double, P2 As Double, P3 As Double, d As Double) As Double
        Return (((-P0 + P1) - P2 + P3) * d * d * d) +
            (((2 * P0) - (2 * P1) + P2 - P3) * d * d) +
            ((-P0 + P2) * d) + P1
    End Function

    'general
    Public Function ResampleBiQ(B As Double, C As Double, P0 As Double, P1 As Double, P2 As Double, P3 As Double, d As Double) As Double
        Return (((((-1.0 / 6.0 * B) - C) * P0) + (((-3.0 / 2.0 * B) - C + 2) * P1) + (((3.0 / 2.0 * B) + C - 2) * P2) + (((1.0 / 6.0 * B) + C) * P3)) * d * d * d) +
            (((((1.0 / 2.0 * B) + (2 * C)) * P0) + (((2 * B) + C - 3) * P1) + (((-5.0 / 2.0 * B) - (2 * C) + 3) * P2) - (C * P3)) * d * d) +
            (((((-1.0 / 2.0 * B) - C) * P0) + (((1.0 / 2.0 * B) + C) * P2)) * d) + ((1.0 / 6.0 * B) * P0) + (((-1.0 / 3.0 * B) + 1) * P1) + ((1.0 / 6.0 * B) * P2)
    End Function

    Public Function GetBmpResult(bmp As Bitmap, antiAliasEdges As Boolean, alphaThreshold As Integer, B As Double, C As Double) As Bitmap
        If AvailMem.AvailMem.checkAvailRam(Me.Pixels.GetLength(0) * Me.Pixels.GetLength(1) * 12L) Then
            Dim bOut As Bitmap = Nothing
            Dim bmD As BitmapData = Nothing
            Dim bmSrc As BitmapData = Nothing

            Dim fp As FloatPointF(,) = Me.Pixels

            Try
                bOut = New Bitmap(Me.Pixels.GetLength(0), Me.Pixels.GetLength(1))
                bmD = bOut.LockBits(New Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
                bmSrc = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)

                Dim w As Integer = bOut.Width
                Dim h As Integer = bOut.Height

                Dim nWidth As Integer = bmp.Width
                Dim nHeight As Integer = bmp.Height

                Dim stride As Integer = bmD.Stride
                Dim scan0 As IntPtr = bmD.Scan0
                Dim strideSrc As Integer = bmSrc.Stride
                Dim srcScan0 As IntPtr = bmSrc.Scan0

                Dim buffer((bmD.Stride * bmD.Height) - 1) As Byte
                Marshal.Copy(bmD.Scan0, buffer, 0, buffer.Length)

                Dim bufferSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, bufferSrc, 0, bufferSrc.Length)

                Parallel.[For](0, h, Sub(y)
                                         For x As Integer = 0 To w - 1
                                             Dim ceil_x As Integer, ceil_y As Integer
                                             Dim p1 As Double, p2 As Double

                                             Dim xOffset As Double = fp(x, y).X
                                             Dim yOffset As Double = fp(x, y).Y

                                             If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso
                                             Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso
                                             Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso
                                             Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then
                                                 ' Setup

                                                 Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                 Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                 If floor_x = w - 1 Then
                                                     floor_x -= 1
                                                 End If

                                                 If floor_y = h - 1 Then
                                                     floor_y -= 1
                                                 End If

                                                 Dim fraction_x As Double = xOffset - floor_x
                                                 Dim fraction_y As Double = yOffset - floor_y

                                                 Dim dOut As Double = 0.0

                                                 If floor_y >= 1 AndAlso floor_y < nHeight - 2 AndAlso floor_x >= 1 AndAlso floor_x < nWidth - 2 Then
                                                     Dim dTmp(3) As Double

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 1),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 1),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 1),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 1), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 1),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 1),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 1),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 1), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 2),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 2),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 2),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 2), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 2),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 2),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 2),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 2), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 3),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 3),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 3),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 3), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 3),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 3),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 3),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 3), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                 ElseIf antiAliasEdges AndAlso (fp(x, y).X <> Single.MinValue OrElse fp(x, y).Y <> Single.MinValue) Then
                                                     Dim one_minus_x As Double = 1.0 - fraction_x
                                                     Dim one_minus_y As Double = 1.0 - fraction_y

                                                     If floor_x <> Single.MinValue Then
                                                         ceil_x = floor_x + 1
                                                     Else
                                                         ceil_x = floor_x
                                                     End If

                                                     If floor_y <> Single.MinValue Then
                                                         ceil_y = floor_y + 1
                                                     Else
                                                         ceil_y = floor_y
                                                     End If

                                                     If floor_y >= 0 AndAlso ceil_y < nHeight AndAlso floor_x >= 0 AndAlso ceil_x < nWidth Then
                                                         ' Blue
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Green
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Red
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Alpha
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 3))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)

                                                         If dOut > alphaThreshold Then
                                                             buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                         End If
                                                     End If
                                                     If floor_x <= nWidth + 1 AndAlso floor_y <= nHeight + 1 AndAlso floor_x >= -1 AndAlso floor_y >= -1 Then
                                                         'right
                                                         If fp(x, y).X > nWidth AndAlso fp(x, y).X >= nWidth + 1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = fraction_y * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + one_minus_y * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'left
                                                         If fp(x, y).X < 0.0 AndAlso fp(x, y).X >= -1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).X) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'top
                                                         If fp(x, y).Y < 0.0 AndAlso fp(x, y).Y >= -1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).Y) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'bottom
                                                         If fp(x, y).Y > nHeight AndAlso fp(x, y).Y <= nHeight + 1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight + 1.0) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                     End If

                                                     'if (yOffset == float.MinValue || xOffset == float.MinValue)
                                                     '{
                                                     '    buffer[x * 4 + y * stride] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 1] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 2] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 3] = (byte)0;
                                                     '}
                                                 End If
                                             End If

                                             If yOffset = Single.MinValue OrElse xOffset = Single.MinValue Then
                                                 buffer(x * 4 + y * stride) = CByte(0)
                                                 buffer(x * 4 + y * stride + 1) = CByte(0)
                                                 buffer(x * 4 + y * stride + 2) = CByte(0)
                                                 buffer(x * 4 + y * stride + 3) = CByte(0)
                                             End If
                                         Next
                                     End Sub)

                Marshal.Copy(buffer, 0, bmD.Scan0, buffer.Length)
                bOut.UnlockBits(bmD)
                bmp.UnlockBits(bmSrc)

                buffer = Nothing
                bufferSrc = Nothing
            Catch
                Try
                    bOut.UnlockBits(bmD)

                Catch
                End Try
                Try
                    bmp.UnlockBits(bmSrc)

                Catch
                End Try
                If bOut IsNot Nothing Then
                    bOut.Dispose()
                    bOut = Nothing
                End If
            End Try

            Return bOut
        End If
        Return Nothing
    End Function

    'uses the already resampled Array (Pixels)
    'this returns a bitmap containing all offsets done in the Me.Pixels array by fetching the values from the original image, which should be passed as parameter bmp
    Public Function GetBmpResult(bmp As Bitmap, antiAliasEdges As Boolean, alphaThreshold As Integer, resampleFactor As Single, B As Double, C As Double) As Bitmap
        Dim w2 As Integer = CInt(Math.Ceiling(bmp.Width * resampleFactor))
        Dim h2 As Integer = CInt(Math.Ceiling(bmp.Height * resampleFactor))
        If AvailMem.AvailMem.checkAvailRam(Me.Pixels.GetLength(0) * Me.Pixels.GetLength(1) * 8L + w2 * h2 * 8L) Then
            Dim bOut As Bitmap = Nothing
            Dim bmD As BitmapData = Nothing
            Dim bmSrc As BitmapData = Nothing
            Dim bSrc As Bitmap = Nothing

            Dim fp As FloatPointF(,) = Me.Pixels

            Try
                'setup, create the output image, lock the bits and copy the buffers
                bOut = New Bitmap(Me.Pixels.GetLength(0), Me.Pixels.GetLength(1))
                bmD = bOut.LockBits(New Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
                bSrc = New Bitmap(w2, h2)
                Using g As Graphics = Graphics.FromImage(bSrc)
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    g.DrawImage(bmp, 0, 0, bmp.Width * resampleFactor, bmp.Height * resampleFactor)
                End Using
                bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)

                Dim w As Integer = bOut.Width
                Dim h As Integer = bOut.Height

                Dim nWidth As Integer = bSrc.Width
                Dim nHeight As Integer = bSrc.Height

                Dim stride As Integer = bmD.Stride
                Dim scan0 As IntPtr = bmD.Scan0
                Dim strideSrc As Integer = bmSrc.Stride
                Dim srcScan0 As IntPtr = bmSrc.Scan0

                Dim buffer((bmD.Stride * bmD.Height) - 1) As Byte
                Marshal.Copy(bmD.Scan0, buffer, 0, buffer.Length)

                Dim bufferSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, bufferSrc, 0, bufferSrc.Length)

                'loop over the target image and fetch the color values from the original
                Parallel.[For](0, h, Sub(y)
                                         For x As Integer = 0 To w - 1
                                             Dim ceil_x As Integer, ceil_y As Integer
                                             Dim p1 As Double, p2 As Double

                                             Dim xOffset As Double = fp(x, y).X
                                             Dim yOffset As Double = fp(x, y).Y

                                             If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso
                                             Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso
                                             Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso
                                             Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then
                                                 ' Setup

                                                 Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                 Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                 If floor_x = w - 1 Then
                                                     floor_x -= 1
                                                 End If

                                                 If floor_y = h - 1 Then
                                                     floor_y -= 1
                                                 End If

                                                 Dim fraction_x As Double = xOffset - floor_x
                                                 Dim fraction_y As Double = yOffset - floor_y

                                                 Dim dOut As Double = 0.0

                                                 'are we in the range for resampling bicubiq? 4 pixels needed for each row/col; left/up of florXY min 1px, right/down min 2px
                                                 If floor_y >= 1 AndAlso floor_y < nHeight - 2 AndAlso floor_x >= 1 AndAlso floor_x < nWidth - 2 Then
                                                     Dim dTmp(3) As Double

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4),
                                                                bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 1),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 1),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 1),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 1), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 1),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 1),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 1),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 1), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 2),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 2),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 2),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 2), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 2),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 2),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 2),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 2), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 3),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 3),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 3),
                                                             bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 3), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 3),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 3),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 3),
                                                            bufferSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 3), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                 ElseIf antiAliasEdges AndAlso (fp(x, y).X <> Single.MinValue OrElse fp(x, y).Y <> Single.MinValue) Then
                                                     Dim one_minus_x As Double = 1.0 - fraction_x
                                                     Dim one_minus_y As Double = 1.0 - fraction_y

                                                     If floor_x <> Single.MinValue Then
                                                         ceil_x = floor_x + 1
                                                     Else
                                                         ceil_x = floor_x
                                                     End If

                                                     If floor_y <> Single.MinValue Then
                                                         ceil_y = floor_y + 1
                                                     Else
                                                         ceil_y = floor_y
                                                     End If

                                                     'resample the edges bilinear (where we cannot get all needed coords for bicubiq resampling)
                                                     If floor_y >= 0 AndAlso ceil_y < nHeight AndAlso floor_x >= 0 AndAlso ceil_x < nWidth Then
                                                         ' Blue
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Green
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Red
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Alpha
                                                         p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3))

                                                         p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 3))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)

                                                         If dOut > alphaThreshold Then
                                                             buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                         End If
                                                     End If
                                                     'this is an Antialiasing/fadint part for the edges (where we get an offset just between the edge pixel and 1 pixel outside)
                                                     If floor_x <= nWidth + 1 AndAlso floor_y <= nHeight + 1 AndAlso floor_x >= -1 AndAlso floor_y >= -1 Then
                                                         'right
                                                         If fp(x, y).X > nWidth AndAlso fp(x, y).X >= nWidth + 1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = fraction_y * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + one_minus_y * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'left
                                                         If fp(x, y).X < 0.0 AndAlso fp(x, y).X >= -1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).X) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'top
                                                         If fp(x, y).Y < 0.0 AndAlso fp(x, y).Y >= -1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).Y) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'bottom
                                                         If fp(x, y).Y > nHeight AndAlso fp(x, y).Y <= nHeight + 1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight + 1.0) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                     End If
                                                 End If
                                             End If

                                             'set pixels blank that are marked by being set to Single.MinValue
                                             If yOffset = Single.MinValue OrElse xOffset = Single.MinValue Then
                                                 buffer(x * 4 + y * stride) = CByte(0)
                                                 buffer(x * 4 + y * stride + 1) = CByte(0)
                                                 buffer(x * 4 + y * stride + 2) = CByte(0)
                                                 buffer(x * 4 + y * stride + 3) = CByte(0)
                                             End If
                                         Next
                                     End Sub)

                Marshal.Copy(buffer, 0, bmD.Scan0, buffer.Length)
                bOut.UnlockBits(bmD)
                bSrc.UnlockBits(bmSrc)
                bSrc.Dispose()

                buffer = Nothing
                bufferSrc = Nothing
            Catch
                Try
                    bOut.UnlockBits(bmD)

                Catch
                End Try
                Try
                    bSrc.UnlockBits(bmSrc)

                Catch
                End Try
                If bOut IsNot Nothing Then
                    bOut.Dispose()
                    bOut = Nothing
                End If
                If bSrc IsNot Nothing Then
                    bSrc.Dispose()
                End If
            End Try

            Return bOut
        End If
        Return Nothing
    End Function

    'uses the already resampled Array (Pixels)
    Public Function GetBmpResult(bmp As Bitmap, antiAliasEdges As Boolean, alphaThreshold As Integer, resampleFactor As Single, pe As ProgressEventArgs,
                                 B As Double, C As Double) As Bitmap
        Dim w2 As Integer = CInt(Math.Ceiling(bmp.Width * resampleFactor))
        Dim h2 As Integer = CInt(Math.Ceiling(bmp.Height * resampleFactor))
        If AvailMem.AvailMem.checkAvailRam(Me.Pixels.GetLength(0) * Me.Pixels.GetLength(1) * 8L + w2 * h2 * 8L) Then
            Dim bOut As Bitmap = Nothing
            Dim bmD As BitmapData = Nothing
            Dim bmSrc As BitmapData = Nothing
            Dim bSrc As Bitmap = Nothing

            Dim fp As FloatPointF(,) = Me.Pixels

            Try
                bOut = New Bitmap(Me.Pixels.GetLength(0), Me.Pixels.GetLength(1))
                bmD = bOut.LockBits(New Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
                bSrc = New Bitmap(w2, h2)
                Using g As Graphics = Graphics.FromImage(bSrc)
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    g.DrawImage(bmp, 0, 0, bmp.Width * resampleFactor, bmp.Height * resampleFactor)
                End Using
                bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)

                Dim w As Integer = bOut.Width
                Dim h As Integer = bOut.Height

                Dim nWidth As Integer = bSrc.Width
                Dim nHeight As Integer = bSrc.Height

                Dim stride As Integer = bmD.Stride
                Dim scan0 As IntPtr = bmD.Scan0
                Dim strideSrc As Integer = bmSrc.Stride
                Dim srcScan0 As IntPtr = bmSrc.Scan0

                Dim p((bmD.Stride * bmD.Height) - 1) As Byte
                Marshal.Copy(bmD.Scan0, p, 0, p.Length)

                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

                Parallel.[For](0, h, Sub(y)
                                         For x As Integer = 0 To w - 1
                                             Dim ceil_x As Integer, ceil_y As Integer
                                             Dim p1 As Double, p2 As Double

                                             Dim xOffset As Double = fp(x, y).X
                                             Dim yOffset As Double = fp(x, y).Y

                                             If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then
                                                 ' Setup

                                                 Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                 Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                 If floor_x = w - 1 Then
                                                     floor_x -= 1
                                                 End If

                                                 If floor_y = h - 1 Then
                                                     floor_y -= 1
                                                 End If

                                                 Dim fraction_x As Double = xOffset - floor_x
                                                 Dim fraction_y As Double = yOffset - floor_y

                                                 Dim dOut As Double = 0.0

                                                 If floor_y >= 1 AndAlso floor_y < nHeight - 2 AndAlso floor_x >= 1 AndAlso floor_x < nWidth - 2 Then
                                                     Dim dTmp(3) As Double

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4),
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x) * 4),
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4),
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4),
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x) * 4),
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4),
                                                                pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     p(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 1),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 1),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 1),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 1), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 1),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 1),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 1),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 1), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     p(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 2),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 2),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 2),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 2), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 2),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 2),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 2),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 2), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     p(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     For yy As Integer = -1 To 2
                                                         If B = 0.0 AndAlso C = 1.0 Then
                                                             dTmp(yy + 1) = ResampleBiQ(
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 3),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 3),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 3),
                                                             pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 3), fraction_x)
                                                         Else
                                                             dTmp(yy + 1) = ResampleBiQ(B, C,
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x - 1) * 4 + 3),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x) * 4 + 3),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x + 1) * 4 + 3),
                                                            pSrc((floor_y + yy) * strideSrc + (floor_x + 2) * 4 + 3), fraction_x)
                                                         End If
                                                     Next

                                                     If B = 0.0 AndAlso C = 1.0 Then
                                                         dOut = ResampleBiQ(dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     Else
                                                         dOut = ResampleBiQ(B, C, dTmp(0), dTmp(1), dTmp(2), dTmp(3), fraction_y)
                                                     End If

                                                     p(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                 ElseIf antiAliasEdges AndAlso (fp(x, y).X <> Single.MinValue OrElse fp(x, y).Y <> Single.MinValue) Then
                                                     Dim one_minus_x As Double = 1.0 - fraction_x
                                                     Dim one_minus_y As Double = 1.0 - fraction_y

                                                     If floor_x <> Single.MinValue Then
                                                         ceil_x = floor_x + 1
                                                     Else
                                                         ceil_x = floor_x
                                                     End If

                                                     If floor_y <> Single.MinValue Then
                                                         ceil_y = floor_y + 1
                                                     Else
                                                         ceil_y = floor_y
                                                     End If

                                                     If floor_y >= 0 AndAlso ceil_y < nHeight AndAlso floor_x >= 0 AndAlso ceil_x < nWidth Then
                                                         ' Blue
                                                         p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4))

                                                         p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         p(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Green
                                                         p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                         p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         p(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Red
                                                         p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                         p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)
                                                         p(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                         ' Alpha
                                                         p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 3))

                                                         p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 3))

                                                         dOut = (one_minus_y * p1 + fraction_y * p2)

                                                         If dOut > alphaThreshold Then
                                                             p(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                         End If
                                                     End If
                                                     If floor_x <= nWidth + 1 AndAlso floor_y <= nHeight + 1 AndAlso floor_x >= -1 AndAlso floor_y >= -1 Then
                                                         'right
                                                         If fp(x, y).X > nWidth AndAlso fp(x, y).X >= nWidth + 1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = fraction_y * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + one_minus_y * CDbl(pSrc(floor_y * strideSrc + floor_x * 4 + 3))

                                                                 dOut = p1
                                                                 p(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'left
                                                         If fp(x, y).X < 0.0 AndAlso fp(x, y).X >= -1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(pSrc(floor_y * strideSrc + ceil_x * 4 + 3)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).X) * p1)
                                                                 p(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'top
                                                         If fp(x, y).Y < 0.0 AndAlso fp(x, y).Y >= -1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).Y) * p1)
                                                                 p(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'bottom
                                                         If fp(x, y).Y > nHeight AndAlso fp(x, y).Y <= nHeight + 1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight + 1.0) AndAlso (ceil_x < nWidth) AndAlso (ceil_y * strideSrc + ceil_x * 4 < pSrc.Length) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 p(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(pSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(pSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = p1
                                                                 p(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                     End If
                                                 End If
                                             End If

                                             If yOffset = Single.MinValue OrElse xOffset = Single.MinValue Then
                                                 p(x * 4 + y * stride) = CByte(0)
                                                 p(x * 4 + y * stride + 1) = CByte(0)
                                                 p(x * 4 + y * stride + 2) = CByte(0)
                                                 p(x * 4 + y * stride + 3) = CByte(0)
                                             End If
                                         Next

                                         'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
                                         'If handler IsNot Nothing Then
                                         SyncLock Me.lockObject
                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                 pe.CurrentProgress += 1
                                             End If
                                             Try
                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                     RaiseEvent ProgressPlus(Me, pe)
                                                 End If

                                             Catch
                                             End Try
                                         End SyncLock
                                         'End If

                                     End Sub)

                Marshal.Copy(p, 0, bmD.Scan0, p.Length)
                bOut.UnlockBits(bmD)
                bSrc.UnlockBits(bmSrc)
                bSrc.Dispose()

                p = Nothing
                pSrc = Nothing
            Catch
                Try
                    bOut.UnlockBits(bmD)

                Catch
                End Try
                Try
                    bSrc.UnlockBits(bmSrc)

                Catch
                End Try
                If bOut IsNot Nothing Then
                    bOut.Dispose()
                    bOut = Nothing
                End If
                If bSrc IsNot Nothing Then
                    bSrc.Dispose()
                End If
            End Try

            Return bOut
        End If
        Return Nothing
    End Function

    'uses the already resampled Array (Pixels)
    Public Function GetBmpResultFastFast(bmp As Bitmap, resampleFactor As Single) As Bitmap
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 16L) Then
            ', ProgressEventArgs pe
            Dim bOut As Bitmap = Nothing
            Dim bmD As BitmapData = Nothing
            Dim bmSrc As BitmapData = Nothing
            Dim bOut2 As Bitmap = Nothing

            Dim fp As FloatPointF(,) = Me.Pixels

            Try
                bOut = New Bitmap(bmp.Width, bmp.Height)
                bmD = bOut.LockBits(New Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
                bmSrc = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)

                Dim w As Integer = bOut.Width
                Dim h As Integer = bOut.Height

                Dim nWidth As Integer = bmp.Width
                Dim nHeight As Integer = bmp.Height

                Dim stride As Integer = bmD.Stride
                Dim scan0 As IntPtr = bmD.Scan0
                Dim strideSrc As Integer = bmSrc.Stride
                Dim srcScan0 As IntPtr = bmSrc.Scan0

                Dim p((bmD.Stride * bmD.Height) - 1) As Byte
                Marshal.Copy(bmD.Scan0, p, 0, p.Length)

                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

                Parallel.[For](0, h, Sub(y)
                                         For x As Integer = 0 To w - 1
                                             Dim xx As Integer = CInt(x * resampleFactor)
                                             Dim yy As Integer = CInt(y * resampleFactor)
                                             If fp(xx, yy).X > Int32.MinValue AndAlso fp(xx, yy).Y > Int32.MinValue Then
                                                 Dim xOffset As Integer = CInt(fp(xx, yy).X / resampleFactor)
                                                 Dim yOffset As Integer = CInt(fp(xx, yy).Y / resampleFactor)

                                                 If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) Then
                                                     If yOffset >= 0 AndAlso yOffset < nHeight AndAlso xOffset >= 0 AndAlso xOffset < nWidth Then
                                                         Dim f As Integer = x * 4 + y * stride
                                                         Dim ff As Integer = yOffset * strideSrc + xOffset * 4
                                                         p(f) = pSrc(ff)
                                                         p(f + 1) = pSrc(ff + 1)
                                                         p(f + 2) = pSrc(ff + 2)
                                                         p(f + 3) = pSrc(ff + 3)
                                                     End If
                                                 End If

                                                 If fp(x, y).Y = Single.MinValue OrElse fp(x, y).X = Single.MinValue Then
                                                     Dim f As Integer = x * 4 + y * stride
                                                     p(f) = CByte(0)
                                                     p(f + 1) = CByte(0)
                                                     p(f + 2) = CByte(0)
                                                     p(f + 3) = CByte(0)
                                                 End If

                                                 'ProgressPlusEventHandler handler = this.ProgressPlus;
                                                 'if (handler != null)
                                                 '{
                                                 '    lock (this.lockObject)
                                                 '    {
                                                 '        if (pe.ImgWidthHeight < Int32.MaxValue)
                                                 '            pe.CurrentProgress++;
                                                 '        try
                                                 '        {
                                                 '            if ((int)pe.CurrentProgress % pe.PrgInterval == 0)
                                                 '                ProgressPlus(pe);
                                                 '        }
                                                 '        catch
                                                 '        {

                                                 '        }
                                                 '    }
                                                 '}
                                             End If
                                         Next

                                     End Sub)

                Marshal.Copy(p, 0, bmD.Scan0, p.Length)
                bOut.UnlockBits(bmD)
                bmp.UnlockBits(bmSrc)

                bOut2 = New Bitmap(Me.Pixels.GetLength(0), Me.Pixels.GetLength(1))
                Using g As Graphics = Graphics.FromImage(bOut2)
                    g.DrawImage(bOut, 0, 0, bOut2.Width, bOut2.Height)
                End Using

                bOut.Dispose()

                p = Nothing
                pSrc = Nothing
            Catch
                Try
                    bOut.UnlockBits(bmD)

                Catch
                End Try
                Try
                    bmp.UnlockBits(bmSrc)

                Catch
                End Try
                If bOut IsNot Nothing Then
                    bOut.Dispose()
                    bOut = Nothing
                End If
                'if (bSrc != null)
                '    bSrc.Dispose();
                If bOut2 IsNot Nothing Then
                    bOut2.Dispose()
                    bOut2 = Nothing
                End If
            End Try

            Return bOut2
        End If
        Return Nothing
    End Function

    'bili versions
    Public Function GetBmpResultBiLi(bmp As Bitmap, antiAliasEdges As Boolean, alphaThreshold As Integer) As Bitmap
        If AvailMem.AvailMem.checkAvailRam(Me.Pixels.GetLength(0) * Me.Pixels.GetLength(1) * 12L) Then
            Dim bOut As Bitmap = Nothing
            Dim bmD As BitmapData = Nothing
            Dim bmSrc As BitmapData = Nothing

            Dim fp As FloatPointF(,) = Me.Pixels

            Try
                bOut = New Bitmap(Me.Pixels.GetLength(0), Me.Pixels.GetLength(1))
                bmD = bOut.LockBits(New Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
                bmSrc = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)

                Dim w As Integer = bOut.Width
                Dim h As Integer = bOut.Height

                Dim nWidth As Integer = bmp.Width
                Dim nHeight As Integer = bmp.Height

                Dim stride As Integer = bmD.Stride
                Dim scan0 As IntPtr = bmD.Scan0
                Dim strideSrc As Integer = bmSrc.Stride
                Dim srcScan0 As IntPtr = bmSrc.Scan0

                Dim buffer((bmD.Stride * bmD.Height) - 1) As Byte
                Marshal.Copy(bmD.Scan0, buffer, 0, buffer.Length)

                Dim bufferSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, bufferSrc, 0, bufferSrc.Length)

                Parallel.[For](0, h, Sub(y)
                                         For x As Integer = 0 To w - 1
                                             Dim ceil_x As Integer, ceil_y As Integer
                                             Dim p1 As Double, p2 As Double

                                             Dim xOffset As Double = fp(x, y).X
                                             Dim yOffset As Double = fp(x, y).Y

                                             If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso
                                             Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso
                                             Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso
                                             Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then
                                                 ' Setup

                                                 Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                 Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                 If floor_x = w - 1 Then
                                                     floor_x -= 1
                                                 End If

                                                 If floor_y = h - 1 Then
                                                     floor_y -= 1
                                                 End If

                                                 If floor_x <> Single.MinValue Then
                                                     ceil_x = floor_x + 1
                                                 Else
                                                     ceil_x = floor_x
                                                 End If

                                                 If floor_y <> Single.MinValue Then
                                                     ceil_y = floor_y + 1
                                                 Else
                                                     ceil_y = floor_y
                                                 End If

                                                 Dim fraction_x As Double = xOffset - floor_x
                                                 Dim fraction_y As Double = yOffset - floor_y
                                                 Dim one_minus_x As Double = 1.0 - fraction_x
                                                 Dim one_minus_y As Double = 1.0 - fraction_y

                                                 Dim dOut As Double = 0.0

                                                 If floor_y >= 0 AndAlso ceil_y < nHeight AndAlso floor_x >= 0 AndAlso ceil_x < nWidth Then
                                                     ' Blue
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)
                                                     buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     ' Green
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)
                                                     buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     ' Red
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)
                                                     buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     ' Alpha
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 3))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)

                                                     If dOut > alphaThreshold Then
                                                         buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                     End If
                                                 ElseIf antiAliasEdges AndAlso (fp(x, y).X <> Single.MinValue OrElse fp(x, y).Y <> Single.MinValue) Then
                                                     If floor_x <= nWidth + 1 AndAlso floor_y <= nHeight + 1 AndAlso floor_x >= -1 AndAlso floor_y >= -1 Then
                                                         'right
                                                         If fp(x, y).X > nWidth AndAlso fp(x, y).X >= nWidth + 1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = fraction_y * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + one_minus_y * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'left
                                                         If fp(x, y).X < 0.0 AndAlso fp(x, y).X >= -1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).X) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'top
                                                         If fp(x, y).Y < 0.0 AndAlso fp(x, y).Y >= -1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).Y) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'bottom
                                                         If fp(x, y).Y > nHeight AndAlso fp(x, y).Y <= nHeight + 1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight + 1.0) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                     End If

                                                     'if (yOffset == float.MinValue || xOffset == float.MinValue)
                                                     '{
                                                     '    buffer[x * 4 + y * stride] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 1] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 2] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 3] = (byte)0;
                                                     '}
                                                 End If
                                             End If

                                             If yOffset = Single.MinValue OrElse xOffset = Single.MinValue Then
                                                 buffer(x * 4 + y * stride) = CByte(0)
                                                 buffer(x * 4 + y * stride + 1) = CByte(0)
                                                 buffer(x * 4 + y * stride + 2) = CByte(0)
                                                 buffer(x * 4 + y * stride + 3) = CByte(0)
                                             End If
                                         Next
                                     End Sub)

                Marshal.Copy(buffer, 0, bmD.Scan0, buffer.Length)
                bOut.UnlockBits(bmD)
                bmp.UnlockBits(bmSrc)

                buffer = Nothing
                bufferSrc = Nothing
            Catch
                Try
                    bOut.UnlockBits(bmD)

                Catch
                End Try
                Try
                    bmp.UnlockBits(bmSrc)

                Catch
                End Try
                If bOut IsNot Nothing Then
                    bOut.Dispose()
                    bOut = Nothing
                End If
            End Try

            Return bOut
        End If
        Return Nothing
    End Function

    'uses the already resampled Array (Pixels)
    Public Function GetBmpResultBiLi(bmp As Bitmap, antiAliasEdges As Boolean, alphaThreshold As Integer, resampleFactor As Single) As Bitmap
        Dim w2 As Integer = CInt(Math.Ceiling(bmp.Width * resampleFactor))
        Dim h2 As Integer = CInt(Math.Ceiling(bmp.Height * resampleFactor))
        If AvailMem.AvailMem.checkAvailRam(Me.Pixels.GetLength(0) * Me.Pixels.GetLength(1) * 8L + w2 * h2 * 8L) Then
            Dim bOut As Bitmap = Nothing
            Dim bmD As BitmapData = Nothing
            Dim bmSrc As BitmapData = Nothing
            Dim bSrc As Bitmap = Nothing

            Dim fp As FloatPointF(,) = Me.Pixels

            Try
                bOut = New Bitmap(Me.Pixels.GetLength(0), Me.Pixels.GetLength(1))
                bmD = bOut.LockBits(New Rectangle(0, 0, bOut.Width, bOut.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
                bSrc = New Bitmap(w2, h2)
                Using g As Graphics = Graphics.FromImage(bSrc)
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    g.DrawImage(bmp, 0, 0, bmp.Width * resampleFactor, bmp.Height * resampleFactor)
                End Using
                bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)

                Dim w As Integer = bOut.Width
                Dim h As Integer = bOut.Height

                Dim nWidth As Integer = bSrc.Width
                Dim nHeight As Integer = bSrc.Height

                Dim stride As Integer = bmD.Stride
                Dim scan0 As IntPtr = bmD.Scan0
                Dim strideSrc As Integer = bmSrc.Stride
                Dim srcScan0 As IntPtr = bmSrc.Scan0

                Dim buffer((bmD.Stride * bmD.Height) - 1) As Byte
                Marshal.Copy(bmD.Scan0, buffer, 0, buffer.Length)

                Dim bufferSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, bufferSrc, 0, bufferSrc.Length)

                Parallel.[For](0, h, Sub(y)
                                         For x As Integer = 0 To w - 1
                                             Dim ceil_x As Integer, ceil_y As Integer
                                             Dim p1 As Double, p2 As Double

                                             Dim xOffset As Double = fp(x, y).X
                                             Dim yOffset As Double = fp(x, y).Y

                                             If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso
                                             Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso
                                             Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso
                                             Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then
                                                 ' Setup

                                                 Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                 Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                 If floor_x = w - 1 Then
                                                     floor_x -= 1
                                                 End If

                                                 If floor_y = h - 1 Then
                                                     floor_y -= 1
                                                 End If

                                                 If floor_x <> Single.MinValue Then
                                                     ceil_x = floor_x + 1
                                                 Else
                                                     ceil_x = floor_x
                                                 End If

                                                 If floor_y <> Single.MinValue Then
                                                     ceil_y = floor_y + 1
                                                 Else
                                                     ceil_y = floor_y
                                                 End If

                                                 Dim fraction_x As Double = xOffset - floor_x
                                                 Dim fraction_y As Double = yOffset - floor_y
                                                 Dim one_minus_x As Double = 1.0 - fraction_x
                                                 Dim one_minus_y As Double = 1.0 - fraction_y

                                                 Dim dOut As Double = 0.0

                                                 If floor_y >= 0 AndAlso ceil_y < nHeight AndAlso floor_x >= 0 AndAlso ceil_x < nWidth Then
                                                     ' Blue
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)
                                                     buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     ' Green
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)
                                                     buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     ' Red
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)
                                                     buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                     ' Alpha
                                                     p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3))

                                                     p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 3))

                                                     dOut = (one_minus_y * p1 + fraction_y * p2)

                                                     If dOut > alphaThreshold Then
                                                         buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                     End If
                                                 ElseIf antiAliasEdges AndAlso (fp(x, y).X <> Single.MinValue OrElse fp(x, y).Y <> Single.MinValue) Then
                                                     If floor_x <= nWidth + 1 AndAlso floor_y <= nHeight + 1 AndAlso floor_x >= -1 AndAlso floor_y >= -1 Then
                                                         'right
                                                         If fp(x, y).X > nWidth AndAlso fp(x, y).X >= nWidth + 1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = fraction_y * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + one_minus_y * CDbl(bufferSrc(floor_y * strideSrc + floor_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'left
                                                         If fp(x, y).X < 0.0 AndAlso fp(x, y).X >= -1.0 Then
                                                             If (floor_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth + 1.0) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(floor_y * strideSrc + ceil_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).X) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'top
                                                         If fp(x, y).Y < 0.0 AndAlso fp(x, y).Y >= -1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = ((1.0 + fp(x, y).Y) * p1)
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                         'bottom
                                                         If fp(x, y).Y > nHeight AndAlso fp(x, y).Y <= nHeight + 1.0 Then
                                                             If (ceil_y * strideSrc + floor_x * 4 >= 0) AndAlso (ceil_y < nHeight + 1.0) AndAlso (ceil_x < nWidth) Then
                                                                 ' Blue
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Green
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 1))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 1)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 1))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 1) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Red
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 2))

                                                                 p2 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 2)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + 4 * ceil_x + 2))

                                                                 dOut = (one_minus_y * p1 + fraction_y * p2)
                                                                 buffer(x * 4 + y * stride + 2) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])

                                                                 ' Alpha
                                                                 p1 = one_minus_x * CDbl(bufferSrc(ceil_y * strideSrc + floor_x * 4 + 3)) + fraction_x * CDbl(bufferSrc(ceil_y * strideSrc + ceil_x * 4 + 3))

                                                                 dOut = p1
                                                                 buffer(x * 4 + y * stride + 3) = CType(Math.Max(Math.Min(dOut, 255.0), 0.0), [Byte])
                                                             End If
                                                         End If
                                                     End If

                                                     'if (yOffset == float.MinValue || xOffset == float.MinValue)
                                                     '{
                                                     '    buffer[x * 4 + y * stride] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 1] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 2] = (byte)0;
                                                     '    buffer[x * 4 + y * stride + 3] = (byte)0;
                                                     '}
                                                 End If
                                             End If

                                             If yOffset = Single.MinValue OrElse xOffset = Single.MinValue Then
                                                 buffer(x * 4 + y * stride) = CByte(0)
                                                 buffer(x * 4 + y * stride + 1) = CByte(0)
                                                 buffer(x * 4 + y * stride + 2) = CByte(0)
                                                 buffer(x * 4 + y * stride + 3) = CByte(0)
                                             End If
                                         Next
                                     End Sub)

                Marshal.Copy(buffer, 0, bmD.Scan0, buffer.Length)
                bOut.UnlockBits(bmD)
                bSrc.UnlockBits(bmSrc)
                bSrc.Dispose()

                buffer = Nothing
                bufferSrc = Nothing
            Catch
                Try
                    bOut.UnlockBits(bmD)

                Catch
                End Try
                Try
                    bSrc.UnlockBits(bmSrc)

                Catch
                End Try
                If bOut IsNot Nothing Then
                    bOut.Dispose()
                    bOut = Nothing
                End If
                If bSrc IsNot Nothing Then
                    bSrc.Dispose()
                End If
            End Try

            Return bOut
        End If
        Return Nothing
    End Function

    'methods to get a part of the FPBmp
    Public Function GetCroppedBmpFP(rect As Rectangle) As FloatPointPxBitmap
        Dim strtX As Integer = rect.X
        Dim strtY As Integer = rect.Y
        Dim width As Integer = rect.Width
        Dim height As Integer = rect.Height

        If AvailMem.AvailMem.checkAvailRam(width * height * 4L) Then
            Dim fp As FloatPointF(,) = New FloatPointF(width - 1, height - 1) {}

            Parallel.[For](0, height, Sub(y)
                                          For x As Integer = 0 To width - 1
                                              fp(x, y).X = Single.MinValue
                                              fp(x, y).Y = Single.MinValue
                                          Next
                                      End Sub)

            strtX = Math.Max(strtX, 0)
            strtY = Math.Max(strtY, 0)
            width += strtX
            height += strtY
            width = Math.Min(width, Me.Pixels.GetLength(0))
            height = Math.Min(height, Me.Pixels.GetLength(1))

            Parallel.[For](strtY, height, Sub(y)
                                              For x As Integer = strtX To width - 1
                                                  fp(x - strtX, y - strtY).X = Me.Pixels(x, y).X
                                                  fp(x - strtX, y - strtY).Y = Me.Pixels(x, y).Y
                                              Next
                                          End Sub)

            Dim bOut As New FloatPointPxBitmap(fp)
            Return bOut
        End If
        Return Nothing
    End Function

    Public Function GetCroppedBmpFP(strtX As Integer, strtY As Integer, width As Integer, height As Integer) As FloatPointPxBitmap
        If AvailMem.AvailMem.checkAvailRam(width * height * 4L) Then
            Dim fp As FloatPointF(,) = New FloatPointF(width - 1, height - 1) {}

            Parallel.[For](0, height, Sub(y)
                                          For x As Integer = 0 To width - 1
                                              fp(x, y).X = Single.MinValue
                                              fp(x, y).Y = Single.MinValue
                                          Next
                                      End Sub)

            Dim shiftPicX As Integer = If(strtX < 0, -strtX, 0)
            Dim shiftPicY As Integer = If(strtY < 0, -strtY, 0)
            strtX = Math.Max(strtX, 0)
            strtY = Math.Max(strtY, 0)
            width += strtX
            height += strtY
            width = Math.Min(width, Me.Pixels.GetLength(0))
            height = Math.Min(height, Me.Pixels.GetLength(1))

            If width - strtX + shiftPicX > fp.GetLength(0) Then
                width = fp.GetLength(0) - (shiftPicX + strtX)
            End If
            If height - strtY + shiftPicY > fp.GetLength(1) Then
                height = fp.GetLength(1) - (shiftPicY + strtY)
            End If

            Parallel.[For](strtY, height, Sub(y)
                                              For x As Integer = strtX To width - 1
                                                  fp(x - strtX + shiftPicX, y - strtY + shiftPicY).X = Me.Pixels(x, y).X
                                                  fp(x - strtX + shiftPicX, y - strtY + shiftPicY).Y = Me.Pixels(x, y).Y
                                              Next
                                          End Sub)

            Dim bOut As New FloatPointPxBitmap(fp)
            Return bOut
        End If
        Return Nothing
    End Function

    'method to write a rectangular part to the image
    Public Sub WriteBmpFPPart(fp As FloatPointF(,), strtX As Integer, strtY As Integer, width As Integer, height As Integer, resetPixels As Boolean)
        If resetPixels Then
            Dim w As Integer = Me.OrigWidth
            Dim h As Integer = Me.OrigHeight
            Me.Pixels = New FloatPointF(w - 1, h - 1) {}

            Parallel.[For](0, h, Sub(y)
                                     For x As Integer = 0 To w - 1
                                         Me.Pixels(x, y).X = Single.MinValue
                                         Me.Pixels(x, y).Y = Single.MinValue
                                     Next
                                 End Sub)
        End If

        Dim wi As Integer = Me.Pixels.GetLength(0)
        Dim he As Integer = Me.Pixels.GetLength(1)

        Dim wi2 As Integer = strtX + width
        Dim he2 As Integer = strtY + height

        Dim l1 As Integer = fp.GetLength(0)
        Dim l2 As Integer = fp.GetLength(1)

        Try
            Parallel.[For](strtY, he2, Sub(y)
                                           'for (int y = strtY; y < he2; y++)
                                           For x As Integer = strtX To wi2 - 1
                                               If x >= 0 AndAlso x < wi AndAlso y >= 0 AndAlso y < he Then
                                                   If x - strtX < l1 AndAlso y - strtY < l2 Then
                                                       If fp(x - strtX, y - strtY).X <> Single.MinValue OrElse fp(x - strtX, y - strtY).Y <> Single.MinValue Then
                                                           'if (this.Pixels[x, y].X == float.MinValue || this.Pixels[x, y].Y == float.MinValue)
                                                           Me.Pixels(x, y).X = fp(x - strtX, y - strtY).X
                                                           Me.Pixels(x, y).Y = fp(x - strtX, y - strtY).Y
                                                       End If
                                                   End If
                                               End If
                                           Next
                                       End Sub)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    'if we need a FPBmp of different size, not used here
    Public Sub Resample(newW As Integer, newH As Integer, interpolationAdd As Integer, useInterpolationAdd As Boolean)
        If AvailMem.AvailMem.checkAvailRam(newW * newH * 4L) Then
            Dim w As Integer = Me.Pixels.GetLength(0)
            Dim h As Integer = Me.Pixels.GetLength(1)
            Dim fp As FloatPointF(,) = New FloatPointF(newW - 1, newH - 1) {}

            Dim lX As Double = CDbl(w) / CDbl(newW)
            Dim lY As Double = CDbl(h) / CDbl(newH)

            Dim st As Integer = interpolationAdd

            Parallel.[For](0, newH, Sub(y)
                                        'for (int y = 0; y < newH; y++)
                                        For x As Integer = 0 To newW - 1
                                            Dim ceil_x As Integer, ceil_y As Integer
                                            Dim xOffset As Double = x * lX
                                            Dim yOffset As Double = y * lY

                                            Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                            Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                            If floor_x = w - 1 Then
                                                floor_x -= 1
                                            End If

                                            If floor_y = h - 1 Then
                                                floor_y -= 1
                                            End If

                                            If floor_x <> Single.MinValue Then
                                                ceil_x = floor_x + 1
                                            Else
                                                ceil_x = floor_x
                                            End If

                                            If floor_y <> Single.MinValue Then
                                                ceil_y = floor_y + 1
                                            Else
                                                ceil_y = floor_y
                                            End If

                                            Dim fraction_x As Double = xOffset - floor_x
                                            Dim fraction_y As Double = yOffset - floor_y
                                            Dim one_minus_x As Double = 1.0 - fraction_x
                                            Dim one_minus_y As Double = 1.0 - fraction_y

                                            'bili resampling
                                            If floor_y >= 0 AndAlso ceil_y < h AndAlso floor_x >= 0 AndAlso ceil_x < w Then
                                                Dim x1 As Double = fraction_x * Me.Pixels(ceil_x, floor_y).X + one_minus_x * Me.Pixels(floor_x, floor_y).X
                                                Dim x2 As Double = fraction_x * Me.Pixels(ceil_x, ceil_y).X + one_minus_x * Me.Pixels(floor_x, ceil_y).X
                                                Dim xx As Double = fraction_y * x2 + one_minus_y * x1

                                                Dim y1 As Double = fraction_x * Me.Pixels(ceil_x, floor_y).Y + one_minus_x * Me.Pixels(floor_x, floor_y).Y
                                                Dim y2 As Double = fraction_x * Me.Pixels(ceil_x, ceil_y).Y + one_minus_x * Me.Pixels(floor_x, ceil_y).Y
                                                Dim yy As Double = fraction_y * y2 + one_minus_y * y1

                                                If useInterpolationAdd Then
                                                    If x < interpolationAdd OrElse y < interpolationAdd Then
                                                        fp(x, y).X = Fix(CInt(xx))
                                                        fp(x, y).Y = Fix(CInt(yy))
                                                    ElseIf x > newW - interpolationAdd OrElse y > newH - interpolationAdd Then
                                                        fp(x, y).X = Fix(CInt(xx))
                                                        fp(x, y).Y = Fix(CInt(yy))
                                                    Else
                                                        fp(x, y).X = CSng(xx)
                                                        fp(x, y).Y = CSng(yy)
                                                    End If
                                                Else
                                                    fp(x, y).X = CSng(xx)
                                                    fp(x, y).Y = CSng(yy)
                                                End If
                                            End If
                                        Next
                                    End Sub)

            Me.OrigWidth = newW
            Me.OrigHeight = newH
            Me.Pixels = fp
        End If
    End Sub

    'main resample method for resampling the complete image to a new size
    Public Sub Resample(newW As Integer, newH As Integer)
        If AvailMem.AvailMem.checkAvailRam(newW * newH * 4L) Then
            Dim w As Integer = Me.Pixels.GetLength(0)
            Dim h As Integer = Me.Pixels.GetLength(1)
            Dim fp As FloatPointF(,) = New FloatPointF(newW - 1, newH - 1) {}

            Dim lX As Double = CDbl(w) / CDbl(newW)
            Dim lY As Double = CDbl(h) / CDbl(newH)

            Parallel.[For](0, newH, Sub(y)
                                        'for (int y = 0; y < newH; y++)
                                        For x As Integer = 0 To newW - 1
                                            Dim ceil_x As Integer, ceil_y As Integer
                                            Dim xOffset As Double = x * lX
                                            Dim yOffset As Double = y * lY

                                            Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                            Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                            If floor_x = w - 1 Then
                                                floor_x -= 1
                                            End If

                                            If floor_y = h - 1 Then
                                                floor_y -= 1
                                            End If

                                            If floor_x <> Single.MinValue Then
                                                ceil_x = floor_x + 1
                                            Else
                                                ceil_x = floor_x
                                            End If

                                            If floor_y <> Single.MinValue Then
                                                ceil_y = floor_y + 1
                                            Else
                                                ceil_y = floor_y
                                            End If

                                            Dim fraction_x As Double = xOffset - floor_x
                                            Dim fraction_y As Double = yOffset - floor_y
                                            Dim one_minus_x As Double = 1.0 - fraction_x
                                            Dim one_minus_y As Double = 1.0 - fraction_y

                                            If floor_y >= 0 AndAlso ceil_y < h AndAlso floor_x >= 0 AndAlso ceil_x < w Then
                                                Dim x1 As Double = fraction_x * Me.Pixels(ceil_x, floor_y).X + one_minus_x * Me.Pixels(floor_x, floor_y).X
                                                Dim x2 As Double = fraction_x * Me.Pixels(ceil_x, ceil_y).X + one_minus_x * Me.Pixels(floor_x, ceil_y).X
                                                Dim xx As Double = fraction_y * x2 + one_minus_y * x1

                                                Dim y1 As Double = fraction_x * Me.Pixels(ceil_x, floor_y).Y + one_minus_x * Me.Pixels(floor_x, floor_y).Y
                                                Dim y2 As Double = fraction_x * Me.Pixels(ceil_x, ceil_y).Y + one_minus_x * Me.Pixels(floor_x, ceil_y).Y
                                                Dim yy As Double = fraction_y * y2 + one_minus_y * y1

                                                fp(x, y).X = CSng(xx / lX)
                                                fp(x, y).Y = CSng(yy / lY)
                                            End If
                                        Next
                                    End Sub)

            Me.OrigWidth = newW
            Me.OrigHeight = newH
            Me.Pixels = fp
        End If
    End Sub

    'title says all
    Public Sub RotateFlip(rotateFlipType__1 As RotateFlipType)
        Dim fp As FloatPointF(,) = Nothing
        If rotateFlipType__1 = RotateFlipType.Rotate180FlipNone Then
            Dim w As Integer = Me.Pixels.GetLength(0)
            Dim h As Integer = Me.Pixels.GetLength(1)
            If AvailMem.AvailMem.checkAvailRam(w * h * 4L) Then
                fp = New FloatPointF(w - 1, h - 1) {}

                Parallel.[For](0, h, Sub(y)
                                         'for (int y = 0; y < h; y++)
                                         For x As Integer = 0 To w - 1
                                             fp(x, y).X = Me.Pixels(w - 1 - x, h - 1 - y).X
                                             fp(x, y).Y = Me.Pixels(w - 1 - x, h - 1 - y).Y
                                         Next
                                     End Sub)
            End If
        End If

        If rotateFlipType__1 = RotateFlipType.RotateNoneFlipX Then
            Dim w As Integer = Me.Pixels.GetLength(0)
            Dim h As Integer = Me.Pixels.GetLength(1)
            If AvailMem.AvailMem.checkAvailRam(w * h * 4L) Then
                fp = New FloatPointF(w - 1, h - 1) {}

                Parallel.[For](0, h, Sub(y)
                                         'for (int y = 0; y < h; y++)
                                         For x As Integer = 0 To w - 1
                                             fp(x, y).X = Me.Pixels(w - 1 - x, y).X
                                             fp(x, y).Y = Me.Pixels(w - 1 - x, y).Y
                                         Next
                                     End Sub)
            End If
        End If

        Me.Pixels = fp
    End Sub

    'public Point GetBitmapOriginOffset(float minVal, out PointF vals)
    '{
    '    int w = this.Pixels.GetLength(0);
    '    int h = this.Pixels.GetLength(1);

    '    int offsetX = w;
    '    int offsetY = h;
    '    float curMinX = w;
    '    float curMinY = h;

    '    for (int y = 0; y < h; y++)
    '    {
    '        for (int x = 0; x < w; x++)
    '        {
    '            double dX = this.Pixels[x, y].X - this.DistX;
    '            double dY = this.Pixels[x, y].Y - this.DistY;
    '            double dist = Math.Sqrt(dX * dX + dY * dY);
    '            if (dist < minVal)
    '            {
    '                minVal = (float)dist;
    '                curMinX = this.Pixels[x, y].X - this.DistX;
    '                curMinY = this.Pixels[x, y].Y - this.DistY;
    '                offsetX = x;
    '                offsetY = y;
    '            }
    '        }
    '    }

    '    //MessageBox.Show(this.Pixels.OfType<FloatPointF>().Where(a => a.X != float.MinValue && a.X > 0).Min(a => a.X).ToString());
    '    //MessageBox.Show(this.Pixels.OfType<FloatPointF>().Where(a => a.Y != float.MinValue && a.Y > 0).Min(a => a.Y).ToString());

    '    vals = new PointF(curMinX, curMinY);
    '    return new Point(offsetX, offsetY);
    '}

    'scan for the bounds of the imageData (eg in rotated FPBmps)
    Public Function GetPicLocationAndSize() As Rectangle
        Dim top As New Point(), left As New Point(), right As New Point(), bottom As New Point()
        Dim complete As Boolean = False

        Dim width As Integer = Me.Pixels.GetLength(0)
        Dim height As Integer = Me.Pixels.GetLength(1)

        For y As Integer = 0 To height - 1
            For x As Integer = 0 To width - 1
                If Me.Pixels(x, y).X >= 0 AndAlso Me.Pixels(x, y).Y >= 0 AndAlso Me.Pixels(x, y).X < width AndAlso Me.Pixels(x, y).Y < height Then
                    top = New Point(x, y)
                    complete = True
                    Exit For
                End If
            Next
            If complete Then
                Exit For
            End If
        Next

        complete = False

        For y As Integer = height - 1 To 0 Step -1
            For x As Integer = 0 To width - 1
                If Me.Pixels(x, y).X >= 0 AndAlso Me.Pixels(x, y).Y >= 0 AndAlso Me.Pixels(x, y).X < width AndAlso Me.Pixels(x, y).Y < height Then
                    bottom = New Point(x + 1, y + 1)
                    complete = True
                    Exit For
                End If
            Next
            If complete Then
                Exit For
            End If
        Next

        complete = False

        For x As Integer = 0 To width - 1
            For y As Integer = 0 To height - 1
                If Me.Pixels(x, y).X >= 0 AndAlso Me.Pixels(x, y).Y >= 0 AndAlso Me.Pixels(x, y).X < width AndAlso Me.Pixels(x, y).Y < height Then
                    left = New Point(x, y)
                    complete = True
                    Exit For
                End If
            Next
            If complete Then
                Exit For
            End If
        Next

        complete = False

        For x As Integer = width - 1 To 0 Step -1
            For y As Integer = 0 To height - 1
                If Me.Pixels(x, y).X >= 0 AndAlso Me.Pixels(x, y).Y >= 0 AndAlso Me.Pixels(x, y).X < width AndAlso Me.Pixels(x, y).Y < height Then
                    right = New Point(x + 1, y + 1)
                    complete = True
                    Exit For
                End If
            Next
            If complete Then
                Exit For
            End If
        Next

        Return New Rectangle(left.X, top.Y, right.X - left.X, bottom.Y - top.Y)
    End Function

    'translateTransform
    Public Sub OffsetFraction(xFraction As Single, yFraction As Single)
        Dim xx As Single = xFraction
        Dim yy As Single = yFraction

        Dim height As Integer = Me.Pixels.GetLength(1)
        Dim width As Integer = Me.Pixels.GetLength(0)

        Parallel.[For](0, height, Sub(y)
                                      For x As Integer = 0 To width - 1
                                          Me.Pixels(x, y).X += xx
                                          Me.Pixels(x, y).Y += yy
                                      Next
                                  End Sub)
    End Sub

    'offset the locations for the fp-array in the float precision representation of the bitmap
    'this is the method where the "new" offsets are written to the Me.Pixels array
    'its almost the same as the OffsetFiAntiAlias used for the normal bitmap-output
    'http://www.codeproject.com/Articles/3419/Image-Processing-for-Dummies-with-C-and-GDI-Part
    Public Sub OffsetBmpFPPart(fp As FloatPointF(,), strtX As Integer, strtY As Integer, drawInCopyMode As Boolean)
        Dim wi As Integer = Me.Pixels.GetLength(0)
        Dim he As Integer = Me.Pixels.GetLength(1)

        'int wi2 = wi; //strtX + wi;
        'int he2 = he; //strtY + he;

        Dim l1 As Integer = fp.GetLength(0)
        Dim l2 As Integer = fp.GetLength(1)

        'create a copy
        Dim pixels As FloatPointF(,) = New FloatPointF(wi - 1, he - 1) {}
        Array.Copy(Me.Pixels, pixels, pixels.Length)

        'make a "hole" for the parts covered by "szSmall" (the rotated smaller Array (covering the real affected pixels) inside fp = "szBig-array") in the original Me.Pixels array
        If drawInCopyMode Then
            Parallel.[For](strtY, he, Sub(y)
                                          For x As Integer = strtX To wi - 1
                                              If x >= 0 AndAlso x < wi AndAlso y >= 0 AndAlso y < he Then
                                                  If x - strtX < l1 AndAlso y - strtY < l2 Then
                                                      If fp(x - strtX, y - strtY).X <> Single.MinValue OrElse fp(x - strtX, y - strtY).Y <> Single.MinValue Then
                                                          Me.Pixels(x, y).X = Single.MinValue
                                                          Me.Pixels(x, y).Y = Single.MinValue
                                                      End If
                                                  End If
                                              End If
                                          Next
                                      End Sub)
        End If

        'loop over the image (write only for coords from startXY to startXY plus fp.Width/Height)
        Try
            Parallel.[For](strtY, he, Sub(y)
                                          'for (int y = strtY; y < he2; y++)
                                          For x As Integer = strtX To wi - 1
                                              If x >= 0 AndAlso x < wi AndAlso y >= 0 AndAlso y < he Then
                                                  If x - strtX < l1 AndAlso y - strtY < l2 Then
                                                      If fp(x - strtX, y - strtY).X <> Single.MinValue OrElse fp(x - strtX, y - strtY).Y <> Single.MinValue Then
                                                          'if (this.Pixels[x, y].X == float.MinValue || this.Pixels[x, y].Y == float.MinValue)
                                                          Dim ceil_x As Integer, ceil_y As Integer
                                                          Dim xOffset As Double = fp(x - strtX, y - strtY).X + strtX
                                                          Dim yOffset As Double = fp(x - strtX, y - strtY).Y + strtY

                                                          If Not Double.IsInfinity(xOffset) AndAlso Not Double.IsNaN(xOffset) AndAlso
                                                              Not Double.IsInfinity(yOffset) AndAlso Not Double.IsNaN(yOffset) AndAlso
                                                              Math.Ceiling(xOffset) < Int32.MaxValue AndAlso Math.Floor(xOffset) > Int32.MinValue AndAlso
                                                              Math.Ceiling(yOffset) < Int32.MaxValue AndAlso Math.Floor(yOffset) > Int32.MinValue Then

                                                              Dim floor_x As Integer = CInt(Math.Floor(xOffset))
                                                              Dim floor_y As Integer = CInt(Math.Floor(yOffset))

                                                              If floor_x = wi - 1 Then
                                                                  floor_x -= 1
                                                              End If

                                                              If floor_y = he - 1 Then
                                                                  floor_y -= 1
                                                              End If

                                                              If floor_x <> Single.MinValue Then
                                                                  ceil_x = floor_x + 1
                                                              Else
                                                                  ceil_x = floor_x
                                                              End If

                                                              If floor_y <> Single.MinValue Then
                                                                  ceil_y = floor_y + 1
                                                              Else
                                                                  ceil_y = floor_y
                                                              End If

                                                              Dim fraction_x As Double = xOffset - floor_x
                                                              Dim fraction_y As Double = yOffset - floor_y
                                                              Dim one_minus_x As Double = 1.0 - fraction_x
                                                              Dim one_minus_y As Double = 1.0 - fraction_y

                                                              If floor_y >= 0 AndAlso ceil_y < he AndAlso floor_x >= 0 AndAlso ceil_x < wi Then
                                                                  Dim x1 As Double = fraction_x * pixels(ceil_x, floor_y).X + one_minus_x * pixels(floor_x, floor_y).X
                                                                  Dim x2 As Double = fraction_x * pixels(ceil_x, ceil_y).X + one_minus_x * pixels(floor_x, ceil_y).X
                                                                  Dim xx As Double = fraction_y * x2 + one_minus_y * x1

                                                                  Dim y1 As Double = fraction_x * pixels(ceil_x, floor_y).Y + one_minus_x * pixels(floor_x, floor_y).Y
                                                                  Dim y2 As Double = fraction_x * pixels(ceil_x, ceil_y).Y + one_minus_x * pixels(floor_x, ceil_y).Y
                                                                  Dim yy As Double = fraction_y * y2 + one_minus_y * y1

                                                                  Me.Pixels(x, y).X = CSng(xx)
                                                                  Me.Pixels(x, y).Y = CSng(yy)
                                                              End If
                                                          End If
                                                      End If
                                                  End If
                                              End If
                                          Next
                                      End Sub)
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        Finally
            pixels = Nothing
        End Try
    End Sub

    Public Shared Function Open(fileName As String) As FloatPointPxBitmap
        Dim bOut As FloatPointPxBitmap = Nothing

        Dim reader As BinaryReader = Nothing
        Dim mem As MemoryStream = Nothing
        Dim bError As Boolean = False

        Try
            If File.Exists(fileName) Then
                reader = New BinaryReader(File.Open(fileName, FileMode.Open))
                If reader.ReadString() = "TGFPBmp" Then
                    bOut = New FloatPointPxBitmap()

                    bOut.OrigWidth = reader.ReadInt32()
                    bOut.OrigHeight = reader.ReadInt32()
                    bOut.RotatedWidth = reader.ReadInt32()
                    bOut.RotatedHeight = reader.ReadInt32()
                    bOut.DistX = reader.ReadSingle()
                    bOut.DistY = reader.ReadSingle()

                    Dim rpX As Single = reader.ReadSingle()
                    Dim rpY As Single = reader.ReadSingle()
                    bOut.RotatedFixPoint = New PointF(rpX, rpY)

                    bOut.Pixels = New FloatPointF(bOut.OrigWidth - 1, bOut.OrigHeight - 1) {}

                    Dim x As Integer = 0
                    Dim y As Integer = 0

                    Dim bytes As Byte() = reader.ReadBytes(CInt(reader.BaseStream.Length) - CInt(reader.BaseStream.Position))

                    mem = New MemoryStream(bytes)
                    Using reader2 As New BinaryReader(mem)
                        mem = Nothing ' https://msdn.microsoft.com/library/ms182334.aspx
                        While reader2.BaseStream.Position <> reader2.BaseStream.Length
                            Dim valueX As Single = Single.MinValue
                            valueX = reader2.ReadSingle()
                            If reader2.BaseStream.Position <> reader2.BaseStream.Length Then
                                Dim valueY As Single = Single.MinValue
                                valueY = reader2.ReadSingle()

                                bOut.Pixels(x, y).X = valueX

                                bOut.Pixels(x, y).Y = valueY
                            End If
                            x += 1

                            If x = bOut.OrigWidth Then
                                x = 0
                                y += 1
                            End If
                        End While
                    End Using
                Else
                    Throw New Exception("Wrong FileType")
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message)
            bError = True
        Finally
            If Not reader Is Nothing Then
                reader.Dispose()
            End If

            If Not mem Is Nothing Then
                mem.Dispose()
            End If
        End Try

        If bError Then
            If Not bOut Is Nothing Then
                bOut.Dispose()
                bOut = Nothing
            End If
        End If

        Return bOut
    End Function

    Public Shared Function Open(fileName As String, b As System.ComponentModel.BackgroundWorker) As FloatPointPxBitmap
        Dim bOut As FloatPointPxBitmap = Nothing
        Dim bError As Boolean = False

        Try
            If File.Exists(fileName) Then
                Using reader As New BinaryReader(File.Open(fileName, FileMode.Open))
                    If reader.ReadString() = "TGFPBmp" Then
                        bOut = New FloatPointPxBitmap()

                        bOut.OrigWidth = reader.ReadInt32()
                        bOut.OrigHeight = reader.ReadInt32()
                        bOut.RotatedWidth = reader.ReadInt32()
                        bOut.RotatedHeight = reader.ReadInt32()
                        bOut.DistX = reader.ReadSingle()
                        bOut.DistY = reader.ReadSingle()

                        Dim rpX As Single = reader.ReadSingle()
                        Dim rpY As Single = reader.ReadSingle()
                        bOut.RotatedFixPoint = New PointF(rpX, rpY)

                        bOut.Pixels = New FloatPointF(bOut.OrigWidth - 1, bOut.OrigHeight - 1) {}

                        If b IsNot Nothing AndAlso b.WorkerReportsProgress Then
                            b.ReportProgress(20)
                        End If

                        Dim bytes As Byte() = reader.ReadBytes(CInt(reader.BaseStream.Length) - CInt(reader.BaseStream.Position))

                        If b IsNot Nothing AndAlso b.WorkerReportsProgress Then
                            b.ReportProgress(40)
                        End If

                        'faster
                        Dim nWidth As Integer = bOut.OrigWidth
                        Dim nHeight As Integer = bOut.OrigHeight

                        Parallel.[For](0, nHeight, Sub(y)
                                                       For x As Integer = 0 To nWidth - 1
                                                           Dim valueX As Single = Single.MinValue
                                                           valueX = BitConverter.ToSingle(bytes, y * nWidth * 8 + x * 8)
                                                           bOut.Pixels(x, y).X = valueX
                                                           Dim valueY As Single = Single.MinValue
                                                           valueY = BitConverter.ToSingle(bytes, y * nWidth * 8 + x * 8 + 4)
                                                           bOut.Pixels(x, y).Y = valueY
                                                       Next
                                                   End Sub)

                        If b IsNot Nothing AndAlso b.WorkerReportsProgress Then
                            b.ReportProgress(70)
                        End If
                    Else
                        bError = True
                    End If
                End Using
            End If
        Catch

        End Try

        If bError Then
            If Not bOut Is Nothing Then
                bOut.Dispose()
                bOut = Nothing
            End If
        End If

        Return bOut
    End Function

    Public Sub Save(fileName As String)
        Dim bw As BinaryWriter = Nothing
        Dim mem As MemoryStream = Nothing
        Dim bw2 As BinaryWriter = Nothing

        Try
            Dim w As Integer = Me.Pixels.GetLength(0)
            Dim h As Integer = Me.Pixels.GetLength(1)

            bw = New BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
            bw.Write("TGFPBmp")
            bw.Write(Me.OrigWidth)
            bw.Write(Me.OrigHeight)
            bw.Write(Me.RotatedWidth)
            bw.Write(Me.RotatedHeight)
            bw.Write(Me.DistX)
            bw.Write(Me.DistY)
            bw.Write(Me.RotatedFixPoint.X)
            bw.Write(Me.RotatedFixPoint.Y)

            mem = New MemoryStream()
            bw2 = New BinaryWriter(mem)
            For y As Integer = 0 To h - 1
                For x As Integer = 0 To w - 1
                    bw2.Write(Me.Pixels(x, y).X)
                    bw2.Write(Me.Pixels(x, y).Y)
                Next
            Next

            bw.Write(mem.ToArray())

        Catch ex As Exception
            MessageBox.Show(ex.Message)
        Finally
            If Not bw Is Nothing Then
                bw.Dispose()
            End If

            If Not bw2 Is Nothing Then
                bw2.Dispose()
            End If

            'If Not mem Is Nothing Then
            '    mem.Dispose()
            'End If
        End Try
    End Sub

    Public Sub Save(fileName As String, b As System.ComponentModel.BackgroundWorker)
        Dim bw As BinaryWriter = Nothing
        Dim mem As MemoryStream = Nothing
        Dim bw2 As BinaryWriter = Nothing

        Try
            Dim w As Integer = Me.Pixels.GetLength(0)
            Dim h As Integer = Me.Pixels.GetLength(1)

            bw = New BinaryWriter(File.Open(fileName, FileMode.OpenOrCreate))
            bw.Write("TGFPBmp")
            bw.Write(Me.OrigWidth)
            bw.Write(Me.OrigHeight)
            bw.Write(Me.RotatedWidth)
            bw.Write(Me.RotatedHeight)
            bw.Write(Me.DistX)
            bw.Write(Me.DistY)
            bw.Write(Me.RotatedFixPoint.X)
            bw.Write(Me.RotatedFixPoint.Y)

            If b IsNot Nothing AndAlso b.WorkerReportsProgress Then
                b.ReportProgress(55)
            End If

            mem = New MemoryStream()
            bw2 = New BinaryWriter(mem)
            For y As Integer = 0 To h - 1
                For x As Integer = 0 To w - 1
                    bw2.Write(Me.Pixels(x, y).X)
                    bw2.Write(Me.Pixels(x, y).Y)
                Next
            Next

            If b IsNot Nothing AndAlso b.WorkerReportsProgress Then
                b.ReportProgress(75)
            End If

            bw.Write(mem.ToArray())

            If b IsNot Nothing AndAlso b.WorkerReportsProgress Then
                b.ReportProgress(99)
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        Finally
            If Not bw Is Nothing Then
                bw.Dispose()
            End If

            If Not bw2 Is Nothing Then
                bw2.Dispose()
            End If

            'If Not mem Is Nothing Then
            '    mem.Dispose()
            'End If
        End Try
    End Sub

    'telerik converter added method
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Dim w As Integer = Me.Pixels.GetLength(0)
        Dim h As Integer = Me.Pixels.GetLength(1)
        If AvailMem.AvailMem.checkAvailRam(w * h * 4L) Then
            Dim fp(,) As FloatPointF = New FloatPointF(w, h) {}

            Parallel.For(0, h, Sub(y)
                                   For x As Integer = 0 To w - 1
                                       fp(x, y).X = Me.Pixels(x, y).X
                                       fp(x, y).Y = Me.Pixels(x, y).Y
                                   Next
                               End Sub)
            Dim f As New FloatPointPxBitmap(fp)
            f.DistX = Me.DistX
            f.DistY = Me.DistY
            f.RotatedFixPoint = Me.RotatedFixPoint
            f.RotatedWidth = Me.RotatedWidth
            f.RotatedHeight = Me.RotatedHeight

            Return f
        End If
        Return Nothing
    End Function

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                If Not Me.Pixels Is Nothing Then
                    Me.Pixels = Nothing
                End If
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class
