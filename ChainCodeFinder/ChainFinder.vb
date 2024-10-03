Option Strict On

Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Threading.Tasks
Imports ChainCodeFinder
Imports System.Drawing.Drawing2D

'This class implements a chaincode finder (crack code), as an adaption of 
'       http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm and
'       http://www.miszalok.de/Lectures/L08_ComputerVision/CrackCode/CrackCode_d.htm (german only). See also
'       http://www.miszalok.de/Samples/CV/ChainCode/chaincode_kovalev_e.htm
'As the name crackcode says, we are moving on the (invisible) "cracks" in between the pixels to find the outlines of objects
'in an image. The algorithm is as follows:
'- Find a start pixel moving left to right and top to bottom over the image that matches the conditions.
'- Now we are at the leftTop location of the *pixel* the crack left of the pixel. 
'- We now set our direction to the only reasonable value "down" and move along the crack, so we truly start the
'  algorithm at the lowest "point" of the left crack, ie. the leftBottom location of the start pixel, and we turn our
'- looking direction also down, so the pixel left before us is the pixel below of the start pixel.

'- Now its getting easy: We

'- Look to the pixel thats left before us, 
'    a) if it doesnt meet the conditions (search criteria is lower than the threshold),
'       we turn left (set the direction to left, turn ourselves left and move along that crack to the next junction, 
'       being now at the BottomRight corner of the start pixel)
'    b) if it meets the conditions, we:
'           a) look to the pixels thats right before us,
'               aa) if it doesnt meet the conditions (search criteria is lower than the threshold),
'                   we go straight on, keeping direction "down", being now at the BottomLeft corner 
'                   of the pixel below our start pixel
'               bb) if it meets the conditions, we turn right (setting the direction and ourselves "right" 
'                   and move along that crack to the next junction, being now at the BottomLeft corner 
'                   of the pixel on the right of our start pixel
'- Since we always turn our looking-direction, we can now repeat the above until we are back at the point we began.
'- Since we move on the cracks, we always will come back to that point, if we only have one pixel matching the
'  conditions, we get a chain of 4 cracks, once around the pixel


Public Class ChainFinder
    Public Delegate Sub ProgressPlusEventHandler(sender As Object, e As ProgEventArgs)
    Public Event ProgressPlus As ProgressPlusEventHandler

    Private _threshold As Integer = 0
    Private _nullCells As Boolean = False
    Private _start As New Point(0, 0)
    Private _height As Integer = 0
    Private _thresholdR As Integer
    Private _thresholdG As Integer
    Private _thresholdB As Integer

    Public Property AllowNullCells() As Boolean
        Get
            Return _nullCells
        End Get
        Set(value As Boolean)
            _nullCells = value
        End Set
    End Property

    Public Function GetOutline(bmp As Bitmap, threshold As Integer, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean, initialValueToCheck As Integer,
     doReverse As Boolean) As List(Of ChainCode)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)

            If doReverse Then
                FindChainCodeRev(bmp, fList, fbits, grayscale, range, excludeInnerOutlines,
                 initialValueToCheck)
            Else
                FindChainCode(bmp, fList, fbits, grayscale, range, excludeInnerOutlines,
                 initialValueToCheck)
            End If

            Return fList
        Catch exc As Exception
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    Public Function GetOutline(bmp As Bitmap, width As Integer, height As Integer, threshold As Integer, grayscale As Boolean, range As Integer,
                               excludeInnerOutlines As Boolean, initialValueToCheck As Integer, doReverse As Boolean) As List(Of ChainCode)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((width + 1) * height, False)

            If doReverse Then
                FindChainCodeRev(bmp, fList, fbits, grayscale, range, excludeInnerOutlines,
                 initialValueToCheck)
            Else
                FindChainCode(bmp, fList, fbits, grayscale, range, excludeInnerOutlines,
                 initialValueToCheck)
            End If

            Return fList
        Catch exc As Exception
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    Public Function GetOutline(bmp As Bitmap, threshold As Integer, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean) As List(Of ChainCode)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)
            FindChainCode(bmp, fList, fbits, grayscale, range, excludeInnerOutlines)

            Return fList
        Catch
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    Public Function GetOutline(range As Integer, bmp As Bitmap, threshold As Integer, grayscale As Boolean, excludeInnerOutlines As Boolean) As List(Of ChainCodeF)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCodeF)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)
            FindChainCode(bmp, fList, fbits, grayscale, range, excludeInnerOutlines)

            Return fList
        Catch
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function


    Public Function GetOutline(bmp As Bitmap, threshold As Integer, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean, initialValueToCheck As Integer,
        doReverse As Boolean, maxIterations As Integer) As List(Of ChainCode)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)

            If doReverse Then
                FindChainCodeRev(bmp, fList, fbits, grayscale, range, excludeInnerOutlines,
                    initialValueToCheck, maxIterations)
            Else
                FindChainCode(bmp, fList, fbits, grayscale, range, excludeInnerOutlines,
                    initialValueToCheck, maxIterations)
            End If

            Return fList
        Catch
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCode(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_search(bmData, p, fbits, grayscale, range)
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            p = Nothing
            b.UnlockBits(bmData)
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)
            Catch
            End Try
        End Try
    End Sub

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCode(b As Bitmap, fList As List(Of ChainCodeF), fbits As BitArray, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_search(bmData, p, fbits, grayscale, range)
                Dim cc As New ChainCodeF()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            p = Nothing
            b.UnlockBits(bmData)
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)
            Catch
            End Try
        End Try
    End Sub





    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeRev(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean,
    initialValueToCheck As Integer, maxIterations As Integer)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_searchRev(bmData, p, fbits, grayscale, range, initialValueToCheck) AndAlso fList.Count <= maxIterations
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) < _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) < _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) < _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) < _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) < _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            b.UnlockBits(bmData)
            p = Nothing
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCode(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean,
    initialValueToCheck As Integer, maxIterations As Integer)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_search(bmData, p, fbits, grayscale, range, initialValueToCheck) AndAlso fList.Count <= maxIterations
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            b.UnlockBits(bmData)
            p = Nothing
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    Private Function start_crack_search(bmData As BitmapData, p As Byte(), fbits As BitArray, grayscale As Boolean, range As Integer) As Boolean
        Dim left As Integer = 0
        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    If Not grayscale Then
                        left = p(y * stride + (x - 1) * 4 + 3)
                    Else
                        left = p(y * stride + (x - 1) * 4)
                    End If
                Else
                    left = 0
                End If

                If Not grayscale Then
                    If (left <= _threshold) AndAlso (p(y * stride + x * 4 + 3) > _threshold) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                        _start.X = x
                        _start.Y = y
                        fbits.[Set](y * (bmData.Width + 1) + x, True)
                        OnProgressPlus()
                        Return True
                    End If
                Else
                    If range > 0 Then
                        If (left <= _threshold) AndAlso (p(y * stride + x * 4) > _threshold) AndAlso (p(y * stride + x * 4) <= _threshold + range) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (left <= _threshold) AndAlso (p(y * stride + x * 4) > _threshold) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
            Next
        Next
        Return False
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeRev(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean,
     initialValueToCheck As Integer)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_searchRev(bmData, p, fbits, grayscale, range, initialValueToCheck)
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) < _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) < _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) < _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) < _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) < _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            p = Nothing
            b.UnlockBits(bmData)
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCode(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean, range As Integer, excludeInnerOutlines As Boolean,
     initialValueToCheck As Integer)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_search(bmData, p, fbits, grayscale, range, initialValueToCheck)
                Dim cc As New ChainCode()

                cc.start = _start
                'cc.Coord.Add(_start)

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            p = Nothing
            b.UnlockBits(bmData)
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    '####################################
    Public Function GetOutlineShifted(bmp As Bitmap, threshold As Integer, grayscale As Boolean, range As Integer,
                                      initialValueToCheck As Integer, doReverse As Boolean, recordOuterOutlines As Boolean) As List(Of ChainCode)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)

            If doReverse Then
                FindChainCodeRevShifted(bmp, fList, fbits, grayscale, range,
                 initialValueToCheck, recordOuterOutlines)
            Else
                FindChainCodeShifted(bmp, fList, fbits, grayscale, range,
                 initialValueToCheck, recordOuterOutlines)
            End If

            Return fList
        Catch exc As Exception
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeShifted(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean,
                                     range As Integer, initialValueToCheck As Integer, recordOuterOutlines As Boolean)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_search(bmData, p, fbits, grayscale, range, initialValueToCheck)
                Dim cc As New ChainCode()

                cc.start = _start
                'cc.Coord.Add(_start)

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) > _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                If cc.Chain(cc.Chain.Count - 1) = 0 Then 'todo: add support for nullcells = true (then last direction may be down (1), so check the 5 last entries in the chain)
                    'isInnerOutline = True  
                    cc.Coord.Add(_start)
                    ShiftInnerOutline(cc)
                    fList.Add(cc)
                    Continue While
                End If

                If recordOuterOutlines Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            p = Nothing
            b.UnlockBits(bmData)
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeRevShifted(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray, grayscale As Boolean,
                                        range As Integer, initialValueToCheck As Integer, recordOuterOutlines As Boolean)
        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_searchRev(bmData, p, fbits, grayscale, range, initialValueToCheck)
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        If Not grayscale Then
                            LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 3) < _threshold
                        Else
                            If range > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _threshold) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _threshold + range))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) < _threshold
                            End If
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        If Not grayscale Then
                            RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 3) < _threshold
                        Else
                            If range > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) < _threshold) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _threshold + range))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) < _threshold
                            End If
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                If cc.Chain(cc.Chain.Count - 1) = 0 Then
                    'isInnerOutline = True  
                    cc.Coord.Add(_start)
                    ShiftInnerOutline(cc)
                    fList.Add(cc)
                    Continue While
                End If

                If recordOuterOutlines Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            p = Nothing
            b.UnlockBits(bmData)
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    Private Sub ShiftInnerOutline(cc As ChainCode)
        For i As Integer = 0 To cc.Chain.Count - 1
            Select Case cc.Chain(i)
                Case 0
                    cc.Coord(i) = New Point(cc.Coord(i).X, cc.Coord(i).Y + 1)
                    Exit Select
                Case 1
                    cc.Coord(i) = New Point(cc.Coord(i).X - 1, cc.Coord(i).Y)
                    Exit Select
                Case 2
                    cc.Coord(i) = New Point(cc.Coord(i).X, cc.Coord(i).Y - 1)
                    Exit Select
                Case 3
                    cc.Coord(i) = New Point(cc.Coord(i).X + 1, cc.Coord(i).Y)
                    Exit Select
            End Select
        Next
    End Sub
    '####################################

    Private Function start_crack_search(bmData As BitmapData, p As Byte(), fbits As BitArray, grayscale As Boolean, range As Integer, initialValueToCheck As Integer) As Boolean
        Dim left As Integer = 0
        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    If Not grayscale Then
                        left = p(y * stride + (x - 1) * 4 + 3)
                    Else
                        left = p(y * stride + (x - 1) * 4)
                    End If
                Else
                    left = initialValueToCheck
                End If

                If Not grayscale Then
                    If (left <= _threshold) AndAlso (p(y * stride + x * 4 + 3) > _threshold) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                        _start.X = x
                        _start.Y = y
                        fbits.[Set](y * (bmData.Width + 1) + x, True)
                        OnProgressPlus()
                        Return True
                    End If
                Else
                    If range > 0 Then
                        If (left <= _threshold) AndAlso (p(y * stride + x * 4) > _threshold) AndAlso (p(y * stride + x * 4) <= _threshold + range) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (left <= _threshold) AndAlso (p(y * stride + x * 4) > _threshold) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
            Next
        Next
        Return False
    End Function

    Private Function start_crack_searchRev(bmData As BitmapData, p As Byte(), fbits As BitArray, grayscale As Boolean, range As Integer, initialValueToCheck As Integer) As Boolean
        Dim left As Integer = 0
        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    If Not grayscale Then
                        left = p(y * stride + (x - 1) * 4 + 3)
                    Else
                        left = p(y * stride + (x - 1) * 4)
                    End If
                Else
                    left = initialValueToCheck
                End If

                If Not grayscale Then
                    If (left >= _threshold) AndAlso (p(y * stride + x * 4 + 3) < _threshold) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                        _start.X = x
                        _start.Y = y
                        fbits.[Set](y * (bmData.Width + 1) + x, True)
                        OnProgressPlus()
                        Return True
                    End If
                Else
                    If range > 0 Then
                        If (left >= _threshold) AndAlso (p(y * stride + x * 4) < _threshold) AndAlso (p(y * stride + x * 4) >= _threshold + range) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (left >= _threshold) AndAlso (p(y * stride + x * 4) < _threshold) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
            Next
        Next
        Return False
    End Function

    Public Function RemoveColinearity(fList As List(Of System.Drawing.Point), removeContiguousMultiples As Boolean) As List(Of System.Drawing.Point)
        Dim zList As New List(Of System.Drawing.Point)()

        If fList.Count > 0 Then
            zList.Add(fList(0))

            Dim i As Integer = 1
            Dim j As Integer = i

            While j < fList.Count - 1
                Dim c As Boolean = True

                While c AndAlso i < fList.Count - 1
                    Dim pt0 As System.Drawing.Point = fList(i)
                    Dim pt1 As System.Drawing.Point = fList(i - 1)
                    Dim pt2 As System.Drawing.Point = fList(i + 1)

                    Dim dx1 As Double = pt2.X - pt0.X
                    Dim dy1 As Double = pt2.Y - pt0.Y

                    Dim sl1 As Double = dy1
                    If dx1 <> 0.0 Then
                        sl1 = dy1 / dx1
                    End If

                    Dim dx2 As Double = pt0.X - pt1.X
                    Dim dy2 As Double = pt0.Y - pt1.Y

                    Dim sl2 As Double = dy2
                    If dy2 <> 0.0 Then
                        sl2 = dy2 / dx2
                    End If

                    If Math.Round(sl1, 4) <> Math.Round(sl2, 4) Then
                        zList.Add(fList(i))
                        c = False
                    End If

                    j = i
                    i += 1
                End While

                j += 1
            End While

            zList.Add(fList(fList.Count - 1))
        End If

        If removeContiguousMultiples Then
            For i As Integer = zList.Count - 2 To 0 Step -1
                If zList(i + 1) = zList(i) Then
                    zList.RemoveAt(i + 1)
                End If
            Next
        End If

        Return zList
    End Function

    Public Function RemoveColinearity(fList As List(Of System.Drawing.PointF), removeContiguousMultiples As Boolean) As List(Of System.Drawing.PointF)
        Dim zList As New List(Of System.Drawing.PointF)()

        If fList.Count > 0 Then
            zList.Add(fList(0))

            Dim i As Integer = 1
            Dim j As Integer = i

            While j < fList.Count - 1
                Dim c As Boolean = True

                While c AndAlso i < fList.Count - 1
                    Dim pt0 As System.Drawing.PointF = fList(i)
                    Dim pt1 As System.Drawing.PointF = fList(i - 1)
                    Dim pt2 As System.Drawing.PointF = fList(i + 1)

                    Dim dx1 As Double = pt2.X - pt0.X
                    Dim dy1 As Double = pt2.Y - pt0.Y

                    Dim sl1 As Double = dy1
                    If dx1 <> 0.0 Then
                        sl1 = dy1 / dx1
                    End If

                    Dim dx2 As Double = pt0.X - pt1.X
                    Dim dy2 As Double = pt0.Y - pt1.Y

                    Dim sl2 As Double = dy2
                    If dy2 <> 0.0 Then
                        sl2 = dy2 / dx2
                    End If

                    If Math.Round(sl1, 4) <> Math.Round(sl2, 4) Then
                        zList.Add(fList(i))
                        c = False
                    End If

                    j = i
                    i += 1
                End While

                j += 1
            End While

            zList.Add(fList(fList.Count - 1))
        End If

        If removeContiguousMultiples Then
            For i As Integer = zList.Count - 2 To 0 Step -1
                If zList(i + 1) = zList(i) Then
                    zList.RemoveAt(i + 1)
                End If
            Next
        End If

        Return zList
    End Function

    Public Function RemoveColinearity(fList As List(Of System.Drawing.Point), removeContiguousMultiples As Boolean,
                                      precision As Integer) As List(Of System.Drawing.Point)
        If precision < 1 Then
            precision = 1
        End If

        Dim zList As New List(Of System.Drawing.Point)()

        If fList.Count > 0 Then
            zList.Add(fList(0))

            Dim i As Integer = 1
            Dim j As Integer = i

            While j < fList.Count - 1
                Dim c As Boolean = True

                While c AndAlso i < fList.Count - 1
                    Dim pt0 As System.Drawing.Point = fList(i)
                    Dim pt1 As System.Drawing.Point = fList(i - 1)
                    Dim pt2 As System.Drawing.Point = fList(i + 1)

                    Dim dx1 As Double = pt2.X - pt0.X
                    Dim dy1 As Double = pt2.Y - pt0.Y

                    Dim sl1 As Double = dy1
                    If dx1 <> 0.0 Then
                        sl1 = dy1 / dx1
                    End If

                    Dim dx2 As Double = pt0.X - pt1.X
                    Dim dy2 As Double = pt0.Y - pt1.Y

                    Dim sl2 As Double = dy2
                    If dy2 <> 0.0 Then
                        sl2 = dy2 / dx2
                    End If

                    If Math.Round(sl1, precision) <> Math.Round(sl2, precision) Then
                        zList.Add(fList(i))
                        c = False
                    End If

                    j = i
                    i += 1
                End While

                j += 1
            End While

            zList.Add(fList(fList.Count - 1))
        End If

        If removeContiguousMultiples Then
            For i As Integer = zList.Count - 2 To 0 Step -1
                If zList(i + 1) = zList(i) Then
                    zList.RemoveAt(i + 1)
                End If
            Next
        End If

        Return zList
    End Function

    Public Function RemoveColinearity(fList As List(Of System.Drawing.PointF), removeContiguousMultiples As Boolean,
                                      precision As Integer) As List(Of System.Drawing.PointF)
        If precision < 1 Then
            precision = 1
        End If

        Dim zList As New List(Of System.Drawing.PointF)()

        If fList.Count > 0 Then
            zList.Add(fList(0))

            Dim i As Integer = 1
            Dim j As Integer = i

            While j < fList.Count - 1
                Dim c As Boolean = True

                While c AndAlso i < fList.Count - 1
                    Dim pt0 As System.Drawing.PointF = fList(i)
                    Dim pt1 As System.Drawing.PointF = fList(i - 1)
                    Dim pt2 As System.Drawing.PointF = fList(i + 1)

                    Dim dx1 As Double = pt2.X - pt0.X
                    Dim dy1 As Double = pt2.Y - pt0.Y

                    Dim sl1 As Double = dy1
                    If dx1 <> 0.0 Then
                        sl1 = dy1 / dx1
                    End If

                    Dim dx2 As Double = pt0.X - pt1.X
                    Dim dy2 As Double = pt0.Y - pt1.Y

                    Dim sl2 As Double = dy2
                    If dy2 <> 0.0 Then
                        sl2 = dy2 / dx2
                    End If

                    If Math.Round(sl1, precision) <> Math.Round(sl2, precision) Then
                        zList.Add(fList(i))
                        c = False
                    End If

                    j = i
                    i += 1
                End While

                j += 1
            End While

            zList.Add(fList(fList.Count - 1))
        End If

        If removeContiguousMultiples Then
            For i As Integer = zList.Count - 2 To 0 Step -1
                If zList(i + 1) = zList(i) Then
                    zList.RemoveAt(i + 1)
                End If
            Next
        End If

        Return zList
    End Function

    Public Function ApproximateLines(lList As List(Of Point), epsilon As Double) As List(Of Point)
        Dim strt As Integer = 0
        Dim [end] As Integer = 0

        Dim dx As Double = 0
        Dim dy As Double = 0

        Dim distPQ As Double = 0
        Dim distT As Double = 0

        Dim fList As New List(Of Point)()

        fList.Add(lList(0))

        For [end] = strt + 2 To lList.Count - 1
            Dim p As Point = lList(strt)
            Dim q As Point = lList([end])

            dx = q.X - p.X
            dy = q.Y - p.Y

            distPQ = Math.Sqrt(dx * dx + dy * dy)

            Dim dd As Double = dy / dx

            For i As Integer = strt + 1 To [end] - 1
                Dim t As Point = lList(i)

                Dim d As Double = q.Y - (q.X * dd)
                Dim vy As Double = ((t.X * dd) + d)
                Dim A As Double = (t.Y - vy) * dx

                distT = CDbl(Math.Abs(A)) / distPQ

                If dx = 0 Then
                    If t.X <> p.X Then
                        distT = Math.Abs(p.X - t.X)
                    End If
                End If

                If distT > epsilon Then
                    strt = [end] - 1
                    fList.Add(lList(strt))
                    Exit For
                End If
            Next
        Next

        If fList(fList.Count - 1).Equals(lList(lList.Count - 1)) = False Then
            fList.Add(lList(lList.Count - 1))
        End If

        CheckSlope(fList)

        Return fList
    End Function

    Public Function ApproximateLines(lList As List(Of PointF), epsilon As Double) As List(Of PointF)
        Dim strt As Integer = 0
        Dim [end] As Integer = 0

        Dim dx As Double = 0
        Dim dy As Double = 0

        Dim distPQ As Double = 0
        Dim distT As Double = 0

        Dim fList As New List(Of PointF)()

        fList.Add(lList(0))

        For [end] = strt + 2 To lList.Count - 1
            Dim p As PointF = lList(strt)
            Dim q As PointF = lList([end])

            dx = q.X - p.X
            dy = q.Y - p.Y

            distPQ = Math.Sqrt(dx * dx + dy * dy)

            Dim dd As Double = dy / dx

            For i As Integer = strt + 1 To [end] - 1
                Dim t As PointF = lList(i)

                Dim d As Double = q.Y - (q.X * dd)
                Dim vy As Double = ((t.X * dd) + d)
                Dim A As Double = (t.Y - vy) * dx

                distT = CDbl(Math.Abs(A)) / distPQ

                If dx = 0 Then
                    If t.X <> p.X Then
                        distT = Math.Abs(p.X - t.X)
                    End If
                End If

                If distT > epsilon Then
                    strt = [end] - 1
                    fList.Add(lList(strt))
                    Exit For
                End If
            Next
        Next

        If fList(fList.Count - 1).Equals(lList(lList.Count - 1)) = False Then
            fList.Add(lList(lList.Count - 1))
        End If

        CheckSlope(fList)

        Return fList
    End Function

    Private Sub CheckSlope(fList As List(Of Point))
        If fList.Count > 2 Then
            Dim c0 As Point = fList(fList.Count - 1)
            Dim cb0 As Point = fList(0)

            Dim slope As Double = 0.0
            Dim xF As Double = (CDbl(c0.X) - cb0.X)
            Dim b As Boolean = (xF = 0)

            If Not b Then
                slope = (CDbl(c0.Y) - cb0.Y) / xF
            End If

            For i As Integer = fList.Count - 2 To 0 Step -1
                Dim c As Point = fList(i)
                Dim cb As Point = fList(i + 1)
                Dim slopeC As Double = 0.0
                Dim xF2 As Double = (CDbl(c.X) - cb.X)
                Dim b2 As Boolean = (xF2 = 0)
                If b Then
                    If b2 Then
                        fList.RemoveAt(i + 1)
                    End If
                Else
                    slopeC = (CDbl(c.Y) - cb.Y) / xF2
                    If slopeC = slope Then
                        fList.RemoveAt(i + 1)
                    End If
                End If

                slope = slopeC
                b = b2
            Next
        End If
    End Sub

    Private Sub CheckSlope(fList As List(Of PointF))
        If fList.Count > 2 Then
            Dim c0 As PointF = fList(fList.Count - 1)
            Dim cb0 As PointF = fList(0)

            Dim slope As Double = 0.0
            Dim xF As Double = (CDbl(c0.X) - cb0.X)
            Dim b As Boolean = (xF = 0)

            If Not b Then
                slope = (CDbl(c0.Y) - cb0.Y) / xF
            End If

            For i As Integer = fList.Count - 2 To 0 Step -1
                Dim c As PointF = fList(i)
                Dim cb As PointF = fList(i + 1)
                Dim slopeC As Double = 0.0
                Dim xF2 As Double = (CDbl(c.X) - cb.X)
                Dim b2 As Boolean = (xF2 = 0)
                If b Then
                    If b2 Then
                        fList.RemoveAt(i + 1)
                    End If
                Else
                    slopeC = (CDbl(c.Y) - cb.Y) / xF2
                    If slopeC = slope Then
                        fList.RemoveAt(i + 1)
                    End If
                End If

                slope = slopeC
                b = b2
            Next
        End If
    End Sub

    Public Sub RemoveOutline(b As Bitmap, fList As List(Of ChainCode))
        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            If fList IsNot Nothing AndAlso fList.Count > 0 Then
                For Each c As ChainCode In fList
                    For i As Integer = 0 To c.Coord.Count - 1
                        Dim x As Integer = c.Coord(i).X
                        Dim y As Integer = c.Coord(i).Y

                        p(y * stride + x * 4 + 3) = CType(0, [Byte])
                    Next
                Next
            End If

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            p = Nothing
        Catch
            Try
                b.UnlockBits(bmData)
            Catch

            End Try
        End Try
    End Sub

    Public Sub RemoveOutline(b As Bitmap, fList As List(Of ChainCode), outerOnly As Boolean)
        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            If fList IsNot Nothing AndAlso fList.Count > 0 Then
                For Each c As ChainCode In fList
                    If outerOnly Then
                        If Not ChainFinder.IsInnerOutline(c) Then
                            For i As Integer = 0 To c.Coord.Count - 1
                                Dim x As Integer = c.Coord(i).X
                                Dim y As Integer = c.Coord(i).Y

                                p(y * stride + x * 4 + 3) = CType(0, [Byte])
                            Next
                        End If
                    Else
                        For i As Integer = 0 To c.Coord.Count - 1
                            Dim x As Integer = c.Coord(i).X
                            Dim y As Integer = c.Coord(i).Y

                            p(y * stride + x * 4 + 3) = CType(0, [Byte])
                        Next
                    End If
                Next
            End If

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            p = Nothing
        Catch
            Try
                b.UnlockBits(bmData)
            Catch

            End Try
        End Try
    End Sub

    Public Sub RemoveOutlineToBlack(b As Bitmap, fList As List(Of ChainCode))
        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            If fList IsNot Nothing AndAlso fList.Count > 0 Then
                For Each c As ChainCode In fList
                    For i As Integer = 0 To c.Coord.Count - 1
                        Dim x As Integer = c.Coord(i).X
                        Dim y As Integer = c.Coord(i).Y
                        p(y * stride + x * 4) = CType(0, [Byte])
                        p(y * stride + x * 4 + 1) = CType(0, [Byte])
                        p(y * stride + x * 4 + 2) = CType(0, [Byte])
                        p(y * stride + x * 4 + 3) = CType(255, [Byte])
                    Next
                Next
            End If

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            p = Nothing
        Catch
            Try
                b.UnlockBits(bmData)
            Catch

            End Try
        End Try
    End Sub

    Public Sub ShiftCoordValues(chainCode As ChainCodeF, shiftX As Single, shiftY As Single)
        Dim chain As List(Of Integer) = chainCode.Chain
        Dim coord As List(Of PointF) = chainCode.Coord

        If chain.Count <> coord.Count Then
            Throw New Exception("List lengths dont match.")
        End If

        Dim x1 As Single = coord.Min(Function(a)
                                         Return a.X
                                     End Function)
        Dim x2 As Single = coord.Max(Function(a)
                                         Return a.X
                                     End Function)
        Dim y1 As Single = coord.Min(Function(a)
                                         Return a.Y
                                     End Function)
        Dim y2 As Single = coord.Max(Function(a)
                                         Return a.Y
                                     End Function)

        Dim width As Single = x2 - x1
        Dim height As Single = y2 - y1
        Dim w As Single = x1 + (width / 2.0F)
        Dim h As Single = y1 + (height / 2.0F)

        Dim scaleX As Single = (width - (shiftX * 2.0F)) / width
        Dim scaleY As Single = (height - (shiftY * 2.0F)) / height

        'translate and shift
        For i As Integer = 0 To coord.Count - 1
            coord(i) = New PointF(coord(i).X - w, coord(i).Y - h)
            coord(i) = New PointF(coord(i).X * scaleX, coord(i).Y * scaleY)
            coord(i) = New PointF(coord(i).X + w, coord(i).Y + h)
        Next
    End Sub

    Public Sub ExtendOutline(b As Bitmap, fList As List(Of ChainCode))
        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            If fList IsNot Nothing AndAlso fList.Count > 0 Then
                For Each c As ChainCode In fList
                    For i As Integer = 0 To c.Coord.Count - 1
                        Dim x As Integer = c.Coord(i).X
                        Dim y As Integer = c.Coord(i).Y
                        Dim x2 As Integer = -1
                        Dim y2 As Integer = -1

                        Select Case c.Chain(i)
                            Case 0 'r
                                y += 1
                            Case 1 'd
                                x -= 1
                            Case 2 'l
                                y -= 1
                            Case 3 'u
                                x += 1
                        End Select

                        If i < c.Coord.Count - 1 Then
                            Select Case c.Chain(i + 1)
                                Case 0
                                    If c.Chain(i) = 1 Then
                                        x2 = x
                                        y2 = y + 1
                                    End If
                                Case 1
                                    If c.Chain(i) = 2 Then
                                        x2 = x - 1
                                        y2 = y
                                    End If
                                Case 2
                                    If c.Chain(i) = 3 Then
                                        x2 = x
                                        y2 = y - 1
                                    End If
                                Case 3
                                    If c.Chain(i) = 0 Then
                                        x2 = x + 1
                                        y2 = y
                                    End If
                            End Select
                        End If

                        If x > -1 AndAlso x < b.Width AndAlso y > -1 AndAlso y < b.Height Then
                            p(y * stride + x * 4 + 3) = CType(255, [Byte])
                            If x2 > -1 AndAlso x2 < b.Width AndAlso y2 > -1 AndAlso y2 < b.Height Then
                                p(y2 * stride + x2 * 4 + 3) = CType(255, [Byte])
                            End If
                        End If
                    Next
                Next
            End If

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            p = Nothing
        Catch
            Try
                b.UnlockBits(bmData)
            Catch

            End Try
        End Try
    End Sub

    Public Function GetOutline(bmp As Bitmap, breite As Integer, threshold As Integer, initialValueToCheck As Integer, doReverse As Boolean) As Bitmap
        Dim b As Bitmap = Nothing

        Try
            If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
                b = DirectCast(bmp.Clone(), Bitmap)
            End If

            Dim bl As Boolean = Me.AllowNullCells
            Me.AllowNullCells = True

            For i As Integer = 0 To breite - 1
                Me._start = New Point(0, 0)
                Dim fList As List(Of ChainCode) = Me.GetOutline(b, threshold, False, 0, False, initialValueToCheck, doReverse)
                RemoveOutline(b, fList)
            Next

            Me.AllowNullCells = bl
            Return b
        Catch
            If b IsNot Nothing Then
                b.Dispose()
            End If

            b = Nothing
        End Try

        Return Nothing
    End Function

    Protected Overridable Sub OnProgressPlus()
        'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
        'If handler IsNot Nothing Then
        RaiseEvent ProgressPlus(Me, New ProgEventArgs(_start.Y, _height))
        'End If
    End Sub

    '####################################

    Public Function GetOutline(bmp As Bitmap, threshold As Integer, grayscale As Boolean, incContrast As Integer, range As Integer, excludeInnerOutlines As Boolean) As List(Of ChainCode)
        Dim fbits As BitArray = Nothing
        _threshold = threshold
        _height = bmp.Height

        Dim bWork As Bitmap = Nothing

        Try
            Dim fList As New List(Of ChainCode)()

            If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
                bWork = CType(bmp.Clone, Bitmap)
            End If

            If grayscale Then
                GrayScaleImage(bWork)
            End If

            If incContrast <> 0 Then
                Contrast(bWork, incContrast)
            End If

            fbits = New BitArray((bWork.Width + 1) * bWork.Height, False)
            FindChainCode(bWork, fList, fbits, grayscale, range, excludeInnerOutlines)

            bWork.Dispose()

            Return fList
        Catch
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If

            If Not bWork Is Nothing Then
                bWork.Dispose()
            End If
        End Try

        Return Nothing
    End Function

    Public Sub GrayScaleImage(bmp As Bitmap)
        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
            Return
        End If

        Try
            bmData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.[WriteOnly], PixelFormat.Format32bppArgb)

            Dim w As Integer = bmp.Width
            Dim h As Integer = bmp.Height

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            'For y As Integer = 0 To bmData.Height - 1
            Parallel.For(0, h, Sub(y)
                                   Dim pos As Integer = 0
                                   pos += (y * bmData.Stride)

                                   For x As Integer = 0 To w - 1
                                       Dim value As Integer = CInt(CDbl(p(pos)) * 0.11 + CDbl(p(pos + 1)) * 0.59 + CDbl(p(pos + 2)) * 0.3)

                                       If value < 0 Then
                                           value = 0
                                       End If
                                       If value > 255 Then
                                           value = 255
                                       End If

                                       Dim v As Byte = CByte(value)
                                       p(pos) = v
                                       p(pos + 1) = v
                                       p(pos + 2) = v

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
    End Sub

    Public Sub Contrast(bmp As Bitmap, nContrast As Integer)
        If nContrast < -100 Then
            Return
        End If
        If nContrast > 100 Then
            Return
        End If

        Dim contrast__1 As Double = (100.0 + nContrast) / 100.0
        contrast__1 *= contrast__1

        Dim bmData As BitmapData = Nothing

        Try
            If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
                bmData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.[WriteOnly], PixelFormat.Format32bppArgb)
            Else
                MessageBox.Show("Not enough Memory")
                Return
            End If
            Dim w As Integer = bmp.Width
            Dim h As Integer = bmp.Height
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Parallel.For(0, h, Sub(y)
                                   'For y As Integer = 0 To h - 1
                                   Dim pos As Integer = 0
                                   pos += (y * stride)

                                   For x As Integer = 0 To w - 1
                                       Dim red As Double = CDbl(p(pos + 2))

                                       Dim fpixel As Double = 0

                                       fpixel = red / 255.0
                                       fpixel -= 0.5
                                       fpixel *= contrast__1
                                       fpixel += 0.5
                                       fpixel *= 255
                                       If fpixel < 0 Then
                                           fpixel = 0
                                       End If
                                       If fpixel > 255 Then
                                           fpixel = 255
                                       End If
                                       p(pos + 2) = CByte(fpixel)

                                       Dim green As Double = CDbl(p(pos + 1))

                                       fpixel = green / 255.0
                                       fpixel -= 0.5
                                       fpixel *= contrast__1
                                       fpixel += 0.5
                                       fpixel *= 255
                                       If fpixel < 0 Then
                                           fpixel = 0
                                       End If
                                       If fpixel > 255 Then
                                           fpixel = 255
                                       End If
                                       p(pos + 1) = CByte(fpixel)

                                       Dim blue As Double = CDbl(p(pos))

                                       fpixel = blue / 255.0
                                       fpixel -= 0.5
                                       fpixel *= contrast__1
                                       fpixel += 0.5
                                       fpixel *= 255
                                       If fpixel < 0 Then
                                           fpixel = 0
                                       End If
                                       If fpixel > 255 Then
                                           fpixel = 255
                                       End If
                                       p(pos) = CByte(fpixel)

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
    End Sub

    '####################################

    Public Sub DrawOutlineToBmp(b As Bitmap, orig As Bitmap, fList As List(Of ChainCode))
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 8L) Then
            MessageBox.Show("Not enough Memory")
            Return
        End If
        Dim bmData As BitmapData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
        Dim Scan0 As System.IntPtr = bmData.Scan0

        Dim bmDataOrig As BitmapData = orig.LockBits(New Rectangle(0, 0, orig.Width, orig.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
        Dim Scan0Orig As System.IntPtr = bmDataOrig.Scan0

        Dim p((bmData.Stride * bmData.Height) - 1) As Byte
        Marshal.Copy(bmData.Scan0, p, 0, p.Length)

        Dim pOrig((bmDataOrig.Stride * bmDataOrig.Height) - 1) As Byte
        Marshal.Copy(bmDataOrig.Scan0, pOrig, 0, pOrig.Length)

        Dim stride As Integer = bmData.Stride

        If fList IsNot Nothing AndAlso fList.Count > 0 Then
            For Each c As ChainCode In fList
                For i As Integer = 0 To c.Coord.Count - 1
                    Dim x As Integer = c.Coord(i).X
                    Dim y As Integer = c.Coord(i).Y

                    p(y * stride + x * 4) = pOrig(y * stride + x * 4)
                    p(y * stride + x * 4 + 1) = pOrig(y * stride + x * 4 + 1)
                    p(y * stride + x * 4 + 2) = pOrig(y * stride + x * 4 + 2)
                    p(y * stride + x * 4 + 3) = pOrig(y * stride + x * 4 + 3)

                    'p(y * stride + x * 4 + 3) = CByte(CDbl(pOrig(y * stride + x * 4 + 3)) * (alpha / 255.0))
                Next
            Next
        End If

        Marshal.Copy(p, 0, bmData.Scan0, p.Length)

        b.UnlockBits(bmData)
        orig.UnlockBits(bmDataOrig)

        p = Nothing
        pOrig = Nothing
    End Sub

    Public Function GetOutline(bmp As Bitmap, thresholdR As Integer, thresholdG As Integer, thresholdB As Integer,
                               doR As Boolean, doG As Boolean, doB As Boolean, rangeR As Integer, rangeG As Integer, rangeB As Integer,
                               excludeInnerOutlines As Boolean, initialValueToCheck As Integer, doReverse As Boolean) As List(Of ChainCode)

        Dim fbits As BitArray = Nothing
        _thresholdR = thresholdR
        _thresholdG = thresholdG
        _thresholdB = thresholdB
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)

            If doReverse Then
                FindChainCodeRev(bmp, fList, fbits, doR, doG, doB, rangeR, rangeG, rangeB, excludeInnerOutlines,
                 initialValueToCheck, Int32.MaxValue)
            Else
                FindChainCode(bmp, fList, fbits, doR, doG, doB, rangeR, rangeG, rangeB, excludeInnerOutlines,
                 initialValueToCheck, Int32.MaxValue)
            End If

            Return fList
        Catch
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCode(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray,
                              doR As Boolean, doG As Boolean, doB As Boolean, rangeR As Integer, rangeG As Integer, rangeB As Integer,
                              excludeInnerOutlines As Boolean, initialValueToCheck As Integer, maxIterations As Integer)

        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_search(bmData, p, fbits, doR, doG, doB, rangeR, rangeG, rangeB, initialValueToCheck) AndAlso fList.Count <= maxIterations
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) <= _thresholdR + rangeR))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) <= _thresholdG + rangeG))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _thresholdB + rangeB))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                LeftBeforeGreaterTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) <= _thresholdR + rangeR))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) <= _thresholdG + rangeG)))
                            Else
                                LeftBeforeGreaterTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG)

                            End If
                        End If
                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                LeftBeforeGreaterTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) <= _thresholdR + rangeR))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _thresholdB + rangeB)))
                            Else
                                LeftBeforeGreaterTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB)
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                LeftBeforeGreaterTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _thresholdB + rangeB))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) <= _thresholdG + rangeG)))
                            Else
                                LeftBeforeGreaterTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG)
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                LeftBeforeGreaterTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) <= _thresholdR + rangeR))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) <= _thresholdG + rangeG))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _thresholdB + rangeB)))
                            Else
                                LeftBeforeGreaterTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB)
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) <= _thresholdR + rangeR))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) <= _thresholdG + rangeG))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _thresholdB + rangeB))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                RightBeforeGreaterTh = (((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) <= _thresholdR + rangeR))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) <= _thresholdG + rangeG)))
                            Else
                                RightBeforeGreaterTh = (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG)
                            End If
                        End If

                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                RightBeforeGreaterTh = (((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) <= _thresholdR + rangeR))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _thresholdB + rangeB)))
                            Else
                                RightBeforeGreaterTh = (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB)
                            End If
                        End If

                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                RightBeforeGreaterTh = (((p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _thresholdB + rangeB))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) <= _thresholdG + rangeG)))
                            Else
                                RightBeforeGreaterTh = (p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG)
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                RightBeforeGreaterTh = (((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) <= _thresholdR + rangeR))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) <= _thresholdG + rangeG))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _thresholdB + rangeB)))
                            Else
                                RightBeforeGreaterTh = (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB)
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            b.UnlockBits(bmData)
            p = Nothing
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    Private Function start_crack_search(bmData As BitmapData, p As Byte(), fbits As BitArray, doRed As Boolean, doGreen As Boolean, doBlue As Boolean, rangeR As Integer,
        rangeG As Integer, rangeB As Integer, initialValue As Integer) As Boolean
        Dim leftR As Integer = 0
        Dim leftG As Integer = 0
        Dim leftB As Integer = 0

        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    leftR = p(y * stride + (x - 1) * 4 + 2)
                    leftG = p(y * stride + (x - 1) * 4 + 1)
                    leftB = p(y * stride + (x - 1) * 4)
                Else
                    leftR = initialValue
                    leftG = initialValue
                    leftB = initialValue
                End If

                '#Region "singleVal"
                If doRed AndAlso Not doGreen AndAlso Not doBlue Then
                    If rangeR > 0 Then
                        If (leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (p(y * stride + x * 4 + 2) <= _thresholdR + rangeR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeG > 0 Then
                        If (leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (p(y * stride + x * 4 + 1) <= _thresholdG + rangeG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso Not doGreen AndAlso doBlue Then
                    If rangeB > 0 Then
                        If (leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (p(y * stride + x * 4) <= _thresholdB + rangeB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "doubleVal"
                If doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeR > 0 OrElse rangeG > 0 Then
                        If ((leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (p(y * stride + x * 4 + 2) <= _thresholdR + rangeR)) AndAlso ((leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (p(y * stride + x * 4 + 1) <= _thresholdG + rangeG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR)) AndAlso ((leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If doRed AndAlso Not doGreen AndAlso doBlue Then

                    If rangeR > 0 OrElse rangeB > 0 Then
                        If ((leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (p(y * stride + x * 4 + 2) <= _thresholdR + rangeR)) AndAlso ((leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (p(y * stride + x * 4) <= _thresholdB + rangeB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR)) AndAlso ((leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso doBlue Then

                    If rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (p(y * stride + x * 4) <= _thresholdB + rangeB)) AndAlso ((leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (p(y * stride + x * 4 + 1) <= _thresholdG + rangeG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB)) AndAlso ((leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "allVals"
                If doRed AndAlso doGreen AndAlso doBlue Then

                    If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (p(y * stride + x * 4 + 2) <= _thresholdR + rangeR)) AndAlso ((leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (p(y * stride + x * 4 + 1) <= _thresholdG + rangeG)) AndAlso ((leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (p(y * stride + x * 4) <= _thresholdB + rangeB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR)) AndAlso
                            ((leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG)) AndAlso
                            ((leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then

                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                    '#End Region
                End If
            Next
        Next
        Return False
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeRev(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray,
                                 doR As Boolean, doG As Boolean, doB As Boolean, rangeR As Integer, rangeG As Integer, rangeB As Integer,
                                 excludeInnerOutlines As Boolean, initialValueToCheck As Integer, maxIterations As Integer)

        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeSmallerTh As Boolean, RightBeforeSmallerTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_searchRev(bmData, p, fbits, doR, doG, doB, rangeR, rangeG, rangeB, initialValueToCheck) AndAlso fList.Count <= maxIterations
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeSmallerTh = False
                    RightBeforeSmallerTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                LeftBeforeSmallerTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) >= _thresholdR - rangeR))
                            Else
                                LeftBeforeSmallerTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                LeftBeforeSmallerTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) >= _thresholdG - rangeG))
                            Else
                                LeftBeforeSmallerTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                LeftBeforeSmallerTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _thresholdB - rangeB))
                            Else
                                LeftBeforeSmallerTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                LeftBeforeSmallerTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) >= _thresholdR - rangeR))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) >= _thresholdG - rangeG)))
                            Else
                                LeftBeforeSmallerTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG)

                            End If
                        End If
                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                LeftBeforeSmallerTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) >= _thresholdR - rangeR))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _thresholdB - rangeB)))
                            Else
                                LeftBeforeSmallerTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB)
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                LeftBeforeSmallerTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _thresholdB - rangeB))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) >= _thresholdG - rangeG)))
                            Else
                                LeftBeforeSmallerTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG)
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                LeftBeforeSmallerTh = (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) >= _thresholdR - rangeR))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) >= _thresholdG - rangeG))) AndAlso (((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _thresholdB - rangeB)))
                            Else
                                LeftBeforeSmallerTh = (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB)
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                RightBeforeSmallerTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) >= _thresholdR - rangeR))
                            Else
                                RightBeforeSmallerTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                RightBeforeSmallerTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) >= _thresholdG - rangeG))
                            Else
                                RightBeforeSmallerTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                RightBeforeSmallerTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _thresholdB - rangeB))
                            Else
                                RightBeforeSmallerTh = p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                RightBeforeSmallerTh = (((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) >= _thresholdR - rangeR))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) >= _thresholdG - rangeG)))
                            Else
                                RightBeforeSmallerTh = (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG)
                            End If
                        End If

                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                RightBeforeSmallerTh = (((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) >= _thresholdR - rangeR))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _thresholdB - rangeB)))
                            Else
                                RightBeforeSmallerTh = (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB)
                            End If
                        End If

                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                RightBeforeSmallerTh = (((p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _thresholdB - rangeB))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) >= _thresholdG - rangeG)))
                            Else
                                RightBeforeSmallerTh = (p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG)
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                RightBeforeSmallerTh = (((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) >= _thresholdR - rangeR))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) >= _thresholdG - rangeG))) AndAlso (((p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _thresholdB - rangeB)))
                            Else
                                RightBeforeSmallerTh = (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB)
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBeforeSmallerTh AndAlso (LeftBeforeSmallerTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeSmallerTh AndAlso (Not RightBeforeSmallerTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            b.UnlockBits(bmData)
            p = Nothing
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    Private Function start_crack_searchRev(bmData As BitmapData, p As Byte(), fbits As BitArray, doRed As Boolean, doGreen As Boolean, doBlue As Boolean, rangeR As Integer,
        rangeG As Integer, rangeB As Integer, initialValue As Integer) As Boolean
        Dim leftR As Integer = 0
        Dim leftG As Integer = 0
        Dim leftB As Integer = 0

        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    leftR = p(y * stride + (x - 1) * 4 + 2)
                    leftG = p(y * stride + (x - 1) * 4 + 1)
                    leftB = p(y * stride + (x - 1) * 4)
                Else
                    leftR = initialValue
                    leftG = initialValue
                    leftB = initialValue
                End If

                '#Region "singleVal"
                If doRed AndAlso Not doGreen AndAlso Not doBlue Then
                    If rangeR > 0 Then
                        If (leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (leftR < _thresholdR + rangeR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeG > 0 Then
                        If (leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (leftG < _thresholdG + rangeG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso Not doGreen AndAlso doBlue Then
                    If rangeB > 0 Then
                        If (leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (leftB < _thresholdB + rangeB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "doubleVal"
                If doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeR > 0 OrElse rangeG > 0 Then
                        If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (leftR < _thresholdR + rangeR)) AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (leftG < _thresholdG + rangeG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR)) AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If doRed AndAlso Not doGreen AndAlso doBlue Then

                    If rangeR > 0 OrElse rangeB > 0 Then
                        If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (leftR < _thresholdR + rangeR)) AndAlso ((leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (leftB < _thresholdB + rangeB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR)) AndAlso ((leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso doBlue Then

                    If rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (leftB < _thresholdB + rangeB)) AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (leftG < _thresholdG + rangeG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB)) AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "allVals"
                If doRed AndAlso doGreen AndAlso doBlue Then

                    If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (leftR < _thresholdR + rangeR)) AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (leftG < _thresholdG + rangeG)) AndAlso ((leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (leftB < _thresholdB + rangeB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR)) AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG)) AndAlso ((leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                    '#End Region
                End If
            Next
        Next
        Return False
    End Function

    Public Sub FillOutlinesInBmp(b As Bitmap, orig As Bitmap, fList As List(Of ChainCode))
        If fList IsNot Nothing AndAlso fList.Count > 0 Then
            For Each c As ChainCode In fList
                Using gP As New Drawing2D.GraphicsPath
                    gP.AddLines(c.Coord.ToArray)

                    Using tb As New TextureBrush(orig)
                        Using g As Graphics = Graphics.FromImage(b)
                            g.FillPath(tb, gP)
                        End Using
                    End Using
                End Using
            Next
        End If
    End Sub

    Public Sub FillOutlinesInBmp(b As Bitmap, orig As Bitmap, gP As GraphicsPath, addHalfForDrawing As Boolean)
        If gP IsNot Nothing AndAlso gP.PointCount > 1 Then
            Using tb As New TextureBrush(orig)
                If addHalfForDrawing Then
                    tb.TranslateTransform(-0.5F, -0.5F)
                End If
                Using g As Graphics = Graphics.FromImage(b)
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    g.FillPath(tb, gP)
                End Using
            End Using
        End If
    End Sub

    Public Sub FillOutlinesInBmp(b As Bitmap, orig As Bitmap, fList As List(Of ChainCode), cleaned As Boolean, approxLines As Boolean,
                                 epsilon As Double, closeFigures As Boolean, connectPaths As Boolean, addCurves As Boolean,
                                 closedCurve As Boolean, shift As Boolean, shiftInwards As Single, precision As Integer,
                                 mapLargestFigureOnly As Boolean, stretchPath As Boolean, fractionMode As ShiftFractionMode)
        If fList IsNot Nothing AndAlso fList.Count > 0 Then
            Dim onlyLargest As Boolean = False
            If mapLargestFigureOnly Then
                fList = fList.OrderByDescending(Function(a) a.Coord.Count).ToList()
                onlyLargest = True
            End If
            For Each c As ChainCode In fList
                Using gP As New Drawing2D.GraphicsPath
                    Dim lList As List(Of Point) = c.Coord

                    Dim rlp2 As Boolean = False
                    If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) = lList(0) Then 'close
                        lList.RemoveAt(lList.Count - 1)
                        rlp2 = True
                    End If

                    If Not shift Then
                        If addCurves Then
                            If closedCurve Then
                                lList = RemoveColinearity(lList, True, precision)

                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                'If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) <> lList(0) Then
                                '    lList.Add(lList(0))
                                'End If

                                If closedCurve Then
                                    If lList.Count > 2 Then
                                        Dim rlp As Boolean = False
                                        If lList(lList.Count - 1).X = lList(0).X AndAlso lList(lList.Count - 1).Y = lList(0).Y Then
                                            lList.RemoveAt(lList.Count - 1)
                                            rlp = True
                                        End If
                                        gP.AddClosedCurve(lList.ToArray())
                                        If rlp Then
                                            lList.Add(lList(0))
                                        End If
                                    Else
                                        If lList.Count > 1 Then
                                            gP.AddLines(lList.ToArray())
                                        Else
                                            Dim p As Point = lList(0)
                                            gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                        End If
                                    End If
                                Else
                                    If lList.Count > 1 Then
                                        gP.AddCurve(lList.ToArray())
                                    Else
                                        Dim p As Point = lList(0)
                                        gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                    End If
                                End If
                            Else
                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                'If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) <> lList(0) Then
                                '    lList.Add(lList(0))
                                'End If

                                If closedCurve Then
                                    If lList.Count > 2 Then
                                        Dim rlp As Boolean = False
                                        If lList(lList.Count - 1).X = lList(0).X AndAlso lList(lList.Count - 1).Y = lList(0).Y Then
                                            lList.RemoveAt(lList.Count - 1)
                                            rlp = True
                                        End If
                                        gP.AddClosedCurve(lList.ToArray())
                                        If rlp Then
                                            lList.Add(lList(0))
                                        End If
                                    Else
                                        If lList.Count > 1 Then
                                            gP.AddLines(lList.ToArray())
                                        Else
                                            Dim p As Point = lList(0)
                                            gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                        End If
                                    End If
                                Else
                                    If lList.Count > 1 Then
                                        gP.AddCurve(lList.ToArray())
                                    Else
                                        Dim p As Point = lList(0)
                                        gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                    End If
                                End If
                            End If
                        Else
                            If closedCurve Then
                                lList = RemoveColinearity(lList, True, precision)

                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                gP.AddLines(lList.ToArray())
                            Else
                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                gP.AddLines(lList.ToArray())
                            End If

                            If closeFigures Then
                                gP.CloseFigure()
                            End If
                        End If
                    Else
                        gP.AddLines(lList.ToArray())
                    End If

                    If rlp2 Then
                        lList.Add(lList(0))
                    End If

                    If shift Then
                        Dim gP2 As GraphicsPathData = ShiftCoords(gP, shiftInwards, mapLargestFigureOnly, stretchPath, fractionMode)

                        If Not gP2 Is Nothing AndAlso Not gP2.AllPoints Is Nothing AndAlso gP2.AllPoints.Count > 0 Then
                            Using gP41 As New GraphicsPath()
                                For cnt As Integer = 0 To gP2.AllPoints.Count - 1
                                    Using gp4 As New GraphicsPath()
                                        Dim lList2 As List(Of PointF) = gP2.AllPoints(cnt)

                                        Dim rlp4 As Boolean = False
                                        If lList2.Count > 0 AndAlso closeFigures AndAlso lList2(lList2.Count - 1) = lList2(0) Then 'close
                                            lList2.RemoveAt(lList2.Count - 1)
                                            rlp4 = True
                                        End If

                                        Try
                                            If lList2.Count > 4 Then
                                                If addCurves Then
                                                    If closedCurve Then
                                                        lList2 = RemoveColinearity(lList2, True, precision)

                                                        If approxLines Then
                                                            lList2 = ApproximateLines(lList2, epsilon)
                                                        End If

                                                        'If lList2.Count > 0 AndAlso closeFigures AndAlso lList2(lList2.Count - 1) <> lList2(0) Then
                                                        '    lList2.Add(lList2(0))
                                                        'End If

                                                        If closedCurve Then
                                                            If lList2.Count > 2 Then
                                                                Dim rlp As Boolean = False
                                                                If lList2(lList2.Count - 1).X = lList2(0).X AndAlso lList2(lList2.Count - 1).Y = lList2(0).Y Then
                                                                    lList2.RemoveAt(lList2.Count - 1)
                                                                    rlp = True
                                                                End If
                                                                gp4.AddClosedCurve(lList2.ToArray())
                                                                If rlp Then
                                                                    lList2.Add(lList2(0))
                                                                End If
                                                            Else
                                                                If lList2.Count > 1 Then
                                                                    gp4.AddLines(lList2.ToArray())
                                                                Else
                                                                    Dim p As PointF = lList2(0)
                                                                    gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                                End If
                                                            End If
                                                        Else
                                                            If lList2.Count > 1 Then
                                                                gp4.AddCurve(lList2.ToArray())
                                                            Else
                                                                Dim p As PointF = lList2(0)
                                                                gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                            End If
                                                        End If
                                                    Else
                                                        If approxLines Then
                                                            lList2 = ApproximateLines(lList2, epsilon)
                                                        End If

                                                        'If lList2.Count > 0 AndAlso closeFigures AndAlso lList2(lList2.Count - 1) <> lList2(0) Then
                                                        '    lList2.Add(lList2(0))
                                                        'End If

                                                        If closedCurve Then
                                                            If lList2.Count > 2 Then
                                                                Dim rlp As Boolean = False
                                                                If lList2(lList2.Count - 1).X = lList2(0).X AndAlso lList2(lList2.Count - 1).Y = lList2(0).Y Then
                                                                    lList2.RemoveAt(lList2.Count - 1)
                                                                    rlp = True
                                                                End If
                                                                gp4.AddClosedCurve(lList2.ToArray())
                                                                If rlp Then
                                                                    lList2.Add(lList2(0))
                                                                End If
                                                            Else
                                                                If lList2.Count > 1 Then
                                                                    gp4.AddLines(lList2.ToArray())
                                                                Else
                                                                    Dim p As PointF = lList2(0)
                                                                    gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                                End If
                                                            End If
                                                        Else
                                                            If lList2.Count > 1 Then
                                                                gp4.AddCurve(lList2.ToArray())
                                                            Else
                                                                Dim p As PointF = lList2(0)
                                                                gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                            End If
                                                        End If
                                                    End If
                                                Else
                                                    If closedCurve Then
                                                        lList2 = RemoveColinearity(lList2, True, precision)

                                                        If approxLines Then
                                                            lList2 = ApproximateLines(lList2, epsilon)
                                                        End If

                                                        gp4.AddLines(lList2.ToArray())
                                                    Else
                                                        If approxLines Then
                                                            lList2 = ApproximateLines(lList2, epsilon)
                                                        End If

                                                        gp4.AddLines(lList2.ToArray())
                                                    End If

                                                    If closeFigures Then
                                                        gp4.CloseFigure()
                                                    End If
                                                End If

                                                If gp4.PointCount > 1 Then
                                                    gp4.FillMode = FillMode.Winding
                                                    gP41.AddPath(gp4, False)

                                                    Using tb As New TextureBrush(orig)
                                                        Using g As Graphics = Graphics.FromImage(b)
                                                            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                                            g.FillPath(tb, gP41)
                                                        End Using
                                                    End Using
                                                End If
                                            End If
                                        Catch

                                        End Try

                                        If rlp4 Then
                                            lList2.Add(lList2(0))
                                        End If
                                    End Using
                                Next
                            End Using
                        End If
                    Else
                        Using tb As New TextureBrush(orig)
                            Using g As Graphics = Graphics.FromImage(b)
                                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                g.FillPath(tb, gP)
                            End Using
                        End Using
                    End If
                End Using
                If onlyLargest Then
                    Exit For
                End If
            Next
        End If
    End Sub

    Public Sub FillOutlinesInBmp2(b As Bitmap, orig As Bitmap, fList As List(Of ChainCode), cleaned As Boolean, approxLines As Boolean,
                                 epsilon As Double, closeFigures As Boolean, connectPaths As Boolean, addCurves As Boolean,
                                 closedCurve As Boolean, shift As Boolean, shiftInwards As Single, precision As Integer,
                                 mapLargestFigureOnly As Boolean, fractionMode As ShiftFractionMode)
        If fList IsNot Nothing AndAlso fList.Count > 0 Then
            Dim onlyLargest As Boolean = False
            If mapLargestFigureOnly Then
                fList = fList.OrderByDescending(Function(a) a.Coord.Count).ToList()
                onlyLargest = True
            End If
            For Each c As ChainCode In fList
                Using gP As New Drawing2D.GraphicsPath
                    Dim lList As List(Of Point) = c.Coord

                    Dim rlp2 As Boolean = False
                    If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) = lList(0) Then 'close
                        lList.RemoveAt(lList.Count - 1)
                        rlp2 = True
                    End If

                    If Not shift Then
                        If addCurves Then
                            If closedCurve Then
                                lList = RemoveColinearity(lList, True, precision)

                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                'If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) <> lList(0) Then
                                '    lList.Add(lList(0))
                                'End If

                                If closedCurve Then
                                    If lList.Count > 2 Then
                                        Dim rlp As Boolean = False
                                        If lList(lList.Count - 1).X = lList(0).X AndAlso lList(lList.Count - 1).Y = lList(0).Y Then
                                            lList.RemoveAt(lList.Count - 1)
                                            rlp = True
                                        End If
                                        gP.AddClosedCurve(lList.ToArray())
                                        If rlp Then
                                            lList.Add(lList(0))
                                        End If
                                    Else
                                        If lList.Count > 1 Then
                                            gP.AddLines(lList.ToArray())
                                        Else
                                            Dim p As Point = lList(0)
                                            gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                        End If
                                    End If
                                Else
                                    If lList.Count > 1 Then
                                        gP.AddCurve(lList.ToArray())
                                    Else
                                        Dim p As Point = lList(0)
                                        gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                    End If
                                End If
                            Else
                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                'If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) <> lList(0) Then
                                '    lList.Add(lList(0))
                                'End If

                                If closedCurve Then
                                    If lList.Count > 2 Then
                                        Dim rlp As Boolean = False
                                        If lList(lList.Count - 1).X = lList(0).X AndAlso lList(lList.Count - 1).Y = lList(0).Y Then
                                            lList.RemoveAt(lList.Count - 1)
                                            rlp = True
                                        End If
                                        gP.AddClosedCurve(lList.ToArray())
                                        If rlp Then
                                            lList.Add(lList(0))
                                        End If
                                    Else
                                        If lList.Count > 1 Then
                                            gP.AddLines(lList.ToArray())
                                        Else
                                            Dim p As Point = lList(0)
                                            gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                        End If
                                    End If
                                Else
                                    If lList.Count > 1 Then
                                        gP.AddCurve(lList.ToArray())
                                    Else
                                        Dim p As Point = lList(0)
                                        gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                    End If
                                End If
                            End If
                        Else
                            If closedCurve Then
                                lList = RemoveColinearity(lList, True, precision)

                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                gP.AddLines(lList.ToArray())
                            Else
                                If approxLines Then
                                    lList = ApproximateLines(lList, epsilon)
                                End If

                                gP.AddLines(lList.ToArray())
                            End If

                            If closeFigures Then
                                gP.CloseFigure()
                            End If
                        End If
                    Else
                        gP.AddLines(lList.ToArray())
                    End If

                    If rlp2 Then
                        lList.Add(lList(0))
                    End If

                    If shift Then
                        Dim gP2 As GraphicsPath = ShiftCoords2(gP, shiftInwards, mapLargestFigureOnly, fractionMode)

                        If Not gP2 Is Nothing AndAlso gP2.PointCount > 1 Then
                            Dim lL As List(Of List(Of PointF)) = SplitPath(gP2)

                            For j As Integer = 0 To lL.Count - 1
                                Dim rlp4 As Boolean = False
                                If lL(j).Count > 0 AndAlso closeFigures AndAlso lL(j)(lL(j).Count - 1) = lL(j)(0) Then 'close
                                    lL(j).RemoveAt(lL(j).Count - 1)
                                    rlp4 = True
                                End If

                                Using gp4 As New GraphicsPath()
                                    Try
                                        If lL(j).Count > 4 Then
                                            If addCurves Then
                                                If closedCurve Then
                                                    lL(j) = RemoveColinearity(lL(j), True, precision)

                                                    If approxLines Then
                                                        lL(j) = ApproximateLines(lL(j), epsilon)
                                                    End If

                                                    'If lL(j).Count > 0 AndAlso closeFigures AndAlso lL(j)(lL(j).Count - 1) <> lL(j)(0) Then
                                                    '    lL(j).Add(lL(j)(0))
                                                    'End If

                                                    If closedCurve Then
                                                        If lL(j).Count > 2 Then
                                                            Dim rlp As Boolean = False
                                                            If lL(j)(lL(j).Count - 1).X = lL(j)(0).X AndAlso lL(j)(lL(j).Count - 1).Y = lL(j)(0).Y Then
                                                                lL(j).RemoveAt(lL(j).Count - 1)
                                                                rlp = True
                                                            End If
                                                            gp4.AddClosedCurve(lL(j).ToArray())
                                                            If rlp Then
                                                                lL(j).Add(lL(j)(0))
                                                            End If
                                                        Else
                                                            If lL(j).Count > 1 Then
                                                                gp4.AddLines(lL(j).ToArray())
                                                            Else
                                                                Dim p As PointF = lL(j)(0)
                                                                gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                            End If
                                                        End If
                                                    Else
                                                        If lL(j).Count > 1 Then
                                                            gp4.AddCurve(lL(j).ToArray())
                                                        Else
                                                            Dim p As PointF = lL(j)(0)
                                                            gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                        End If
                                                    End If
                                                Else
                                                    If approxLines Then
                                                        lL(j) = ApproximateLines(lL(j), epsilon)
                                                    End If

                                                    'If lL(j).Count > 0 AndAlso closeFigures AndAlso lL(j)(lL(j).Count - 1) <> lL(j)(0) Then
                                                    '    lL(j).Add(lL(j)(0))
                                                    'End If

                                                    If closedCurve Then
                                                        If lL(j).Count > 2 Then
                                                            Dim rlp As Boolean = False
                                                            If lL(j)(lL(j).Count - 1).X = lL(j)(0).X AndAlso lL(j)(lL(j).Count - 1).Y = lL(j)(0).Y Then
                                                                lL(j).RemoveAt(lL(j).Count - 1)
                                                                rlp = True
                                                            End If
                                                            gp4.AddClosedCurve(lL(j).ToArray())
                                                            If rlp Then
                                                                lL(j).Add(lL(j)(0))
                                                            End If
                                                        Else
                                                            If lL(j).Count > 1 Then
                                                                gp4.AddLines(lL(j).ToArray())
                                                            Else
                                                                Dim p As PointF = lL(j)(0)
                                                                gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                            End If
                                                        End If
                                                    Else
                                                        If lL(j).Count > 1 Then
                                                            gp4.AddCurve(lL(j).ToArray())
                                                        Else
                                                            Dim p As PointF = lL(j)(0)
                                                            gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
                                                        End If
                                                    End If
                                                End If
                                            Else
                                                If closedCurve Then
                                                    lL(j) = RemoveColinearity(lL(j), True, precision)

                                                    If approxLines Then
                                                        lL(j) = ApproximateLines(lL(j), epsilon)
                                                    End If

                                                    gp4.AddLines(lL(j).ToArray())
                                                Else
                                                    If approxLines Then
                                                        lL(j) = ApproximateLines(lL(j), epsilon)
                                                    End If

                                                    gp4.AddLines(lL(j).ToArray())
                                                End If

                                                If closeFigures Then
                                                    gp4.CloseFigure()
                                                End If
                                            End If

                                            If gp4.PointCount > 1 Then
                                                gp4.FillMode = FillMode.Winding

                                                Using tb As New TextureBrush(orig)
                                                    Using g As Graphics = Graphics.FromImage(b)
                                                        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                                        g.FillPath(tb, gp4)
                                                    End Using
                                                End Using
                                            End If
                                        End If
                                    Catch

                                    End Try
                                End Using

                                If rlp4 Then
                                    lL(j).Add(lL(j)(0))
                                End If
                            Next
                        End If
                    Else
                        Using tb As New TextureBrush(orig)
                            Using g As Graphics = Graphics.FromImage(b)
                                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                                g.FillPath(tb, gP)
                            End Using
                        End Using
                    End If
                End Using
                If onlyLargest Then
                    Exit For
                End If
            Next
        End If
    End Sub

    Public Function SplitPath(gP As GraphicsPath) As List(Of List(Of PointF))
        If gP.PointCount > 1 Then
            'do it separately for each PathFigure...
            Dim t(gP.PathTypes.Length - 1) As Byte
            gP.PathTypes.CopyTo(t, 0)
            Dim pts(gP.PathPoints.Length - 1) As PointF
            gP.PathPoints.CopyTo(pts, 0)

            Dim pTAll As New List(Of List(Of Byte))
            Dim pPAll As New List(Of List(Of PointF))

            Dim cnt As Integer = 0

            While cnt < t.Length
                Dim pT As New List(Of Byte)
                Dim pP As New List(Of PointF)

                pP.Add(pts(cnt))
                pT.Add(0) 'pT.Add(t(cnt))
                cnt += 1

                While cnt < t.Length AndAlso Not t(cnt) = 0
                    pP.Add(pts(cnt))
                    pT.Add(t(cnt))
                    cnt += 1
                End While

                pPAll.Add(pP)
                pT(pT.Count - 1) = 129 'fffffff
                pTAll.Add(pT)
            End While

            pPAll = pPAll.OrderByDescending(Function(a) a.Count).ToList()
            pTAll = pTAll.OrderByDescending(Function(a) a.Count).ToList()

            Return pPAll
        End If

        Return Nothing
    End Function

    'Public Sub FillOutlinesInBmp2(b As Bitmap, orig As Bitmap, fList As List(Of ChainCode), cleaned As Boolean, approxLines As Boolean,
    '                             epsilon As Double, closeFigures As Boolean, connectPaths As Boolean, addCurves As Boolean,
    '                             closedCurve As Boolean, shift As Boolean, shiftInwards As Single, precision As Integer,
    '                             mapLargestFigureOnly As Boolean)
    '    If fList IsNot Nothing AndAlso fList.Count > 0 Then
    '        For Each c As ChainCode In fList
    '            Using gP As New Drawing2D.GraphicsPath
    '                Dim lList As List(Of Point) = c.Coord

    '                If Not shift Then
    '                    If addCurves Then
    '                        If closedCurve Then
    '                            lList = RemoveColinearity(lList, True, precision)

    '                            If approxLines Then
    '                                lList = ApproximateLines(lList, epsilon)
    '                            End If

    '                            If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) <> lList(0) Then
    '                                lList.Add(lList(0))
    '                            End If

    '                            If closedCurve Then
    '                                If lList.Count > 2 Then
    'Dim rlp As Boolean = False
    'If lList(lList.Count - 1).X = lList(0).X AndAlso lList(lList.Count - 1).Y = lList(0).Y Then
    '                                    lList.RemoveAt(lList.Count - 1)
    '                                    rlp = True
    '                                End If
    '                                    gP.AddClosedCurve(lList.ToArray())
    'If rlp Then
    '                                    lList.Add(lList(0))
    '                                End If
    '                                Else
    '                                    If lList.Count > 1 Then
    '                                        gP.AddLines(lList.ToArray())
    '                                    Else
    '                                        Dim p As Point = lList(0)
    '                                        gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                    End If
    '                                End If
    '                            Else
    '                                If lList.Count > 1 Then
    '                                    gP.AddCurve(lList.ToArray())
    '                                Else
    '                                    Dim p As Point = lList(0)
    '                                    gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                End If
    '                            End If
    '                        Else
    '                            If approxLines Then
    '                                lList = ApproximateLines(lList, epsilon)
    '                            End If

    '                            If lList.Count > 0 AndAlso closeFigures AndAlso lList(lList.Count - 1) <> lList(0) Then
    '                                lList.Add(lList(0))
    '                            End If

    '                            If closedCurve Then
    '                                If lList.Count > 2 Then
    'Dim rlp As Boolean = False
    'If lList(lList.Count - 1).X = lList(0).X AndAlso lList(lList.Count - 1).Y = lList(0).Y Then
    '                                    lList.RemoveAt(lList.Count - 1)
    '                                    rlp = True
    '                                End If
    '                                    gP.AddClosedCurve(lList.ToArray())
    'If rlp Then
    '                                    lList.Add(lList(0))
    '                                End If
    '                                Else
    '                                    If lList.Count > 1 Then
    '                                        gP.AddLines(lList.ToArray())
    '                                    Else
    '                                        Dim p As Point = lList(0)
    '                                        gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                    End If
    '                                End If
    '                            Else
    '                                If lList.Count > 1 Then
    '                                    gP.AddCurve(lList.ToArray())
    '                                Else
    '                                    Dim p As Point = lList(0)
    '                                    gP.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                End If
    '                            End If
    '                        End If
    '                    Else
    '                        If closedCurve Then
    '                            lList = RemoveColinearity(lList, True, precision)

    '                            If approxLines Then
    '                                lList = ApproximateLines(lList, epsilon)
    '                            End If

    '                            gP.AddLines(lList.ToArray())
    '                        Else
    '                            If approxLines Then
    '                                lList = ApproximateLines(lList, epsilon)
    '                            End If

    '                            gP.AddLines(lList.ToArray())
    '                        End If

    '                        If closeFigures Then
    '                            gP.CloseFigure()
    '                        End If
    '                    End If
    '                Else
    '                    gP.AddLines(lList.ToArray())
    '                End If

    '                If shift Then
    '                    Dim gP2 As GraphicsPath = ShiftCoords2(gP, shiftInwards, mapLargestFigureOnly)

    '                    If Not gP2 Is Nothing AndAlso gP2.PointCount > 1 Then
    '                        Using gP41 As New GraphicsPath()

    '                            Using gp4 As New GraphicsPath()
    '                                Dim lList2 As List(Of PointF) = gP2.PathPoints.ToList()
    '                                Try
    '                                    If lList2.Count > 4 Then
    '                                        If addCurves Then
    '                                            If closedCurve Then
    '                                                lList2 = RemoveColinearity(lList2, True, precision)

    '                                                If approxLines Then
    '                                                    lList2 = ApproximateLines(lList2, epsilon)
    '                                                End If

    '                                                If lList2.Count > 0 AndAlso closeFigures AndAlso lList2(lList2.Count - 1) <> lList2(0) Then
    '                                                    lList2.Add(lList2(0))
    '                                                End If

    '                                                If closedCurve Then
    '                                                    If lList2.Count > 2 Then
    'Dim rlp As Boolean = False
    'If lList2(lList2.Count - 1).X = lList2(0).X AndAlso lList2(lList2.Count - 1).Y = lList2(0).Y Then
    '                                    lList2.RemoveAt(lList2.Count - 1)
    '                                    rlp = True
    '                                End If
    '                                                        gp4.AddClosedCurve(lList2.ToArray())
    'If rlp Then
    '                                    lList2.Add(lList2(0))
    '                                End If
    '                                                    Else
    '                                                        If lList2.Count > 1 Then
    '                                                            gp4.AddLines(lList2.ToArray())
    '                                                        Else
    '                                                            Dim p As PointF = lList2(0)
    '                                                            gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                                        End If
    '                                                    End If
    '                                                Else
    '                                                    If lList2.Count > 1 Then
    '                                                        gp4.AddCurve(lList2.ToArray())
    '                                                    Else
    '                                                        Dim p As PointF = lList2(0)
    '                                                        gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                                    End If
    '                                                End If
    '                                            Else
    '                                                If approxLines Then
    '                                                    lList2 = ApproximateLines(lList2, epsilon)
    '                                                End If

    '                                                If lList2.Count > 0 AndAlso closeFigures AndAlso lList2(lList2.Count - 1) <> lList2(0) Then
    '                                                    lList2.Add(lList2(0))
    '                                                End If

    '                                                If closedCurve Then
    '                                                    If lList2.Count > 2 Then
    'Dim rlp As Boolean = False
    'If lList2(lList2.Count - 1).X = lList2(0).X AndAlso lList2(lList2.Count - 1).Y = lList2(0).Y Then
    '                                    lList2.RemoveAt(lList2.Count - 1)
    '                                    rlp = True
    '                                End If
    '                                                        gp4.AddClosedCurve(lList2.ToArray())
    'If rlp Then
    '                                    lList2.Add(lList2(0))
    '                                End If
    '                                                    Else
    '                                                        If lList2.Count > 1 Then
    '                                                            gp4.AddLines(lList2.ToArray())
    '                                                        Else
    '                                                            Dim p As PointF = lList2(0)
    '                                                            gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                                        End If
    '                                                    End If
    '                                                Else
    '                                                    If lList2.Count > 1 Then
    '                                                        gp4.AddCurve(lList2.ToArray())
    '                                                    Else
    '                                                        Dim p As PointF = lList2(0)
    '                                                        gp4.AddRectangle(New RectangleF(p.X, p.Y, 1.0F, 1.0F))
    '                                                    End If
    '                                                End If
    '                                            End If
    '                                        Else
    '                                            If closedCurve Then
    '                                                lList2 = RemoveColinearity(lList2, True, precision)

    '                                                If approxLines Then
    '                                                    lList2 = ApproximateLines(lList2, epsilon)
    '                                                End If

    '                                                gp4.AddLines(lList2.ToArray())
    '                                            Else
    '                                                If approxLines Then
    '                                                    lList2 = ApproximateLines(lList2, epsilon)
    '                                                End If

    '                                                gp4.AddLines(lList2.ToArray())
    '                                            End If

    '                                            If closeFigures Then
    '                                                gp4.CloseFigure()
    '                                            End If
    '                                        End If

    '                                        If gp4.PointCount > 1 Then
    '                                            gp4.FillMode = FillMode.Winding
    '                                            gP41.AddPath(gp4, False)

    '                                            Using tb As New TextureBrush(orig)
    '                                                Using g As Graphics = Graphics.FromImage(b)
    '                                                    g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
    '                                                    g.FillPath(tb, gP41)
    '                                                End Using
    '                                            End Using
    '                                        End If
    '                                    End If
    '                                Catch

    '                                End Try
    '                            End Using
    '                        End Using
    '                    End If
    '                Else
    '                    Using tb As New TextureBrush(orig)
    '                        Using g As Graphics = Graphics.FromImage(b)
    '                            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
    '                            g.FillPath(tb, gP)
    '                        End Using
    '                    End Using
    '                End If
    '            End Using
    '        Next
    '    End If
    'End Sub

    'Public Function ShiftCoords(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean) As GraphicsPathData
    '    If gP.PointCount > 1 Then
    '        'do it separately for each PathFigure...
    '        Dim t(gP.PathTypes.Length - 1) As Byte
    '        gP.PathTypes.CopyTo(t, 0)
    '        Dim pts(gP.PathPoints.Length - 1) As PointF
    '        gP.PathPoints.CopyTo(pts, 0)

    '        Dim pTAll As New List(Of List(Of Byte))
    '        Dim pPAll As New List(Of List(Of PointF))

    '        Dim cnt As Integer = 0

    '        While cnt < t.Length
    '            Dim pT As New List(Of Byte)
    '            Dim pP As New List(Of PointF)

    '            pP.Add(pts(cnt))
    '            pT.Add(0) 'pT.Add(t(cnt))
    '            cnt += 1

    '            While cnt < t.Length AndAlso Not t(cnt) = 0
    '                pP.Add(pts(cnt))
    '                pT.Add(t(cnt))
    '                cnt += 1
    '            End While

    '            pPAll.Add(pP)
    '            pT(pT.Count - 1) = 129 'fffffff
    '            pTAll.Add(pT)
    '        End While

    '        pPAll = pPAll.OrderByDescending(Function(a) a.Count).ToList()
    '        pTAll = pTAll.OrderByDescending(Function(a) a.Count).ToList()

    '        Dim shiftedPath As New GraphicsPathData()

    '        For cnt2 As Integer = 0 To pPAll.Count - 1
    '            If (mapLargestOnly AndAlso cnt2 = 0) OrElse Not mapLargestOnly Then
    '                If pPAll(cnt2).Count > 1 Then
    '                    Using outerPath As GraphicsPath = New GraphicsPath(pPAll(cnt2).ToArray(), pTAll(cnt2).ToArray())
    '                        If outerPath.PointCount > 1 Then
    '                            Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
    '                            outerPath.PathPoints.CopyTo(outerPts, 0)
    '                            Dim rc As RectangleF = outerPath.GetBounds()
    '                            Dim w As Integer = CInt(Math.Ceiling(rc.Width))
    '                            Dim h As Integer = CInt(Math.Ceiling(rc.Height))

    '                            If w > 0 AndAlso h > 0 Then
    '                                Dim bmpTmp As Bitmap = Nothing
    '                                Dim bmpInner As Bitmap = Nothing

    '                                Dim breite As Integer = CInt(Math.Ceiling(shiftInwards))
    '                                Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards)))

    '                                If shiftInwards < 0 Then
    '                                    Dim bmpTmp2 As Bitmap = Nothing
    '                                    Dim bmpOuter As Bitmap = Nothing

    '                                    Try
    '                                        Dim br As Integer = CInt(Math.Floor(shiftInwards))
    '                                        If AvailMem.AvailMem.checkAvailRam((w - br * 2) * (h - br * 2) * 4L) Then
    '                                            bmpTmp2 = New Bitmap(w - br * 2, h - br * 2)
    '                                            Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
    '                                                Using p As New Pen(Color.Black, -br)
    '                                                    Using mx As New Matrix(1, 0, 0, 1, -rc.X - br, -rc.Y - br)
    '                                                        outerPath2.Transform(mx)
    '                                                    End Using

    '                                                    Using g As Graphics = Graphics.FromImage(bmpTmp2)
    '                                                        g.FillPath(Brushes.Black, outerPath2)
    '                                                    End Using

    '                                                    bmpOuter = ExtOutline(bmpTmp2, -breite, Nothing)

    '                                                    Dim lOuter As List(Of ChainCodeF) = GetBoundaryF(bmpOuter)

    '                                                    outerPath.Reset()
    '                                                    For i As Integer = 0 To lOuter.Count - 1
    '                                                        'translate
    '                                                        For j As Integer = 0 To lOuter(i).Coord.Count - 1
    '                                                            lOuter(i).Coord(j) = New PointF(lOuter(i).Coord(j).X + rc.X + br, lOuter(i).Coord(j).Y + rc.Y + br)
    '                                                        Next
    '                                                        outerPath.AddLines(lOuter(i).Coord.ToArray())
    '                                                        outerPath.CloseFigure()
    '                                                    Next

    '                                                    Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
    '                                                    outerPath.PathPoints.CopyTo(outerPts2, 0)
    '                                                    outerPts = outerPts2
    '                                                    rc = outerPath.GetBounds()
    '                                                    w = CInt(Math.Ceiling(rc.Width))
    '                                                    h = CInt(Math.Ceiling(rc.Height))
    '                                                    shiftInwards *= -1
    '                                                    fraction = 1.0 - fraction
    '                                                End Using
    '                                            End Using
    '                                        End If

    '                                    Catch

    '                                    Finally
    '                                        If Not bmpTmp2 Is Nothing Then
    '                                            bmpTmp2.Dispose()
    '                                            bmpTmp2 = Nothing
    '                                        End If
    '                                        If Not bmpOuter Is Nothing Then
    '                                            bmpOuter.Dispose()
    '                                            bmpOuter = Nothing
    '                                        End If
    '                                    End Try
    '                                End If

    '                                If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
    '                                    Dim shiftedPathPts As New List(Of PointF)

    '                                    If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
    '                                        Try
    '                                            bmpTmp = New Bitmap(w, h)
    '                                            Using g As Graphics = Graphics.FromImage(bmpTmp)
    '                                                Using mx As New Matrix(1, 0, 0, 1, -rc.X, -rc.Y)
    '                                                    outerPath.Transform(mx)
    '                                                End Using
    '                                                g.FillPath(Brushes.Black, outerPath)
    '                                                Using mx As New Matrix(1, 0, 0, 1, rc.X, rc.Y)
    '                                                    outerPath.Transform(mx)
    '                                                End Using

    '                                                bmpInner = RemOutline(bmpTmp, breite, Nothing)

    '                                                If Not bmpInner Is Nothing Then
    '                                                    'get the new outline (path) and prolongate it to have the same amount of points as the large path
    '                                                    'then find the closest points and resample
    '                                                    'innerPath can have more figures than outerPath
    '                                                    Dim lInner As List(Of ChainCode) = GetBoundary(bmpInner)

    '                                                    If lInner.Count > 0 Then
    '                                                        lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()

    '                                                        Dim newInnerPtsAll As New List(Of List(Of PointF))

    '                                                        If mapLargestOnly Then
    '                                                            Dim newInnerPts As New List(Of PointF)
    '                                                            For j As Integer = 0 To lInner(0).Coord.Count - 1
    '                                                                newInnerPts.Add(New PointF(lInner(0).Coord(j).X, lInner(0).Coord(j).Y))
    '                                                            Next
    '                                                            newInnerPtsAll.Add(newInnerPts)
    '                                                        Else
    '                                                            For i As Integer = 0 To lInner.Count - 1
    '                                                                Dim newInnerPts As New List(Of PointF)
    '                                                                For j As Integer = 0 To lInner(i).Coord.Count - 1
    '                                                                    newInnerPts.Add(New PointF(lInner(i).Coord(j).X, lInner(i).Coord(j).Y))
    '                                                                Next
    '                                                                newInnerPtsAll.Add(newInnerPts)
    '                                                            Next
    '                                                        End If

    '                                                        Dim factor As Double = outerPath.PointCount / lInner.Sum(Function(a) a.Coord.Count)
    '                                                        Dim dFracSum As Double = 0.0
    '                                                        Dim pointCountAll As Integer = 0

    '                                                        For l As Integer = 0 To newInnerPtsAll.Count - 1
    '                                                            Dim dLength As Double = newInnerPtsAll(l).Count * factor
    '                                                            Dim pointCount As Integer = CInt(Math.Ceiling(dLength))
    '                                                            dFracSum += pointCount - dLength

    '                                                            If dFracSum > 1.0 Then
    '                                                                dFracSum -= 1
    '                                                                pointCount -= 1
    '                                                            End If

    '                                                            pointCountAll += pointCount

    '                                                            If pointCountAll > outerPath.PointCount Then
    '                                                                pointCount -= pointCountAll - outerPath.PointCount
    '                                                            End If

    '                                                            newInnerPtsAll(l) = TranslateNewInnerPath(newInnerPtsAll(l), rc)
    '                                                            Dim newInnerPathPoints As List(Of PointF) = ProlongateInnerPath(newInnerPtsAll(l), pointCount, factor)
    '                                                            Dim bIsClosed As Boolean = False

    '                                                            If newInnerPtsAll(l)(newInnerPtsAll(l).Count - 1).X = newInnerPtsAll(l)(0).X AndAlso newInnerPtsAll(l)(newInnerPtsAll(l).Count - 1).Y = newInnerPtsAll(l)(0).Y Then
    '                                                                newInnerPathPoints(newInnerPathPoints.Count - 1) = New PointF(newInnerPathPoints(0).X, newInnerPathPoints(0).Y)
    '                                                                bIsClosed = True
    '                                                            End If

    '                                                            Dim outerFraction As Double = 1.0 - fraction

    '                                                            pTAll(cnt2)(shiftedPathPts.Count - CInt(Math.Floor(dFracSum))) = 0

    '                                                            'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
    '                                                            'for keeping the amount of figures of the innerPath
    '                                                            For i As Integer = 0 To newInnerPathPoints.Count - 1
    '                                                                Dim minDist As Double = Double.MaxValue
    '                                                                Dim minIndex As Integer = -1

    '                                                                For j As Integer = 0 To outerPts.Count - 1
    '                                                                    Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
    '                                                                    Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
    '                                                                    Dim dist As Double = Math.Sqrt(dx * dx + dy * dy)

    '                                                                    If dist < minDist Then
    '                                                                        minDist = dist
    '                                                                        minIndex = j
    '                                                                    End If
    '                                                                Next

    '                                                                If minIndex > -1 Then 'always, todo: comment out test for -1
    '                                                                    Dim outerX As Single = outerPts(minIndex).X
    '                                                                    Dim outerY As Single = outerPts(minIndex).Y
    '                                                                    Dim innerX As Single = newInnerPathPoints(i).X
    '                                                                    Dim innerY As Single = newInnerPathPoints(i).Y

    '                                                                    Dim newX As Double = innerX * fraction + outerX * outerFraction
    '                                                                    Dim newY As Double = innerY * fraction + outerY * outerFraction

    '                                                                    shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
    '                                                                End If
    '                                                            Next

    '                                                            If bIsClosed Then
    '                                                                pTAll(cnt2)(shiftedPathPts.Count - 1) = 129
    '                                                            End If
    '                                                        Next

    '                                                        If shiftedPathPts.Count <> outerPath.PointCount Then
    '                                                            shiftedPathPts = RestrictOuterPath(shiftedPathPts, outerPath.PointCount)
    '                                                        End If
    '                                                    Else

    '                                                    End If
    '                                                End If
    '                                            End Using
    '                                        Catch
    '                                        Finally
    '                                            If Not bmpTmp Is Nothing Then
    '                                                bmpTmp.Dispose()
    '                                                bmpTmp = Nothing
    '                                            End If
    '                                            If Not bmpInner Is Nothing Then
    '                                                bmpInner.Dispose()
    '                                                bmpInner = Nothing
    '                                            End If
    '                                        End Try
    '                                    End If

    '                                    If shiftedPathPts.Count > 1 Then
    '                                        Dim cnt4 As Integer = 0

    '                                        While cnt4 < pTAll(cnt2).Count
    '                                            Dim pT As New List(Of Byte)
    '                                            Dim pP As New List(Of PointF)

    '                                            pP.Add(shiftedPathPts(cnt4))
    '                                            pT.Add(0) 'pT.Add(t(cnt))
    '                                            cnt4 += 1

    '                                            While cnt4 < pTAll(cnt2).Count AndAlso Not pTAll(cnt2)(cnt4) = 0
    '                                                pP.Add(shiftedPathPts(cnt4))
    '                                                pT.Add(pTAll(cnt2)(cnt4))
    '                                                cnt4 += 1
    '                                            End While

    '                                            pT(pT.Count - 1) = 129 'fffffff

    '                                            shiftedPath.Add(pP, pT)
    '                                        End While
    '                                    End If
    '                                End If
    '                            End If
    '                        End If
    '                    End Using
    '                End If
    '            Else
    '                Exit For
    '            End If
    '        Next

    '        Return shiftedPath
    '    Else
    '        Return New GraphicsPathData()
    '    End If
    'End Function

    ''this is the central method in this program
    ''#############################################################################################################################################################################
    ''Note: Usually you would use a LevelSet method to solve this problem, or use graph theory methods like:
    ''-orient the outline
    ''-shift the outline in normal direction
    ''-annihilate reversed parts (where the normals are pointing outwards, because of "over[-cross]-shifting"),
    '' you could do this by following the normals for each point, and count the path crossings, an even omount of self crossings
    '' would indicate that the current figure is "inside out", watch for points where the normal projection only touches the path,
    '' and dont cross it, (a dot-product approach of the meeting normals (the projection of the normal of our current point and the normal of the point touched by the projection))
    '' could maybe help).
    '' you then still will have the problem of reconnection of the path parts, when one figure is split into two, you will have to find the new neighbor
    '' points at the location where the path is split. (Just like magnetic field reconnexion).
    ''
    '' We here use an "implicit Level Set method" using the benefits of the .Net Framework, a GraphicsPath and a natural coord-system to represent regions: A Bitmap.
    ''#############################################################################################################################################################################
    ''we fill the [translated] path in a bitmap and remove the outline, rescan the outline, and interpolate the points to get the result("shifted")-locations
    'Public Function ShiftCoords(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean, stretch As Boolean) As GraphicsPathData
    '    If gP.PointCount > 1 Then 'analyze and split the current path
    '        'do it separately for each PathFigure...
    '        Dim t(gP.PathTypes.Length - 1) As Byte
    '        gP.PathTypes.CopyTo(t, 0)
    '        Dim pts(gP.PathPoints.Length - 1) As PointF
    '        gP.PathPoints.CopyTo(pts, 0)

    '        Dim pTAll As New List(Of List(Of Byte))
    '        Dim pPAll As New List(Of List(Of PointF))

    '        Dim cnt As Integer = 0

    '        While cnt < t.Length
    '            Dim pT As New List(Of Byte)
    '            Dim pP As New List(Of PointF)

    '            pP.Add(pts(cnt))
    '            pT.Add(0)  'start figure
    '            cnt += 1

    '            While cnt < t.Length AndAlso Not t(cnt) = 0 'if 0, new figure
    '                pP.Add(pts(cnt))
    '                pT.Add(t(cnt))
    '                cnt += 1
    '            End While

    '            pPAll.Add(pP)
    '            pT(pT.Count - 1) = 129 'close figure
    '            pTAll.Add(pT)
    '        End While

    '        pPAll = pPAll.OrderByDescending(Function(a) a.Count).ToList()
    '        pTAll = pTAll.OrderByDescending(Function(a) a.Count).ToList()

    '        Dim shiftedPath As New GraphicsPathData()

    '        For cnt2 As Integer = 0 To pPAll.Count - 1 'loop over all figures
    '            If (mapLargestOnly AndAlso cnt2 = 0) OrElse Not mapLargestOnly Then
    '                If pPAll(cnt2).Count > 1 Then
    '                    Using outerPath As GraphicsPath = New GraphicsPath(pPAll(cnt2).ToArray(), pTAll(cnt2).ToArray())
    '                        If outerPath.PointCount > 1 Then
    '                            Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
    '                            outerPath.PathPoints.CopyTo(outerPts, 0)
    '                            Dim rc As RectangleF = outerPath.GetBounds()
    '                            Dim w As Integer = CInt(Math.Ceiling(rc.Width))
    '                            Dim h As Integer = CInt(Math.Ceiling(rc.Height))

    '                            If w > 0 AndAlso h > 0 Then
    '                                Dim bmpTmp As Bitmap = Nothing
    '                                Dim bmpInner As Bitmap = Nothing

    '                                Dim breite As Integer = CInt(Math.Ceiling(shiftInwards)) 'width to remove
    '                                Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards))) 'the weight of the inner path when interpolating points

    '                                If shiftInwards < 0 Then 'maybe we want to shift outwards?
    '                                    Dim bmpTmp2 As Bitmap = Nothing
    '                                    Dim bmpOuter As Bitmap = Nothing

    '                                    Try
    '                                        Dim br As Integer = CInt(Math.Floor(shiftInwards)) 'width
    '                                        If AvailMem.AvailMem.checkAvailRam((w - br * 2) * (h - br * 2) * 4L) Then
    '                                            bmpTmp2 = New Bitmap(w - br * 2, h - br * 2)
    '                                            Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
    '                                                Using p As New Pen(Color.Black, -br)
    '                                                    Using mx As New Matrix(1, 0, 0, 1, -rc.X - br, -rc.Y - br) 'translate close to origin -> so we can draw the entire path to the bmp
    '                                                        outerPath2.Transform(mx)
    '                                                    End Using

    '                                                    Using g As Graphics = Graphics.FromImage(bmpTmp2)
    '                                                        g.FillPath(Brushes.Black, outerPath2)
    '                                                    End Using

    '                                                    bmpOuter = ExtOutline(bmpTmp2, -breite, Nothing) 'add outline

    '                                                    Dim lOuter As List(Of ChainCodeF) = GetBoundaryF(bmpOuter) 'get the new outline

    '                                                    outerPath.Reset() 'refill the new outer path
    '                                                    For i As Integer = 0 To lOuter.Count - 1
    '                                                        'translate
    '                                                        For j As Integer = 0 To lOuter(i).Coord.Count - 1
    '                                                            lOuter(i).Coord(j) = New PointF(lOuter(i).Coord(j).X + rc.X + br, lOuter(i).Coord(j).Y + rc.Y + br)
    '                                                        Next
    '                                                        outerPath.AddLines(lOuter(i).Coord.ToArray())
    '                                                        outerPath.CloseFigure()
    '                                                    Next

    '                                                    Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
    '                                                    outerPath.PathPoints.CopyTo(outerPts2, 0)
    '                                                    outerPts = outerPts2 'new points
    '                                                    rc = outerPath.GetBounds() 'new bounds
    '                                                    w = CInt(Math.Ceiling(rc.Width))
    '                                                    h = CInt(Math.Ceiling(rc.Height))
    '                                                    shiftInwards *= -1 'switch sign -> outerpath is now the extended outer path, original outerpath is inner path now
    '                                                    fraction = 1.0 - fraction 'switch fraction
    '                                                End Using
    '                                            End Using
    '                                        End If

    '                                    Catch

    '                                    Finally
    '                                        If Not bmpTmp2 Is Nothing Then
    '                                            bmpTmp2.Dispose()
    '                                            bmpTmp2 = Nothing
    '                                        End If
    '                                        If Not bmpOuter Is Nothing Then
    '                                            bmpOuter.Dispose()
    '                                            bmpOuter = Nothing
    '                                        End If
    '                                    End Try
    '                                End If

    '                                If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
    '                                    Dim shiftedPathPts As New List(Of PointF)

    '                                    If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
    '                                        Try
    '                                            bmpTmp = New Bitmap(w, h)
    '                                            Using g As Graphics = Graphics.FromImage(bmpTmp)
    '                                                'translate path close to origin, to be drawn entirely into the bmp
    '                                                Using mx As New Matrix(1, 0, 0, 1, -rc.X, -rc.Y)
    '                                                    outerPath.Transform(mx)
    '                                                End Using
    '                                                g.FillPath(Brushes.Black, outerPath)
    '                                                're-translate
    '                                                Using mx As New Matrix(1, 0, 0, 1, rc.X, rc.Y)
    '                                                    outerPath.Transform(mx)
    '                                                End Using

    '                                                bmpInner = RemOutline(bmpTmp, breite, Nothing) 'remove the entire width (if breite is negative, the method checks for it and reverses sign)

    '                                                If Not bmpInner Is Nothing Then
    '                                                    'get the new outline (path) [optional: and prolongate it to have the same amount of points as the large path]
    '                                                    'then find the closest points and resample
    '                                                    'innerPath can have more figures than outerPath
    '                                                    Dim lInner As List(Of ChainCode) = GetBoundary(bmpInner) 'current contours

    '                                                    If lInner.Count > 0 Then
    '                                                        lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()

    '                                                        'get all points
    '                                                        Dim newInnerPtsAll As New List(Of List(Of PointF))

    '                                                        If mapLargestOnly Then
    '                                                            Dim newInnerPts As New List(Of PointF)
    '                                                            For j As Integer = 0 To lInner(0).Coord.Count - 1
    '                                                                newInnerPts.Add(New PointF(lInner(0).Coord(j).X, lInner(0).Coord(j).Y))
    '                                                            Next
    '                                                            newInnerPtsAll.Add(newInnerPts)
    '                                                        Else
    '                                                            For i As Integer = 0 To lInner.Count - 1
    '                                                                Dim newInnerPts As New List(Of PointF)
    '                                                                For j As Integer = 0 To lInner(i).Coord.Count - 1
    '                                                                    newInnerPts.Add(New PointF(lInner(i).Coord(j).X, lInner(i).Coord(j).Y))
    '                                                                Next
    '                                                                newInnerPtsAll.Add(newInnerPts)
    '                                                            Next
    '                                                        End If

    '                                                        'relation outer point amount to inner point amount (if we stretch the inner array to the length of the outer one)
    '                                                        Dim factor As Double = If(stretch, outerPath.PointCount / lInner.Sum(Function(a) a.Coord.Count), 1.0)
    '                                                        Dim dFracSum As Double = 0.0 'value that sums the accumulated fractions (differences of exact product to index in array)
    '                                                        Dim pointCountAll As Integer = 0 'accumulated pointsamount

    '                                                        For l As Integer = 0 To newInnerPtsAll.Count - 1
    '                                                            Dim dLength As Double = newInnerPtsAll(l).Count * factor '"exact" location of start of current figure in [maybe] elongated array
    '                                                            Dim pointCount As Integer = CInt(Math.Ceiling(dLength)) 'integer (real) location
    '                                                            dFracSum += pointCount - dLength 'add up the difference

    '                                                            If dFracSum > 1.0 Then 'difference of exact product to index in array should be 1 or less
    '                                                                dFracSum -= 1
    '                                                                pointCount -= 1
    '                                                            End If

    '                                                            pointCountAll += pointCount 'add up pointsamount

    '                                                            If pointCountAll > outerPath.PointCount Then 'set to outerpath pointcount, if needed
    '                                                                pointCount -= pointCountAll - outerPath.PointCount
    '                                                            End If

    '                                                            newInnerPtsAll(l) = TranslateNewInnerPath(newInnerPtsAll(l), rc) 'translate back (we got the new innerpath from the bmp, which holds the translated path)
    '                                                            Dim newInnerPathPoints As List(Of PointF) = newInnerPtsAll(l)
    '                                                            If stretch Then
    '                                                                ProlongateInnerPath(newInnerPtsAll(l), pointCount, factor) 'if wanted, elongate the innerpath figure
    '                                                            End If

    '                                                            'check last point equal to first point
    '                                                            Dim bIsClosed As Boolean = False

    '                                                            If newInnerPtsAll(l)(newInnerPtsAll(l).Count - 1).X = newInnerPtsAll(l)(0).X AndAlso newInnerPtsAll(l)(newInnerPtsAll(l).Count - 1).Y = newInnerPtsAll(l)(0).Y Then
    '                                                                newInnerPathPoints(newInnerPathPoints.Count - 1) = New PointF(newInnerPathPoints(0).X, newInnerPathPoints(0).Y)
    '                                                                bIsClosed = True
    '                                                            End If

    '                                                            Dim outerFraction As Double = 1.0 - fraction 'weight of outerpath in interpolating points

    '                                                            pTAll(cnt2)(shiftedPathPts.Count - CInt(Math.Floor(dFracSum))) = 0 'first byte for new figure in pathtypes list

    '                                                            'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
    '                                                            'for keeping the amount of figures of the innerPath
    '                                                            For i As Integer = 0 To newInnerPathPoints.Count - 1
    '                                                                Dim minDist As Double = Double.MaxValue 'find closest point innouterpath
    '                                                                Dim minIndex As Integer = -1

    '                                                                For j As Integer = 0 To outerPts.Count - 1
    '                                                                    Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
    '                                                                    Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
    '                                                                    Dim dist As Double = Math.Sqrt(dx * dx + dy * dy) 'get distance of inner to outer point(s)

    '                                                                    If dist < minDist Then 'record closest point
    '                                                                        minDist = dist
    '                                                                        minIndex = j
    '                                                                    End If
    '                                                                Next

    '                                                                If minIndex > -1 Then 'should always be, todo: comment out test for -1
    '                                                                    Dim outerX As Single = outerPts(minIndex).X
    '                                                                    Dim outerY As Single = outerPts(minIndex).Y
    '                                                                    Dim innerX As Single = newInnerPathPoints(i).X
    '                                                                    Dim innerY As Single = newInnerPathPoints(i).Y

    '                                                                    'interpolate linearly
    '                                                                    Dim newX As Double = innerX * fraction + outerX * outerFraction
    '                                                                    Dim newY As Double = innerY * fraction + outerY * outerFraction

    '                                                                    'add to list
    '                                                                    shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
    '                                                                End If
    '                                                            Next

    '                                                            If bIsClosed Then 'do we have a closed figure? We should
    '                                                                pTAll(cnt2)(shiftedPathPts.Count - 1) = 129
    '                                                            End If
    '                                                        Next

    '                                                        'do a check for equal lengths
    '                                                        If stretch AndAlso shiftedPathPts.Count <> outerPath.PointCount Then
    '                                                            shiftedPathPts = RestrictOuterPath(shiftedPathPts, outerPath.PointCount)
    '                                                        End If
    '                                                    Else

    '                                                    End If
    '                                                End If
    '                                            End Using
    '                                        Catch
    '                                        Finally
    '                                            If Not bmpTmp Is Nothing Then
    '                                                bmpTmp.Dispose()
    '                                                bmpTmp = Nothing
    '                                            End If
    '                                            If Not bmpInner Is Nothing Then
    '                                                bmpInner.Dispose()
    '                                                bmpInner = Nothing
    '                                            End If
    '                                        End Try
    '                                    End If

    '                                    'add current results to the resulting large pathdata
    '                                    If shiftedPathPts.Count > 1 Then 'do we have a valid figure
    '                                        If stretch Then 'here we have to parse the elongated results for starting and ending points of figures
    '                                            Dim cnt4 As Integer = 0

    '                                            While cnt4 < pTAll(cnt2).Count
    '                                                Dim pT As New List(Of Byte)
    '                                                Dim pP As New List(Of PointF)

    '                                                pP.Add(shiftedPathPts(cnt4))
    '                                                pT.Add(0)
    '                                                cnt4 += 1

    '                                                While cnt4 < pTAll(cnt2).Count AndAlso Not pTAll(cnt2)(cnt4) = 0
    '                                                    pP.Add(shiftedPathPts(cnt4))
    '                                                    pT.Add(pTAll(cnt2)(cnt4))
    '                                                    cnt4 += 1
    '                                                End While

    '                                                pT(pT.Count - 1) = 129
    '                                                shiftedPath.Add(pP, pT)
    '                                            End While
    '                                        Else 'no stretch figures have all the same length as in the input path
    '                                            'setup and fill the data lists
    '                                            Dim cnt4 As Integer = 0
    '                                            Dim pT As New List(Of Byte)
    '                                            Dim pP As New List(Of PointF)
    '                                            While cnt4 < shiftedPathPts.Count
    '                                                pP.Add(shiftedPathPts(cnt4))
    '                                                pT.Add(0)
    '                                                cnt4 += 1
    '                                            End While

    '                                            pT(pT.Count - 1) = 129 'ensure close
    '                                            shiftedPath.Add(pP, pT)
    '                                        End If
    '                                    End If
    '                                End If
    '                            End If
    '                        End If
    '                    End Using
    '                End If
    '            Else
    '                Exit For
    '            End If
    '        Next

    '        Return shiftedPath
    '    Else
    '        Return New GraphicsPathData()
    '    End If
    'End Function

    ''we draw the [translated] path in a bitmap and [re-]scan the inner-outline of the drawn path, and interpolate the points to get the result("shifted")-locations
    ''we have to draw smeared out a bit, else (interpolationmode nearestneighbor and pixeloffsetmod half and a one px wide pen) we would get a lot of one pixel figures by scanning with a 4-Chaincode ->
    ''we then would get diagonally shifted single pixel filled structures that would result in "nullcells" or outlines going around those pixels
    'Public Function ShiftCoords2(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean) As GraphicsPath
    '    If gP.PointCount > 1 Then
    '        Dim shiftedPath As New GraphicsPath()

    '        Using outerPath As GraphicsPath = CType(gP.Clone(), GraphicsPath)
    '            If outerPath.PointCount > 1 Then
    '                Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
    '                outerPath.PathPoints.CopyTo(outerPts, 0)
    '                Dim rc As RectangleF = outerPath.GetBounds()
    '                Dim w As Integer = CInt(Math.Ceiling(rc.Width))
    '                Dim h As Integer = CInt(Math.Ceiling(rc.Height))

    '                If w > 0 AndAlso h > 0 Then
    '                    Dim bmpTmp As Bitmap = Nothing

    '                    Dim breite As Integer = CInt(Math.Ceiling(shiftInwards)) 'width to remove
    '                    Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards))) 'the weight of the inner path when interpolating points

    '                    If shiftInwards < 0 Then 'maybe we want to shift outwards?
    '                        Dim bmpTmp2 As Bitmap = Nothing
    '                        Dim bmpOuter As Bitmap = Nothing

    '                        Try
    '                            Dim br As Integer = CInt(Math.Floor(shiftInwards)) 'width
    '                            If AvailMem.AvailMem.checkAvailRam((w - br * 2) * (h - br * 2) * 4L) Then
    '                                bmpTmp2 = New Bitmap(w - br * 2, h - br * 2)
    '                                Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
    '                                    Using p As New Pen(Color.Black, -br)
    '                                        Using mx As New Matrix(1, 0, 0, 1, -rc.X - br, -rc.Y - br) 'translate close to origin -> so we can draw the entire path to the bmp
    '                                            outerPath2.Transform(mx)
    '                                        End Using

    '                                        Using g As Graphics = Graphics.FromImage(bmpTmp2)
    '                                            g.FillPath(Brushes.Black, outerPath2)
    '                                        End Using

    '                                        bmpOuter = ExtOutline(bmpTmp2, -breite, Nothing) 'add outline

    '                                        Dim lOuter As List(Of ChainCodeF) = GetBoundaryF(bmpOuter) 'get the new outline

    '                                        outerPath.Reset() 'refill the new outer path
    '                                        For i As Integer = 0 To lOuter.Count - 1
    '                                            'translate
    '                                            For j As Integer = 0 To lOuter(i).Coord.Count - 1
    '                                                lOuter(i).Coord(j) = New PointF(lOuter(i).Coord(j).X + rc.X + br, lOuter(i).Coord(j).Y + rc.Y + br)
    '                                            Next
    '                                            outerPath.AddLines(lOuter(i).Coord.ToArray())
    '                                            outerPath.CloseFigure()
    '                                        Next

    '                                        Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
    '                                        outerPath.PathPoints.CopyTo(outerPts2, 0)
    '                                        outerPts = outerPts2 'new points
    '                                        rc = outerPath.GetBounds() 'new bounds
    '                                        w = CInt(Math.Ceiling(rc.Width))
    '                                        h = CInt(Math.Ceiling(rc.Height))
    '                                        shiftInwards *= -1 'switch sign -> outerpath is now the extended outer path, original outerpath is inner path now
    '                                        fraction = 1.0 - fraction 'switch fraction
    '                                    End Using
    '                                End Using
    '                            End If

    '                        Catch

    '                        Finally
    '                            If Not bmpTmp2 Is Nothing Then
    '                                bmpTmp2.Dispose()
    '                                bmpTmp2 = Nothing
    '                            End If
    '                            If Not bmpOuter Is Nothing Then
    '                                bmpOuter.Dispose()
    '                                bmpOuter = Nothing
    '                            End If
    '                        End Try
    '                    End If

    '                    'more comments will follow...
    '                    If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
    '                        Dim shiftedPathPts As New List(Of PointF)
    '                        Dim shiftedPathTypes As New List(Of Byte)

    '                        If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
    '                            Try
    '                                bmpTmp = New Bitmap(w + 2, h + 2)
    '                                Dim lInner As List(Of ChainCode) = Nothing
    '                                Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
    '                                    For cntW As Integer = 0 To Math.Abs(breite) - 1
    '                                        'Dim fff As New Form()
    '                                        'fff.BackgroundImage = bmpTmp
    '                                        'fff.ShowDialog()

    '                                        Using g As Graphics = Graphics.FromImage(bmpTmp)
    '                                            g.Clear(Color.Transparent)
    '                                            g.InterpolationMode = InterpolationMode.HighQualityBicubic 'InterpolationMode.NearestNeighbor
    '                                            'g.PixelOffsetMode = PixelOffsetMode.Half '(if you use this, you probably need to translate back with only the rc-values (not rc-values -1 in line 4191f))
    '                                            g.SmoothingMode = SmoothingMode.AntiAlias 'needed for setting nullcells = false
    '                                            If cntW = 0 Then
    '                                                Using mx As New Matrix(1, 0, 0, 1, -rc.X + 1, -rc.Y + 1)
    '                                                    outerPath2.Transform(mx)
    '                                                End Using
    '                                            End If

    '                                            outerPath2.CloseAllFigures()
    '                                            Using pen As New Pen(Color.Black, 1)
    '                                                pen.LineJoin = LineJoin.Round
    '                                                g.DrawPath(pen, outerPath2)
    '                                            End Using

    '                                            'Dim pts(outerPath2.PointCount - 1) As PointF
    '                                            'outerPath2.PathPoints.CopyTo(pts, 0)
    '                                            'For ii As Integer = 0 To outerPath2.PointCount - 1
    '                                            '    g.FillRectangle(Brushes.Black, New RectangleF(pts(ii).X, pts(ii).Y, 2, 1))
    '                                            'Next

    '                                            'lInner = GetBoundary(bmpTmp)
    '                                            lInner = GetBoundaryShifted(bmpTmp)

    '                                            If lInner.Count = 0 Then
    '                                                lInner = lInner
    '                                            End If

    '                                            If lInner.Count > 0 Then
    '                                                lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()
    '                                                'removeOuterOutlines from linner
    '                                                For cnt As Integer = lInner.Count - 1 To 0 Step -1
    '                                                    If lInner(cnt).Coord.Count <= 4 Then
    '                                                        lInner.RemoveAt(cnt)
    '                                                        Continue For
    '                                                    End If
    '                                                    If lInner(cnt).Chain(lInner(cnt).Chain.Count - 1) = 2 Then 'not an inner outline
    '                                                        lInner.RemoveAt(cnt)
    '                                                        Continue For
    '                                                    Else
    '                                                        'we dont need the chain here, only the coords, and we always close the figure, so we can do:
    '                                                        lInner(cnt).Coord = lInner(cnt).Coord.Distinct().ToList()
    '                                                    End If
    '                                                Next
    '                                            End If

    '                                            outerPath2.Reset()
    '                                            For cnt As Integer = 0 To lInner.Count - 1
    '                                                outerPath2.StartFigure()
    '                                                outerPath2.AddLines(lInner(cnt).Coord.ToArray())
    '                                                outerPath2.CloseFigure()
    '                                            Next
    '                                            outerPath2.FillMode = FillMode.Alternate
    '                                        End Using
    '                                    Next
    '                                End Using

    '                                If mapLargestOnly Then
    '                                    If lInner.Count > 1 Then
    '                                        lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()
    '                                        lInner.RemoveRange(1, lInner.Count - 1)
    '                                    End If
    '                                End If

    '                                If lInner.Count > 0 Then
    '                                    Dim outerFraction As Double = 1.0 - fraction

    '                                    For cnt As Integer = 0 To lInner.Count - 1
    '                                        'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
    '                                        'for keeping the amount of figures of the innerPath

    '                                        Dim newInnerPathPoints As New List(Of PointF)

    '                                        For cnt2 As Integer = 0 To lInner(cnt).Coord.Count - 1
    '                                            newInnerPathPoints.Add(New PointF(lInner(cnt).Coord(cnt2).X + rc.X - 1,
    '                                                                              lInner(cnt).Coord(cnt2).Y + rc.Y - 1))
    '                                        Next

    '                                        For i As Integer = 0 To newInnerPathPoints.Count - 1
    '                                            Dim minDist As Double = Double.MaxValue
    '                                            Dim minIndex As Integer = -1

    '                                            For j As Integer = 0 To outerPts.Count - 1
    '                                                Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
    '                                                Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
    '                                                Dim dist As Double = Math.Sqrt(dx * dx + dy * dy)

    '                                                If dist < minDist Then
    '                                                    minDist = dist
    '                                                    minIndex = j
    '                                                End If
    '                                            Next

    '                                            If minIndex > -1 Then 'always, todo: comment out test for -1
    '                                                Dim outerX As Single = outerPts(minIndex).X
    '                                                Dim outerY As Single = outerPts(minIndex).Y
    '                                                Dim innerX As Single = newInnerPathPoints(i).X
    '                                                Dim innerY As Single = newInnerPathPoints(i).Y

    '                                                Dim newX As Double = innerX * fraction + outerX * outerFraction
    '                                                Dim newY As Double = innerY * fraction + outerY * outerFraction

    '                                                shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
    '                                                shiftedPathTypes.Add(If(i = 0, CByte(0), CByte(1)))
    '                                            End If
    '                                        Next

    '                                        shiftedPathTypes(shiftedPathTypes.Count - 1) = 129
    '                                    Next
    '                                End If

    '                            Catch
    '                            Finally
    '                                If Not bmpTmp Is Nothing Then
    '                                    bmpTmp.Dispose()
    '                                    bmpTmp = Nothing
    '                                End If
    '                            End Try
    '                        End If

    '                        If shiftedPathPts.Count > 1 Then
    '                            Dim gP4 As New GraphicsPath(shiftedPathPts.ToArray(), shiftedPathTypes.ToArray())
    '                            gP4.FillMode = FillMode.Alternate
    '                            gP4.CloseFigure()
    '                            shiftedPath.AddPath(gP4, False)
    '                        End If
    '                    End If
    '                End If
    '            End If
    '        End Using

    '        Return shiftedPath
    '    Else
    '        Return New GraphicsPath
    '    End If
    'End Function

    'this is the central method in this program
    '#############################################################################################################################################################################
    'Note: Usually you would use a LevelSet method to solve this problem, or use graph theory methods like:
    '-orient the outline
    '-shift the outline in normal direction
    '-annihilate reversed parts (where the normals are pointing outwards, because of "over[-cross]-shifting"),
    ' you could do this by following the normals for each point, and count the path crossings, an even omount of self crossings
    ' would indicate that the current figure is "inside out", watch for points where the normal projection only touches the path,
    ' and dont cross it, (a dot-product approach of the meeting normals (the projection of the normal of our current point and the normal of the point touched by the projection))
    ' could maybe help).
    ' you then still will have the problem of reconnection of the path parts, when one figure is split into two, you will have to find the new neighbor
    ' points at the location where the path is split. (Just like magnetic field reconnexion).
    '
    ' We here use an "implicit Level Set method" using the benefits of the .Net Framework, a GraphicsPath and a natural coord-system to represent regions: A Bitmap.
    '#############################################################################################################################################################################
    'we fill the [translated] path in a bitmap and remove the outline, rescan the outline, and interpolate the points to get the result("shifted")-locations
    Public Function ShiftCoords(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean, stretch As Boolean, fractionMode As ShiftFractionMode) As GraphicsPathData
        If gP.PointCount > 1 Then 'analyze and split the current path
            Dim pTAll As New List(Of List(Of Byte))
            Dim pPAll As New List(Of List(Of PointF))
            AnalyzeAndSplit(gP, pPAll, pTAll)

            Dim shiftedPath As New GraphicsPathData()

            For cnt2 As Integer = 0 To pPAll.Count - 1 'loop over all figures
                If (mapLargestOnly AndAlso cnt2 = 0) OrElse Not mapLargestOnly Then
                    If pPAll(cnt2).Count > 1 Then
                        Using outerPath As GraphicsPath = New GraphicsPath(pPAll(cnt2).ToArray(), pTAll(cnt2).ToArray())
                            If outerPath.PointCount > 1 Then
                                Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
                                outerPath.PathPoints.CopyTo(outerPts, 0)
                                Dim rc As RectangleF = outerPath.GetBounds()
                                Dim w As Integer = CInt(Math.Ceiling(rc.Width))
                                Dim h As Integer = CInt(Math.Ceiling(rc.Height))

                                If w > 0 AndAlso h > 0 Then
                                    Dim bmpTmp As Bitmap = Nothing
                                    Dim bmpInner As Bitmap = Nothing

                                    Dim breite As Integer = CInt(Math.Ceiling(shiftInwards)) 'width to remove
                                    Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards))) 'the weight of the inner path when interpolating points

                                    If shiftInwards < 0 Then 'maybe we want to shift outwards?
                                        ShiftOutwards(shiftInwards, outerPath, outerPts, breite, rc, w, h)

                                        Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
                                        outerPath.PathPoints.CopyTo(outerPts2, 0)
                                        outerPts = outerPts2 'new points
                                        rc = outerPath.GetBounds() 'new bounds
                                        w = CInt(Math.Ceiling(rc.Width))
                                        h = CInt(Math.Ceiling(rc.Height))
                                        shiftInwards *= -1 'switch sign -> outerpath is now the extended outer path, original outerpath is inner path now
                                        'fraction = 1.0 - fraction 'switch fraction

                                        'we extended by breite so we only need to shift back by the fraction
                                        shiftInwards -= CInt(Math.Floor(shiftInwards))
                                        breite = 1
                                        fraction = 1.0 - shiftInwards '/ If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards)))
                                    End If

                                    If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
                                        Dim shiftedPathPts As New List(Of PointF)

                                        If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
                                            Try
                                                bmpTmp = New Bitmap(w, h)
                                                Using g As Graphics = Graphics.FromImage(bmpTmp)
                                                    'translate path close to origin, to be drawn entirely into the bmp
                                                    Using mx As New Matrix(1, 0, 0, 1, -rc.X, -rc.Y)
                                                        outerPath.Transform(mx)
                                                    End Using
                                                    g.InterpolationMode = InterpolationMode.NearestNeighbor
                                                    g.PixelOffsetMode = PixelOffsetMode.Half
                                                    g.FillPath(Brushes.Black, outerPath)
                                                    're-translate
                                                    Using mx As New Matrix(1, 0, 0, 1, rc.X, rc.Y)
                                                        outerPath.Transform(mx)
                                                    End Using

                                                    bmpInner = RemOutline(bmpTmp, breite, Nothing) 'remove the entire width (if breite is negative, the method checks for it and reverses sign)

                                                    If Not bmpInner Is Nothing Then
                                                        'get the new outline (path) [optional: and prolongate it to have the same amount of points as the large path]
                                                        'then find the closest points and resample
                                                        'innerPath can have more figures than outerPath
                                                        Dim lInner As List(Of ChainCode) = GetBoundary(bmpInner) 'current contours

                                                        'rem last point for each figure
                                                        For i As Integer = 0 To lInner.Count - 1
                                                            If lInner(i).Coord(lInner(i).Coord.Count - 1).X = lInner(i).Coord(0).X AndAlso lInner(i).Coord(lInner(i).Coord.Count - 1).Y = lInner(i).Coord(0).Y Then
                                                                lInner(i).Coord.RemoveAt(lInner(i).Coord.Count - 1)
                                                            End If
                                                        Next

                                                        If lInner.Count > 0 Then
                                                            lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()

                                                            'get all points
                                                            Dim newInnerPtsAll As New List(Of List(Of PointF))

                                                            If mapLargestOnly Then
                                                                Dim newInnerPts As New List(Of PointF)
                                                                For j As Integer = 0 To lInner(0).Coord.Count - 1
                                                                    newInnerPts.Add(New PointF(lInner(0).Coord(j).X, lInner(0).Coord(j).Y))
                                                                Next
                                                                newInnerPtsAll.Add(newInnerPts)
                                                            Else
                                                                For i As Integer = 0 To lInner.Count - 1
                                                                    Dim newInnerPts As New List(Of PointF)
                                                                    For j As Integer = 0 To lInner(i).Coord.Count - 1
                                                                        newInnerPts.Add(New PointF(lInner(i).Coord(j).X, lInner(i).Coord(j).Y))
                                                                    Next
                                                                    newInnerPtsAll.Add(newInnerPts)
                                                                Next
                                                            End If

                                                            'relation outer point amount to inner point amount (if we stretch the inner array to the length of the outer one)
                                                            Dim factor As Double = If(stretch, outerPath.PointCount / lInner.Sum(Function(a) a.Coord.Count), 1.0)
                                                            Dim dFracSum As Double = 0.0 'value that sums the accumulated fractions (differences of exact product to index in array)
                                                            Dim pointCountAll As Integer = 0 'accumulated pointsamount

                                                            For l As Integer = 0 To newInnerPtsAll.Count - 1
                                                                Dim dLength As Double = newInnerPtsAll(l).Count * factor '"exact" location of start of current figure in [maybe] elongated array
                                                                Dim pointCount As Integer = CInt(Math.Ceiling(dLength)) 'integer (real) location
                                                                dFracSum += pointCount - dLength 'add up the difference

                                                                If dFracSum > 1.0 Then 'difference of exact product to index in array should be 1 or less
                                                                    dFracSum -= 1
                                                                    pointCount -= 1
                                                                End If

                                                                pointCountAll += pointCount 'add up pointsamount

                                                                If pointCountAll > outerPath.PointCount Then 'set to outerpath pointcount, if needed
                                                                    pointCount -= pointCountAll - outerPath.PointCount
                                                                End If

                                                                newInnerPtsAll(l) = TranslateNewInnerPath(newInnerPtsAll(l), rc) 'translate back (we got the new innerpath from the bmp, which holds the translated path)
                                                                Dim newInnerPathPoints As List(Of PointF) = newInnerPtsAll(l)
                                                                'lets have a dx or dy at all points when comparing neighbors
                                                                newInnerPathPoints = newInnerPathPoints.Distinct().ToList()

                                                                If stretch Then
                                                                    newInnerPathPoints = ProlongateInnerPath(newInnerPtsAll(l), pointCount, factor) 'if wanted, elongate the innerpath figure
                                                                End If

                                                                Dim outerFraction As Double = 1.0 - fraction 'weight of outerpath in interpolating points

                                                                pTAll(cnt2)(shiftedPathPts.Count - CInt(Math.Floor(dFracSum))) = 0 'first byte for new figure in pathtypes list

                                                                GetNewAndInterpolatedPoints(outerPath, newInnerPathPoints, shiftedPathPts,
                                                                                            outerPts, fractionMode, breite, w, h, fraction, outerFraction, rc) 'do the work
                                                            Next

                                                            'do a check for equal lengths
                                                            If stretch AndAlso shiftedPathPts.Count <> outerPath.PointCount Then
                                                                shiftedPathPts = RestrictOuterPath(shiftedPathPts, outerPath.PointCount)
                                                            End If
                                                        Else

                                                        End If
                                                    End If
                                                End Using
                                            Catch
                                            Finally
                                                If Not bmpTmp Is Nothing Then
                                                    bmpTmp.Dispose()
                                                    bmpTmp = Nothing
                                                End If
                                                If Not bmpInner Is Nothing Then
                                                    bmpInner.Dispose()
                                                    bmpInner = Nothing
                                                End If
                                            End Try
                                        End If

                                        'add current results to the resulting large pathdata
                                        If shiftedPathPts.Count > 1 Then 'do we have a valid figure
                                            If stretch Then 'here we have to parse the elongated results for starting and ending points of figures
                                                'make sure length of both arrays are the same
                                                If stretch AndAlso shiftedPathPts.Count <> pTAll(cnt2).Count Then
                                                    shiftedPathPts = RestrictOuterPath(shiftedPathPts, pTAll(cnt2).Count)
                                                End If

                                                Dim cnt4 As Integer = 0

                                                While cnt4 < pTAll(cnt2).Count
                                                    Dim pT As New List(Of Byte)
                                                    Dim pP As New List(Of PointF)

                                                    pP.Add(shiftedPathPts(cnt4))
                                                    pT.Add(0)
                                                    cnt4 += 1

                                                    While cnt4 < pTAll(cnt2).Count AndAlso Not pTAll(cnt2)(cnt4) = 0
                                                        pP.Add(shiftedPathPts(cnt4))
                                                        pT.Add(pTAll(cnt2)(cnt4))
                                                        cnt4 += 1
                                                    End While

                                                    pT(pT.Count - 1) = 129
                                                    shiftedPath.Add(pP, pT)
                                                End While
                                            Else 'no stretch figures have all the same length
                                                'setup and fill the data lists
                                                Dim cnt4 As Integer = 0
                                                Dim pT As New List(Of Byte)
                                                Dim pP As New List(Of PointF)
                                                While cnt4 < shiftedPathPts.Count
                                                    pP.Add(shiftedPathPts(cnt4))
                                                    pT.Add(0)
                                                    cnt4 += 1
                                                End While

                                                pT(pT.Count - 1) = 129 'ensure close
                                                shiftedPath.Add(pP, pT)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End Using
                    End If
                Else
                    Exit For
                End If
            Next

            Return shiftedPath
        Else
            Return New GraphicsPathData()
        End If
    End Function

    Private Sub GetNewAndInterpolatedPoints(outerPath As GraphicsPath, newInnerPathPoints As List(Of PointF), shiftedPathPts As List(Of PointF),
                         outerPts As PointF(), fractionMode As ShiftFractionMode, breite As Integer, w As Integer, h As Integer, fraction As Double,
                         outerFraction As Double, rc As RectangleF)
        If fractionMode = ShiftFractionMode.FollowNormal Then
            Dim w2 As Integer = w
            Dim h2 As Integer = h

            'draw the outerpath to a bitmap
            If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 8L) Then
                Using bmpTmp2 As New Bitmap(w2, h2)
                    Using mx As New Matrix(1, 0, 0, 1, -rc.X, -rc.Y)
                        outerPath.Transform(mx)
                    End Using

                    Using gx As Graphics = Graphics.FromImage(bmpTmp2)
                        'gx.Clear(Color.Transparent)
                        gx.InterpolationMode = InterpolationMode.HighQualityBicubic
                        gx.SmoothingMode = SmoothingMode.AntiAlias

                        Using pen As New Pen(Color.Black, 1)
                            pen.LineJoin = LineJoin.Round
                            gx.DrawPath(pen, outerPath)
                        End Using
                    End Using

                    Using mx As New Matrix(1, 0, 0, 1, rc.X, rc.Y)
                        outerPath.Transform(mx)
                    End Using

                    Dim bmData As BitmapData = Nothing
                    Dim p(w2 * h2 * 4 - 1) As Byte
                    Dim stride As Integer = 0

                    Try
                        bmData = bmpTmp2.LockBits(New Rectangle(0, 0, w2, h2), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                        stride = bmData.Stride
                        Marshal.Copy(bmData.Scan0, p, 0, p.Length)
                        bmpTmp2.UnlockBits(bmData)
                    Catch
                        Try
                            bmpTmp2.UnlockBits(bmData)
                        Catch

                        End Try
                    End Try

                    For i As Integer = 0 To newInnerPathPoints.Count - 1
                        Dim dxN As Double = 0
                        Dim dyN As Double = 0

                        'get the slope's dx & dy
                        If newInnerPathPoints.Count > 2 AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).X = newInnerPathPoints(0).X AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).Y = newInnerPathPoints(0).Y Then
                            If i = 0 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 2).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 2).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i = newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(1).Y)
                            End If
                        Else
                            If i = 0 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i = newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(0).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(0).Y)
                            End If
                        End If

                        'normalize
                        Dim r As Double = Math.Max(Math.Abs(dxN), Math.Abs(dyN))
                        dxN /= r
                        dyN /= r

                        Dim innerX As Single = newInnerPathPoints(i).X - rc.X + 0.5F 'fixed add (needed by drawing path antialiased)
                        Dim innerY As Single = newInnerPathPoints(i).Y - rc.Y + 0.5F

                        Dim outerX As Single = innerX
                        Dim outerY As Single = innerY

                        Dim outerXF As Integer = Math.Min(Math.Max(CInt(Math.Floor(outerX)), 0), w2 - 1)
                        Dim outerXC As Integer = Math.Min(CInt(Math.Ceiling(outerX)), w2 - 1)
                        Dim outerYF As Integer = Math.Min(Math.Max(CInt(Math.Floor(outerY)), 0), h2 - 1)
                        Dim outerYC As Integer = Math.Min(CInt(Math.Ceiling(outerY)), h2 - 1)
                        Dim cntPic As Integer = 0

                        'now search the bitmap for the outerCoord starting at the innerCoord, stepping in normal direction
                        Try
                            If dxN > dyN Then
                                While p(outerYF * stride + outerXF * 4 + 3) = 0 AndAlso p(outerYF * stride + outerXC * 4 + 3) = 0 AndAlso cntPic < breite
                                    outerX = CSng(outerX + dyN)
                                    outerY = CSng(outerY + dxN)

                                    outerXF = Math.Min(Math.Max(CInt(Math.Floor(outerX)), 0), w2 - 1)
                                    outerXC = Math.Min(CInt(Math.Ceiling(outerX)), w2 - 1)
                                    outerYF = Math.Min(Math.Max(CInt(Math.Floor(outerY)), 0), h2 - 1)
                                    outerYC = Math.Min(CInt(Math.Ceiling(outerY)), h2 - 1)

                                    cntPic += 1
                                End While
                            Else
                                While p(outerYF * stride + outerXF * 4 + 3) = 0 AndAlso p(outerYC * stride + outerXF * 4 + 3) = 0 AndAlso cntPic < breite
                                    outerX = CSng(outerX + dyN)
                                    outerY = CSng(outerY + dxN)

                                    outerXF = Math.Min(Math.Max(CInt(Math.Floor(outerX)), 0), w2 - 1)
                                    outerXC = Math.Min(CInt(Math.Ceiling(outerX)), w2 - 1)
                                    outerYF = Math.Min(Math.Max(CInt(Math.Floor(outerY)), 0), h2 - 1)
                                    outerYC = Math.Min(CInt(Math.Ceiling(outerY)), h2 - 1)

                                    cntPic += 1
                                End While
                            End If
                        Catch

                        End Try

                        'interpolate linearly
                        Dim newX As Double = innerX * fraction + outerX * outerFraction
                        Dim newY As Double = innerY * fraction + outerY * outerFraction

                        'add to list
                        shiftedPathPts.Add(New PointF(CSng(newX + rc.X), CSng(newY + rc.Y)))
                    Next

                    p = Nothing
                End Using
            End If
        Else
            'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
            'for keeping the amount of figures of the innerPath
            For i As Integer = 0 To newInnerPathPoints.Count - 1
                Dim minDist As Double = Double.MaxValue 'find closest point innouterpath
                Dim minIndex As Integer = -1

                For j As Integer = 0 To outerPts.Count - 1
                    Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
                    Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
                    Dim dist As Double = Math.Sqrt(dx * dx + dy * dy) 'get distance of inner to outer point(s)

                    If dist < minDist Then 'record closest point
                        minDist = dist
                        minIndex = j
                    End If
                Next

                'Note: this is just a rough estimation of the correct index, when the (integer-) shift is done in both directions.
                'We just multiply the smaller slope-direction of the inner path with the large shift direction and 
                'subtract the current shift in that small direction to get an estimate of how far the correct index is away 
                'from our current one which represents the shortest distance between outer and inner path.
                'This is more or less fun, I will do some tests, and if the results are good, I'll keep it, else we would have to
                'follow the normal of the inner path at the current coord until it crosses the outerpath.
                Dim addX As Integer = 0
                Dim addY As Integer = 0

                If fractionMode = ShiftFractionMode.ClosestAndEstimate Then
                    Dim dxN As Double = 0
                    Dim dyN As Double = 0

                    If newInnerPathPoints.Count > 2 AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).X = newInnerPathPoints(0).X AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).Y = newInnerPathPoints(0).Y Then
                        If i = 0 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 2).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 2).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i = newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(1).Y)
                        End If
                    Else
                        If i = 0 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i = newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(0).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(0).Y)
                        End If
                    End If
                    Dim r As Double = Math.Max(Math.Abs(dxN), Math.Abs(dyN))
                    dxN /= r
                    dyN /= r

                    Dim dx As Double = (outerPts(minIndex).X - newInnerPathPoints(i).X)
                    Dim dy As Double = (outerPts(minIndex).Y - newInnerPathPoints(i).Y)

                    Dim r2 As Double = Math.Max(Math.Abs(dx), Math.Abs(dy))

                    'we assume that in the outerpoints each point differs 1 from the point before,
                    'so we estimate the correct index by:
                    If Not dx / r2 = -dyN OrElse Not dy / r2 = dxN Then 'comp reversed (from dxN and dyN to normal)
                        If dxN > dyN Then 'large shift is in y direction
                            addX = CInt(dy * -dyN - dx) 'large direction * small offset - current distance
                        Else 'large shift is in x direction
                            addY = CInt(dx * -dxN - dy)
                        End If
                    End If
                End If

                If minIndex > -1 Then 'should always be, todo: comment out test for -1
                    Dim outerX As Single = outerPts(minIndex).X
                    Dim outerY As Single = outerPts(minIndex).Y

                    If addX <> 0 Then
                        If minIndex + addX >= 0 AndAlso minIndex + addX < outerPts.Length Then
                            outerX = outerPts(minIndex + addX).X
                        Else
                            If minIndex + addX < 0 Then
                                outerX = outerPts(outerPts.Length + (minIndex + addX)).X
                            End If
                            If minIndex + addX > outerPts.Length - 1 Then
                                outerX = outerPts(minIndex + addX - outerPts.Length).X
                            End If
                        End If
                    End If
                    If addY <> 0 Then
                        If minIndex + addY >= 0 AndAlso minIndex + addY < outerPts.Length Then
                            outerY = outerPts(minIndex + addY).Y
                        Else
                            If minIndex + addY < 0 Then
                                outerY = outerPts(outerPts.Length + (minIndex + addY)).Y
                            End If
                            If minIndex + addY > outerPts.Length - 1 Then
                                outerY = outerPts(minIndex + addY - outerPts.Length).Y
                            End If
                        End If
                    End If

                    Dim innerX As Single = newInnerPathPoints(i).X
                    Dim innerY As Single = newInnerPathPoints(i).Y

                    'interpolate linearly
                    Dim newX As Double = innerX * fraction + outerX * outerFraction
                    Dim newY As Double = innerY * fraction + outerY * outerFraction

                    'add to list
                    shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
                End If
            Next
        End If
    End Sub

    Private Sub ShiftOutwards(shiftInwards As Single, outerPath As GraphicsPath, outerPts As PointF(), breite As Integer,
                           rc As RectangleF, w As Integer, h As Integer)
        Dim bmpTmp2 As Bitmap = Nothing
        Dim bmpOuter As Bitmap = Nothing

        Try
            Dim br As Integer = CInt(Math.Floor(shiftInwards)) 'width
            If AvailMem.AvailMem.checkAvailRam((w - br * 2) * (h - br * 2) * 4L) Then
                bmpTmp2 = New Bitmap(w - br * 2, h - br * 2)
                Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
                    Using p As New Pen(Color.Black, -br)
                        Using mx As New Matrix(1, 0, 0, 1, -rc.X - br, -rc.Y - br) 'translate close to origin -> so we can draw the entire path to the bmp
                            outerPath2.Transform(mx)
                        End Using

                        Using g As Graphics = Graphics.FromImage(bmpTmp2)
                            g.FillPath(Brushes.Black, outerPath2)
                        End Using

                        bmpOuter = ExtOutline(bmpTmp2, -breite + 1, Nothing) 'add outline

                        Dim lOuter As List(Of ChainCodeF) = GetBoundaryF(bmpOuter) 'get the new outline

                        outerPath.Reset() 'refill the new outer path
                        For i As Integer = 0 To lOuter.Count - 1
                            'translate
                            For j As Integer = 0 To lOuter(i).Coord.Count - 1
                                lOuter(i).Coord(j) = New PointF(lOuter(i).Coord(j).X + rc.X + br, lOuter(i).Coord(j).Y + rc.Y + br)
                            Next

                            If lOuter(i).Coord(lOuter(i).Coord.Count - 1).X = lOuter(i).Coord(0).X AndAlso lOuter(i).Coord(lOuter(i).Coord.Count - 1).Y = lOuter(i).Coord(0).Y Then
                                lOuter(i).Coord.RemoveAt(lOuter(i).Coord.Count - 1)
                            End If

                            outerPath.AddLines(lOuter(i).Coord.ToArray())
                            outerPath.CloseFigure()
                        Next
                    End Using
                End Using
            End If
        Catch

        Finally
            If Not bmpTmp2 Is Nothing Then
                bmpTmp2.Dispose()
                bmpTmp2 = Nothing
            End If
            If Not bmpOuter Is Nothing Then
                bmpOuter.Dispose()
                bmpOuter = Nothing
            End If
        End Try
    End Sub

    Private Sub AnalyzeAndSplit(gP As GraphicsPath, pPAll As List(Of List(Of PointF)), pTAll As List(Of List(Of Byte)))
        'do it separately for each PathFigure...
        Dim t(gP.PathTypes.Length - 1) As Byte
        gP.PathTypes.CopyTo(t, 0)
        Dim pts(gP.PathPoints.Length - 1) As PointF
        gP.PathPoints.CopyTo(pts, 0)

        Dim cnt As Integer = 0

        While cnt < t.Length
            Dim pT As New List(Of Byte)
            Dim pP As New List(Of PointF)

            pP.Add(pts(cnt))
            pT.Add(0)  'start figure
            cnt += 1

            While cnt < t.Length AndAlso Not t(cnt) = 0 'if 0, new figure
                pP.Add(pts(cnt))
                pT.Add(t(cnt))
                cnt += 1
            End While

            pPAll.Add(pP)
            pT(pT.Count - 1) = 129 'close figure
            pTAll.Add(pT)
        End While

        pPAll = pPAll.OrderByDescending(Function(a) a.Count).ToList()
        pTAll = pTAll.OrderByDescending(Function(a) a.Count).ToList()
    End Sub

    'we draw the [translated] path in a bitmap and [re-]scan the inner-outline of the drawn path, and interpolate the points to get the result("shifted")-locations
    'we have to draw smeared out a bit, else (interpolationmode nearestneighbor and pixeloffsetmod half and a one px wide pen) we would get a lot of one pixel figures by scanning with a 4-Chaincode ->
    'we then would get diagonally shifted single pixel filled structures that would result in "nullcells" or outlines going around those pixels
    Public Function ShiftCoords2(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean, fractionMode As ShiftFractionMode) As GraphicsPath
        If gP.PointCount > 1 Then
            Dim shiftedPath As New GraphicsPath()

            Using outerPath As GraphicsPath = CType(gP.Clone(), GraphicsPath)
                If outerPath.PointCount > 1 Then
                    Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
                    outerPath.PathPoints.CopyTo(outerPts, 0)
                    Dim rc As RectangleF = outerPath.GetBounds()
                    Dim w As Integer = CInt(Math.Ceiling(rc.Width))
                    Dim h As Integer = CInt(Math.Ceiling(rc.Height))

                    If w > 0 AndAlso h > 0 Then
                        Dim bmpTmp As Bitmap = Nothing

                        Dim breite As Integer = CInt(Math.Ceiling(shiftInwards)) 'width to remove
                        Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards))) 'the weight of the inner path when interpolating points

                        If shiftInwards < 0 Then 'maybe we want to shift outwards?
                            ShiftOutwards(shiftInwards, outerPath, outerPts, breite, rc, w, h)

                            Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
                            outerPath.PathPoints.CopyTo(outerPts2, 0)
                            outerPts = outerPts2 'new points
                            rc = outerPath.GetBounds() 'new bounds
                            w = CInt(Math.Ceiling(rc.Width))
                            h = CInt(Math.Ceiling(rc.Height))
                            shiftInwards *= -1 'switch sign -> outerpath is now the extended outer path, original outerpath is inner path now
                            'fraction = 1.0 - fraction 'switch fraction

                            'we extended by breite so we only need to shift back by the fraction
                            shiftInwards -= CInt(Math.Floor(shiftInwards))
                            breite = 1
                            fraction = 1.0 - shiftInwards
                        End If

                        If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
                            Dim shiftedPathPts As New List(Of PointF)
                            Dim shiftedPathTypes As New List(Of Byte)

                            If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
                                Try
                                    bmpTmp = New Bitmap(w + 2, h + 2)
                                    Dim lInner As List(Of ChainCode) = Nothing
                                    Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
                                        For cntW As Integer = 0 To Math.Abs(breite) - 1 'do it one (px) by one
                                            Using g As Graphics = Graphics.FromImage(bmpTmp)
                                                g.Clear(Color.Transparent)
                                                g.InterpolationMode = InterpolationMode.HighQualityBicubic 'InterpolationMode.NearestNeighbor
                                                'g.PixelOffsetMode = PixelOffsetMode.Half '(if you use this, you probably need to translate back with only the rc-values (not rc-values -1 in line 4191f))
                                                g.SmoothingMode = SmoothingMode.AntiAlias 'needed for setting nullcells = false
                                                If cntW = 0 Then 'transform only once
                                                    Using mx As New Matrix(1, 0, 0, 1, -rc.X + 1, -rc.Y + 1)
                                                        outerPath2.Transform(mx)
                                                    End Using
                                                End If

                                                outerPath2.CloseAllFigures()
                                                Using pen As New Pen(Color.Black, 1) 'draw the path
                                                    pen.LineJoin = LineJoin.Round
                                                    g.DrawPath(pen, outerPath2)
                                                End Using

                                                'lInner = GetBoundary(bmpTmp)
                                                lInner = GetBoundaryShifted(bmpTmp) 'get the inner outlines shifted by one px

                                                'rem last point for each figure
                                                For i As Integer = 0 To lInner.Count - 1
                                                    If lInner(i).Coord(lInner(i).Coord.Count - 1).X = lInner(i).Coord(0).X AndAlso lInner(i).Coord(lInner(i).Coord.Count - 1).Y = lInner(i).Coord(0).Y Then
                                                        lInner(i).Coord.RemoveAt(lInner(i).Coord.Count - 1)
                                                    End If
                                                Next

                                                If lInner.Count > 0 Then
                                                    lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()
                                                    'removeOuterOutlines from linner
                                                    For cnt As Integer = lInner.Count - 1 To 0 Step -1
                                                        If lInner(cnt).Coord.Count <= 4 Then 'just one pixel
                                                            lInner.RemoveAt(cnt)
                                                            Continue For
                                                        End If
                                                        If lInner(cnt).Chain(lInner(cnt).Chain.Count - 1) = 2 Then 'not an inner outline
                                                            lInner.RemoveAt(cnt)
                                                            Continue For
                                                        Else
                                                            'we dont need the chain here, only the coords, and we always close the figure, so we can do:
                                                            lInner(cnt).Coord = lInner(cnt).Coord.Distinct().ToList()
                                                        End If
                                                    Next
                                                End If

                                                outerPath2.Reset()
                                                For cnt As Integer = 0 To lInner.Count - 1 'add a figure for each chain
                                                    outerPath2.StartFigure()
                                                    outerPath2.AddLines(lInner(cnt).Coord.ToArray())
                                                    outerPath2.CloseFigure()
                                                Next
                                                outerPath2.FillMode = FillMode.Alternate
                                            End Using
                                        Next
                                    End Using

                                    If mapLargestOnly Then
                                        If lInner.Count > 1 Then
                                            lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()
                                            lInner.RemoveRange(1, lInner.Count - 1)
                                        End If
                                    End If

                                    If lInner.Count > 0 Then
                                        Dim outerFraction As Double = 1.0 - fraction 'complementary fraction

                                        For cnt As Integer = 0 To lInner.Count - 1
                                            'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
                                            'for keeping the amount of figures of the innerPath

                                            Dim newInnerPathPoints As New List(Of PointF)

                                            For cnt2 As Integer = 0 To lInner(cnt).Coord.Count - 1 'add (re-)translated
                                                newInnerPathPoints.Add(New PointF(lInner(cnt).Coord(cnt2).X + rc.X - 1,
                                                                                  lInner(cnt).Coord(cnt2).Y + rc.Y - 1))
                                            Next

                                            GetNewAndInterpolatedPoints2(outerPath, newInnerPathPoints, shiftedPathPts, shiftedPathTypes,
                                                                                            outerPts, fractionMode, breite, w, h, fraction, outerFraction, rc) 'do the work

                                        Next
                                    End If
                                Catch
                                Finally
                                    If Not bmpTmp Is Nothing Then
                                        bmpTmp.Dispose()
                                        bmpTmp = Nothing
                                    End If
                                End Try
                            End If

                            If shiftedPathPts.Count > 1 Then 'valid path? If so, add to the resulting path
                                'make sure length of both arrays are the same
                                If shiftedPathPts.Count <> shiftedPathTypes.Count Then
                                    shiftedPathPts = RestrictOuterPath(shiftedPathPts, shiftedPathTypes.Count)
                                End If
                                Dim gP4 As New GraphicsPath(shiftedPathPts.ToArray(), shiftedPathTypes.ToArray())
                                gP4.FillMode = FillMode.Alternate
                                gP4.CloseFigure()
                                shiftedPath.AddPath(gP4, False)
                            End If
                        End If
                    End If
                End If
            End Using

            Return shiftedPath
        Else
            Return New GraphicsPath
        End If
    End Function

    Private Sub GetNewAndInterpolatedPoints2(outerPath As GraphicsPath, newInnerPathPoints As List(Of PointF), shiftedPathPts As List(Of PointF),
                                       shiftedPathTypes As List(Of Byte), outerPts() As PointF, fractionMode As ShiftFractionMode, breite As Integer,
                                       w As Integer, h As Integer, fraction As Double, outerFraction As Double, rc As RectangleF)

        If fractionMode = ShiftFractionMode.FollowNormal Then
            Dim w2 As Integer = w
            Dim h2 As Integer = h

            'draw the outerpath to a bitmap
            If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 8L) Then
                Using bmpTmp2 As New Bitmap(w2, h2)
                    Using mx As New Matrix(1, 0, 0, 1, -rc.X, -rc.Y)
                        outerPath.Transform(mx)
                    End Using

                    Using gx As Graphics = Graphics.FromImage(bmpTmp2)
                        'gx.Clear(Color.Transparent)
                        gx.InterpolationMode = InterpolationMode.HighQualityBicubic
                        gx.SmoothingMode = SmoothingMode.AntiAlias

                        Using pen As New Pen(Color.Black, 1)
                            pen.LineJoin = LineJoin.Round
                            gx.DrawPath(pen, outerPath)
                        End Using
                    End Using

                    Using mx As New Matrix(1, 0, 0, 1, rc.X, rc.Y)
                        outerPath.Transform(mx)
                    End Using

                    Dim bmData As BitmapData = Nothing
                    Dim p(w2 * h2 * 4 - 1) As Byte
                    Dim stride As Integer = 0

                    Try
                        bmData = bmpTmp2.LockBits(New Rectangle(0, 0, w2, h2), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
                        stride = bmData.Stride
                        Marshal.Copy(bmData.Scan0, p, 0, p.Length)
                        bmpTmp2.UnlockBits(bmData)
                    Catch
                        Try
                            bmpTmp2.UnlockBits(bmData)
                        Catch

                        End Try
                    End Try

                    For i As Integer = 0 To newInnerPathPoints.Count - 1
                        Dim dxN As Double = 0
                        Dim dyN As Double = 0

                        'get the slope's dx & dy
                        If newInnerPathPoints.Count > 2 AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).X = newInnerPathPoints(0).X AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).Y = newInnerPathPoints(0).Y Then
                            If i = 0 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 2).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 2).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i = newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(1).Y)
                            End If
                        Else
                            If i = 0 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                            ElseIf i = newInnerPathPoints.Count - 1 Then
                                dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(0).X)
                                dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(0).Y)
                            End If
                        End If

                        'normalize
                        Dim r As Double = Math.Max(Math.Abs(dxN), Math.Abs(dyN))
                        dxN /= r
                        dyN /= r

                        Dim innerX As Single = newInnerPathPoints(i).X - rc.X + 0.5F 'fixed add (needed by drawing path antialiased)
                        Dim innerY As Single = newInnerPathPoints(i).Y - rc.Y + 0.5F

                        Dim outerX As Single = innerX
                        Dim outerY As Single = innerY

                        Dim outerXF As Integer = Math.Min(Math.Max(CInt(Math.Floor(outerX)), 0), w2 - 1)
                        Dim outerXC As Integer = Math.Min(CInt(Math.Ceiling(outerX)), w2 - 1)
                        Dim outerYF As Integer = Math.Min(Math.Max(CInt(Math.Floor(outerY)), 0), h2 - 1)
                        Dim outerYC As Integer = Math.Min(CInt(Math.Ceiling(outerY)), h2 - 1)
                        Dim cntPic As Integer = 0

                        'now search the bitmap for the outerCoord starting at the innerCoord, stepping in normal direction
                        Try
                            If dxN > dyN Then
                                While p(outerYF * stride + outerXF * 4 + 3) = 0 AndAlso p(outerYF * stride + outerXC * 4 + 3) = 0 AndAlso cntPic < breite
                                    outerX = CSng(outerX + dyN)
                                    outerY = CSng(outerY + dxN)

                                    outerXF = Math.Min(Math.Max(CInt(Math.Floor(outerX)), 0), w2 - 1)
                                    outerXC = Math.Min(CInt(Math.Ceiling(outerX)), w2 - 1)
                                    outerYF = Math.Min(Math.Max(CInt(Math.Floor(outerY)), 0), h2 - 1)
                                    outerYC = Math.Min(CInt(Math.Ceiling(outerY)), h2 - 1)

                                    cntPic += 1
                                End While
                            Else
                                While p(outerYF * stride + outerXF * 4 + 3) = 0 AndAlso p(outerYC * stride + outerXF * 4 + 3) = 0 AndAlso cntPic < breite
                                    outerX = CSng(outerX + dyN)
                                    outerY = CSng(outerY + dxN)

                                    outerXF = Math.Min(Math.Max(CInt(Math.Floor(outerX)), 0), w2 - 1)
                                    outerXC = Math.Min(CInt(Math.Ceiling(outerX)), w2 - 1)
                                    outerYF = Math.Min(Math.Max(CInt(Math.Floor(outerY)), 0), h2 - 1)
                                    outerYC = Math.Min(CInt(Math.Ceiling(outerY)), h2 - 1)

                                    cntPic += 1
                                End While
                            End If
                        Catch

                        End Try

                        'interpolate linearly
                        Dim newX As Double = innerX * fraction + outerX * outerFraction
                        Dim newY As Double = innerY * fraction + outerY * outerFraction

                        'add to list
                        shiftedPathPts.Add(New PointF(CSng(newX + rc.X), CSng(newY + rc.Y)))
                        shiftedPathTypes.Add(If(i = 0, CByte(0), CByte(1)))
                    Next

                    shiftedPathTypes(shiftedPathTypes.Count - 1) = 129 'close figure

                    p = Nothing
                End Using
            End If
        Else
            For i As Integer = 0 To newInnerPathPoints.Count - 1
                Dim minDist As Double = Double.MaxValue
                Dim minIndex As Integer = -1

                For j As Integer = 0 To outerPts.Count - 1 'get closest point from the outerpath
                    Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
                    Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
                    Dim dist As Double = Math.Sqrt(dx * dx + dy * dy)

                    If dist < minDist Then
                        minDist = dist
                        minIndex = j
                    End If
                Next

                'Note: this is just a rough estimation of the correct index, when the (integer-) shift is done in both directions.
                'We just multiply the smaller slope-direction of the inner path with the large shift direction and 
                'subtract the current shift in that small direction to get an estimate of how far the correct index is away 
                'from our current one which represents the shortest distance between outer and inner path.
                'This is more or less fun, I will do some tests, and if the results are good, I'll keep it, else we would have to
                'follow the normal of the inner path at the current coord until it crosses the outerpath.
                Dim addX As Integer = 0
                Dim addY As Integer = 0

                If fractionMode = ShiftFractionMode.ClosestAndEstimate Then
                    Dim dxN As Double = 0
                    Dim dyN As Double = 0

                    If newInnerPathPoints.Count > 2 AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).X = newInnerPathPoints(0).X AndAlso newInnerPathPoints(newInnerPathPoints.Count - 1).Y = newInnerPathPoints(0).Y Then
                        If i = 0 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 2).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 2).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i = newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(1).Y)
                        End If
                    Else
                        If i = 0 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(newInnerPathPoints.Count - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(newInnerPathPoints.Count - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i > 0 AndAlso i < newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(i + 1).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(i + 1).Y)
                        ElseIf i = newInnerPathPoints.Count - 1 Then
                            dxN = (newInnerPathPoints(i).X - newInnerPathPoints(i - 1).X) - (newInnerPathPoints(i).X - newInnerPathPoints(0).X)
                            dyN = (newInnerPathPoints(i).Y - newInnerPathPoints(i - 1).Y) - (newInnerPathPoints(i).Y - newInnerPathPoints(0).Y)
                        End If
                    End If
                    Dim r As Double = Math.Max(Math.Abs(dxN), Math.Abs(dyN))
                    dxN /= r
                    dyN /= r

                    Dim dx As Double = (outerPts(minIndex).X - newInnerPathPoints(i).X)
                    Dim dy As Double = (outerPts(minIndex).Y - newInnerPathPoints(i).Y)

                    Dim r2 As Double = Math.Max(Math.Abs(dx), Math.Abs(dy))

                    'we assume that in the outerpoints each point differs 1 from the point before,
                    'so we estimate the correct index by:
                    If Not dx / r2 = -dyN OrElse Not dy / r2 = dxN Then 'comp reversed (from dxN and dyN to normal)
                        If dxN > dyN Then 'large shift is in y direction
                            addX = CInt(dy * -dyN - dx) 'large direction * small offset - current distance
                        Else 'large shift is in x direction
                            addY = CInt(dx * -dxN - dy)
                        End If
                    End If
                End If

                'now interpolate linearly
                If minIndex > -1 Then 'always, todo: comment out test for -1
                    Dim outerX As Single = outerPts(minIndex).X
                    Dim outerY As Single = outerPts(minIndex).Y

                    If addX <> 0 Then
                        If minIndex + addX >= 0 AndAlso minIndex + addX < outerPts.Length Then
                            outerX = outerPts(minIndex + addX).X
                        Else
                            If minIndex + addX < 0 Then
                                outerX = outerPts(outerPts.Length + (minIndex + addX)).X
                            End If
                            If minIndex + addX > outerPts.Length - 1 Then
                                outerX = outerPts(minIndex + addX - outerPts.Length).X
                            End If
                        End If
                    End If
                    If addY <> 0 Then
                        If minIndex + addY >= 0 AndAlso minIndex + addY < outerPts.Length Then
                            outerY = outerPts(minIndex + addY).Y
                        Else
                            If minIndex + addY < 0 Then
                                outerY = outerPts(outerPts.Length + (minIndex + addY)).Y
                            End If
                            If minIndex + addY > outerPts.Length - 1 Then
                                outerY = outerPts(minIndex + addY - outerPts.Length).Y
                            End If
                        End If
                    End If

                    Dim innerX As Single = newInnerPathPoints(i).X
                    Dim innerY As Single = newInnerPathPoints(i).Y

                    Dim newX As Double = innerX * fraction + outerX * outerFraction
                    Dim newY As Double = innerY * fraction + outerY * outerFraction

                    shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
                    shiftedPathTypes.Add(If(i = 0, CByte(0), CByte(1)))
                End If
            Next
        End If
        shiftedPathTypes(shiftedPathTypes.Count - 1) = 129 'close figure
    End Sub



    Public Function ShiftCoords_old(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean, stretch As Boolean) As GraphicsPathData
        If gP.PointCount > 1 Then
            'do it separately for each PathFigure...
            Dim t(gP.PathTypes.Length - 1) As Byte
            gP.PathTypes.CopyTo(t, 0)
            Dim pts(gP.PathPoints.Length - 1) As PointF
            gP.PathPoints.CopyTo(pts, 0)

            Dim pTAll As New List(Of List(Of Byte))
            Dim pPAll As New List(Of List(Of PointF))

            Dim cnt As Integer = 0

            While cnt < t.Length
                Dim pT As New List(Of Byte)
                Dim pP As New List(Of PointF)

                pP.Add(pts(cnt))
                pT.Add(0) 'pT.Add(t(cnt))
                cnt += 1

                While cnt < t.Length AndAlso Not t(cnt) = 0
                    pP.Add(pts(cnt))
                    pT.Add(t(cnt))
                    cnt += 1
                End While

                pPAll.Add(pP)
                pT(pT.Count - 1) = 129 'fffffff
                pTAll.Add(pT)
            End While

            pPAll = pPAll.OrderByDescending(Function(a) a.Count).ToList()
            pTAll = pTAll.OrderByDescending(Function(a) a.Count).ToList()

            Dim shiftedPath As New GraphicsPathData()

            For cnt2 As Integer = 0 To pPAll.Count - 1
                If (mapLargestOnly AndAlso cnt2 = 0) OrElse Not mapLargestOnly Then
                    If pPAll(cnt2).Count > 1 Then
                        Using outerPath As GraphicsPath = New GraphicsPath(pPAll(cnt2).ToArray(), pTAll(cnt2).ToArray())
                            If outerPath.PointCount > 1 Then
                                Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
                                outerPath.PathPoints.CopyTo(outerPts, 0)
                                Dim rc As RectangleF = outerPath.GetBounds()
                                Dim w As Integer = CInt(Math.Ceiling(rc.Width))
                                Dim h As Integer = CInt(Math.Ceiling(rc.Height))

                                If w > 0 AndAlso h > 0 Then
                                    Dim bmpTmp As Bitmap = Nothing
                                    Dim bmpInner As Bitmap = Nothing

                                    Dim breite As Integer = CInt(Math.Ceiling(shiftInwards))
                                    Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards)))

                                    If shiftInwards < 0 Then
                                        Dim bmpTmp2 As Bitmap = Nothing
                                        Dim bmpOuter As Bitmap = Nothing

                                        Try
                                            Dim br As Integer = CInt(Math.Floor(shiftInwards))
                                            If AvailMem.AvailMem.checkAvailRam((w - br * 2) * (h - br * 2) * 4L) Then
                                                bmpTmp2 = New Bitmap(w - br * 2, h - br * 2)
                                                Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
                                                    Using p As New Pen(Color.Black, -br)
                                                        Using mx As New Matrix(1, 0, 0, 1, -rc.X - br, -rc.Y - br)
                                                            outerPath2.Transform(mx)
                                                        End Using

                                                        Using g As Graphics = Graphics.FromImage(bmpTmp2)
                                                            g.FillPath(Brushes.Black, outerPath2)
                                                        End Using

                                                        bmpOuter = ExtOutline(bmpTmp2, -breite, Nothing)

                                                        Dim lOuter As List(Of ChainCodeF) = GetBoundaryF(bmpOuter)

                                                        outerPath.Reset()
                                                        For i As Integer = 0 To lOuter.Count - 1
                                                            'translate
                                                            For j As Integer = 0 To lOuter(i).Coord.Count - 1
                                                                lOuter(i).Coord(j) = New PointF(lOuter(i).Coord(j).X + rc.X + br, lOuter(i).Coord(j).Y + rc.Y + br)
                                                            Next
                                                            outerPath.AddLines(lOuter(i).Coord.ToArray())
                                                            outerPath.CloseFigure()
                                                        Next

                                                        Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
                                                        outerPath.PathPoints.CopyTo(outerPts2, 0)
                                                        outerPts = outerPts2
                                                        rc = outerPath.GetBounds()
                                                        w = CInt(Math.Ceiling(rc.Width))
                                                        h = CInt(Math.Ceiling(rc.Height))
                                                        shiftInwards *= -1
                                                        fraction = 1.0 - fraction
                                                    End Using
                                                End Using
                                            End If

                                        Catch

                                        Finally
                                            If Not bmpTmp2 Is Nothing Then
                                                bmpTmp2.Dispose()
                                                bmpTmp2 = Nothing
                                            End If
                                            If Not bmpOuter Is Nothing Then
                                                bmpOuter.Dispose()
                                                bmpOuter = Nothing
                                            End If
                                        End Try
                                    End If

                                    If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
                                        Dim shiftedPathPts As New List(Of PointF)

                                        If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
                                            Try
                                                bmpTmp = New Bitmap(w, h)
                                                Using g As Graphics = Graphics.FromImage(bmpTmp)
                                                    Using mx As New Matrix(1, 0, 0, 1, -rc.X, -rc.Y)
                                                        outerPath.Transform(mx)
                                                    End Using
                                                    g.FillPath(Brushes.Black, outerPath)
                                                    Using mx As New Matrix(1, 0, 0, 1, rc.X, rc.Y)
                                                        outerPath.Transform(mx)
                                                    End Using

                                                    bmpInner = RemOutline(bmpTmp, breite, Nothing)

                                                    If Not bmpInner Is Nothing Then
                                                        'get the new outline (path) and prolongate it to have the same amount of points as the large path
                                                        'then find the closest points and resample
                                                        'innerPath can have more figures than outerPath
                                                        Dim lInner As List(Of ChainCode) = GetBoundary(bmpInner)

                                                        If lInner.Count > 0 Then
                                                            lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()

                                                            Dim newInnerPtsAll As New List(Of List(Of PointF))

                                                            If mapLargestOnly Then
                                                                Dim newInnerPts As New List(Of PointF)
                                                                For j As Integer = 0 To lInner(0).Coord.Count - 1
                                                                    newInnerPts.Add(New PointF(lInner(0).Coord(j).X, lInner(0).Coord(j).Y))
                                                                Next
                                                                newInnerPtsAll.Add(newInnerPts)
                                                            Else
                                                                For i As Integer = 0 To lInner.Count - 1
                                                                    Dim newInnerPts As New List(Of PointF)
                                                                    For j As Integer = 0 To lInner(i).Coord.Count - 1
                                                                        newInnerPts.Add(New PointF(lInner(i).Coord(j).X, lInner(i).Coord(j).Y))
                                                                    Next
                                                                    newInnerPtsAll.Add(newInnerPts)
                                                                Next
                                                            End If

                                                            Dim factor As Double = outerPath.PointCount / lInner.Sum(Function(a) a.Coord.Count)
                                                            Dim dFracSum As Double = 0.0
                                                            Dim pointCountAll As Integer = 0

                                                            For l As Integer = 0 To newInnerPtsAll.Count - 1
                                                                Dim dLength As Double = newInnerPtsAll(l).Count * factor
                                                                Dim pointCount As Integer = CInt(Math.Ceiling(dLength))
                                                                dFracSum += pointCount - dLength

                                                                If dFracSum > 1.0 Then
                                                                    dFracSum -= 1
                                                                    pointCount -= 1
                                                                End If

                                                                pointCountAll += pointCount

                                                                If pointCountAll > outerPath.PointCount Then
                                                                    pointCount -= pointCountAll - outerPath.PointCount
                                                                End If

                                                                newInnerPtsAll(l) = TranslateNewInnerPath(newInnerPtsAll(l), rc)
                                                                Dim newInnerPathPoints As List(Of PointF) = newInnerPtsAll(l)
                                                                If stretch Then
                                                                    ProlongateInnerPath(newInnerPtsAll(l), pointCount, factor)
                                                                End If

                                                                Dim bIsClosed As Boolean = False

                                                                If newInnerPtsAll(l)(newInnerPtsAll(l).Count - 1).X = newInnerPtsAll(l)(0).X AndAlso newInnerPtsAll(l)(newInnerPtsAll(l).Count - 1).Y = newInnerPtsAll(l)(0).Y Then
                                                                    newInnerPathPoints(newInnerPathPoints.Count - 1) = New PointF(newInnerPathPoints(0).X, newInnerPathPoints(0).Y)
                                                                    bIsClosed = True
                                                                End If

                                                                Dim outerFraction As Double = 1.0 - fraction

                                                                pTAll(cnt2)(shiftedPathPts.Count - CInt(Math.Floor(dFracSum))) = 0

                                                                'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
                                                                'for keeping the amount of figures of the innerPath
                                                                For i As Integer = 0 To newInnerPathPoints.Count - 1
                                                                    Dim minDist As Double = Double.MaxValue
                                                                    Dim minIndex As Integer = -1

                                                                    For j As Integer = 0 To outerPts.Count - 1
                                                                        Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
                                                                        Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
                                                                        Dim dist As Double = Math.Sqrt(dx * dx + dy * dy)

                                                                        If dist < minDist Then
                                                                            minDist = dist
                                                                            minIndex = j
                                                                        End If
                                                                    Next

                                                                    If minIndex > -1 Then 'always, todo: comment out test for -1
                                                                        Dim outerX As Single = outerPts(minIndex).X
                                                                        Dim outerY As Single = outerPts(minIndex).Y
                                                                        Dim innerX As Single = newInnerPathPoints(i).X
                                                                        Dim innerY As Single = newInnerPathPoints(i).Y

                                                                        Dim newX As Double = innerX * fraction + outerX * outerFraction
                                                                        Dim newY As Double = innerY * fraction + outerY * outerFraction

                                                                        shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
                                                                    End If
                                                                Next

                                                                If bIsClosed Then
                                                                    pTAll(cnt2)(shiftedPathPts.Count - 1) = 129
                                                                End If
                                                            Next

                                                            If stretch AndAlso shiftedPathPts.Count <> outerPath.PointCount Then
                                                                shiftedPathPts = RestrictOuterPath(shiftedPathPts, outerPath.PointCount)
                                                            End If
                                                        Else

                                                        End If
                                                    End If
                                                End Using
                                            Catch
                                            Finally
                                                If Not bmpTmp Is Nothing Then
                                                    bmpTmp.Dispose()
                                                    bmpTmp = Nothing
                                                End If
                                                If Not bmpInner Is Nothing Then
                                                    bmpInner.Dispose()
                                                    bmpInner = Nothing
                                                End If
                                            End Try
                                        End If

                                        If shiftedPathPts.Count > 1 Then
                                            If stretch Then
                                                Dim cnt4 As Integer = 0

                                                While cnt4 < pTAll(cnt2).Count
                                                    Dim pT As New List(Of Byte)
                                                    Dim pP As New List(Of PointF)

                                                    pP.Add(shiftedPathPts(cnt4))
                                                    pT.Add(0) 'pT.Add(t(cnt))
                                                    cnt4 += 1

                                                    While cnt4 < pTAll(cnt2).Count AndAlso Not pTAll(cnt2)(cnt4) = 0
                                                        pP.Add(shiftedPathPts(cnt4))
                                                        pT.Add(pTAll(cnt2)(cnt4))
                                                        cnt4 += 1
                                                    End While

                                                    pT(pT.Count - 1) = 129
                                                    shiftedPath.Add(pP, pT)
                                                End While
                                            Else
                                                Dim cnt4 As Integer = 0
                                                Dim pT As New List(Of Byte)
                                                Dim pP As New List(Of PointF)
                                                While cnt4 < shiftedPathPts.Count
                                                    pP.Add(shiftedPathPts(cnt4))
                                                    pT.Add(0)
                                                    cnt4 += 1
                                                End While

                                                pT(pT.Count - 1) = 129
                                                shiftedPath.Add(pP, pT)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End Using
                    End If
                Else
                    Exit For
                End If
            Next

            Return shiftedPath
        Else
            Return New GraphicsPathData()
        End If
    End Function

    'test... in progress
    Public Function ShiftCoords2_old(gP As GraphicsPath, shiftInwards As Single, mapLargestOnly As Boolean) As GraphicsPath
        If gP.PointCount > 1 Then
            Dim shiftedPath As New GraphicsPath()

            Using outerPath As GraphicsPath = CType(gP.Clone(), GraphicsPath)
                If outerPath.PointCount > 1 Then
                    Dim outerPts(outerPath.PathPoints.Length - 1) As PointF
                    outerPath.PathPoints.CopyTo(outerPts, 0)
                    Dim rc As RectangleF = outerPath.GetBounds()
                    Dim w As Integer = CInt(Math.Ceiling(rc.Width))
                    Dim h As Integer = CInt(Math.Ceiling(rc.Height))

                    If w > 0 AndAlso h > 0 Then
                        Dim bmpTmp As Bitmap = Nothing

                        Dim breite As Integer = CInt(Math.Ceiling(shiftInwards))
                        Dim fraction As Double = shiftInwards / If(breite <> 0, breite, shiftInwards - (CInt(shiftInwards)))

                        If shiftInwards < 0 Then
                            Dim bmpTmp2 As Bitmap = Nothing
                            Dim bmpOuter As Bitmap = Nothing

                            Try
                                Dim br As Integer = CInt(Math.Floor(shiftInwards))
                                If AvailMem.AvailMem.checkAvailRam((w - br * 2) * (h - br * 2) * 4L) Then
                                    bmpTmp2 = New Bitmap(w - br * 2, h - br * 2)
                                    Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
                                        Using p As New Pen(Color.Black, -br)
                                            Using mx As New Matrix(1, 0, 0, 1, -rc.X - br, -rc.Y - br)
                                                outerPath2.Transform(mx)
                                            End Using

                                            Using g As Graphics = Graphics.FromImage(bmpTmp2)
                                                g.FillPath(Brushes.Black, outerPath2)
                                            End Using

                                            bmpOuter = ExtOutline(bmpTmp2, -breite, Nothing)

                                            Dim lOuter As List(Of ChainCodeF) = GetBoundaryF(bmpOuter)

                                            outerPath.Reset()
                                            For i As Integer = 0 To lOuter.Count - 1
                                                'translate
                                                For j As Integer = 0 To lOuter(i).Coord.Count - 1
                                                    lOuter(i).Coord(j) = New PointF(lOuter(i).Coord(j).X + rc.X + br, lOuter(i).Coord(j).Y + rc.Y + br)
                                                Next
                                                outerPath.AddLines(lOuter(i).Coord.ToArray())
                                                outerPath.CloseFigure()
                                            Next

                                            Dim outerPts2(outerPath.PathPoints.Length - 1) As PointF
                                            outerPath.PathPoints.CopyTo(outerPts2, 0)
                                            outerPts = outerPts2
                                            rc = outerPath.GetBounds()
                                            w = CInt(Math.Ceiling(rc.Width))
                                            h = CInt(Math.Ceiling(rc.Height))
                                            shiftInwards *= -1
                                            fraction = 1.0 - fraction
                                        End Using
                                    End Using
                                End If

                            Catch

                            Finally
                                If Not bmpTmp2 Is Nothing Then
                                    bmpTmp2.Dispose()
                                    bmpTmp2 = Nothing
                                End If
                                If Not bmpOuter Is Nothing Then
                                    bmpOuter.Dispose()
                                    bmpOuter = Nothing
                                End If
                            End Try
                        End If

                        If outerPath.PointCount > 4 Then '4 and less will result in 0 after removing the outline
                            Dim shiftedPathPts As New List(Of PointF)
                            Dim shiftedPathTypes As New List(Of Byte)

                            If AvailMem.AvailMem.checkAvailRam(w * h * 8L) Then
                                Try
                                    bmpTmp = New Bitmap(w + 2, h + 2)
                                    Dim lInner As List(Of ChainCode) = Nothing
                                    Using outerPath2 As GraphicsPath = CType(outerPath.Clone(), GraphicsPath)
                                        For cntW As Integer = 0 To breite - 1
                                            'Dim fff As New Form()
                                            'fff.BackgroundImage = bmpTmp
                                            'fff.ShowDialog()

                                            Using g As Graphics = Graphics.FromImage(bmpTmp)
                                                g.Clear(Color.Transparent)
                                                g.InterpolationMode = InterpolationMode.HighQualityBicubic 'InterpolationMode.NearestNeighbor
                                                'g.PixelOffsetMode = PixelOffsetMode.Half '(if you use this, you probably need to translate back with only the rc-values (not rc-values -1 in line 4191f))
                                                g.SmoothingMode = SmoothingMode.AntiAlias 'needed for setting nullcells = false
                                                If cntW = 0 Then
                                                    Using mx As New Matrix(1, 0, 0, 1, -rc.X + 1, -rc.Y + 1)
                                                        outerPath2.Transform(mx)
                                                    End Using
                                                End If

                                                outerPath2.CloseAllFigures()
                                                Using pen As New Pen(Color.Black, 1)
                                                    pen.LineJoin = LineJoin.Round
                                                    g.DrawPath(pen, outerPath2)
                                                End Using

                                                'Dim pts(outerPath2.PointCount - 1) As PointF
                                                'outerPath2.PathPoints.CopyTo(pts, 0)
                                                'For ii As Integer = 0 To outerPath2.PointCount - 1
                                                '    g.FillRectangle(Brushes.Black, New RectangleF(pts(ii).X, pts(ii).Y, 2, 1))
                                                'Next

                                                'lInner = GetBoundary(bmpTmp)
                                                lInner = GetBoundaryShifted(bmpTmp)

                                                If lInner.Count > 0 Then
                                                    lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()
                                                    'removeOuterOutlines from linner
                                                    For cnt As Integer = lInner.Count - 1 To 0 Step -1
                                                        If lInner(cnt).Coord.Count <= 4 Then
                                                            lInner.RemoveAt(cnt)
                                                            Continue For
                                                        End If
                                                        If lInner(cnt).Chain(lInner(cnt).Chain.Count - 1) = 2 Then 'not an inner outline
                                                            lInner.RemoveAt(cnt)
                                                            Continue For
                                                        Else
                                                            'we dont need the chain here, only the coords, and we always close the figure, so we can do:
                                                            lInner(cnt).Coord = lInner(cnt).Coord.Distinct().ToList()
                                                        End If
                                                    Next
                                                End If

                                                outerPath2.Reset()
                                                For cnt As Integer = 0 To lInner.Count - 1
                                                    outerPath2.StartFigure()
                                                    outerPath2.AddLines(lInner(cnt).Coord.ToArray())
                                                    outerPath2.CloseFigure()
                                                Next
                                                outerPath2.FillMode = FillMode.Winding
                                            End Using
                                        Next
                                    End Using

                                    If mapLargestOnly Then
                                        If lInner.Count > 1 Then
                                            lInner = lInner.OrderByDescending(Function(a) a.Coord.Count).ToList()
                                            lInner.RemoveRange(1, lInner.Count - 1)
                                        End If
                                    End If

                                    If lInner.Count > 0 Then
                                        Dim outerFraction As Double = 1.0 - fraction

                                        For cnt As Integer = 0 To lInner.Count - 1
                                            'since the shift back is less or equal 1, loop over the innerPoints and set FillMode to Winding
                                            'for keeping the amount of figures of the innerPath

                                            Dim newInnerPathPoints As New List(Of PointF)

                                            For cnt2 As Integer = 0 To lInner(cnt).Coord.Count - 1
                                                newInnerPathPoints.Add(New PointF(lInner(cnt).Coord(cnt2).X + rc.X - 1,
                                                                                  lInner(cnt).Coord(cnt2).Y + rc.Y - 1))
                                            Next

                                            For i As Integer = 0 To newInnerPathPoints.Count - 1
                                                Dim minDist As Double = Double.MaxValue
                                                Dim minIndex As Integer = -1

                                                For j As Integer = 0 To outerPts.Count - 1
                                                    Dim dx As Double = outerPts(j).X - newInnerPathPoints(i).X
                                                    Dim dy As Double = outerPts(j).Y - newInnerPathPoints(i).Y
                                                    Dim dist As Double = Math.Sqrt(dx * dx + dy * dy)

                                                    If dist < minDist Then
                                                        minDist = dist
                                                        minIndex = j
                                                    End If
                                                Next

                                                If minIndex > -1 Then 'always, todo: comment out test for -1
                                                    Dim outerX As Single = outerPts(minIndex).X
                                                    Dim outerY As Single = outerPts(minIndex).Y
                                                    Dim innerX As Single = newInnerPathPoints(i).X
                                                    Dim innerY As Single = newInnerPathPoints(i).Y

                                                    Dim newX As Double = innerX * fraction + outerX * outerFraction
                                                    Dim newY As Double = innerY * fraction + outerY * outerFraction

                                                    shiftedPathPts.Add(New PointF(CSng(newX), CSng(newY)))
                                                    shiftedPathTypes.Add(If(i = 0, CByte(0), CByte(1)))
                                                End If
                                            Next

                                            shiftedPathTypes(shiftedPathTypes.Count - 1) = 129
                                        Next
                                    End If

                                Catch
                                Finally
                                    If Not bmpTmp Is Nothing Then
                                        bmpTmp.Dispose()
                                        bmpTmp = Nothing
                                    End If
                                End Try
                            End If

                            If shiftedPathPts.Count > 1 Then
                                Dim gP4 As New GraphicsPath(shiftedPathPts.ToArray(), shiftedPathTypes.ToArray())
                                gP4.FillMode = FillMode.Winding
                                shiftedPath.AddPath(gP4, False)
                            End If
                        End If
                    End If
                End If
            End Using

            Return shiftedPath
        Else
            Return New GraphicsPath
        End If
    End Function

    Private Function TranslateNewInnerPath(newInnerPath As List(Of PointF), rc As RectangleF) As List(Of PointF)
        Dim lOut As New List(Of PointF)

        For i As Integer = 0 To newInnerPath.Count - 1
            lOut.Add(New PointF(newInnerPath(i).X + rc.X, newInnerPath(i).Y + rc.Y))
        Next

        Return lOut
    End Function

    Private Function ProlongateInnerPath(lInner As List(Of PointF), pointCount As Integer, factor As Double) As List(Of PointF)
        Dim pts(pointCount - 1) As PointF

        For i As Integer = 0 To pts.Length - 1
            Dim d As Double = i / factor

            Dim l As Integer = CInt(Math.Floor(d))
            Dim r As Integer = CInt(Math.Ceiling(d))
            Dim f As Double = d - l
            Dim t As Double = 1 - f

            If l > lInner.Count - 1 Then
                l -= 1
            End If
            If r > lInner.Count - 1 Then
                r -= 1
            End If

            Dim x As Double = lInner(l).X * f + lInner(r).X * t
            Dim y As Double = lInner(l).Y * f + lInner(r).Y * t

            pts(i) = New PointF(CSng(x), CSng(y))
        Next

        Return pts.ToList()
    End Function

    Private Function RestrictOuterPath(lInner As List(Of PointF), pointCount As Integer) As List(Of PointF)
        Dim factor As Double = pointCount / lInner.Count
        Dim pts(pointCount - 1) As PointF

        For i As Integer = 0 To pts.Length - 1
            Dim d As Double = i / factor

            Dim l As Integer = CInt(Math.Floor(d))
            Dim r As Integer = CInt(Math.Ceiling(d))
            Dim f As Double = d - l
            Dim t As Double = 1 - f

            If l > lInner.Count - 1 Then
                l -= 1
            End If
            If r > lInner.Count - 1 Then
                r -= 1
            End If

            Dim x As Double = lInner(l).X * f + lInner(r).X * t
            Dim y As Double = lInner(l).Y * f + lInner(r).Y * t

            pts(i) = New PointF(CSng(x), CSng(y))
        Next

        Return pts.ToList()
    End Function

    Private Function GetBoundary(upperImg As Bitmap) As List(Of ChainCode)
        Dim anc As Boolean = Me.AllowNullCells
        Me.AllowNullCells = True
        Dim l As List(Of ChainCode) = Me.GetOutline(upperImg, 0, False, 0, False, 0, False)
        Me.AllowNullCells = anc
        Return l
    End Function

    Private Function GetBoundaryShifted(upperImg As Bitmap) As List(Of ChainCode)
        Dim anc As Boolean = Me.AllowNullCells
        Me.AllowNullCells = False 'needed at current stage
        Dim l As List(Of ChainCode) = Me.GetOutlineShifted(upperImg, 0, False, 0, 0, False, False)
        Me.AllowNullCells = anc
        Return l
    End Function

    Private Function GetBoundaryF(upperImg As Bitmap) As List(Of ChainCodeF)
        Dim anc As Boolean = Me.AllowNullCells
        Me.AllowNullCells = True
        Dim l As List(Of ChainCodeF) = Me.GetOutline(0, upperImg, 0, False, False)
        Me.AllowNullCells = anc
        Return l
    End Function

    Public Function RemOutline(bmp As Bitmap, breite As Integer, bgw As System.ComponentModel.BackgroundWorker) As Bitmap
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L) Then
            Dim b As Bitmap = Nothing
            Dim fbits As BitArray = Nothing

            breite = Math.Abs(breite)

            Try
                b = DirectCast(bmp.Clone(), Bitmap)

                For i As Integer = 0 To breite - 1
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                    Dim cf As New ChainFinder()
                    cf.AllowNullCells = True

                    Dim fList As List(Of ChainCode) = cf.GetOutline(b, 0, False, 0, False)

                    RemoveOutline(b, fList)

                    fbits = Nothing
                Next

                Return b
            Catch
                If fbits IsNot Nothing Then
                    fbits = Nothing
                End If
                If b IsNot Nothing Then
                    b.Dispose()
                End If

                b = Nothing
            End Try
        End If

        Return Nothing
    End Function

    Public Function ExtOutline(bmp As Bitmap, breite As Integer, bgw As System.ComponentModel.BackgroundWorker) As Bitmap
        If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 8L) Then
            Dim b As Bitmap = Nothing
            Dim fbits As BitArray = Nothing

            Try
                b = DirectCast(bmp.Clone(), Bitmap)

                For i As Integer = 0 To breite - 1
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                    Dim cf As New ChainFinder()
                    cf.AllowNullCells = True

                    Dim fList As List(Of ChainCode) = cf.GetOutline(b, 0, False, 0, False)

                    cf.ExtendOutline(b, fList)

                    fbits = Nothing
                Next

                Return b
            Catch
                If fbits IsNot Nothing Then
                    fbits = Nothing
                End If
                If b IsNot Nothing Then
                    b.Dispose()
                End If

                b = Nothing
            End Try
        End If

        Return Nothing
    End Function

    Public Sub HighLightOutlines(bmp As Bitmap, fList As List(Of ChainCode), dark As Boolean)
        If fList IsNot Nothing AndAlso fList.Count > 0 Then
            For Each c As ChainCode In fList
                Using gP As New Drawing2D.GraphicsPath
                    gP.AddLines(c.Coord.ToArray)

                    Using p As New Pen(Color.Black, 2)
                        If dark Then
                            p.Color = Color.White
                        End If
                        Using g As Graphics = Graphics.FromImage(bmp)
                            g.DrawPath(p, gP)
                        End Using
                    End Using
                End Using
            Next
        End If
    End Sub

    Public Function GetOutlineColDist(bmp As Bitmap, thresholdR As Integer, thresholdG As Integer, thresholdB As Integer,
                           doR As Boolean, doG As Boolean, doB As Boolean, rangeR As Integer, rangeG As Integer, rangeB As Integer,
                           excludeInnerOutlines As Boolean, initialValueToCheck As Integer, doReverse As Boolean) As List(Of ChainCode)

        Dim fbits As BitArray = Nothing
        _thresholdR = thresholdR
        _thresholdG = thresholdG
        _thresholdB = thresholdB
        _height = bmp.Height

        Try
            Dim fList As New List(Of ChainCode)()

            fbits = New BitArray((bmp.Width + 1) * bmp.Height, False)

            If doReverse Then
                FindChainCodeColDistRev(bmp, fList, fbits, doR, doG, doB, rangeR, rangeG, rangeB, excludeInnerOutlines,
                 initialValueToCheck, Int32.MaxValue)
            Else
                FindChainCodeColDist(bmp, fList, fbits, doR, doG, doB, rangeR, rangeG, rangeB, excludeInnerOutlines,
                 initialValueToCheck, Int32.MaxValue)
            End If

            Return fList
        Catch
            If fbits IsNot Nothing Then
                fbits = Nothing
            End If
        End Try

        Return Nothing
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeColDist(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray,
                              doR As Boolean, doG As Boolean, doB As Boolean, rangeR As Integer, rangeG As Integer, rangeB As Integer,
                              excludeInnerOutlines As Boolean, initialValueToCheck As Integer, maxIterations As Integer)

        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeGreaterTh As Boolean, RightBeforeGreaterTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_searchColDist(bmData, p, fbits, doR, doG, doB, rangeR, rangeG, rangeB, initialValueToCheck) AndAlso fList.Count <= maxIterations
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeGreaterTh = False
                    RightBeforeGreaterTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) <= _thresholdR + rangeR))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) > _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) <= _thresholdG + rangeG))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) > _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                LeftBeforeGreaterTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) <= _thresholdB + rangeB))
                            Else
                                LeftBeforeGreaterTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) > _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                LeftBeforeGreaterTh =
                                  ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG) AndAlso
                                  Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG))))
                            Else
                                LeftBeforeGreaterTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG))
                            End If
                        End If
                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                LeftBeforeGreaterTh =
                                  ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB) AndAlso
                                  Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdB + rangeB) * (_thresholdB + rangeB))))
                            Else
                                LeftBeforeGreaterTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB))
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                LeftBeforeGreaterTh =
                                  ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) > Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG) AndAlso
                                  Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) <= Math.Sqrt((_thresholdB + rangeB) * (_thresholdB + rangeB) + (_thresholdG + rangeG) * (_thresholdG + rangeG))))
                            Else
                                LeftBeforeGreaterTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) > Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG))
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                LeftBeforeGreaterTh =
                                  ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB) AndAlso
                                  Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG) + (_thresholdB + rangeB) * (_thresholdB + rangeB))))
                            Else
                                LeftBeforeGreaterTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB))
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) <= _thresholdR + rangeR))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 2) > _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) <= _thresholdG + rangeG))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 1) > _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                RightBeforeGreaterTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) <= _thresholdB + rangeB))
                            Else
                                RightBeforeGreaterTh = p(RightBefore.Y * stride + RightBefore.X * 4) > _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                RightBeforeGreaterTh = ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG) AndAlso
                                Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG))))
                            Else
                                RightBeforeGreaterTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG))
                            End If
                        End If

                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                RightBeforeGreaterTh = ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB) AndAlso
                                Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdB + rangeB) * (_thresholdB + rangeB))))
                            Else
                                RightBeforeGreaterTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB))
                            End If
                        End If

                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                RightBeforeGreaterTh = ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) > Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG) AndAlso
                                Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) <= Math.Sqrt((_thresholdB + rangeB) * (_thresholdB + rangeB) + (_thresholdG + rangeG) * (_thresholdG + rangeG))))
                            Else
                                RightBeforeGreaterTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) > Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG))
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                RightBeforeGreaterTh = ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB) AndAlso
                                Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG) + (_thresholdB + rangeB) * (_thresholdB + rangeB))))
                            Else
                                RightBeforeGreaterTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB))
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBeforeGreaterTh AndAlso (LeftBeforeGreaterTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeGreaterTh AndAlso (Not RightBeforeGreaterTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            b.UnlockBits(bmData)
            p = Nothing
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    Private Function start_crack_searchColDist(bmData As BitmapData, p As Byte(), fbits As BitArray, doRed As Boolean, doGreen As Boolean, doBlue As Boolean, rangeR As Integer,
        rangeG As Integer, rangeB As Integer, initialValue As Integer) As Boolean
        Dim leftR As Integer = 0
        Dim leftG As Integer = 0
        Dim leftB As Integer = 0

        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    leftR = p(y * stride + (x - 1) * 4 + 2)
                    leftG = p(y * stride + (x - 1) * 4 + 1)
                    leftB = p(y * stride + (x - 1) * 4)
                Else
                    leftR = initialValue
                    leftG = initialValue
                    leftB = initialValue
                End If

                '#Region "singleVal"
                If doRed AndAlso Not doGreen AndAlso Not doBlue Then
                    If rangeR > 0 Then
                        If (leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (p(y * stride + x * 4 + 2) <= _thresholdR + rangeR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftR <= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) > _thresholdR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeG > 0 Then
                        If (leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (p(y * stride + x * 4 + 1) <= _thresholdG + rangeG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftG <= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) > _thresholdG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso Not doGreen AndAlso doBlue Then
                    If rangeB > 0 Then
                        If (leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (p(y * stride + x * 4) <= _thresholdB + rangeB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftB <= _thresholdB) AndAlso (p(y * stride + x * 4) > _thresholdB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "doubleVal"
                If doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeR > 0 OrElse rangeG > 0 Then
                        If ((leftR <= _thresholdR) AndAlso (leftG <= _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR <= _thresholdR) AndAlso (leftG <= _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If doRed AndAlso Not doGreen AndAlso doBlue Then
                    If rangeR > 0 OrElse rangeB > 0 Then
                        If ((leftR <= _thresholdR) AndAlso (leftB <= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdB + rangeB) * (_thresholdB + rangeB))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR <= _thresholdR) AndAlso (leftB <= _thresholdB) AndAlso
                                               Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB)) AndAlso
                                               (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso doBlue Then
                    If rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftB <= _thresholdB) AndAlso (leftG <= _thresholdG) AndAlso
                                             Math.Sqrt(CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) > Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG) AndAlso
                                             Math.Sqrt(CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) <= Math.Sqrt((_thresholdB + rangeB) * (_thresholdB + rangeB) + (_thresholdG + rangeG) * (_thresholdG + rangeG))) AndAlso
                                             (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftB <= _thresholdB) AndAlso (leftG <= _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) > Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "allVals"
                If doRed AndAlso doGreen AndAlso doBlue Then
                    If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftR <= _thresholdR) AndAlso (leftG <= _thresholdG) AndAlso (leftB <= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) <= Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG) + (_thresholdB + rangeB) * (_thresholdB + rangeB))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR <= _thresholdR) AndAlso (leftG <= _thresholdG) AndAlso (leftB <= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) > Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                    '#End Region
                End If
            Next
        Next
        Return False
    End Function

    'Adaption von Herrn Prof. Dr.Ing. Dr.med. Volkmar Miszalok, siehe: http://www.miszalok.de/Samples/CV/ChainCode/chain_code.htm
    Private Sub FindChainCodeColDistRev(b As Bitmap, fList As List(Of ChainCode), fbits As BitArray,
                                 doR As Boolean, doG As Boolean, doB As Boolean, rangeR As Integer, rangeG As Integer, rangeB As Integer,
                                 excludeInnerOutlines As Boolean, initialValueToCheck As Integer, maxIterations As Integer)

        Dim Negative As [SByte](,) = New [SByte](,) {{0, -1}, {0, 0}, {-1, 0}, {-1, -1}}
        Dim Positive As [SByte](,) = New [SByte](,) {{0, 0}, {-1, 0}, {-1, -1}, {0, -1}}

        Dim LeftBefore As New Point()
        Dim RightBefore As New Point()

        Dim LeftBeforeSmallerTh As Boolean, RightBeforeSmallerTh As Boolean
        Dim direction As Integer = 1

        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            While start_crack_searchColDistRev(bmData, p, fbits, doR, doG, doB, rangeR, rangeG, rangeB, initialValueToCheck) AndAlso fList.Count <= maxIterations
                Dim cc As New ChainCode()

                cc.start = _start

                Dim x As Integer = _start.X
                Dim y As Integer = _start.Y + 1
                direction = 1

                cc.Chain.Add(direction)

                While x <> _start.X OrElse y <> _start.Y
                    LeftBefore.X = x + Negative(direction, 0)
                    LeftBefore.Y = y + Negative(direction, 1)
                    RightBefore.X = x + Positive(direction, 0)
                    RightBefore.Y = y + Positive(direction, 1)

                    Select Case direction
                        Case 0
                            cc.Coord.Add(New Point(LeftBefore.X - 1, LeftBefore.Y))
                            Exit Select
                        Case 1
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y - 1))
                            Exit Select
                        Case 2
                            cc.Coord.Add(New Point(LeftBefore.X + 1, LeftBefore.Y))
                            Exit Select
                        Case 3
                            cc.Coord.Add(New Point(LeftBefore.X, LeftBefore.Y + 1))
                            Exit Select
                    End Select

                    LeftBeforeSmallerTh = False
                    RightBeforeSmallerTh = False

                    If LeftBefore.X >= 0 AndAlso LeftBefore.X < b.Width AndAlso LeftBefore.Y >= 0 AndAlso LeftBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                LeftBeforeSmallerTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) >= _thresholdR - rangeR))
                            Else
                                LeftBeforeSmallerTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2) < _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                LeftBeforeSmallerTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) >= _thresholdG - rangeG))
                            Else
                                LeftBeforeSmallerTh = p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1) < _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                LeftBeforeSmallerTh = ((p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB) AndAlso (p(LeftBefore.Y * stride + LeftBefore.X * 4) >= _thresholdB - rangeB))
                            Else
                                LeftBeforeSmallerTh = p(LeftBefore.Y * stride + LeftBefore.X * 4) < _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                LeftBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG) AndAlso
                                    Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) >= Math.Sqrt((_thresholdR - rangeR) * (_thresholdR - rangeR) + (_thresholdG - rangeG) * (_thresholdG - rangeG))))
                            Else
                                LeftBeforeSmallerTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG))
                            End If
                        End If
                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                LeftBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB) AndAlso
                                    Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * (p(LeftBefore.Y * stride + LeftBefore.X * 4))) >= Math.Sqrt((_thresholdR - rangeR) * (_thresholdR - rangeR) + (_thresholdB - rangeB) * (_thresholdB - rangeB))))
                            Else
                                LeftBeforeSmallerTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB))
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                LeftBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) < Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG) AndAlso
                                    Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) >= Math.Sqrt((_thresholdB - rangeB) * (_thresholdB - rangeB) + (_thresholdG - rangeG) * (_thresholdG - rangeG))))
                            Else
                                LeftBeforeSmallerTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1))) < Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG))
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                LeftBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB) AndAlso
                                    Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * (p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) >= Math.Sqrt((_thresholdR - rangeR) * (_thresholdR - rangeR) + (_thresholdG - rangeG) * (_thresholdG - rangeG) + (_thresholdB - rangeB) * (_thresholdB - rangeB))))
                            Else
                                LeftBeforeSmallerTh = (Math.Sqrt(CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 2)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4 + 1)) + CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4)) * CDbl(p(LeftBefore.Y * stride + LeftBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB))
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBefore.X >= 0 AndAlso RightBefore.X < b.Width AndAlso RightBefore.Y >= 0 AndAlso RightBefore.Y < b.Height Then
                        '#Region "singleVal"
                        If doR AndAlso Not doG AndAlso Not doB Then
                            If rangeR > 0 Then
                                RightBeforeSmallerTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 2) >= _thresholdR - rangeR))
                            Else
                                RightBeforeSmallerTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 2) < _thresholdR
                            End If
                        End If
                        If Not doR AndAlso doG AndAlso Not doB Then
                            If rangeG > 0 Then
                                RightBeforeSmallerTh = ((p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4 + 1) >= _thresholdG - rangeG))
                            Else
                                RightBeforeSmallerTh = p(RightBefore.Y * stride + RightBefore.X * 4 + 1) < _thresholdG
                            End If
                        End If
                        If Not doR AndAlso Not doG AndAlso doB Then
                            If rangeB > 0 Then
                                RightBeforeSmallerTh = ((p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB) AndAlso (p(RightBefore.Y * stride + RightBefore.X * 4) >= _thresholdB - rangeB))
                            Else
                                RightBeforeSmallerTh = p(RightBefore.Y * stride + RightBefore.X * 4) < _thresholdB
                            End If
                        End If
                        '#End Region

                        '#Region "doubleVal"
                        If doR AndAlso doG AndAlso Not doB Then
                            If rangeR > 0 OrElse rangeG > 0 Then
                                RightBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG) AndAlso
                                    Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) >= Math.Sqrt((_thresholdR - rangeR) * (_thresholdR - rangeR) + (_thresholdG - rangeG) * (_thresholdG - rangeG))))
                            Else
                                RightBeforeSmallerTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG))
                            End If
                        End If

                        If doR AndAlso Not doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeB > 0 Then
                                RightBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB) AndAlso
                                    Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) >= Math.Sqrt((_thresholdR - rangeR) * (_thresholdR - rangeR) + (_thresholdB - rangeB) * (_thresholdB - rangeB))))
                            Else
                                RightBeforeSmallerTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB))
                            End If
                        End If

                        If Not doR AndAlso doG AndAlso doB Then
                            If rangeB > 0 OrElse rangeG > 0 Then
                                RightBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) < Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG) AndAlso
                                    Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) >= Math.Sqrt((_thresholdB - rangeB) * (_thresholdB - rangeB) + (_thresholdG - rangeG) * (_thresholdG - rangeG))))
                            Else
                                RightBeforeSmallerTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1))) < Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG))
                            End If
                        End If
                        '#End Region

                        '#Region "allVals"
                        If doR AndAlso doG AndAlso doB Then
                            If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                                RightBeforeSmallerTh =
                                    ((Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB) AndAlso
                                    Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) + +CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) >= Math.Sqrt((_thresholdR - rangeR) * (_thresholdR - rangeR) + (_thresholdG - rangeG) * (_thresholdG - rangeG) + (_thresholdB - rangeB) * (_thresholdB - rangeB))))
                            Else
                                RightBeforeSmallerTh = (Math.Sqrt(CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 2)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4 + 1)) + CDbl(p(RightBefore.Y * stride + RightBefore.X * 4)) * CDbl(p(RightBefore.Y * stride + RightBefore.X * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB))
                            End If
                            '#End Region
                        End If
                    End If

                    If RightBeforeSmallerTh AndAlso (LeftBeforeSmallerTh OrElse _nullCells) Then
                        direction = (direction + 1) Mod 4
                    ElseIf Not LeftBeforeSmallerTh AndAlso (Not RightBeforeSmallerTh OrElse Not _nullCells) Then
                        direction = (direction + 3) Mod 4
                    End If

                    cc.Chain.Add(direction)

                    'fbits (immer oberen punkt aufzeichnen)
                    Select Case direction
                        Case 0
                            x += 1
                            cc.Area += y
                            Exit Select
                        Case 1
                            y += 1
                            fbits.[Set]((y - 1) * (b.Width + 1) + x, True)
                            Exit Select
                        Case 2
                            x -= 1
                            cc.Area -= y
                            Exit Select
                        Case 3
                            y -= 1
                            fbits.[Set](y * (b.Width + 1) + x, True)
                            Exit Select
                    End Select

                    If x = _start.X AndAlso y = _start.Y Then
                        If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 OrElse Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).X - x) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X + 1, cc.Coord(cc.Coord.Count - 1).Y))
                                cc.Chain.Add(0)
                            End If
                            If Math.Abs(cc.Coord(cc.Coord.Count - 1).Y - y) > 1 Then
                                cc.Coord.Add(New Point(cc.Coord(cc.Coord.Count - 1).X, cc.Coord(cc.Coord.Count - 1).Y + 1))
                                cc.Chain.Add(1)
                            End If
                            Exit While
                        End If
                    End If
                End While

                Dim isInnerOutline As Boolean = False

                If excludeInnerOutlines Then
                    If cc.Chain(cc.Chain.Count - 1) = 0 Then
                        isInnerOutline = True
                        Exit While
                    End If
                End If

                If Not isInnerOutline Then
                    cc.Coord.Add(_start)
                    fList.Add(cc)
                End If
            End While

            b.UnlockBits(bmData)
            p = Nothing
        Catch ex As Exception
            MessageBox.Show(ex.Message)

            Try
                b.UnlockBits(bmData)

            Catch
            End Try
        End Try
    End Sub

    Private Function start_crack_searchColDistRev(bmData As BitmapData, p As Byte(), fbits As BitArray, doRed As Boolean, doGreen As Boolean, doBlue As Boolean, rangeR As Integer,
        rangeG As Integer, rangeB As Integer, initialValue As Integer) As Boolean
        Dim leftR As Integer = 0
        Dim leftG As Integer = 0
        Dim leftB As Integer = 0

        Dim stride As Integer = bmData.Stride

        For y As Integer = _start.Y To bmData.Height - 1
            For x As Integer = 0 To bmData.Width - 1
                If x > 0 Then
                    leftR = p(y * stride + (x - 1) * 4 + 2)
                    leftG = p(y * stride + (x - 1) * 4 + 1)
                    leftB = p(y * stride + (x - 1) * 4)
                Else
                    leftR = initialValue
                    leftG = initialValue
                    leftB = initialValue
                End If

                '#Region "singleVal"
                If doRed AndAlso Not doGreen AndAlso Not doBlue Then
                    If rangeR > 0 Then
                        If (leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (leftR < _thresholdR + rangeR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeG > 0 Then
                        If (leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (leftG < _thresholdG + rangeG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso Not doGreen AndAlso doBlue Then
                    If rangeB > 0 Then
                        If (leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (leftB < _thresholdB + rangeB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If (leftB >= _thresholdB) AndAlso (p(y * stride + x * 4) < _thresholdB) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                ' If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR) AndAlso (leftR < _thresholdR + rangeR))
                'AndAlso ((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG) AndAlso (leftG < _thresholdG + rangeG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then

                ' If ((leftR >= _thresholdR) AndAlso (p(y * stride + x * 4 + 2) < _thresholdR)) AndAlso 
                '((leftG >= _thresholdG) AndAlso (p(y * stride + x * 4 + 1) < _thresholdG)) AndAlso (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then


                '#Region "doubleVal"
                If doRed AndAlso doGreen AndAlso Not doBlue Then
                    If rangeR > 0 OrElse rangeG > 0 Then
                        If ((leftR >= _thresholdR) AndAlso (leftG >= _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG) AndAlso
                            Math.Sqrt(leftR * leftR + leftG * leftG) < Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR >= _thresholdR) AndAlso (leftG >= _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If doRed AndAlso Not doGreen AndAlso doBlue Then
                    If rangeR > 0 OrElse rangeB > 0 Then
                        If ((leftR >= _thresholdR) AndAlso (leftB >= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB) AndAlso
                            Math.Sqrt(leftR * leftR + leftB * leftB) < Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdB + rangeB) * (_thresholdB + rangeB))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR >= _thresholdR) AndAlso (leftB >= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdB * _thresholdB)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If

                If Not doRed AndAlso doGreen AndAlso doBlue Then
                    If rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftB >= _thresholdB) AndAlso (leftB >= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) < Math.Sqrt(_thresholdB * _thresholdB + _thresholdB * _thresholdB) AndAlso
                            Math.Sqrt(leftB * leftB + leftB * leftB) < Math.Sqrt((_thresholdB + rangeB) * (_thresholdB + rangeB) + (_thresholdB + rangeB) * (_thresholdB + rangeB))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftB >= _thresholdB) AndAlso (leftG >= _thresholdG) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1))) < Math.Sqrt(_thresholdB * _thresholdB + _thresholdG * _thresholdG)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                End If
                '#End Region

                '#Region "allVals"
                If doRed AndAlso doGreen AndAlso doBlue Then
                    If rangeR > 0 OrElse rangeG > 0 OrElse rangeB > 0 Then
                        If ((leftR >= _thresholdR) AndAlso (leftG >= _thresholdG) AndAlso (leftB >= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB) AndAlso
                            Math.Sqrt(leftR * leftR + leftG * leftG + leftB * leftB) < Math.Sqrt((_thresholdR + rangeR) * (_thresholdR + rangeR) + (_thresholdG + rangeG) * (_thresholdG + rangeG) + (_thresholdB + rangeB) * (_thresholdB + rangeB))) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    Else
                        If ((leftR >= _thresholdR) AndAlso (leftG >= _thresholdG) AndAlso (leftB >= _thresholdB) AndAlso
                            Math.Sqrt(CDbl(p(y * stride + x * 4 + 2)) * CDbl(p(y * stride + x * 4 + 2)) + CDbl(p(y * stride + x * 4 + 1)) * CDbl(p(y * stride + x * 4 + 1)) + CDbl(p(y * stride + x * 4)) * CDbl(p(y * stride + x * 4))) < Math.Sqrt(_thresholdR * _thresholdR + _thresholdG * _thresholdG + _thresholdB * _thresholdB)) AndAlso
                            (fbits.[Get](y * (bmData.Width + 1) + x) = False) Then
                            _start.X = x
                            _start.Y = y
                            fbits.[Set](y * (bmData.Width + 1) + x, True)
                            OnProgressPlus()
                            Return True
                        End If
                    End If
                    '#End Region
                End If
            Next
        Next
        Return False
    End Function

    Public Sub RemoveOutlines(fList As List(Of ChainCode), maxRemoveDistinctCoords As Integer)
        If Not fList Is Nothing AndAlso fList.Count > 0 Then
            For i As Integer = fList.Count - 1 To 0 Step -1
                Dim lList As List(Of Point) = fList(i).Coord.Distinct().ToList()
                If lList.Count <= maxRemoveDistinctCoords Then
                    fList.RemoveAt(i)
                End If
            Next
        End If
    End Sub

    Public Shared Function GetChain(coord As List(Of Point)) As IEnumerable(Of Integer)
        '1 u, 2 l, 3 o, 4 r

        Dim lOut As New List(Of Integer)

        For i As Integer = 1 To coord.Count - 1
            Dim dx As Integer = coord(i).X - coord(i - 1).X
            Dim dy As Integer = coord(i).Y - coord(i - 1).Y
            If Math.Abs(dx) > Math.Abs(dy) Then
                If dx > 0 Then
                    lOut.Add(4)
                Else
                    lOut.Add(2)
                End If
            Else
                If dy > 0 Then
                    lOut.Add(1)
                Else
                    lOut.Add(3)
                End If
            End If
        Next

        Return lOut
    End Function

    Public Shared Function IsInnerOutline(chainCode As ChainCode) As Boolean
        If chainCode IsNot Nothing AndAlso chainCode.Chain IsNot Nothing AndAlso chainCode.Chain.Count > 0 Then
            Return chainCode.Chain(chainCode.Chain.Count - 1) = 0
        End If
        Return False
    End Function

    Public Shared Function RotateChainCodeCopy(coord As List(Of Point)) As List(Of Point)
        Dim lOut As New List(Of Point)

        For i As Integer = 0 To coord.Count - 1
            lOut.Add(New Point(-coord(i).X, -coord(i).Y))
        Next

        Return lOut
    End Function

    Public Sub Reset()
        Me._start = New Point(0, 0)
    End Sub

    'ab hier neu
    Public Sub ShiftChains(b As Bitmap, fList As List(Of ChainCode), shiftX As Integer, shiftY As Integer)
        Dim bmData As BitmapData = Nothing

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 4L) Then
            Return
        End If

        Try
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            Dim w As Integer = b.Width
            Dim h As Integer = b.Height
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Parallel.For(0, h, Sub(y)
                                   For x As Integer = 0 To w - 1
                                       p(y * stride + x * 4) = 0
                                       p(y * stride + x * 4 + 1) = 0
                                       p(y * stride + x * 4 + 2) = 0
                                       p(y * stride + x * 4 + 3) = 0
                                   Next
                               End Sub)

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            p = Nothing

            If fList IsNot Nothing AndAlso fList.Count > 0 Then
                For Each c As ChainCode In fList
                    For i As Integer = 0 To c.Coord.Count - 1
                        Dim x As Integer = c.Coord(i).X
                        Dim y As Integer = c.Coord(i).Y

                        c.Coord(i) = New Point(x + shiftX, y + shiftY)
                    Next
                Next
            End If

            'CleanChains(fList, w, h)

            Using gx As Graphics = Graphics.FromImage(b)
                gx.SmoothingMode = SmoothingMode.None
                gx.InterpolationMode = InterpolationMode.NearestNeighbor
                For i As Integer = 0 To fList.Count - 1
                    Dim fl As ChainCode = fList(i)
                    gx.CompositingMode = CompositingMode.SourceOver
                    If Not ChainFinder.IsInnerOutline(fl) Then
                        Using gP As New GraphicsPath
                            gP.StartFigure()
                            gP.AddLines(fl.Coord.ToArray())
                            gP.CloseFigure()
                            gx.FillPath(Brushes.Black, gP)
                            gx.DrawPath(Pens.Black, gP)
                        End Using
                    Else
                        gx.CompositingMode = CompositingMode.SourceCopy

                        Using gP As New GraphicsPath
                            gP.StartFigure()
                            gP.AddLines(fl.Coord.ToArray())
                            gP.CloseFigure()
                            gx.FillPath(Brushes.Transparent, gP)
                            gx.DrawPath(Pens.Transparent, gP)
                        End Using
                    End If
                Next
            End Using
        Catch
            Try
                b.UnlockBits(bmData)
            Catch

            End Try
        End Try
    End Sub

    Private Sub CleanChains(fList As List(Of ChainCode), w As Integer, h As Integer)
        If fList IsNot Nothing AndAlso fList.Count > 0 Then
            For Each c As ChainCode In fList
                For i As Integer = c.Coord.Count - 1 To 0 Step -1
                    Dim x As Integer = c.Coord(i).X
                    Dim y As Integer = c.Coord(i).Y

                    If x < 0 OrElse x > w OrElse y < 0 OrElse y > h Then
                        c.Chain.RemoveAt(i)
                        c.Coord.RemoveAt(i)
                    End If
                Next

                RecompArea(c)
            Next
        End If
    End Sub

    Public Shared Sub RecompArea(c As ChainCode)
        c.Area = 0

        For i As Integer = 0 To c.Chain.Count - 1
            Dim direction As Integer = c.Chain(i)
            Select Case direction
                Case 0
                    c.Area += c.Coord(i).Y + 1
                    Exit Select
                Case 1
                    Exit Select
                Case 2
                    c.Area -= c.Coord(i).Y
                    Exit Select
                Case 3
                    Exit Select
            End Select
        Next
    End Sub

    Public Shared Function GetCentroid(c As ChainCode) As PointF
        Dim pt As New PointF(0F, 0F)

        Dim x As Double = c.Coord.Select(Function(a) a.X).Sum()
        Dim y As Double = c.Coord.Select(Function(a) a.Y).Sum()

        If x > 0 AndAlso y > 0 Then
            pt = New PointF(CSng(x / c.Chain.Count), CSng(y / c.Chain.Count))
        End If

        Return pt
    End Function
End Class