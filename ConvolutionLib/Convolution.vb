Option Strict On

Imports System.Drawing.Imaging
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Threading

Partial Public Class Convolution
    Public Delegate Sub ProgressPlusEventHandler(sender As Object, e As ProgressEventArgs)
    Public Event ProgressPlus As ProgressPlusEventHandler

    Private _sum As Long = 0
    Private _pe As ProgressEventArgs = Nothing

    Private _cancelLoops As Boolean = False

    Public Property CancelLoops() As Boolean
        Get
            Return _cancelLoops
        End Get
        Set(value As Boolean)
            _cancelLoops = value
        End Set
    End Property

    Public Property pb() As System.Windows.Forms.ProgressBar
        Get
            Return m_pb
        End Get
        Set(value As System.Windows.Forms.ProgressBar)
            m_pb = value
        End Set
    End Property
    Private m_pb As System.Windows.Forms.ProgressBar

    Public lockObject As New Object()

    Public Property IgnoreEdges() As Boolean
        Get
            Return m_IgnoreEdges
        End Get
        Set(value As Boolean)
            m_IgnoreEdges = value
        End Set
    End Property
    Private m_IgnoreEdges As Boolean
    Public Property LineRanges() As BitArray
        Get
            Return m_LineRanges
        End Get
        Set(value As BitArray)
            m_LineRanges = value
        End Set
    End Property
    Private m_LineRanges As BitArray

    'get the additional values for processing the edges
    Public Function CalculateStandardAddVals(Kernel As Double(), MaxVal As Integer) As Double()
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim AddVals As Double() = New Double(Kernel.Length - 1) {}

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim KSum As Double = 0.0

        For i As Integer = 0 To Kernel.Length - 1
            KSum += Kernel(i)
        Next

        Dim Sum As Double = 0.0

        For c As Integer = h To Kernel.Length - 1
            Sum += Kernel(c)
        Next

        AddVals(0) = KSum - Sum

        For x As Integer = 1 To Kernel.Length - 2
            Sum = 0.0

            For c As Integer = Math.Max(h - x, 0) To Kernel.Length + (Math.Min(h - x, 0) - 1)
                Sum += Kernel(c)
            Next

            AddVals(x) = KSum - Sum
        Next

        Sum = 0.0

        For c As Integer = 0 To Kernel.Length - h - 1
            Sum += Kernel(c)
        Next

        AddVals(Kernel.Length - 1) = KSum - Sum

        Return AddVals
    End Function

    'get the additional values for processing the edges
    Public Function CalculateStandardAddVals(Kernel As Double(,)) As Double(,)
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If

        Dim AddVals As Double(,) = New Double(Kernel.GetLength(0) - 1, Kernel.GetLength(1) - 1) {}

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim KSum As Double = 0.0

        For i As Integer = 0 To Kernel.GetLength(1) - 1
            For j As Integer = 0 To Kernel.GetLength(0) - 1
                KSum += Kernel(j, i)
            Next
        Next

        For l As Integer = 0 To h - 1
            Dim Sum As Double = 0.0

            For r As Integer = h - l To Kernel.GetLength(1) - 1
                For c As Integer = h To Kernel.GetLength(0) - 1
                    Sum += Kernel(c, r)
                Next
            Next
            AddVals(0, l) = KSum - Sum

            For x As Integer = 1 To Kernel.GetLength(0) - 2
                Sum = 0.0

                For r As Integer = h - l To Kernel.GetLength(1) - 1
                    For c As Integer = Math.Max(h - x, 0) To Kernel.GetLength(0) + (Math.Min(h - x, 0) - 1)
                        Sum += Kernel(c, r)
                    Next
                Next

                AddVals(x, l) = KSum - Sum
            Next

            Sum = 0.0

            For r As Integer = h - l To Kernel.GetLength(1) - 1
                For c As Integer = 0 To Kernel.GetLength(0) - h - 1
                    Sum += Kernel(c, r)
                Next
            Next
            AddVals(Kernel.GetLength(0) - 1, l) = KSum - Sum

            '############
            Sum = 0.0

            For r As Integer = 0 To Kernel.GetLength(1) - 1
                For c As Integer = h - l To Kernel.GetLength(0) - 1
                    Sum += Kernel(c, r)
                Next
            Next
            AddVals(l, h) = KSum - Sum

            Sum = 0.0

            For r As Integer = 0 To Kernel.GetLength(1) - 1
                For c As Integer = 0 To Kernel.GetLength(0) - l - 2
                    Sum += Kernel(c, r)
                Next
            Next
            AddVals(h + 1 + l, h) = KSum - Sum
            '############

            Sum = 0.0

            For r As Integer = 0 To Kernel.GetLength(1) - (l + 1) - 1
                For c As Integer = h To Kernel.GetLength(0) - 1
                    Sum += Kernel(c, r)
                Next
            Next
            AddVals(0, h + 1 + l) = KSum - Sum

            For x As Integer = 1 To Kernel.GetLength(0) - 2
                Sum = 0.0

                For r As Integer = 0 To Kernel.GetLength(1) - (l + 1) - 1
                    For c As Integer = Math.Max(h - x, 0) To Kernel.GetLength(0) + (Math.Min(h - x, 0) - 1)
                        Sum += Kernel(c, r)
                    Next
                Next

                AddVals(x, h + 1 + l) = KSum - Sum
            Next

            Sum = 0.0

            For r As Integer = 0 To Kernel.GetLength(1) - (l + 1) - 1
                For c As Integer = 0 To Kernel.GetLength(0) - h - 1
                    Sum += Kernel(c, r)
                Next
            Next
            AddVals(Kernel.GetLength(0) - 1, h + 1 + l) = KSum - Sum
        Next

        Return AddVals
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par(b As Bitmap, Kernel As Double(,), AddVals As Double(,), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        SrcOnSigma As Boolean, bgw As System.ComponentModel.BackgroundWorker) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (h - l)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If

                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0
                                                                     Dim fCount As Double = 0.0, fCount2 As Double = 0.0, fCount3 As Double = 0.0, fCount4 As Double = 0.0
                                                                     Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + ll)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + ll)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + ll)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + ll)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     z = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + llr + lc))) <= Sigma Then
                                                                                     fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + llr + lc))) <= Sigma Then
                                                                                     fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + llr + lc))) <= Sigma Then
                                                                                     fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + llr + lc))) <= Sigma Then
                                                                                         fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(col, row))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                         '#End Region
                                                                     Next

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (l + 1)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par(b As Bitmap, Kernel As Double(,), AddVals As Double(,), divisor As Double, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If
                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                'Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                'Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If
                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 Dim lcc As Integer = cc * 4

                                                                                 fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum2 / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum3 / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                 fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                 fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum2 / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum3 / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 Dim lcc As Integer = cc * 4

                                                                                 fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum2 / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum3 / KfSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         '#End Region
                                                                     Next

                                                                     SyncLock Me.lockObject
                                                                         If Not pe Is Nothing Then
                                                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                                                 pe.CurrentProgress += 1
                                                                             End If
                                                                             Try
                                                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                                                     RaiseEvent ProgressPlus(Me, pe)
                                                                                 End If

                                                                             Catch
                                                                             End Try
                                                                         End If
                                                                     End SyncLock

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If
                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum) / divisor, Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par(b As Bitmap, Kernel As Double(,), AddVals As Double(,), pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If
                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                'Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                'Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If
                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 Dim lcc As Integer = cc * 4

                                                                                 fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum2 / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum3 / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                 fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                 fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum2 / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum3 / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 Dim lcc As Integer = cc * 4

                                                                                 fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                 fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum2 / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(fSum3 / KfSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                                                                         '#End Region
                                                                     Next

                                                                     SyncLock Me.lockObject
                                                                         If Not pe Is Nothing Then
                                                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                                                 pe.CurrentProgress += 1
                                                                             End If
                                                                             Try
                                                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                                                     RaiseEvent ProgressPlus(Me, pe)
                                                                                 End If

                                                                             Catch
                                                                             End Try
                                                                         End If
                                                                     End SyncLock

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If
                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                        Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                        Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum2 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min(Math.Abs(Sum3 / KSum), Int32.MaxValue), Int32.MinValue)), 255), 0), [Byte])
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par(b As Bitmap, Kernel As Double(,), AddVals As Double(,), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (h - l)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If

                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0
                                                                     Dim fCount As Double = 0.0, fCount2 As Double = 0.0, fCount3 As Double = 0.0, fCount4 As Double = 0.0
                                                                     Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + ll)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + ll)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + ll)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + ll)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     z = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + llr + lc))) <= Sigma Then
                                                                                     fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + llr + lc))) <= Sigma Then
                                                                                     fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + llr + lc))) <= Sigma Then
                                                                                     fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + llr + lc))) <= Sigma Then
                                                                                         fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(col, row))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                         '#End Region
                                                                     Next

                                                                     SyncLock Me.lockObject
                                                                         If Not pe Is Nothing Then
                                                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                                                 pe.CurrentProgress += 1
                                                                             End If
                                                                             Try
                                                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                                                     RaiseEvent ProgressPlus(Me, pe)
                                                                                 End If

                                                                             Catch
                                                                             End Try
                                                                         End If
                                                                     End SyncLock

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (l + 1)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par(b As Bitmap, Kernel As Double(,), AddVals As Double(,), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, divisor As Double) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If divisor = 0 Then
            Throw New Exception("divisor must not be 0.")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (h - l)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If

                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0
                                                                     Dim fCount As Double = 0.0, fCount2 As Double = 0.0, fCount3 As Double = 0.0, fCount4 As Double = 0.0
                                                                     Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + ll)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + ll)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + ll)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + ll)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     z = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + llr + lc))) <= Sigma Then
                                                                                     fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + llr + lc))) <= Sigma Then
                                                                                     fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + llr + lc))) <= Sigma Then
                                                                                     fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + llr + lc))) <= Sigma Then
                                                                                         fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(col, row))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                         '#End Region
                                                                     Next

                                                                     SyncLock Me.lockObject
                                                                         If Not pe Is Nothing Then
                                                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                                                 pe.CurrentProgress += 1
                                                                             End If
                                                                             Try
                                                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                                                     RaiseEvent ProgressPlus(Me, pe)
                                                                                 End If

                                                                             Catch
                                                                             End Try
                                                                         End If
                                                                     End SyncLock

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (l + 1)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par_Pos(b As Bitmap, Kernel As Double(,), AddVals As Double(,), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, divisor As Double) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (h - l)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If

                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0
                                                                     Dim fCount As Double = 0.0, fCount2 As Double = 0.0, fCount3 As Double = 0.0, fCount4 As Double = 0.0
                                                                     Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + ll)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + ll)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + ll)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + ll)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     z = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + llr + lc))) <= Sigma Then
                                                                                     fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + llr + lc))) <= Sigma Then
                                                                                     fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + llr + lc))) <= Sigma Then
                                                                                     fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + llr + lc))) <= Sigma Then
                                                                                         fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(col, row))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                         '#End Region
                                                                     Next

                                                                     SyncLock Me.lockObject
                                                                         If Not pe Is Nothing Then
                                                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                                                 pe.CurrentProgress += 1
                                                                             End If
                                                                             Try
                                                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                                                     RaiseEvent ProgressPlus(Me, pe)
                                                                                 End If

                                                                             Catch
                                                                             End Try
                                                                         End If
                                                                     End SyncLock

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (l + 1)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve with a full 2-dim kernel
    Public Function Convolve_par_Neg(b As Bitmap, Kernel As Double(,), AddVals As Double(,), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, divisor As Double) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If Kernel.GetLength(0) <> Kernel.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> AddVals.GetLength(1) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If AddVals.GetLength(0) <> Kernel.GetLength(0) Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If (Kernel.GetLength(0) And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.GetLength(0) < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If
        If Kernel.GetLength(0) > Math.Min(b.Width - 1, b.Height - 1) Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to" + (Math.Min(b.Width - 1, b.Height - 1)).ToString() + ".")
        End If

        Dim h As Integer = Kernel.GetLength(0) \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "First Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = h - l, rc As Integer = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    posSrc = 0

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = h - l
                    rc = 0
                    While r < Kernel.GetLength(1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (h - l)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), l)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += lf + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nWidth - h - 1) * 4

                r = h - l
                rc = 0
                While r < Kernel.GetLength(1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        Dim lcc As Integer = cc * 4

                        z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += lf + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
            Next
            '#End Region

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - Kernel.GetLength(1)
            Parallel.[For](0, nHeight - Kernel.GetLength(1) + 1, Sub(y, loopState)
                                                                     Try
                                                                         If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                                             loopState.Break()
                                                                         End If

                                                                     Catch

                                                                     End Try
                                                                     Dim fSum As Double = 0.0, fSum2 As Double = 0.0, fSum3 As Double = 0.0, fSum4 As Double = 0.0, KfSum As Double = 0.0
                                                                     Dim fCount As Double = 0.0, fCount2 As Double = 0.0, fCount3 As Double = 0.0, fCount4 As Double = 0.0
                                                                     Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     Dim pos As Integer = 0
                                                                     Dim posSrc As Integer = 0

                                                                     '#Region "First Pixels"
                                                                     For l As Integer = 0 To h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride
                                                                         posSrc = 0
                                                                         posSrc += y * stride

                                                                         Dim ll As Integer = l * 4

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                                             While c < Kernel.GetLength(0)
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(l, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(l, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + ll
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + ll)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + ll)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + ll)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + ll)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Standard"
                                                                     z = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                                                                     For x As Integer = 0 To nWidth - Kernel.GetLength(0)
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         pos = 0
                                                                         pos += y * stride + x * 4

                                                                         posSrc = 0
                                                                         posSrc += y * stride + x * 4

                                                                         For row As Integer = 0 To Kernel.GetLength(1) - 1
                                                                             Dim llr As Integer = row * stride

                                                                             For col As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                 Dim lc As Integer = col * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + llr + lc))) <= Sigma Then
                                                                                     fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(col, row))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + llr + lc))) <= Sigma Then
                                                                                     fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(col, row))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + llr + lc))) <= Sigma Then
                                                                                     fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(col, row))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + llr + lc))) <= Sigma Then
                                                                                         fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(col, row))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 KfSum += Kernel(col, row)
                                                                             Next
                                                                         Next

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                     Next
                                                                     '#End Region

                                                                     '#Region "Last Pixels"
                                                                     For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                                         fSum = 0.0
                                                                         fSum2 = 0.0
                                                                         fSum3 = 0.0
                                                                         fSum4 = 0.0
                                                                         KfSum = 0.0
                                                                         fCount = 0.0
                                                                         fCount2 = 0.0
                                                                         fCount3 = 0.0
                                                                         fCount4 = 0.0
                                                                         Dim fIgnore As Boolean = False, fIgnore2 As Boolean = False, fIgnore3 As Boolean = False, fIgnore4 As Boolean = False

                                                                         Dim ll As Integer = l * 4

                                                                         pos = 0
                                                                         pos += (y * stride) + ll
                                                                         posSrc = 0
                                                                         posSrc += (y * stride) + ll

                                                                         Dim fR As Integer = 0
                                                                         Dim fRc As Integer = 0
                                                                         While fR < Kernel.GetLength(1)
                                                                             Dim lr As Integer = fRc * stride

                                                                             Dim c As Integer = 0, cc As Integer = 0
                                                                             While c < nWidth - l
                                                                                 z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                 Dim lcc As Integer = cc * 4

                                                                                 If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                     fSum += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount += z
                                                                                 Else
                                                                                     fIgnore = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                     fSum2 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount2 += z
                                                                                 Else
                                                                                     fIgnore2 = True
                                                                                 End If

                                                                                 If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                     fSum3 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, fR)))
                                                                                     fCount3 += z
                                                                                 Else
                                                                                     fIgnore3 = True
                                                                                 End If

                                                                                 If DoTrans Then
                                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                         fSum4 += If((fR = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, fR)))
                                                                                         fCount4 += z
                                                                                     Else
                                                                                         fIgnore4 = True
                                                                                     End If
                                                                                 End If

                                                                                 If fR = h AndAlso c = h Then
                                                                                     KfSum += Kernel(c, fR) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, fR)
                                                                                 Else
                                                                                     KfSum += Kernel(c, fR)
                                                                                 End If
                                                                                 c += 1
                                                                                 cc += 1
                                                                             End While
                                                                             fR += 1
                                                                             fRc += 1
                                                                         End While

                                                                         If KfSum = 0.0 Then
                                                                             KfSum = 1.0
                                                                         End If

                                                                         If fCount = 0.0 Then
                                                                             fCount = 1.0
                                                                         End If
                                                                         If fCount2 = 0.0 Then
                                                                             fCount2 = 1.0
                                                                         End If
                                                                         If fCount3 = 0.0 Then
                                                                             fCount3 = 1.0
                                                                         End If
                                                                         If fCount4 = 0.0 Then
                                                                             fCount4 = 1.0
                                                                         End If

                                                                         pos += llh + lh
                                                                         p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum2 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias, 255), 0), [Byte])
                                                                         p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum3 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias, 255), 0), [Byte])
                                                                         If DoTrans Then
                                                                             p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-fSum4 / KfSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias, 255), 0), [Byte])
                                                                         End If

                                                                         If SrcOnSigma Then
                                                                             If fIgnore Then
                                                                                 p(pos) = pSrc(posSrc + llh + lh)
                                                                             End If
                                                                             If fIgnore2 Then
                                                                                 p(pos + 1) = pSrc(posSrc + 1 + llh + lh)
                                                                             End If
                                                                             If fIgnore3 Then
                                                                                 p(pos + 2) = pSrc(posSrc + 2 + llh + lh)
                                                                             End If
                                                                             If fIgnore4 Then
                                                                                 p(pos + 3) = pSrc(posSrc + 3 + llh + lh)
                                                                             End If
                                                                         End If
                                                                         '#End Region
                                                                     Next

                                                                     SyncLock Me.lockObject
                                                                         If Not pe Is Nothing Then
                                                                             If pe.ImgWidthHeight < Int32.MaxValue Then
                                                                                 pe.CurrentProgress += 1
                                                                             End If
                                                                             Try
                                                                                 If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                                                     RaiseEvent ProgressPlus(Me, pe)
                                                                                 End If

                                                                             Catch
                                                                             End Try
                                                                         End If
                                                                     End SyncLock

                                                                 End Sub)
            'Next
            '#End Region

            '#Region "Last Part"
            For l As Integer = 0 To h - 1
                Try
                    If Not bgw Is Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                        Exit For
                    End If

                Catch

                End Try
                Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                Dim z As Double = 1.0 / (Kernel.GetLength(0) * Kernel.GetLength(1))

                Dim pos As Integer = 0
                Dim posSrc As Integer = 0

                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                Dim lf As Integer = l * stride
                Dim ll As Integer = l * 4

                Dim r As Integer = 0
                Dim rc As Integer = 0
                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = h, cc As Integer = 0
                    While c < Kernel.GetLength(0)
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(0, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + ll)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + ll)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + ll)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + ll)
                    End If
                End If

                For x As Integer = 1 To nWidth - 2
                    Sum = 0.0
                    Sum2 = 0.0
                    Sum3 = 0.0
                    Sum4 = 0.0
                    KSum = 0.0
                    count = 0.0
                    count2 = 0.0
                    count3 = 0.0
                    count4 = 0.0
                    ignore = False
                    ignore2 = False
                    ignore3 = False
                    ignore4 = False

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    posSrc = 0
                    posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                    If x > Kernel.GetLength(0) - (h + 1) Then
                        pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                        posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                    End If

                    r = 0
                    rc = 0
                    While r < Kernel.GetLength(1) - (l + 1)
                        Dim lr As Integer = rc * stride

                        Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                        While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            Dim lcc As Integer = cc * 4

                            Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                            z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (l + 1)))

                            If Math.Abs(CInt(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                count += z
                            Else
                                ignore = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                count2 += z
                            Else
                                ignore2 = True
                            End If

                            If Math.Abs(CInt(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                count3 += z
                            Else
                                ignore3 = True
                            End If

                            If DoTrans Then
                                If Math.Abs(CInt(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                    Sum4 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                    count4 += z
                                Else
                                    ignore4 = True
                                End If
                            End If

                            If r = h AndAlso c = h Then
                                If x - h + Kernel.GetLength(0) <= b.Width Then
                                    KSum += Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1)
                                Else
                                    KSum += Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1)
                                End If
                            Else
                                KSum += Kernel(c, r)
                            End If
                            c += 1
                            cc += 1
                        End While
                        r += 1
                        rc += 1
                    End While

                    If KSum = 0.0 Then
                        KSum = 1.0
                    End If

                    If count = 0.0 Then
                        count = 1.0
                    End If
                    If count2 = 0.0 Then
                        count2 = 1.0
                    End If
                    If count3 = 0.0 Then
                        count3 = 1.0
                    End If
                    If count4 = 0.0 Then
                        count4 = 1.0
                    End If

                    pos = 0
                    pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                    pos += llh + (x * 4)
                    p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                    p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                    p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                    If DoTrans Then
                        p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                    End If

                    If SrcOnSigma Then
                        If ignore Then
                            p(pos) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore2 Then
                            p(pos + 1) = pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore3 Then
                            p(pos + 2) = pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))
                        End If
                        If ignore4 Then
                            p(pos + 3) = pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))
                        End If
                    End If
                Next

                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0
                ignore = False
                ignore2 = False
                ignore3 = False
                ignore4 = False

                pos = 0
                pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                pos += (nWidth - h - 1) * 4

                posSrc = 0
                posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                posSrc += (nWidth - h - 1) * 4

                While r < Kernel.GetLength(1) - (l + 1)
                    Dim lr As Integer = rc * stride

                    Dim c As Integer = 0, cc As Integer = 0
                    While c < Kernel.GetLength(0) - h
                        z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                        Dim lcc As Integer = cc * 4

                        If Math.Abs(CInt(pSrc(posSrc + lf + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                            Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                            count += z
                        Else
                            ignore = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 1 + lf + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                            Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                            count2 += z
                        Else
                            ignore2 = True
                        End If

                        If Math.Abs(CInt(pSrc(posSrc + 2 + lf + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                            Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                            count3 += z
                        Else
                            ignore3 = True
                        End If

                        If DoTrans Then
                            If Math.Abs(CInt(pSrc(posSrc + 3 + lf + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                Sum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                count4 += z
                            Else
                                ignore4 = True
                            End If
                        End If

                        If r = h AndAlso c = h Then
                            KSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1)
                        Else
                            KSum += Kernel(c, r)
                        End If
                        c += 1
                        cc += 1
                    End While
                    r += 1
                    rc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += llh + lh
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum2 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum3 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((-Sum4 / KSum / divisor), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + lf + lh)
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + lf + lh)
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + lf + lh)
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + lf + lh)
                    End If
                End If
                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    'convolve in horz direction
    Public Function ConvolveH_Par(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker) As Boolean

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If AddVals.Length <> Kernel.Length Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            '#Region "Main Body"

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            'For y As Integer = 0 To nHeight - 1
            Parallel.[For](0, nHeight, Sub(y, loopState)
                                           Try
                                               If bgw IsNot Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                   loopState.Break()
                                               End If

                                           Catch
                                           End Try

                                           Dim pos As Integer = 0
                                           Dim posSrc As Integer = 0

                                           Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                                           Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                                           Dim z As Double = 1.0 / Kernel.Length

                                           '#Region "First Pixels"
                                           For l As Integer = 0 To h - 1
                                               Sum = 0.0
                                               Sum2 = 0.0
                                               Sum3 = 0.0
                                               Sum4 = 0.0
                                               KSum = 0.0
                                               count = 0.0
                                               count2 = 0.0
                                               count3 = 0.0
                                               count4 = 0.0
                                               Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                               pos = 0
                                               pos += y * stride
                                               posSrc = 0
                                               posSrc += y * stride

                                               Dim c As Integer = h - l, cc As Integer = 0
                                               While c < Kernel.Length
                                                   z = 1.0 / (Kernel.Length - (h - l))

                                                   If Math.Abs(CInt(pSrc(posSrc + 0 + (l * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                       Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                       count += z
                                                   Else
                                                       ignore = True
                                                   End If

                                                   If Math.Abs(CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                       Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                       count2 += z
                                                   Else
                                                       ignore2 = True
                                                   End If

                                                   If Math.Abs(CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                       Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                       count3 += z
                                                   Else
                                                       ignore3 = True
                                                   End If

                                                   If DoTrans Then
                                                       If Math.Abs(CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                           Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                           count4 += z
                                                       Else
                                                           ignore4 = True
                                                       End If
                                                   End If

                                                   If c = h Then
                                                       KSum += Kernel(c) + AddVals(l)
                                                   Else
                                                       KSum += Kernel(c)
                                                   End If
                                                   c += 1
                                                   cc += 1
                                               End While

                                               If KSum = 0.0 Then
                                                   KSum = 1.0
                                               End If

                                               If count = 0.0 Then
                                                   count = 1.0
                                               End If
                                               If count2 = 0.0 Then
                                                   count2 = 1.0
                                               End If
                                               If count3 = 0.0 Then
                                                   count3 = 1.0
                                               End If
                                               If count4 = 0.0 Then
                                                   count4 = 1.0
                                               End If

                                               pos += (l * 4)
                                               p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                               p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                               p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                               If DoTrans Then
                                                   p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                               End If

                                               If SrcOnSigma Then
                                                   If ignore Then
                                                       p(pos + 0) = pSrc(posSrc + 0 + (l * 4))
                                                   End If
                                                   If ignore2 Then
                                                       p(pos + 1) = pSrc(posSrc + 1 + (l * 4))
                                                   End If
                                                   If ignore3 Then
                                                       p(pos + 2) = pSrc(posSrc + 2 + (l * 4))
                                                   End If
                                                   If ignore4 Then
                                                       p(pos + 3) = pSrc(posSrc + 3 + (l * 4))
                                                   End If
                                               End If
                                           Next
                                           '#End Region

                                           '#Region "Standard"
                                           z = 1.0 / Kernel.Length

                                           For x As Integer = 0 To nWidth - Kernel.Length
                                               Sum = 0.0
                                               Sum2 = 0.0
                                               Sum3 = 0.0
                                               Sum4 = 0.0
                                               KSum = 0.0
                                               count = 0.0
                                               count2 = 0.0
                                               count3 = 0.0
                                               count4 = 0.0

                                               Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                               pos = 0
                                               pos += y * stride + x * 4

                                               posSrc = 0
                                               posSrc += y * stride + x * 4

                                               For c As Integer = 0 To Kernel.Length - 1
                                                   If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                       Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                       count += z
                                                   Else
                                                       ignore = True
                                                   End If

                                                   If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                       Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                       count2 += z
                                                   Else
                                                       ignore2 = True
                                                   End If

                                                   If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                       Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                       count3 += z
                                                   Else
                                                       ignore3 = True
                                                   End If

                                                   If DoTrans Then
                                                       If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                           Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                           count4 += z
                                                       Else
                                                           ignore4 = True
                                                       End If
                                                   End If

                                                   KSum += Kernel(c)
                                               Next

                                               If KSum = 0.0 Then
                                                   KSum = 1.0
                                               End If

                                               If count = 0.0 Then
                                                   count = 1.0
                                               End If
                                               If count2 = 0.0 Then
                                                   count2 = 1.0
                                               End If
                                               If count3 = 0.0 Then
                                                   count3 = 1.0
                                               End If
                                               If count4 = 0.0 Then
                                                   count4 = 1.0
                                               End If

                                               pos += (h * 4)
                                               p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                               p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                               p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                               If DoTrans Then
                                                   p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                               End If

                                               If SrcOnSigma Then
                                                   If ignore Then
                                                       p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                   End If
                                                   If ignore2 Then
                                                       p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                   End If
                                                   If ignore3 Then
                                                       p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                   End If
                                                   If ignore4 Then
                                                       p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                   End If
                                               End If
                                           Next
                                           '#End Region

                                           '#Region "Last Pixels"
                                           For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                               Sum = 0.0
                                               Sum2 = 0.0
                                               Sum3 = 0.0
                                               Sum4 = 0.0
                                               KSum = 0.0
                                               count = 0.0
                                               count2 = 0.0
                                               count3 = 0.0
                                               count4 = 0.0
                                               Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                               pos = 0
                                               pos += (y * stride) + (l * 4)
                                               posSrc = 0
                                               posSrc += (y * stride) + (l * 4)

                                               Dim c As Integer = 0, cc As Integer = 0
                                               While c < nWidth - l
                                                   z = 1.0 / (nWidth - l)

                                                   If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                       Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                       count += z
                                                   Else
                                                       ignore = True
                                                   End If

                                                   If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                       Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                       count2 += z
                                                   Else
                                                       ignore2 = True
                                                   End If

                                                   If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                       Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                       count3 += z
                                                   Else
                                                       ignore3 = True
                                                   End If

                                                   If DoTrans Then
                                                       If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                           Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                           count4 += z
                                                       Else
                                                           ignore4 = True
                                                       End If
                                                   End If

                                                   If c = h Then
                                                       KSum += Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)
                                                   Else
                                                       KSum += Kernel(c)
                                                   End If
                                                   c += 1
                                                   cc += 1
                                               End While

                                               If KSum = 0.0 Then
                                                   KSum = 1.0
                                               End If

                                               If count = 0.0 Then
                                                   count = 1.0
                                               End If
                                               If count2 = 0.0 Then
                                                   count2 = 1.0
                                               End If
                                               If count3 = 0.0 Then
                                                   count3 = 1.0
                                               End If
                                               If count4 = 0.0 Then
                                                   count4 = 1.0
                                               End If

                                               pos += (h * 4)
                                               p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                               p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                               p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                               If DoTrans Then
                                                   p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                               End If

                                               If SrcOnSigma Then
                                                   If ignore Then
                                                       p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                   End If
                                                   If ignore2 Then
                                                       p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                   End If
                                                   If ignore3 Then
                                                       p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                   End If
                                                   If ignore4 Then
                                                       p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                   End If
                                               End If
                                           Next
                                           '#End Region

                                           'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
                                           'If handler IsNot Nothing Then
                                           SyncLock Me.lockObject
                                               If Not pe Is Nothing Then
                                                   If pe.ImgWidthHeight < Int32.MaxValue Then
                                                       pe.CurrentProgress += 1
                                                   End If
                                                   Try
                                                       If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                           RaiseEvent ProgressPlus(Me, pe)
                                                       End If

                                                   Catch
                                                   End Try
                                               End If
                                           End SyncLock
                                           'End If

                                           '#End Region
                                       End Sub)
            'Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    Public Function ConvolveH_Par(b As Bitmap, Kernel As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean, MaxVal As Integer,
     SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, bob As Boolean) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = DirectCast(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            '#Region "Main Body"

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            'For y As Integer = 0 To nHeight - 1
            Parallel.[For](0, nHeight, Sub(y, loopState)
                                           Try
                                               If bgw IsNot Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                   loopState.Break()
                                               End If

                                           Catch
                                           End Try

                                           Dim pos As Integer = 0
                                           Dim posSrc As Integer = 0

                                           Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                                           Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                                           Dim z As Double = 1.0 / Kernel.Length

                                           '#Region "Standard"
                                           z = 1.0 / Kernel.Length

                                           Dim yy As Integer = y * stride

                                           If bob Then
                                               Dim fp As Integer = 0
                                               Dim lp As Integer = nWidth

                                               For xx As Integer = 0 To nWidth - 1
                                                   Dim zz As Integer = yy + (xx * 4)
                                                   If pSrc(posSrc + zz + 3) > 0 Then
                                                       fp = Math.Max(xx - Kernel.Length, 0)
                                                       Exit For
                                                   End If
                                               Next

                                               For xx As Integer = (nWidth - 1) To 0 Step -1
                                                   Dim zz As Integer = yy + (xx * 4)
                                                   If pSrc(posSrc + zz + 3) > 0 Then
                                                       lp = Math.Min(xx + Kernel.Length - 1, nWidth)
                                                       Exit For
                                                   End If
                                               Next

                                               For x As Integer = fp To lp - Kernel.Length
                                                   Sum = 0.0
                                                   Sum2 = 0.0
                                                   Sum3 = 0.0
                                                   Sum4 = 0.0
                                                   KSum = 0.0
                                                   count = 0.0
                                                   count2 = 0.0
                                                   count3 = 0.0
                                                   count4 = 0.0

                                                   Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                                   pos = 0
                                                   pos += y * stride + x * 4

                                                   posSrc = 0
                                                   posSrc += y * stride + x * 4

                                                   If (x < fp + Kernel.Length) OrElse x > (lp - (Kernel.Length * 2) + 1) Then
                                                       For c As Integer = 0 To Kernel.Length - 1
                                                           If Math.Abs(CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (c * 4)))) <= Sigma Then
                                                               Sum += (CDbl(pSrc(posSrc + (c * 4))) * Kernel(c))
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                               Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                               Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                                   Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                                   count4 += z
                                                               Else
                                                                   ignore4 = True
                                                               End If
                                                           End If

                                                           KSum += Kernel(c)
                                                       Next

                                                       If KSum = 0.0 Then
                                                           KSum = 1.0
                                                       End If

                                                       If count = 0.0 Then
                                                           count = 1.0
                                                       End If
                                                       If count2 = 0.0 Then
                                                           count2 = 1.0
                                                       End If
                                                       If count3 = 0.0 Then
                                                           count3 = 1.0
                                                       End If
                                                       If count4 = 0.0 Then
                                                           count4 = 1.0
                                                       End If

                                                       pos += (h * 4)
                                                       p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                       p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                       p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                       If DoTrans Then
                                                           p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                       End If

                                                       If SrcOnSigma Then
                                                           If ignore Then
                                                               p(pos) = pSrc(posSrc + (h * 4))
                                                           End If
                                                           If ignore2 Then
                                                               p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                           End If
                                                           If ignore3 Then
                                                               p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                           End If
                                                           If ignore4 Then
                                                               p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                           End If
                                                       End If
                                                   Else
                                                       pos += (h * 4)
                                                       posSrc += (h * 4)
                                                       p(pos) = pSrc(posSrc)
                                                       p(pos + 1) = pSrc(posSrc + 1)
                                                       p(pos + 2) = pSrc(posSrc + 2)
                                                       p(pos + 3) = pSrc(posSrc + 3)
                                                   End If
                                               Next
                                           Else
                                               For x As Integer = 0 To nWidth - Kernel.Length
                                                   Sum = 0.0
                                                   Sum2 = 0.0
                                                   Sum3 = 0.0
                                                   Sum4 = 0.0
                                                   KSum = 0.0
                                                   count = 0.0
                                                   count2 = 0.0
                                                   count3 = 0.0
                                                   count4 = 0.0

                                                   Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                                   pos = 0
                                                   pos += y * stride + x * 4

                                                   posSrc = 0
                                                   posSrc += y * stride + x * 4

                                                   For c As Integer = 0 To Kernel.Length - 1
                                                       If Math.Abs(CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (c * 4)))) <= Sigma Then
                                                           Sum += (CDbl(pSrc(posSrc + (c * 4))) * Kernel(c))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                           Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                           Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                               Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                               count4 += z
                                                           Else
                                                               ignore4 = True
                                                           End If
                                                       End If

                                                       KSum += Kernel(c)
                                                   Next

                                                   If KSum = 0.0 Then
                                                       KSum = 1.0
                                                   End If

                                                   If count = 0.0 Then
                                                       count = 1.0
                                                   End If
                                                   If count2 = 0.0 Then
                                                       count2 = 1.0
                                                   End If
                                                   If count3 = 0.0 Then
                                                       count3 = 1.0
                                                   End If
                                                   If count4 = 0.0 Then
                                                       count4 = 1.0
                                                   End If

                                                   pos += (h * 4)
                                                   p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                   p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                   p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                   If DoTrans Then
                                                       p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                   End If

                                                   If SrcOnSigma Then
                                                       If ignore Then
                                                           p(pos) = pSrc(posSrc + (h * 4))
                                                       End If
                                                       If ignore2 Then
                                                           p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                       End If
                                                       If ignore3 Then
                                                           p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                       End If
                                                       If ignore4 Then
                                                           p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                       End If
                                                   End If
                                               Next
                                           End If
                                           '#End Region

                                           'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
                                           'If handler IsNot Nothing Then
                                           SyncLock Me.lockObject
                                               If Not pe Is Nothing Then
                                                   If pe.ImgWidthHeight < Int32.MaxValue Then
                                                       pe.CurrentProgress += 1
                                                   End If
                                                   Try
                                                       If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                           RaiseEvent ProgressPlus(Me, pe)
                                                       End If

                                                   Catch
                                                   End Try
                                               End If
                                           End SyncLock
                                           'End If

                                           '#End Region
                                       End Sub)
            'Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function


    Public Function ConvolveH_par_SigmaAsDistance(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If AddVals.Length <> Kernel.Length Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            '#Region "Main Body"

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            'For y As Integer = 0 To nHeight - 1
            Parallel.[For](0, nHeight, Sub(y, loopState)
                                           Try
                                               If bgw IsNot Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                   loopState.Break()
                                               End If

                                           Catch
                                           End Try
                                           Dim pos As Integer = 0
                                           Dim posSrc As Integer = 0

                                           Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                                           Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                                           Dim z As Double = 1.0 / Kernel.Length

                                           '#Region "First Pixels"
                                           For l As Integer = 0 To h - 1
                                               Sum = 0.0
                                               Sum2 = 0.0
                                               Sum3 = 0.0
                                               Sum4 = 0.0
                                               KSum = 0.0
                                               count = 0.0
                                               count2 = 0.0
                                               count3 = 0.0
                                               count4 = 0.0

                                               Dim ignore As Boolean = False

                                               pos = y * stride
                                               posSrc = y * stride

                                               Dim c As Integer = h - l, cc As Integer = 0
                                               While c < Kernel.Length
                                                   z = 1.0 / (Kernel.Length - (h - l))

                                                   Dim distance As Double = Math.Sqrt((CInt(pSrc(posSrc + (l * 4))) - CInt(pSrc(posSrc + (cc * 4)))) * (CInt(pSrc(posSrc + (l * 4))) - CInt(pSrc(posSrc + (cc * 4)))) + (CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) * (CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) + (CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) * (CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) + (CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) * (CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))))

                                                   If distance <= Sigma * 2 Then
                                                       Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c)))
                                                       count += z

                                                       Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                       count2 += z

                                                       Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                       count3 += z

                                                       If DoTrans Then
                                                           Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                           count4 += z
                                                       End If
                                                   Else
                                                       ignore = True
                                                   End If

                                                   If c = h Then
                                                       KSum += Kernel(c) + AddVals(l)
                                                   Else
                                                       KSum += Kernel(c)
                                                   End If
                                                   c += 1
                                                   cc += 1
                                               End While

                                               If KSum = 0.0 Then
                                                   KSum = 1.0
                                               End If

                                               If count = 0.0 Then
                                                   count = 1.0
                                               End If
                                               If count2 = 0.0 Then
                                                   count2 = 1.0
                                               End If
                                               If count3 = 0.0 Then
                                                   count3 = 1.0
                                               End If
                                               If count4 = 0.0 Then
                                                   count4 = 1.0
                                               End If

                                               pos += l * 4
                                               p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                               p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                               p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                               If DoTrans Then
                                                   p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                               End If

                                               If SrcOnSigma Then
                                                   If ignore Then
                                                       p(pos) = pSrc(posSrc + (l * 4))
                                                       p(pos + 1) = pSrc(posSrc + 1 + (l * 4))
                                                       p(pos + 2) = pSrc(posSrc + 2 + (l * 4))
                                                       p(pos + 3) = pSrc(posSrc + 3 + (l * 4))
                                                   End If
                                               End If
                                           Next
                                           '#End Region

                                           '#Region "Standard"
                                           z = 1.0 / Kernel.Length

                                           For x As Integer = 0 To nWidth - Kernel.Length
                                               Sum = 0.0
                                               Sum2 = 0.0
                                               Sum3 = 0.0
                                               Sum4 = 0.0
                                               KSum = 0.0
                                               count = 0.0
                                               count2 = 0.0
                                               count3 = 0.0
                                               count4 = 0.0

                                               Dim ignore As Boolean = False

                                               pos = y * stride + x * 4
                                               posSrc = y * stride + x * 4

                                               For c As Integer = 0 To Kernel.Length - 1
                                                   Dim distance As Double = Math.Sqrt((CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (c * 4)))) * (CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (c * 4)))) + (CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) * (CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) + (CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) * (CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) + (CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) * (CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))))

                                                   If distance <= Sigma * 2 Then
                                                       Sum += (CDbl(pSrc(posSrc + (c * 4))) * Kernel(c))
                                                       count += z

                                                       Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                       count2 += z

                                                       Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                       count3 += z

                                                       If DoTrans Then
                                                           Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                           count4 += z
                                                       End If
                                                   Else
                                                       ignore = True
                                                   End If

                                                   KSum += Kernel(c)
                                               Next

                                               If KSum = 0.0 Then
                                                   KSum = 1.0
                                               End If

                                               If count = 0.0 Then
                                                   count = 1.0
                                               End If
                                               If count2 = 0.0 Then
                                                   count2 = 1.0
                                               End If
                                               If count3 = 0.0 Then
                                                   count3 = 1.0
                                               End If
                                               If count4 = 0.0 Then
                                                   count4 = 1.0
                                               End If

                                               pos += h * 4
                                               p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                               p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                               p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                               If DoTrans Then
                                                   p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                               End If

                                               If SrcOnSigma Then
                                                   If ignore Then
                                                       p(pos) = pSrc(posSrc + (h * 4))
                                                       p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                       p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                       p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                   End If
                                               End If
                                           Next
                                           '#End Region

                                           '#Region "Last Pixels"
                                           For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                               Sum = 0.0
                                               Sum2 = 0.0
                                               Sum3 = 0.0
                                               Sum4 = 0.0
                                               KSum = 0.0
                                               count = 0.0
                                               count2 = 0.0
                                               count3 = 0.0
                                               count4 = 0.0

                                               Dim ignore As Boolean = False

                                               pos = (y * stride) + (l * 4)
                                               posSrc = (y * stride) + (l * 4)

                                               Dim c As Integer = 0, cc As Integer = 0
                                               While c < nWidth - l
                                                   z = 1.0 / (nWidth - l)

                                                   Dim distance As Double = Math.Sqrt((CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (cc * 4)))) * (CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (cc * 4)))) + (CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) * (CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) + (CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) * (CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) + (CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) * (CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))))

                                                   If distance <= Sigma * 2 Then
                                                       Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c)))
                                                       count += z

                                                       Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                       count2 += z

                                                       Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                       count3 += z

                                                       If DoTrans Then
                                                           Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                           count4 += z
                                                       End If
                                                   Else
                                                       ignore = True
                                                   End If

                                                   If c = h Then
                                                       KSum += Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)
                                                   Else
                                                       KSum += Kernel(c)
                                                   End If
                                                   c += 1
                                                   cc += 1
                                               End While

                                               If KSum = 0.0 Then
                                                   KSum = 1.0
                                               End If

                                               If count = 0.0 Then
                                                   count = 1.0
                                               End If
                                               If count2 = 0.0 Then
                                                   count2 = 1.0
                                               End If
                                               If count3 = 0.0 Then
                                                   count3 = 1.0
                                               End If
                                               If count4 = 0.0 Then
                                                   count4 = 1.0
                                               End If

                                               pos += h * 4
                                               p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                               p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                               p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                               If DoTrans Then
                                                   p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                               End If

                                               If SrcOnSigma Then
                                                   If ignore Then
                                                       p(pos) = pSrc(posSrc + (h * 4))
                                                       p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                       p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                       p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                   End If
                                               End If
                                           Next
                                           '#End Region

                                           'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
                                           'If handler IsNot Nothing Then
                                           SyncLock Me.lockObject
                                               If Not pe Is Nothing Then
                                                   If pe.ImgWidthHeight < Int32.MaxValue Then
                                                       pe.CurrentProgress += 1
                                                   End If
                                                   Try
                                                       If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                           RaiseEvent ProgressPlus(Me, pe)
                                                       End If

                                                   Catch
                                                   End Try
                                               End If
                                           End SyncLock
                                           'End If

                                           '#End Region
                                       End Sub)
            'Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function



    'Test-Method for Parallelizing. If you get errors, choose the method above.
    'faster then version with local intvars for reducing arithmetic operations
    Public Function ConvolveH_Par_X(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
     MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, bob2 As Boolean, xStart As Integer,
     xEnd As Integer) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "Main Body"

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim xs As Integer = Math.Max(xStart - Kernel.Length, 0)
            Dim xe As Integer = Math.Min(xEnd + Kernel.Length + 1, nWidth)

            Dim doFirstPixels As Boolean = False
            Dim doLastPixels As Boolean = False
            Dim h1 As Integer = h + 1

            Dim ppos As Integer = 0

            For x As Integer = 0 To h1 - 1
                ppos = 0
                ppos += x * 4
                For y As Integer = 0 To nHeight - 1
                    If pSrc(ppos + 3) > 0 Then
                        doFirstPixels = True
                        Exit For
                    End If
                    ppos += stride
                Next

                If doFirstPixels Then
                    Exit For
                End If
            Next

            ppos = 0

            For x As Integer = nWidth - h1 To nWidth - 1
                ppos = 0
                ppos += x * 4
                For y As Integer = 0 To nHeight - 1
                    If pSrc(ppos + 3) > 0 Then
                        doLastPixels = True
                        Exit For
                    End If
                    ppos += stride
                Next

                If doLastPixels Then
                    Exit For
                End If
            Next

            'For y As Integer = 0 To nHeight - 1
            Parallel.[For](0, nHeight, Sub(y, loopState)
                                           Try
                                               If bgw IsNot Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                   loopState.Break()
                                               End If

                                           Catch
                                           End Try

                                           Dim pos As Integer = 0
                                           Dim posSrc As Integer = 0

                                           Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                                           Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                                           Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False
                                           Dim z As Double = 1.0 / Kernel.Length

                                           '#Region "Standard"
                                           z = 1.0 / Kernel.Length

                                           Dim yy As Integer = y * stride

                                           If bob2 Then
                                               Dim fp As Integer = 0
                                               Dim lp As Integer = nWidth

                                               For xx As Integer = 0 To nWidth - 1
                                                   Dim zz As Integer = yy + (xx * 4)
                                                   If pSrc(zz + 3) > 0 Then
                                                       fp = Math.Max(xx - Kernel.Length, 0)
                                                       Exit For
                                                   End If
                                               Next

                                               For xx As Integer = (nWidth - 1) To 0 Step -1
                                                   Dim zz As Integer = yy + (xx * 4)
                                                   If pSrc(zz + 3) > 0 Then
                                                       lp = Math.Min(xx + Kernel.Length - 1, nWidth)
                                                       Exit For
                                                   End If
                                               Next

                                               For x As Integer = xs To xe - 1
                                                   Sum = 0.0
                                                   Sum2 = 0.0
                                                   Sum3 = 0.0
                                                   Sum4 = 0.0
                                                   KSum = 0.0
                                                   count = 0.0
                                                   count2 = 0.0
                                                   count3 = 0.0
                                                   count4 = 0.0
                                                   ignore = False
                                                   ignore2 = False
                                                   ignore3 = False
                                                   ignore4 = False

                                                   pos = 0
                                                   pos += y * stride + x * 4

                                                   posSrc = 0
                                                   posSrc += y * stride + x * 4

                                                   If x < (fp + Kernel.Length) OrElse x > lp Then
                                                       ' upper bound!
                                                       For c As Integer = 0 To Kernel.Length - 1
                                                           If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                               Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                               Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                               Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                                   Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                                   count4 += z
                                                               Else
                                                                   ignore4 = True
                                                               End If
                                                           End If

                                                           KSum += Kernel(c)
                                                       Next

                                                       If KSum = 0.0 Then
                                                           KSum = 1.0
                                                       End If

                                                       If count = 0.0 Then
                                                           count = 1.0
                                                       End If
                                                       If count2 = 0.0 Then
                                                           count2 = 1.0
                                                       End If
                                                       If count3 = 0.0 Then
                                                           count3 = 1.0
                                                       End If
                                                       If count4 = 0.0 Then
                                                           count4 = 1.0
                                                       End If

                                                       pos += (h * 4)
                                                       p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                       p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                       p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                       If DoTrans Then
                                                           p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                       End If

                                                       If SrcOnSigma Then
                                                           If ignore Then
                                                               p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                           End If
                                                           If ignore2 Then
                                                               p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                           End If
                                                           If ignore3 Then
                                                               p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                           End If
                                                           If ignore4 Then
                                                               p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                           End If
                                                       End If
                                                   Else
                                                       pos += (h * 4)
                                                       posSrc += (h * 4)
                                                       p(pos + 0) = pSrc(posSrc + 0)
                                                       p(pos + 1) = pSrc(posSrc + 1)
                                                       p(pos + 2) = pSrc(posSrc + 2)
                                                       p(pos + 3) = pSrc(posSrc + 3)
                                                   End If
                                               Next
                                           Else
                                               If doFirstPixels Then
                                                   '#Region "First Pixels"
                                                   For l As Integer = 0 To h - 1
                                                       Sum = 0.0
                                                       Sum2 = 0.0
                                                       Sum3 = 0.0
                                                       Sum4 = 0.0
                                                       KSum = 0.0
                                                       count = 0.0
                                                       count2 = 0.0
                                                       count3 = 0.0
                                                       count4 = 0.0
                                                       ignore = False
                                                       ignore2 = False
                                                       ignore3 = False
                                                       ignore4 = False

                                                       pos = 0
                                                       pos += y * stride
                                                       posSrc = 0
                                                       posSrc += y * stride

                                                       Dim c As Integer = h - l, cc As Integer = 0
                                                       While c < Kernel.Length
                                                           z = 1.0 / (Kernel.Length - (h - l))

                                                           If Math.Abs(CInt(pSrc(posSrc + 0 + (l * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                               Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                               Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                               Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                                   Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                                   count4 += z
                                                               Else
                                                                   ignore4 = True
                                                               End If
                                                           End If

                                                           If c = h Then
                                                               KSum += Kernel(c) + AddVals(l)
                                                           Else
                                                               KSum += Kernel(c)
                                                           End If
                                                           c += 1
                                                           cc += 1
                                                       End While

                                                       If KSum = 0.0 Then
                                                           KSum = 1.0
                                                       End If

                                                       If count = 0.0 Then
                                                           count = 1.0
                                                       End If
                                                       If count2 = 0.0 Then
                                                           count2 = 1.0
                                                       End If
                                                       If count3 = 0.0 Then
                                                           count3 = 1.0
                                                       End If
                                                       If count4 = 0.0 Then
                                                           count4 = 1.0
                                                       End If

                                                       pos += (l * 4)
                                                       p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                       p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                       p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                       If DoTrans Then
                                                           p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                       End If

                                                       If SrcOnSigma Then
                                                           If ignore Then
                                                               p(pos + 0) = pSrc(posSrc + 0 + (l * 4))
                                                           End If
                                                           If ignore2 Then
                                                               p(pos + 1) = pSrc(posSrc + 1 + (l * 4))
                                                           End If
                                                           If ignore3 Then
                                                               p(pos + 2) = pSrc(posSrc + 2 + (l * 4))
                                                           End If
                                                           If ignore4 Then
                                                               p(pos + 3) = pSrc(posSrc + 3 + (l * 4))
                                                           End If
                                                       End If
                                                       '#End Region
                                                   Next
                                               End If

                                               '#Region "Standard"
                                               z = 1.0 / Kernel.Length

                                               For x As Integer = 0 To nWidth - Kernel.Length
                                                   If x > xs - (Kernel.Length + 1) AndAlso x < xe + Kernel.Length + 1 Then
                                                       Sum = 0.0
                                                       Sum2 = 0.0
                                                       Sum3 = 0.0
                                                       Sum4 = 0.0
                                                       KSum = 0.0
                                                       count = 0.0
                                                       count2 = 0.0
                                                       count3 = 0.0
                                                       count4 = 0.0
                                                       ignore = False
                                                       ignore2 = False
                                                       ignore3 = False
                                                       ignore4 = False

                                                       pos = 0
                                                       pos += y * stride + x * 4

                                                       posSrc = 0
                                                       posSrc += y * stride + x * 4

                                                       For c As Integer = 0 To Kernel.Length - 1
                                                           If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                               Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                               Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                               Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                                   Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                                   count4 += z
                                                               Else
                                                                   ignore4 = True
                                                               End If
                                                           End If

                                                           KSum += Kernel(c)
                                                       Next

                                                       If KSum = 0.0 Then
                                                           KSum = 1.0
                                                       End If

                                                       If count = 0.0 Then
                                                           count = 1.0
                                                       End If
                                                       If count2 = 0.0 Then
                                                           count2 = 1.0
                                                       End If
                                                       If count3 = 0.0 Then
                                                           count3 = 1.0
                                                       End If
                                                       If count4 = 0.0 Then
                                                           count4 = 1.0
                                                       End If

                                                       pos += (h * 4)
                                                       p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                       p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                       p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                       If DoTrans Then
                                                           p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                       End If

                                                       If SrcOnSigma Then
                                                           If ignore Then
                                                               p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                           End If
                                                           If ignore2 Then
                                                               p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                           End If
                                                           If ignore3 Then
                                                               p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                           End If
                                                           If ignore4 Then
                                                               p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                           End If
                                                       End If
                                                   End If
                                               Next
                                               '#End Region

                                               If doLastPixels Then
                                                   '#Region "Last Pixels"
                                                   For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                       Sum = 0.0
                                                       Sum2 = 0.0
                                                       Sum3 = 0.0
                                                       Sum4 = 0.0
                                                       KSum = 0.0
                                                       count = 0.0
                                                       count2 = 0.0
                                                       count3 = 0.0
                                                       count4 = 0.0
                                                       ignore = False
                                                       ignore2 = False
                                                       ignore3 = False
                                                       ignore4 = False

                                                       pos = 0
                                                       pos += (y * stride) + (l * 4)
                                                       posSrc = 0
                                                       posSrc += (y * stride) + (l * 4)

                                                       Dim c As Integer = 0, cc As Integer = 0
                                                       While c < nWidth - l
                                                           z = 1.0 / (nWidth - l)

                                                           If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                               Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                               Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                               Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                                   Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                                   count4 += z
                                                               Else
                                                                   ignore4 = True
                                                               End If
                                                           End If

                                                           If c = h Then
                                                               KSum += Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)
                                                           Else
                                                               KSum += Kernel(c)
                                                           End If
                                                           c += 1
                                                           cc += 1
                                                       End While

                                                       If KSum = 0.0 Then
                                                           KSum = 1.0
                                                       End If

                                                       If count = 0.0 Then
                                                           count = 1.0
                                                       End If
                                                       If count2 = 0.0 Then
                                                           count2 = 1.0
                                                       End If
                                                       If count3 = 0.0 Then
                                                           count3 = 1.0
                                                       End If
                                                       If count4 = 0.0 Then
                                                           count4 = 1.0
                                                       End If

                                                       pos += (h * 4)
                                                       p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                       p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                       p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                       If DoTrans Then
                                                           p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                       End If

                                                       If SrcOnSigma Then
                                                           If ignore Then
                                                               p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                           End If
                                                           If ignore2 Then
                                                               p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                           End If
                                                           If ignore3 Then
                                                               p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                           End If
                                                           If ignore4 Then
                                                               p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                           End If
                                                       End If
                                                       '#End Region
                                                   Next
                                               End If
                                           End If
                                           '#End Region

                                           'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
                                           'If handler IsNot Nothing Then
                                           SyncLock Me.lockObject
                                               If Not pe Is Nothing Then
                                                   If pe.ImgWidthHeight < Int32.MaxValue Then
                                                       pe.CurrentProgress += 1
                                                   End If
                                                   Try
                                                       If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                           RaiseEvent ProgressPlus(Me, pe)
                                                       End If

                                                   Catch
                                                   End Try
                                               End If
                                           End SyncLock
                                           'End If

                                           '#End Region
                                       End Sub)
            'Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    Public Function ConvolveH_Par_Y(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
     MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, bob2 As Boolean, yStart As Integer,
     yEnd As Integer) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "Main Body"

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim ys As Integer = Math.Max(yStart, 0)
            Dim ye As Integer = Math.Min(yEnd, nHeight)

            Dim xs As Integer = 0
            Dim xe As Integer = nWidth

            Dim doFirstPixels As Boolean = False
            Dim doLastPixels As Boolean = False
            Dim h1 As Integer = h + 1

            Dim ppos As Integer = 0

            For x As Integer = 0 To h1 - 1
                ppos = 0
                ppos += x * 4
                For y As Integer = 0 To nHeight - 1
                    If pSrc(ppos + 3) > 0 Then
                        doFirstPixels = True
                        Exit For
                    End If
                    ppos += stride
                Next

                If doFirstPixels Then
                    Exit For
                End If
            Next

            ppos = 0

            For x As Integer = nWidth - h1 To nWidth - 1
                ppos = 0
                ppos += x * 4
                For y As Integer = 0 To nHeight - 1
                    If pSrc(ppos + 3) > 0 Then
                        doLastPixels = True
                        Exit For
                    End If
                    ppos += stride
                Next

                If doLastPixels Then
                    Exit For
                End If
            Next

            'for (int y = 0; y < nHeight; y++)
            Parallel.[For](0, nHeight, Sub(y, loopState)
                                           If y > ys - (Kernel.Length + 1) AndAlso y < ye + Kernel.Length + 1 Then
                                               Try
                                                   If bgw IsNot Nothing AndAlso bgw.WorkerSupportsCancellation AndAlso bgw.CancellationPending Then
                                                       loopState.Break()
                                                   End If

                                               Catch
                                               End Try

                                               Dim pos As Integer = 0
                                               Dim posSrc As Integer = 0

                                               Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                                               Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                                               Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False
                                               Dim z As Double = 1.0 / Kernel.Length

                                               '#Region "Standard"
                                               z = 1.0 / Kernel.Length

                                               Dim yy As Integer = y * stride

                                               If bob2 Then
                                                   Dim fp As Integer = 0
                                                   Dim lp As Integer = nWidth

                                                   For xx As Integer = 0 To nWidth - 1
                                                       Dim zz As Integer = yy + (xx * 4)
                                                       If pSrc(zz + 3) > 0 Then
                                                           fp = Math.Max(xx - Kernel.Length, 0)
                                                           Exit For
                                                       End If
                                                   Next

                                                   For xx As Integer = (nWidth - 1) To 0 Step -1
                                                       Dim zz As Integer = yy + (xx * 4)
                                                       If pSrc(zz + 3) > 0 Then
                                                           lp = Math.Min(xx + Kernel.Length - 1, nWidth)
                                                           Exit For
                                                       End If
                                                   Next

                                                   For x As Integer = xs To xe - 1
                                                       Sum = 0.0
                                                       Sum2 = 0.0
                                                       Sum3 = 0.0
                                                       Sum4 = 0.0
                                                       KSum = 0.0
                                                       count = 0.0
                                                       count2 = 0.0
                                                       count3 = 0.0
                                                       count4 = 0.0
                                                       ignore = False
                                                       ignore2 = False
                                                       ignore3 = False
                                                       ignore4 = False

                                                       pos = 0
                                                       pos += y * stride + x * 4

                                                       posSrc = 0
                                                       posSrc += y * stride + x * 4

                                                       If x < (fp + Kernel.Length) OrElse x > lp Then
                                                           ' upper bound!
                                                           For c As Integer = 0 To Kernel.Length - 1
                                                               If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                                   Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                                   count += z
                                                               Else
                                                                   ignore = True
                                                               End If

                                                               If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                                   Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                                   count2 += z
                                                               Else
                                                                   ignore2 = True
                                                               End If

                                                               If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                                   Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                                   count3 += z
                                                               Else
                                                                   ignore3 = True
                                                               End If

                                                               If DoTrans Then
                                                                   If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                                       Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                                       count4 += z
                                                                   Else
                                                                       ignore4 = True
                                                                   End If
                                                               End If

                                                               KSum += Kernel(c)
                                                           Next

                                                           If KSum = 0.0 Then
                                                               KSum = 1.0
                                                           End If

                                                           If count = 0.0 Then
                                                               count = 1.0
                                                           End If
                                                           If count2 = 0.0 Then
                                                               count2 = 1.0
                                                           End If
                                                           If count3 = 0.0 Then
                                                               count3 = 1.0
                                                           End If
                                                           If count4 = 0.0 Then
                                                               count4 = 1.0
                                                           End If

                                                           pos += (h * 4)
                                                           p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                           p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                           p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                           If DoTrans Then
                                                               p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                           End If

                                                           If SrcOnSigma Then
                                                               If ignore Then
                                                                   p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                               End If
                                                               If ignore2 Then
                                                                   p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                               End If
                                                               If ignore3 Then
                                                                   p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                               End If
                                                               If ignore4 Then
                                                                   p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                               End If
                                                           End If
                                                       Else
                                                           pos += (h * 4)
                                                           posSrc += (h * 4)
                                                           p(pos + 0) = pSrc(posSrc + 0)
                                                           p(pos + 1) = pSrc(posSrc + 1)
                                                           p(pos + 2) = pSrc(posSrc + 2)
                                                           p(pos + 3) = pSrc(posSrc + 3)
                                                       End If
                                                   Next
                                               Else
                                                   If doFirstPixels Then
                                                       '#Region "First Pixels"
                                                       For l As Integer = 0 To h - 1
                                                           Sum = 0.0
                                                           Sum2 = 0.0
                                                           Sum3 = 0.0
                                                           Sum4 = 0.0
                                                           KSum = 0.0
                                                           count = 0.0
                                                           count2 = 0.0
                                                           count3 = 0.0
                                                           count4 = 0.0
                                                           ignore = False
                                                           ignore2 = False
                                                           ignore3 = False
                                                           ignore4 = False

                                                           pos = 0
                                                           pos += y * stride
                                                           posSrc = 0
                                                           posSrc += y * stride

                                                           Dim c As Integer = h - l, cc As Integer = 0
                                                           While c < Kernel.Length
                                                               z = 1.0 / (Kernel.Length - (h - l))

                                                               If Math.Abs(CInt(pSrc(posSrc + 0 + (l * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                                   Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                                   count += z
                                                               Else
                                                                   ignore = True
                                                               End If

                                                               If Math.Abs(CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                                   Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                                   count2 += z
                                                               Else
                                                                   ignore2 = True
                                                               End If

                                                               If Math.Abs(CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                                   Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                                   count3 += z
                                                               Else
                                                                   ignore3 = True
                                                               End If

                                                               If DoTrans Then
                                                                   If Math.Abs(CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                                       Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                                       count4 += z
                                                                   Else
                                                                       ignore4 = True
                                                                   End If
                                                               End If

                                                               If c = h Then
                                                                   KSum += Kernel(c) + AddVals(l)
                                                               Else
                                                                   KSum += Kernel(c)
                                                               End If
                                                               c += 1
                                                               cc += 1
                                                           End While

                                                           If KSum = 0.0 Then
                                                               KSum = 1.0
                                                           End If

                                                           If count = 0.0 Then
                                                               count = 1.0
                                                           End If
                                                           If count2 = 0.0 Then
                                                               count2 = 1.0
                                                           End If
                                                           If count3 = 0.0 Then
                                                               count3 = 1.0
                                                           End If
                                                           If count4 = 0.0 Then
                                                               count4 = 1.0
                                                           End If

                                                           pos += (l * 4)
                                                           p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                           p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                           p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                           If DoTrans Then
                                                               p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                           End If

                                                           If SrcOnSigma Then
                                                               If ignore Then
                                                                   p(pos + 0) = pSrc(posSrc + 0 + (l * 4))
                                                               End If
                                                               If ignore2 Then
                                                                   p(pos + 1) = pSrc(posSrc + 1 + (l * 4))
                                                               End If
                                                               If ignore3 Then
                                                                   p(pos + 2) = pSrc(posSrc + 2 + (l * 4))
                                                               End If
                                                               If ignore4 Then
                                                                   p(pos + 3) = pSrc(posSrc + 3 + (l * 4))
                                                               End If
                                                           End If
                                                           '#End Region
                                                       Next
                                                   End If

                                                   '#Region "Standard"
                                                   z = 1.0 / Kernel.Length

                                                   For x As Integer = 0 To nWidth - Kernel.Length
                                                       Sum = 0.0
                                                       Sum2 = 0.0
                                                       Sum3 = 0.0
                                                       Sum4 = 0.0
                                                       KSum = 0.0
                                                       count = 0.0
                                                       count2 = 0.0
                                                       count3 = 0.0
                                                       count4 = 0.0
                                                       ignore = False
                                                       ignore2 = False
                                                       ignore3 = False
                                                       ignore4 = False

                                                       pos = 0
                                                       pos += y * stride + x * 4

                                                       posSrc = 0
                                                       posSrc += y * stride + x * 4

                                                       For c As Integer = 0 To Kernel.Length - 1
                                                           If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                               Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                               Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                               Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                                   Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                                   count4 += z
                                                               Else
                                                                   ignore4 = True
                                                               End If
                                                           End If

                                                           KSum += Kernel(c)
                                                       Next

                                                       If KSum = 0.0 Then
                                                           KSum = 1.0
                                                       End If

                                                       If count = 0.0 Then
                                                           count = 1.0
                                                       End If
                                                       If count2 = 0.0 Then
                                                           count2 = 1.0
                                                       End If
                                                       If count3 = 0.0 Then
                                                           count3 = 1.0
                                                       End If
                                                       If count4 = 0.0 Then
                                                           count4 = 1.0
                                                       End If

                                                       pos += (h * 4)
                                                       p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                       p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                       p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                       If DoTrans Then
                                                           p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                       End If

                                                       If SrcOnSigma Then
                                                           If ignore Then
                                                               p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                           End If
                                                           If ignore2 Then
                                                               p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                           End If
                                                           If ignore3 Then
                                                               p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                           End If
                                                           If ignore4 Then
                                                               p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                           End If
                                                       End If
                                                   Next
                                                   '#End Region

                                                   If doLastPixels Then
                                                       '#Region "Last Pixels"
                                                       For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                           Sum = 0.0
                                                           Sum2 = 0.0
                                                           Sum3 = 0.0
                                                           Sum4 = 0.0
                                                           KSum = 0.0
                                                           count = 0.0
                                                           count2 = 0.0
                                                           count3 = 0.0
                                                           count4 = 0.0
                                                           ignore = False
                                                           ignore2 = False
                                                           ignore3 = False
                                                           ignore4 = False

                                                           pos = 0
                                                           pos += (y * stride) + (l * 4)
                                                           posSrc = 0
                                                           posSrc += (y * stride) + (l * 4)

                                                           Dim c As Integer = 0, cc As Integer = 0
                                                           While c < nWidth - l
                                                               z = 1.0 / (nWidth - l)

                                                               If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                                   Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                                   count += z
                                                               Else
                                                                   ignore = True
                                                               End If

                                                               If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                                   Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                                   count2 += z
                                                               Else
                                                                   ignore2 = True
                                                               End If

                                                               If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                                   Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                                   count3 += z
                                                               Else
                                                                   ignore3 = True
                                                               End If

                                                               If DoTrans Then
                                                                   If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                                       Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                                       count4 += z
                                                                   Else
                                                                       ignore4 = True
                                                                   End If
                                                               End If

                                                               If c = h Then
                                                                   KSum += Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)
                                                               Else
                                                                   KSum += Kernel(c)
                                                               End If
                                                               c += 1
                                                               cc += 1
                                                           End While

                                                           If KSum = 0.0 Then
                                                               KSum = 1.0
                                                           End If

                                                           If count = 0.0 Then
                                                               count = 1.0
                                                           End If
                                                           If count2 = 0.0 Then
                                                               count2 = 1.0
                                                           End If
                                                           If count3 = 0.0 Then
                                                               count3 = 1.0
                                                           End If
                                                           If count4 = 0.0 Then
                                                               count4 = 1.0
                                                           End If

                                                           pos += (h * 4)
                                                           p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                           p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                           p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                           If DoTrans Then
                                                               p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                           End If

                                                           If SrcOnSigma Then
                                                               If ignore Then
                                                                   p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                               End If
                                                               If ignore2 Then
                                                                   p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                               End If
                                                               If ignore3 Then
                                                                   p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                               End If
                                                               If ignore4 Then
                                                                   p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                               End If
                                                           End If
                                                           '#End Region
                                                       Next
                                                   End If
                                               End If
                                               '#End Region

                                               'Dim handler As ProgressPlusEventHandler = Me.ProgressPlus
                                               'If handler IsNot Nothing Then
                                               SyncLock Me.lockObject
                                                   If Not pe Is Nothing Then
                                                       If pe.ImgWidthHeight < Int32.MaxValue Then
                                                           pe.CurrentProgress += 1
                                                       End If
                                                       Try
                                                           If CInt(pe.CurrentProgress) Mod pe.PrgInterval = 0 Then
                                                               RaiseEvent ProgressPlus(Me, pe)
                                                           End If

                                                       Catch
                                                       End Try
                                                   End If
                                               End SyncLock
                                               'End If
                                           End If

                                           '#End Region
                                       End Sub)

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function


    Public Function ConvolveH_Par_SingleRow(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, row As Integer) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If AddVals.Length <> Kernel.Length Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        Try
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            '#Region "Main Body"

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height
            Dim y As Integer = row

            Dim pos As Integer = 0
            Dim posSrc As Integer = 0

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
            Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
            Dim z As Double = 1.0 / Kernel.Length

            '#Region "First Pixels"
            For l As Integer = 0 To h - 1
                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False
                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0

                pos = 0
                pos += y * stride
                posSrc = 0
                posSrc += y * stride

                Dim c As Integer = h - l, cc As Integer = 0
                While c < Kernel.Length
                    z = 1.0 / (Kernel.Length - (h - l))

                    If Math.Abs(CInt(pSrc(posSrc + (l * 4))) - CInt(pSrc(posSrc + (cc * 4)))) <= Sigma Then
                        Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c)))
                        count += z
                    Else
                        ignore = True
                    End If

                    If Math.Abs(CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                        Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                        count2 += z
                    Else
                        ignore2 = True
                    End If

                    If Math.Abs(CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                        Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                        count3 += z
                    Else
                        ignore3 = True
                    End If

                    If DoTrans Then
                        If Math.Abs(CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                            Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                            count4 += z
                        Else
                            ignore4 = True
                        End If
                    End If

                    If c = h Then
                        KSum += Kernel(c) + AddVals(l)
                    Else
                        KSum += Kernel(c)
                    End If
                    c += 1
                    cc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += (l * 4)
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + (l * 4))
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + (l * 4))
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + (l * 4))
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + (l * 4))
                    End If
                End If
            Next
            '#End Region

            '#Region "Standard"
            z = 1.0 / Kernel.Length

            For x As Integer = 0 To nWidth - Kernel.Length
                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False
                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0

                pos = 0
                pos += y * stride + x * 4

                posSrc = 0
                posSrc += y * stride + x * 4

                For c As Integer = 0 To Kernel.Length - 1
                    If Math.Abs(CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (c * 4)))) <= Sigma Then
                        Sum += (CDbl(pSrc(posSrc + (c * 4))) * Kernel(c))
                        count += z
                    Else
                        ignore = True
                    End If

                    If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                        Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                        count2 += z
                    Else
                        ignore2 = True
                    End If

                    If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                        Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                        count3 += z
                    Else
                        ignore3 = True
                    End If

                    If DoTrans Then
                        If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                            Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                            count4 += z
                        Else
                            ignore4 = True
                        End If
                    End If

                    KSum += Kernel(c)
                Next

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += (h * 4)
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + (h * 4))
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                    End If
                End If
            Next
            '#End Region

            '#Region "Last Pixels"
            For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False
                Sum = 0.0
                Sum2 = 0.0
                Sum3 = 0.0
                Sum4 = 0.0
                KSum = 0.0
                count = 0.0
                count2 = 0.0
                count3 = 0.0
                count4 = 0.0

                pos = 0
                pos += (y * stride) + (l * 4)
                posSrc = 0
                posSrc += (y * stride) + (l * 4)

                Dim c As Integer = 0, cc As Integer = 0
                While c < nWidth - l
                    z = 1.0 / (nWidth - l)

                    If Math.Abs(CInt(pSrc(posSrc + (h * 4))) - CInt(pSrc(posSrc + (cc * 4)))) <= Sigma Then
                        Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c)))
                        count += z
                    Else
                        ignore = True
                    End If

                    If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                        Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                        count2 += z
                    Else
                        ignore2 = True
                    End If

                    If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                        Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                        count3 += z
                    Else
                        ignore3 = True
                    End If

                    If DoTrans Then
                        If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                            Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                            count4 += z
                        Else
                            ignore4 = True
                        End If
                    End If

                    If c = h Then
                        KSum += Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)
                    Else
                        KSum += Kernel(c)
                    End If
                    c += 1
                    cc += 1
                End While

                If KSum = 0.0 Then
                    KSum = 1.0
                End If

                If count = 0.0 Then
                    count = 1.0
                End If
                If count2 = 0.0 Then
                    count2 = 1.0
                End If
                If count3 = 0.0 Then
                    count3 = 1.0
                End If
                If count4 = 0.0 Then
                    count4 = 1.0
                End If

                pos += (h * 4)
                p(pos) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias, 255), 0), [Byte])
                p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias, 255), 0), [Byte])
                p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias, 255), 0), [Byte])
                If DoTrans Then
                    p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias, 255), 0), [Byte])
                End If

                If SrcOnSigma Then
                    If ignore Then
                        p(pos) = pSrc(posSrc + (h * 4))
                    End If
                    If ignore2 Then
                        p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                    End If
                    If ignore3 Then
                        p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                    End If
                    If ignore4 Then
                        p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                    End If
                End If
                '#End Region

                '#End Region
            Next

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function

    Public Function ConvolveH_par(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
         DoR As Boolean, DoG As Boolean, DoB As Boolean, LeaveNotSelectedChannelsAtCurrentValue As Boolean, MaxVal As Integer, SrcOnSigma As Boolean,
         pe As ProgressEventArgs, InitialSum As Integer, conv As Convolution) As Boolean

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 12L) Then
            Throw New OutOfMemoryException("Not enough Memory.")
        End If
        If (Kernel.Length And &H1) <> 1 Then
            Throw New Exception("Kernelrows Length must be Odd.")
        End If
        If AddVals.Length <> Kernel.Length Then
            Throw New Exception("Kernel must be quadratic.")
        End If
        If Kernel.Length < 3 Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If
        If Kernel.Length > MaxVal Then
            Throw New Exception("Kernelrows Length must be in the range from 3 to " + MaxVal.ToString() + ".")
        End If

        conv.CancelLoops = False

        Dim h As Integer = Kernel.Length \ 2

        Dim bSrc As Bitmap = Nothing
        Dim bmData As BitmapData = Nothing
        Dim bmSrc As BitmapData = Nothing

        conv._pe = pe
        _sum = InitialSum

        Try
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim p((bmData.Stride * bmData.Height) - 1) As Byte
            Marshal.Copy(bmData.Scan0, p, 0, p.Length)

            Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
            Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

            '#Region "Main Body"

            'For y As Integer = 0 To nHeight - 1
            Parallel.[For](0, nHeight, Function() 0, Function(y, loopState, localSum)
                                                         Dim cancelFlag As Boolean = False

                                                         Dim Sum As Double = 0.0, Sum2 As Double = 0.0, Sum3 As Double = 0.0, Sum4 As Double = 0.0, KSum As Double = 0.0
                                                         Dim count As Double = 0.0, count2 As Double = 0.0, count3 As Double = 0.0, count4 As Double = 0.0
                                                         Dim z As Double = 1.0 / Kernel.Length

                                                         Dim pos As Integer = 0
                                                         Dim posSrc As Integer = 0

                                                         '#Region "First Pixels"
                                                         For l As Integer = 0 To h - 1
                                                             If _cancelLoops Then
                                                                 cancelFlag = True
                                                                 Exit For
                                                             End If

                                                             Sum = 0.0
                                                             Sum2 = 0.0
                                                             Sum3 = 0.0
                                                             Sum4 = 0.0
                                                             KSum = 0.0
                                                             count = 0.0
                                                             count2 = 0.0
                                                             count3 = 0.0
                                                             count4 = 0.0
                                                             Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                                             pos = y * stride
                                                             posSrc = y * stride

                                                             Dim c As Integer = h - l, cc As Integer = 0
                                                             While c < Kernel.Length
                                                                 z = 1.0 / (Kernel.Length - (h - l))

                                                                 If DoB Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 0 + (l * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                                         Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                                         count += z
                                                                     Else
                                                                         ignore = True
                                                                     End If
                                                                 End If

                                                                 If DoG Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                                         Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                                         count2 += z
                                                                     Else
                                                                         ignore2 = True
                                                                     End If
                                                                 End If

                                                                 If DoR Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                                         Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                                         count3 += z
                                                                     Else
                                                                         ignore3 = True
                                                                     End If
                                                                 End If

                                                                 If DoTrans Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                                         Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                                         count4 += z
                                                                     Else
                                                                         ignore4 = True
                                                                     End If
                                                                 End If

                                                                 If c = h Then
                                                                     KSum += Kernel(c) + AddVals(l)
                                                                 Else
                                                                     KSum += Kernel(c)
                                                                 End If
                                                                 c += 1
                                                                 cc += 1
                                                             End While

                                                             If KSum = 0.0 Then
                                                                 KSum = 1.0
                                                             End If

                                                             If count = 0.0 Then
                                                                 count = 1.0
                                                             End If
                                                             If count2 = 0.0 Then
                                                                 count2 = 1.0
                                                             End If
                                                             If count3 = 0.0 Then
                                                                 count3 = 1.0
                                                             End If
                                                             If count4 = 0.0 Then
                                                                 count4 = 1.0
                                                             End If

                                                             pos += (l * 4)
                                                             If DoB OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 0) = CType(Math.Max(Math.Min(CInt((Sum / KSum) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoG OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 1) = CType(Math.Max(Math.Min(CInt((Sum2 / KSum) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoR OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 2) = CType(Math.Max(Math.Min(CInt((Sum3 / KSum) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoTrans Then
                                                                 p(pos + 3) = CType(Math.Max(Math.Min(CInt((Sum4 / KSum) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                             End If

                                                             If SrcOnSigma Then
                                                                 If ignore Then
                                                                     p(pos + 0) = pSrc(posSrc + 0 + (l * 4))
                                                                 End If
                                                                 If ignore2 Then
                                                                     p(pos + 1) = pSrc(posSrc + 1 + (l * 4))
                                                                 End If
                                                                 If ignore3 Then
                                                                     p(pos + 2) = pSrc(posSrc + 2 + (l * 4))
                                                                 End If
                                                                 If ignore4 Then
                                                                     p(pos + 3) = pSrc(posSrc + 3 + (l * 4))
                                                                 End If
                                                             End If
                                                         Next
                                                         '#End Region

                                                         '#Region "Standard"
                                                         z = 1.0 / Kernel.Length

                                                         For x As Integer = 0 To nWidth - Kernel.Length
                                                             If _cancelLoops Then
                                                                 cancelFlag = True
                                                                 Exit For
                                                             End If

                                                             Sum = 0.0
                                                             Sum2 = 0.0
                                                             Sum3 = 0.0
                                                             Sum4 = 0.0
                                                             KSum = 0.0
                                                             count = 0.0
                                                             count2 = 0.0
                                                             count3 = 0.0
                                                             count4 = 0.0
                                                             Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                                             pos = y * stride + x * 4

                                                             posSrc = y * stride + x * 4

                                                             For c As Integer = 0 To Kernel.Length - 1
                                                                 If DoB Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                                         Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                                         count += z
                                                                     Else
                                                                         ignore = True
                                                                     End If
                                                                 End If

                                                                 If DoG Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                                         Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                                         count2 += z
                                                                     Else
                                                                         ignore2 = True
                                                                     End If
                                                                 End If

                                                                 If DoR Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                                         Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                                         count3 += z
                                                                     Else
                                                                         ignore3 = True
                                                                     End If
                                                                 End If

                                                                 If DoTrans Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (c * 4)))) <= Sigma Then
                                                                         Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c))
                                                                         count4 += z
                                                                     Else
                                                                         ignore4 = True
                                                                     End If
                                                                 End If

                                                                 KSum += Kernel(c)
                                                             Next

                                                             If KSum = 0.0 Then
                                                                 KSum = 1.0
                                                             End If

                                                             If count = 0.0 Then
                                                                 count = 1.0
                                                             End If
                                                             If count2 = 0.0 Then
                                                                 count2 = 1.0
                                                             End If
                                                             If count3 = 0.0 Then
                                                                 count3 = 1.0
                                                             End If
                                                             If count4 = 0.0 Then
                                                                 count4 = 1.0
                                                             End If

                                                             pos += (h * 4)
                                                             If DoB OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 0) = CType(Math.Max(Math.Min(CInt((Sum / KSum) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoG OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 1) = CType(Math.Max(Math.Min(CInt((Sum2 / KSum) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoR OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 2) = CType(Math.Max(Math.Min(CInt((Sum3 / KSum) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoTrans Then
                                                                 p(pos + 3) = CType(Math.Max(Math.Min(CInt((Sum4 / KSum) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                             End If

                                                             If SrcOnSigma Then
                                                                 If ignore Then
                                                                     p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                                 End If
                                                                 If ignore2 Then
                                                                     p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                                 End If
                                                                 If ignore3 Then
                                                                     p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                                 End If
                                                                 If ignore4 Then
                                                                     p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                                 End If
                                                             End If
                                                         Next
                                                         '#End Region

                                                         '#Region "Last Pixels"
                                                         For l As Integer = nWidth - Kernel.GetLength(0) + 1 To nWidth - h - 1
                                                             If _cancelLoops Then
                                                                 cancelFlag = True
                                                                 Exit For
                                                             End If

                                                             Sum = 0.0
                                                             Sum2 = 0.0
                                                             Sum3 = 0.0
                                                             Sum4 = 0.0
                                                             KSum = 0.0
                                                             count = 0.0
                                                             count2 = 0.0
                                                             count3 = 0.0
                                                             count4 = 0.0
                                                             Dim ignore As Boolean = False, ignore2 As Boolean = False, ignore3 As Boolean = False, ignore4 As Boolean = False

                                                             pos = (y * stride) + (l * 4)
                                                             posSrc = (y * stride) + (l * 4)

                                                             Dim c As Integer = 0, cc As Integer = 0
                                                             While c < nWidth - l
                                                                 z = 1.0 / (nWidth - l)

                                                                 If DoB Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma Then
                                                                         Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                                         count += z
                                                                     Else
                                                                         ignore = True
                                                                     End If
                                                                 End If

                                                                 If DoG Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma Then
                                                                         Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                                         count2 += z
                                                                     Else
                                                                         ignore2 = True
                                                                     End If
                                                                 End If

                                                                 If DoR Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma Then
                                                                         Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                                         count3 += z
                                                                     Else
                                                                         ignore3 = True
                                                                     End If
                                                                 End If

                                                                 If DoTrans Then
                                                                     If Math.Abs(CInt(pSrc(posSrc + 3 + (h * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma Then
                                                                         Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c)))
                                                                         count4 += z
                                                                     Else
                                                                         ignore4 = True
                                                                     End If
                                                                 End If

                                                                 If c = h Then
                                                                     KSum += Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)
                                                                 Else
                                                                     KSum += Kernel(c)
                                                                 End If
                                                                 c += 1
                                                                 cc += 1
                                                             End While

                                                             If KSum = 0.0 Then
                                                                 KSum = 1.0
                                                             End If

                                                             If count = 0.0 Then
                                                                 count = 1.0
                                                             End If
                                                             If count2 = 0.0 Then
                                                                 count2 = 1.0
                                                             End If
                                                             If count3 = 0.0 Then
                                                                 count3 = 1.0
                                                             End If
                                                             If count4 = 0.0 Then
                                                                 count4 = 1.0
                                                             End If

                                                             pos += (h * 4)
                                                             If DoB OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 0) = CType(Math.Max(Math.Min(CInt((Sum / KSum) / CDbl(count)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoG OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 1) = CType(Math.Max(Math.Min(CInt((Sum2 / KSum) / CDbl(count2)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoR OrElse LeaveNotSelectedChannelsAtCurrentValue = False Then
                                                                 p(pos + 2) = CType(Math.Max(Math.Min(CInt((Sum3 / KSum) / CDbl(count3)) + Bias, 255), 0), [Byte])
                                                             End If
                                                             If DoTrans Then
                                                                 p(pos + 3) = CType(Math.Max(Math.Min(CInt((Sum4 / KSum) / CDbl(count4)) + Bias, 255), 0), [Byte])
                                                             End If

                                                             If SrcOnSigma Then
                                                                 If ignore Then
                                                                     p(pos + 0) = pSrc(posSrc + 0 + (h * 4))
                                                                 End If
                                                                 If ignore2 Then
                                                                     p(pos + 1) = pSrc(posSrc + 1 + (h * 4))
                                                                 End If
                                                                 If ignore3 Then
                                                                     p(pos + 2) = pSrc(posSrc + 2 + (h * 4))
                                                                 End If
                                                                 If ignore4 Then
                                                                     p(pos + 3) = pSrc(posSrc + 3 + (h * 4))
                                                                 End If
                                                             End If
                                                         Next
                                                         '#End Region

                                                         If cancelFlag Then
                                                             loopState.Break()
                                                         End If

                                                         localSum += nHeight
                                                         Return localSum

                                                     End Function, Sub(localSum)
                                                                       Interlocked.Add(_sum, localSum)
                                                                       SyncLock Me.lockObject
                                                                           Me.Report()
                                                                       End SyncLock

                                                                       '#End Region
                                                                   End Sub)

            Marshal.Copy(p, 0, bmData.Scan0, p.Length)
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

            p = Nothing
            pSrc = Nothing

            Return True
        Catch
            Try
                b.UnlockBits(bmData)

            Catch
            End Try

            Try
                bSrc.UnlockBits(bmSrc)

            Catch
            End Try

            If bSrc IsNot Nothing Then
                bSrc.Dispose()
                bSrc = Nothing
            End If
        End Try
        Return False
    End Function
End Class
