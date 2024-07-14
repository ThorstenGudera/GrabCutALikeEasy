Option Strict On

Imports System.Drawing.Imaging
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices
Imports System.Drawing
Imports System.Threading

Partial Public Class Convolution
    'Der Sigmafilter funktioniert normalerweise nur mit ungewichteten Kernels. Bei gewichteten kommt es zwangsläufig zu Fehlern, die sich
    'in Farbfehlern, oder haloartigen Strukturen zeigen. Man kann aber ganz ordentliche Ergebnisse erzielen, durch das setzen der Variable
    'SrcOnSigma (auf true), dann wird bei entsprechenden Abständen schlichtweg der (Farb-)Wert aus dem Originalbild genommen.
    Public Function Convolve_par_adjusted(b As Bitmap, Kernel As Double(,), AddVals As Double(,), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, logarithmic As Boolean) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 44L) Then
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
            bSrc = CType(b.Clone(), Bitmap)
            bmData = b.LockBits(New Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb)
            bmSrc = bSrc.LockBits(New Rectangle(0, 0, bSrc.Width, bSrc.Height), ImageLockMode.[ReadOnly], PixelFormat.Format32bppArgb)
            Dim stride As Integer = bmData.Stride

            Dim Scan0 As System.IntPtr = bmData.Scan0
            Dim SrcScan0 As System.IntPtr = bmSrc.Scan0

            Dim nWidth As Integer = b.Width
            Dim nHeight As Integer = b.Height

            Dim llh As Integer = h * stride
            Dim lh As Integer = h * 4

            If Not logarithmic Then
                Dim p((bmData.Stride * bmData.Height) - 1) As Byte
                Marshal.Copy(bmData.Scan0, p, 0, p.Length)

                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Marshal.Copy(bmSrc.Scan0, pSrc, 0, pSrc.Length)

                '#Region "First Part"

                If Not IgnoreEdges Then
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
                        p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias)), 255), 0), [Byte])
                        p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias)), 255), 0), [Byte])
                        p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias)), 255), 0), [Byte])
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias)), 255), 0), [Byte])
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + ll)
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
                            p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias)), 255), 0), [Byte])
                            p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias)), 255), 0), [Byte])
                            p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias)), 255), 0), [Byte])
                            If DoTrans Then
                                p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias)), 255), 0), [Byte])
                            End If

                            If SrcOnSigma Then
                                If ignore Then
                                    p(pos + 0) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
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
                        posSrc = 0

                        pos += (nWidth - h - 1) * 4
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
                        p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias)), 255), 0), [Byte])
                        p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias)), 255), 0), [Byte])
                        p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias)), 255), 0), [Byte])
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias)), 255), 0), [Byte])
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + lh)
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
                End If
                '#End Region

                '#Region "Main Body"

                'for (int y = 0; y < nHeight - Kernel.GetLength(1) + 1; y++)
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
                                                                         If Not IgnoreEdges Then
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

                                                                                 Dim r As Integer = 0, rc As Integer = 0
                                                                                 While r < Kernel.GetLength(1)
                                                                                     Dim lr As Integer = rc * stride

                                                                                     Dim c As Integer = h - l, cc As Integer = 0
                                                                                     While c < Kernel.GetLength(0)
                                                                                         z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                         Dim lcc As Integer = cc * 4

                                                                                         If Math.Abs(CInt(pSrc(posSrc + llh + ll)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                             fSum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount += z
                                                                                         Else
                                                                                             fIgnore = True
                                                                                         End If

                                                                                         If Math.Abs(CInt(pSrc(posSrc + 1 + llh + ll)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                             fSum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount2 += z
                                                                                         Else
                                                                                             fIgnore2 = True
                                                                                         End If

                                                                                         If Math.Abs(CInt(pSrc(posSrc + 2 + llh + ll)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                             fSum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount3 += z
                                                                                         Else
                                                                                             fIgnore3 = True
                                                                                         End If

                                                                                         If DoTrans Then
                                                                                             If Math.Abs(CInt(pSrc(posSrc + 3 + llh + ll)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                                 fSum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                                                                                 fCount4 += z
                                                                                             Else
                                                                                                 fIgnore4 = True
                                                                                             End If
                                                                                         End If

                                                                                         If r = h AndAlso c = h Then
                                                                                             KfSum += Kernel(c, r) + AddVals(l, r)
                                                                                         Else
                                                                                             KfSum += Kernel(c, r)
                                                                                         End If
                                                                                         c += 1
                                                                                         cc += 1
                                                                                     End While
                                                                                     r += 1
                                                                                     rc += 1
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
                                                                                 p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias)), 255), 0), [Byte])
                                                                                 p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias)), 255), 0), [Byte])
                                                                                 p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias)), 255), 0), [Byte])
                                                                                 If DoTrans Then
                                                                                     p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias)), 255), 0), [Byte])
                                                                                 End If

                                                                                 If SrcOnSigma Then
                                                                                     If fIgnore Then
                                                                                         p(pos + 0) = pSrc(posSrc + llh + ll)
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
                                                                         End If
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

                                                                             If LineRanges Is Nothing OrElse (LineRanges.[Get](y * nWidth + x) = True AndAlso LineRanges.[Get]((y + h) * nWidth + x + h) = True) Then
                                                                                 For r As Integer = 0 To Kernel.GetLength(1) - 1
                                                                                     Dim llr As Integer = r * stride

                                                                                     For c As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                         Dim lc As Integer = c * 4

                                                                                         If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + llr + lc))) <= Sigma Then
                                                                                             fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(c, r))
                                                                                             fCount += z
                                                                                         Else
                                                                                             fIgnore = True
                                                                                         End If

                                                                                         If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + llr + lc))) <= Sigma Then
                                                                                             fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(c, r))
                                                                                             fCount2 += z
                                                                                         Else
                                                                                             fIgnore2 = True
                                                                                         End If

                                                                                         If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + llr + lc))) <= Sigma Then
                                                                                             fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(c, r))
                                                                                             fCount3 += z
                                                                                         Else
                                                                                             fIgnore3 = True
                                                                                         End If

                                                                                         If DoTrans Then
                                                                                             If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + llr + lc))) <= Sigma Then
                                                                                                 fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(c, r))
                                                                                                 fCount4 += z
                                                                                             Else
                                                                                                 fIgnore4 = True
                                                                                             End If
                                                                                         End If

                                                                                         KfSum += Kernel(c, r)
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
                                                                                 p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias)), 255), 0), [Byte])
                                                                                 p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias)), 255), 0), [Byte])
                                                                                 p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias)), 255), 0), [Byte])
                                                                                 If DoTrans Then
                                                                                     p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias)), 255), 0), [Byte])
                                                                                 End If

                                                                                 If SrcOnSigma Then
                                                                                     If fIgnore Then
                                                                                         p(pos + 0) = pSrc(posSrc + llh + lh)
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
                                                                             End If
                                                                         Next
                                                                         '#End Region

                                                                         '#Region "Last Pixels"
                                                                         If Not IgnoreEdges Then
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

                                                                                 Dim r As Integer = 0, rc As Integer = 0
                                                                                 While r < Kernel.GetLength(1)
                                                                                     Dim lr As Integer = rc * stride

                                                                                     Dim c As Integer = 0, cc As Integer = 0
                                                                                     While c < nWidth - l
                                                                                         z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                         Dim lcc As Integer = cc * 4

                                                                                         If Math.Abs(CInt(pSrc(posSrc + llh + lh)) - CInt(pSrc(posSrc + lr + lcc))) <= Sigma Then
                                                                                             fSum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount += z
                                                                                         Else
                                                                                             fIgnore = True
                                                                                         End If

                                                                                         If Math.Abs(CInt(pSrc(posSrc + 1 + llh + lh)) - CInt(pSrc(posSrc + 1 + lr + lcc))) <= Sigma Then
                                                                                             fSum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount2 += z
                                                                                         Else
                                                                                             fIgnore2 = True
                                                                                         End If

                                                                                         If Math.Abs(CInt(pSrc(posSrc + 2 + llh + lh)) - CInt(pSrc(posSrc + 2 + lr + lcc))) <= Sigma Then
                                                                                             fSum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount3 += z
                                                                                         Else
                                                                                             fIgnore3 = True
                                                                                         End If

                                                                                         If DoTrans Then
                                                                                             If Math.Abs(CInt(pSrc(posSrc + 3 + llh + lh)) - CInt(pSrc(posSrc + 3 + lr + lcc))) <= Sigma Then
                                                                                                 fSum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                                                                                 fCount4 += z
                                                                                             Else
                                                                                                 fIgnore4 = True
                                                                                             End If
                                                                                         End If

                                                                                         If r = h AndAlso c = h Then
                                                                                             KfSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r)
                                                                                         Else
                                                                                             KfSum += Kernel(c, r)
                                                                                         End If
                                                                                         c += 1
                                                                                         cc += 1
                                                                                     End While
                                                                                     r += 1
                                                                                     rc += 1
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
                                                                                 p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)) + Bias)), 255), 0), [Byte])
                                                                                 p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)) + Bias)), 255), 0), [Byte])
                                                                                 p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)) + Bias)), 255), 0), [Byte])
                                                                                 If DoTrans Then
                                                                                     p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)) + Bias)), 255), 0), [Byte])
                                                                                 End If

                                                                                 If SrcOnSigma Then
                                                                                     If fIgnore Then
                                                                                         p(pos + 0) = pSrc(posSrc + llh + lh)
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
                                                                         End If

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
                '#End Region

                '#Region "Last Part"
                If Not IgnoreEdges Then
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

                        Dim r As Integer = 0, rc As Integer = 0
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
                        p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias)), 255), 0), [Byte])
                        p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias)), 255), 0), [Byte])
                        p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias)), 255), 0), [Byte])
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias)), 255), 0), [Byte])
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + ll)
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

                            pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                            posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                            If x > Kernel.GetLength(0) - (h + 1) Then
                                pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                                posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                            End If

                            r = h - l
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
                            p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias)), 255), 0), [Byte])
                            p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias)), 255), 0), [Byte])
                            p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias)), 255), 0), [Byte])
                            If DoTrans Then
                                p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias)), 255), 0), [Byte])
                            End If

                            If SrcOnSigma Then
                                If ignore Then
                                    p(pos + 0) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
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
                        posSrc = 0

                        pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                        pos += (nWidth - h - 1) * 4

                        posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                        posSrc += (nWidth - h - 1) * 4

                        r = h - l
                        rc = 0
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
                        p(pos + 0) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)) + Bias)), 255), 0), [Byte])
                        p(pos + 1) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)) + Bias)), 255), 0), [Byte])
                        p(pos + 2) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)) + Bias)), 255), 0), [Byte])
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min(CInt(Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)) + Bias)), 255), 0), [Byte])
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + lh)
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
                End If

                Marshal.Copy(p, 0, bmData.Scan0, p.Length)

                p = Nothing
                pSrc = Nothing
            Else
                Dim p0((bmData.Stride * bmData.Height) - 1) As Byte
                Dim nL As Integer = p0.Length
                Marshal.Copy(bmData.Scan0, p0, 0, p0.Length)
                Dim p((bmData.Stride * bmData.Height) - 1) As Single
                Parallel.For(0, nL, Sub(i)
                                        p(i) = CSng(p0(i))
                                    End Sub)
                p0 = Nothing

                Dim pSrc0((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Dim nLSrc As Integer = pSrc0.Length
                Marshal.Copy(bmSrc.Scan0, pSrc0, 0, pSrc0.Length)
                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Single
                Parallel.For(0, nLSrc, Sub(i)
                                           pSrc(i) = CSng(pSrc0(i))
                                       End Sub)
                pSrc0 = Nothing

                GetLogImage(p, pSrc)

                Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
                Dim Sigma2 As Double = Math.Log(1 + Sigma) * f

                '#Region "First Part"

                If Not IgnoreEdges Then
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

                                If Math.Abs(CDbl(pSrc(posSrc + lf + ll)) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                    Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                    count += z
                                Else
                                    ignore = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 1 + lf + ll)) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                    Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                    count2 += z
                                Else
                                    ignore2 = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 2 + lf + ll)) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                    Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                    count3 += z
                                Else
                                    ignore3 = True
                                End If

                                If DoTrans Then
                                    If Math.Abs(CDbl(pSrc(posSrc + 3 + lf + ll)) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
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
                        p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)))), 255), 0), Single)
                        p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)))), 255), 0), Single)
                        p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)))), 255), 0), Single)
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)))), 255), 0), Single)
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + ll)
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

                                    If Math.Abs(CDbl(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                        Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                        count += z
                                    Else
                                        ignore = True
                                    End If

                                    If Math.Abs(CDbl(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                        Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                        count2 += z
                                    Else
                                        ignore2 = True
                                    End If

                                    If Math.Abs(CDbl(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                        Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), l))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                        count3 += z
                                    Else
                                        ignore3 = True
                                    End If

                                    If DoTrans Then
                                        If Math.Abs(CDbl(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
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
                            p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)))), 255), 0), Single)
                            p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)))), 255), 0), Single)
                            p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)))), 255), 0), Single)
                            If DoTrans Then
                                p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)))), 255), 0), Single)
                            End If

                            If SrcOnSigma Then
                                If ignore Then
                                    p(pos + 0) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
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
                        posSrc = 0

                        pos += (nWidth - h - 1) * 4
                        posSrc += (nWidth - h - 1) * 4

                        r = h - l
                        rc = 0
                        While r < Kernel.GetLength(1)
                            Dim lr As Integer = rc * stride

                            Dim c As Integer = 0, cc As Integer = 0
                            While c < Kernel.GetLength(0) - h
                                Dim lcc As Integer = cc * 4

                                z = 1.0 / ((Kernel.GetLength(1) - (h - l)) * (Kernel.GetLength(0) - h))

                                If Math.Abs(CDbl(pSrc(posSrc + lf + lh)) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                    Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                    count += z
                                Else
                                    ignore = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 1 + lf + lh)) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                    Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                    count2 += z
                                Else
                                    ignore2 = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 2 + lf + lh)) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                    Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, l))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                    count3 += z
                                Else
                                    ignore3 = True
                                End If

                                If DoTrans Then
                                    If Math.Abs(CDbl(pSrc(posSrc + 3 + lf + lh)) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
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
                        p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)))), 255), 0), Single)
                        p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)))), 255), 0), Single)
                        p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)))), 255), 0), Single)
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)))), 255), 0), Single)
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + lh)
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
                End If
                '#End Region

                '#Region "Main Body"

                'for (int y = 0; y < nHeight - Kernel.GetLength(1) + 1; y++)
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
                                                                         If Not IgnoreEdges Then
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

                                                                                 Dim r As Integer = 0, rc As Integer = 0
                                                                                 While r < Kernel.GetLength(1)
                                                                                     Dim lr As Integer = rc * stride

                                                                                     Dim c As Integer = h - l, cc As Integer = 0
                                                                                     While c < Kernel.GetLength(0)
                                                                                         z = 1.0 / (Kernel.GetLength(1) * (Kernel.GetLength(0) - (h - l)))

                                                                                         Dim lcc As Integer = cc * 4

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + llh + ll)) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                                                                             fSum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount += z
                                                                                         Else
                                                                                             fIgnore = True
                                                                                         End If

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + 1 + llh + ll)) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                                                                             fSum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount2 += z
                                                                                         Else
                                                                                             fIgnore2 = True
                                                                                         End If

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + 2 + llh + ll)) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                                                                             fSum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount3 += z
                                                                                         Else
                                                                                             fIgnore3 = True
                                                                                         End If

                                                                                         If DoTrans Then
                                                                                             If Math.Abs(CDbl(pSrc(posSrc + 3 + llh + ll)) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
                                                                                                 fSum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(l, r))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                                                                                 fCount4 += z
                                                                                             Else
                                                                                                 fIgnore4 = True
                                                                                             End If
                                                                                         End If

                                                                                         If r = h AndAlso c = h Then
                                                                                             KfSum += Kernel(c, r) + AddVals(l, r)
                                                                                         Else
                                                                                             KfSum += Kernel(c, r)
                                                                                         End If
                                                                                         c += 1
                                                                                         cc += 1
                                                                                     End While
                                                                                     r += 1
                                                                                     rc += 1
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
                                                                                 p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)))), 255), 0), Single)
                                                                                 p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)))), 255), 0), Single)
                                                                                 p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)))), 255), 0), Single)
                                                                                 If DoTrans Then
                                                                                     p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)))), 255), 0), Single)
                                                                                 End If

                                                                                 If SrcOnSigma Then
                                                                                     If fIgnore Then
                                                                                         p(pos + 0) = pSrc(posSrc + llh + ll)
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
                                                                         End If
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

                                                                             If LineRanges Is Nothing OrElse (LineRanges.[Get](y * nWidth + x) = True AndAlso LineRanges.[Get]((y + h) * nWidth + x + h) = True) Then
                                                                                 For r As Integer = 0 To Kernel.GetLength(1) - 1
                                                                                     Dim llr As Integer = r * stride

                                                                                     For c As Integer = 0 To Kernel.GetLength(0) - 1
                                                                                         Dim lc As Integer = c * 4

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + llh + lh)) - (pSrc(posSrc + llr + lc))) <= Sigma2 Then
                                                                                             fSum += (CDbl(pSrc(posSrc + llr + lc)) * Kernel(c, r))
                                                                                             fCount += z
                                                                                         Else
                                                                                             fIgnore = True
                                                                                         End If

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + 1 + llh + lh)) - (pSrc(posSrc + 1 + llr + lc))) <= Sigma2 Then
                                                                                             fSum2 += (CDbl(pSrc(posSrc + 1 + llr + lc)) * Kernel(c, r))
                                                                                             fCount2 += z
                                                                                         Else
                                                                                             fIgnore2 = True
                                                                                         End If

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + 2 + llh + lh)) - (pSrc(posSrc + 2 + llr + lc))) <= Sigma2 Then
                                                                                             fSum3 += (CDbl(pSrc(posSrc + 2 + llr + lc)) * Kernel(c, r))
                                                                                             fCount3 += z
                                                                                         Else
                                                                                             fIgnore3 = True
                                                                                         End If

                                                                                         If DoTrans Then
                                                                                             If Math.Abs(CDbl(pSrc(posSrc + 3 + llh + lh)) - (pSrc(posSrc + 3 + llr + lc))) <= Sigma2 Then
                                                                                                 fSum4 += (CDbl(pSrc(posSrc + 3 + llr + lc)) * Kernel(c, r))
                                                                                                 fCount4 += z
                                                                                             Else
                                                                                                 fIgnore4 = True
                                                                                             End If
                                                                                         End If

                                                                                         KfSum += Kernel(c, r)
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
                                                                                 p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)))), 255), 0), Single)
                                                                                 p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)))), 255), 0), Single)
                                                                                 p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)))), 255), 0), Single)
                                                                                 If DoTrans Then
                                                                                     p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)))), 255), 0), Single)
                                                                                 End If

                                                                                 If SrcOnSigma Then
                                                                                     If fIgnore Then
                                                                                         p(pos + 0) = pSrc(posSrc + llh + lh)
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
                                                                             End If
                                                                         Next
                                                                         '#End Region

                                                                         '#Region "Last Pixels"
                                                                         If Not IgnoreEdges Then
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

                                                                                 Dim r As Integer = 0, rc As Integer = 0
                                                                                 While r < Kernel.GetLength(1)
                                                                                     Dim lr As Integer = rc * stride

                                                                                     Dim c As Integer = 0, cc As Integer = 0
                                                                                     While c < nWidth - l
                                                                                         z = 1.0 / (Kernel.GetLength(1) * (nWidth - l))

                                                                                         Dim lcc As Integer = cc * 4

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + llh + lh)) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                                                                             fSum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount += z
                                                                                         Else
                                                                                             fIgnore = True
                                                                                         End If

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + 1 + llh + lh)) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                                                                             fSum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount2 += z
                                                                                         Else
                                                                                             fIgnore2 = True
                                                                                         End If

                                                                                         If Math.Abs(CDbl(pSrc(posSrc + 2 + llh + lh)) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                                                                             fSum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                                                                             fCount3 += z
                                                                                         Else
                                                                                             fIgnore3 = True
                                                                                         End If

                                                                                         If DoTrans Then
                                                                                             If Math.Abs(CDbl(pSrc(posSrc + 3 + llh + lh)) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
                                                                                                 fSum4 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r))), (CDbl(pSrc(posSrc + 3 + lr + lcc)) * Kernel(c, r)))
                                                                                                 fCount4 += z
                                                                                             Else
                                                                                                 fIgnore4 = True
                                                                                             End If
                                                                                         End If

                                                                                         If r = h AndAlso c = h Then
                                                                                             KfSum += Kernel(c, r) + AddVals(Kernel.GetLength(0) - (nWidth - l) + h, r)
                                                                                         Else
                                                                                             KfSum += Kernel(c, r)
                                                                                         End If
                                                                                         c += 1
                                                                                         cc += 1
                                                                                     End While
                                                                                     r += 1
                                                                                     rc += 1
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
                                                                                 p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount)))), 255), 0), Single)
                                                                                 p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum2 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount2)))), 255), 0), Single)
                                                                                 p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum3 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount3)))), 255), 0), Single)
                                                                                 If DoTrans Then
                                                                                     p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((fSum4 / KfSum), Int32.MaxValue), Int32.MinValue) / CDbl(fCount4)))), 255), 0), Single)
                                                                                 End If

                                                                                 If SrcOnSigma Then
                                                                                     If fIgnore Then
                                                                                         p(pos + 0) = pSrc(posSrc + llh + lh)
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
                                                                         End If

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
                '#End Region

                '#Region "Last Part"
                If Not IgnoreEdges Then
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

                        Dim r As Integer = 0, rc As Integer = 0
                        While r < Kernel.GetLength(1) - (l + 1)
                            Dim lr As Integer = rc * stride

                            Dim c As Integer = h, cc As Integer = 0
                            While c < Kernel.GetLength(0)
                                z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                                Dim lcc As Integer = cc * 4

                                If Math.Abs(CDbl(pSrc(posSrc + lf + ll)) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                    Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                    count += z
                                Else
                                    ignore = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 1 + lf + ll)) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                    Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                    count2 += z
                                Else
                                    ignore2 = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 2 + lf + ll)) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                    Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(0, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                    count3 += z
                                Else
                                    ignore3 = True
                                End If

                                If DoTrans Then
                                    If Math.Abs(CDbl(pSrc(posSrc + 3 + lf + ll)) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
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
                        p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)))), 255), 0), Single)
                        p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)))), 255), 0), Single)
                        p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)))), 255), 0), Single)
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)))), 255), 0), Single)
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + ll)
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

                            pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                            posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride

                            If x > Kernel.GetLength(0) - (h + 1) Then
                                pos += (x - Kernel.GetLength(0) + (h + 1)) * 4
                                posSrc += (x - Kernel.GetLength(0) + (h + 1)) * 4
                            End If

                            r = h - l
                            rc = 0
                            While r < Kernel.GetLength(1) - (l + 1)
                                Dim lr As Integer = rc * stride

                                Dim c As Integer = Math.Max(h - x, 0), cc As Integer = 0
                                While If((x - h + Kernel.GetLength(0) <= b.Width), c < Kernel.GetLength(0), c < (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                                    Dim lcc As Integer = cc * 4

                                    Dim zz As Double = If((x - h + Kernel.GetLength(0) <= b.Width), Kernel.GetLength(0), (Kernel.GetLength(0) - (x - h + Kernel.GetLength(0) - b.Width)))
                                    z = 1.0 / ((zz - Math.Max(h - x, 0)) * (Kernel.GetLength(1) - (l + 1)))

                                    If Math.Abs(CDbl(pSrc(posSrc + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                        Sum += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                        count += z
                                    Else
                                        ignore = True
                                    End If

                                    If Math.Abs(CDbl(pSrc(posSrc + 1 + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                        Sum2 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                        count2 += z
                                    Else
                                        ignore2 = True
                                    End If

                                    If Math.Abs(CDbl(pSrc(posSrc + 2 + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                        Sum3 += If((r = h AndAlso c = h), (If((x - h + Kernel.GetLength(0) <= b.Width), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Math.Min(x, h), h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals((x + Kernel.GetLength(0) - b.Width), h + l + 1))))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                        count3 += z
                                    Else
                                        ignore3 = True
                                    End If

                                    If DoTrans Then
                                        If Math.Abs(CDbl(pSrc(posSrc + 3 + lf + (Math.Min(x, h) * 4))) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
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
                            p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)))), 255), 0), Single)
                            p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)))), 255), 0), Single)
                            p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)))), 255), 0), Single)
                            If DoTrans Then
                                p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)))), 255), 0), Single)
                            End If

                            If SrcOnSigma Then
                                If ignore Then
                                    p(pos + 0) = pSrc(posSrc + lf + (Math.Min(x, h) * 4))
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
                        posSrc = 0

                        pos += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                        pos += (nWidth - h - 1) * 4

                        posSrc += (nHeight - Kernel.GetLength(1) + l + 1) * stride
                        posSrc += (nWidth - h - 1) * 4

                        r = h - l
                        rc = 0
                        While r < Kernel.GetLength(1) - (l + 1)
                            Dim lr As Integer = rc * stride

                            Dim c As Integer = 0, cc As Integer = 0
                            While c < Kernel.GetLength(0) - h
                                z = 1.0 / ((Kernel.GetLength(1) - (l + 1)) * (Kernel.GetLength(0) - h))

                                Dim lcc As Integer = cc * 4

                                If Math.Abs(CDbl(pSrc(posSrc + lf + lh)) - (pSrc(posSrc + lr + lcc))) <= Sigma2 Then
                                    Sum += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + lr + lcc)) * Kernel(c, r)))
                                    count += z
                                Else
                                    ignore = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 1 + lf + lh)) - (pSrc(posSrc + 1 + lr + lcc))) <= Sigma2 Then
                                    Sum2 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 1 + lr + lcc)) * Kernel(c, r)))
                                    count2 += z
                                Else
                                    ignore2 = True
                                End If

                                If Math.Abs(CDbl(pSrc(posSrc + 2 + lf + lh)) - (pSrc(posSrc + 2 + lr + lcc))) <= Sigma2 Then
                                    Sum3 += If((r = h AndAlso c = h), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * (Kernel(c, r) + AddVals(Kernel.GetLength(0) - 1, h + l + 1))), (CDbl(pSrc(posSrc + 2 + lr + lcc)) * Kernel(c, r)))
                                    count3 += z
                                Else
                                    ignore3 = True
                                End If

                                If DoTrans Then
                                    If Math.Abs(CDbl(pSrc(posSrc + 3 + lf + lh)) - (pSrc(posSrc + 3 + lr + lcc))) <= Sigma2 Then
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
                        p(pos + 0) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count)))), 255), 0), Single)
                        p(pos + 1) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum2 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count2)))), 255), 0), Single)
                        p(pos + 2) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum3 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count3)))), 255), 0), Single)
                        If DoTrans Then
                            p(pos + 3) = CType(Math.Max(Math.Min((Math.Ceiling((Math.Max(Math.Min((Sum4 / KSum), Int32.MaxValue), Int32.MinValue) / CDbl(count4)))), 255), 0), Single)
                        End If

                        If SrcOnSigma Then
                            If ignore Then
                                p(pos + 0) = pSrc(posSrc + lf + lh)
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
                End If

                pSrc = Nothing
                Dim p4() As Byte = GetExpImage(p, Bias)
                p = Nothing

                Marshal.Copy(p4, 0, bmData.Scan0, p4.Length)

                p0 = Nothing
                pSrc0 = Nothing
            End If
            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

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

    Public Function ConvolveH_par_SigmaAsDistance(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, logarithmic As Boolean) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 44L) Then
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

            If Not logarithmic Then
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
                p = Nothing
                pSrc = Nothing
            Else
                Dim p0((bmData.Stride * bmData.Height) - 1) As Byte
                Dim nL As Integer = p0.Length
                Marshal.Copy(bmData.Scan0, p0, 0, p0.Length)
                Dim p((bmData.Stride * bmData.Height) - 1) As Single
                Parallel.For(0, nL, Sub(i)
                                        p(i) = CSng(p0(i))
                                    End Sub)
                p0 = Nothing

                Dim pSrc0((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Dim nLSrc As Integer = pSrc0.Length
                Marshal.Copy(bmSrc.Scan0, pSrc0, 0, pSrc0.Length)
                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Single
                Parallel.For(0, nLSrc, Sub(i)
                                           pSrc(i) = CSng(pSrc0(i))
                                       End Sub)
                pSrc0 = Nothing

                GetLogImage(p, pSrc)

                Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
                Dim Sigma2 As Double = Math.Log(1 + Sigma * 2) * f

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

                                                       Dim distance As Double = Math.Sqrt((CDbl(pSrc(posSrc + (l * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) * (CDbl(pSrc(posSrc + (l * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) + (CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) * (CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) + (CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) * (CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) + (CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) * (CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))))

                                                       If distance <= Sigma2 Then
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
                                                   p(pos) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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
                                                       Dim distance As Double = Math.Sqrt((CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (c * 4)))) * (CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (c * 4)))) + (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) * (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) + (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) * (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) + (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))) * (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))))

                                                       If distance <= Sigma2 Then
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
                                                   p(pos) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                                                       Dim distance As Double = Math.Sqrt((CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) * (CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) + (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) * (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) + (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) * (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) + (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) * (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))))

                                                       If distance <= Sigma2 Then
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
                                                   p(pos) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                pSrc = Nothing
                Dim p4() As Byte = GetExpImage(p, Bias)
                p = Nothing

                Marshal.Copy(p4, 0, bmData.Scan0, p4.Length)

                p0 = Nothing
                pSrc0 = Nothing
            End If

            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

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

    Public Function ConvolveH_Par(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, logarithmic As Boolean) As Boolean

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 44L) Then
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

            If Not logarithmic Then
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

                p = Nothing
                pSrc = Nothing
            Else
                Dim p0((bmData.Stride * bmData.Height) - 1) As Byte
                Dim nL As Integer = p0.Length
                Marshal.Copy(bmData.Scan0, p0, 0, p0.Length)
                Dim p((bmData.Stride * bmData.Height) - 1) As Single
                Parallel.For(0, nL, Sub(i)
                                        p(i) = CSng(p0(i))
                                    End Sub)
                p0 = Nothing

                Dim pSrc0((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Dim nLSrc As Integer = pSrc0.Length
                Marshal.Copy(bmSrc.Scan0, pSrc0, 0, pSrc0.Length)
                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Single
                Parallel.For(0, nLSrc, Sub(i)
                                           pSrc(i) = CSng(pSrc0(i))
                                       End Sub)
                pSrc0 = Nothing

                GetLogImage(p, pSrc)

                Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
                Dim Sigma2 As Double = Math.Log(1 + Sigma) * f

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

                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (l * 4))) - CDbl(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma2 Then
                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma2 Then
                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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
                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (h * 4))) - CDbl(pSrc(posSrc + 0 + (c * 4)))) <= Sigma2 Then
                                                           Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) <= Sigma2 Then
                                                           Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) <= Sigma2 Then
                                                           Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (h * 4))) - CDbl(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma2 Then
                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma2 Then
                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                pSrc = Nothing
                Dim p4() As Byte = GetExpImage(p, Bias)
                p = Nothing

                Marshal.Copy(p4, 0, bmData.Scan0, p4.Length)

                p0 = Nothing
                pSrc0 = Nothing
            End If

            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

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

    Public Function ConvolveH_Par_ER(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, logarithmic As Boolean) As Boolean

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 44L) Then
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
            Return True
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

            If Not logarithmic Then
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

                p = Nothing
                pSrc = Nothing
            Else
                Dim p0((bmData.Stride * bmData.Height) - 1) As Byte
                Dim nL As Integer = p0.Length
                Marshal.Copy(bmData.Scan0, p0, 0, p0.Length)
                Dim p((bmData.Stride * bmData.Height) - 1) As Single
                Parallel.For(0, nL, Sub(i)
                                        p(i) = CSng(p0(i))
                                    End Sub)
                p0 = Nothing

                Dim pSrc0((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Dim nLSrc As Integer = pSrc0.Length
                Marshal.Copy(bmSrc.Scan0, pSrc0, 0, pSrc0.Length)
                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Single
                Parallel.For(0, nLSrc, Sub(i)
                                           pSrc(i) = CSng(pSrc0(i))
                                       End Sub)
                pSrc0 = Nothing

                GetLogImage(p, pSrc)

                Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
                Dim Sigma2 As Double = Math.Log(1 + Sigma) * f

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

                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (l * 4))) - CDbl(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma2 Then
                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma2 Then
                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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
                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (h * 4))) - CDbl(pSrc(posSrc + 0 + (c * 4)))) <= Sigma2 Then
                                                           Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) <= Sigma2 Then
                                                           Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) <= Sigma2 Then
                                                           Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (h * 4))) - CDbl(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma2 Then
                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma2 Then
                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                pSrc = Nothing
                Dim p4() As Byte = GetExpImage(p, Bias)
                p = Nothing

                Marshal.Copy(p4, 0, bmData.Scan0, p4.Length)

                p0 = Nothing
                pSrc0 = Nothing
            End If

            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

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

    'extends the border color outside
    Public Function ConvolveH_Par_WithAlphaCheck(b As Bitmap, Kernel As Double(), AddVals As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, logarithmic As Boolean,
                                                 alphaThreshold As Integer) As Boolean

        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 44L) Then
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

            If Not logarithmic Then
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

                                                   If IsDataToProcess(pSrc, posSrc, Kernel.Length) Then
                                                       For c As Integer = 0 To Kernel.Length - 1
                                                           If Math.Abs(CInt(pSrc(posSrc + 0 + (h * 4))) - CInt(pSrc(posSrc + 0 + (c * 4)))) <= Sigma Then
                                                               If pSrc(posSrc + 3 + (c * 4)) = 0 Then
                                                                   Dim d As Double = GetBorderValue(pSrc, posSrc + h * 4, h, x, 0, nWidth, c, alphaThreshold)
                                                                   Sum += (d * Kernel(c))
                                                               Else
                                                                   Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                               End If
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 1 + (h * 4))) - CInt(pSrc(posSrc + 1 + (c * 4)))) <= Sigma Then
                                                               If pSrc(posSrc + 3 + (c * 4)) = 0 Then
                                                                   Dim d As Double = GetBorderValue(pSrc, posSrc + h * 4, h, x, 1, nWidth, c, alphaThreshold)
                                                                   Sum2 += (d * Kernel(c))
                                                               Else
                                                                   Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                               End If
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CInt(pSrc(posSrc + 2 + (h * 4))) - CInt(pSrc(posSrc + 2 + (c * 4)))) <= Sigma Then
                                                               If pSrc(posSrc + 3 + (c * 4)) = 0 Then
                                                                   Dim d As Double = GetBorderValue(pSrc, posSrc + h * 4, h, x, 2, nWidth, c, alphaThreshold)
                                                                   Sum3 += (d * Kernel(c))
                                                               Else
                                                                   Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                               End If
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

                p = Nothing
                pSrc = Nothing
            Else
                Dim p0((bmData.Stride * bmData.Height) - 1) As Byte
                Dim nL As Integer = p0.Length
                Marshal.Copy(bmData.Scan0, p0, 0, p0.Length)
                Dim p((bmData.Stride * bmData.Height) - 1) As Single
                Parallel.For(0, nL, Sub(i)
                                        p(i) = CSng(p0(i))
                                    End Sub)
                p0 = Nothing

                Dim pSrc0((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Dim nLSrc As Integer = pSrc0.Length
                Marshal.Copy(bmSrc.Scan0, pSrc0, 0, pSrc0.Length)
                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Single
                Parallel.For(0, nLSrc, Sub(i)
                                           pSrc(i) = CSng(pSrc0(i))
                                       End Sub)
                pSrc0 = Nothing

                GetLogImage(p, pSrc)

                Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
                Dim Sigma2 As Double = Math.Log(1 + Sigma) * f

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

                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (l * 4))) - CDbl(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma2 Then
                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma2 Then
                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                                                   If IsDataToProcess(pSrc, posSrc, Kernel.Length) Then
                                                       For c As Integer = 0 To Kernel.Length - 1
                                                           If Math.Abs(CDbl(pSrc(posSrc + 0 + (h * 4))) - CDbl(pSrc(posSrc + 0 + (c * 4)))) <= Sigma2 Then
                                                               If pSrc(posSrc + 3 + (c * 4)) = 0 Then
                                                                   Dim d As Double = GetBorderValue(pSrc, posSrc + h * 4, h, x, 0, nWidth, c, alphaThreshold)
                                                                   Sum += (d * Kernel(c))
                                                               Else
                                                                   Sum += (CDbl(pSrc(posSrc + 0 + (c * 4))) * Kernel(c))
                                                               End If
                                                               count += z
                                                           Else
                                                               ignore = True
                                                           End If

                                                           If Math.Abs(CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) <= Sigma2 Then
                                                               If pSrc(posSrc + 3 + (c * 4)) = 0 Then
                                                                   Dim d As Double = GetBorderValue(pSrc, posSrc + h * 4, h, x, 1, nWidth, c, alphaThreshold)
                                                                   Sum2 += (d * Kernel(c))
                                                               Else
                                                                   Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c))
                                                               End If
                                                               count2 += z
                                                           Else
                                                               ignore2 = True
                                                           End If

                                                           If Math.Abs(CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) <= Sigma2 Then
                                                               If pSrc(posSrc + 3 + (c * 4)) = 0 Then
                                                                   Dim d As Double = GetBorderValue(pSrc, posSrc + h * 4, h, x, 2, nWidth, c, alphaThreshold)
                                                                   Sum3 += (d * Kernel(c))
                                                               Else
                                                                   Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c))
                                                               End If
                                                               count3 += z
                                                           Else
                                                               ignore3 = True
                                                           End If

                                                           If DoTrans Then
                                                               If Math.Abs(CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))) <= Sigma2 Then
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
                                                       p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                       p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                       p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                       If DoTrans Then
                                                           p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                                                       If Math.Abs(CDbl(pSrc(posSrc + 0 + (h * 4))) - CDbl(pSrc(posSrc + 0 + (cc * 4)))) <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 0 + (cc * 4))) * Kernel(c)))
                                                           count += z
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) <= Sigma2 Then
                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c)))
                                                           count2 += z
                                                       Else
                                                           ignore2 = True
                                                       End If

                                                       If Math.Abs(CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) <= Sigma2 Then
                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h))), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c)))
                                                           count3 += z
                                                       Else
                                                           ignore3 = True
                                                       End If

                                                       If DoTrans Then
                                                           If Math.Abs(CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) <= Sigma2 Then
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
                                                   p(pos + 0) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                pSrc = Nothing
                Dim p4() As Byte = GetExpImage(p, Bias)
                p = Nothing

                Marshal.Copy(p4, 0, bmData.Scan0, p4.Length)

                p0 = Nothing
                pSrc0 = Nothing
            End If

            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

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

    Private Function GetBorderValue(pSrc() As Byte, posSrc As Integer, h As Integer, x As Integer, plane As Integer, width As Integer,
                                    c As Integer, alphaThreshold As Integer) As Double
        For i As Integer = 0 To h
            If x + i >= 0 AndAlso x + i < width Then
                If pSrc(posSrc + 3 + (i * 4)) > alphaThreshold Then
                    Return CDbl(pSrc(posSrc + plane + (i * 4)))
                End If
            End If
        Next

        For i As Integer = 0 To -h Step -1
            If x + i >= 0 AndAlso x + i < width Then
                If pSrc(posSrc + 3 + (i * 4)) > alphaThreshold Then
                    Return CDbl(pSrc(posSrc + plane + (i * 4)))
                End If
            End If
        Next

        Return CDbl(pSrc(posSrc + plane + (c * 4)))
    End Function

    Private Function GetBorderValue(pSrc() As Single, posSrc As Integer, h As Integer, x As Integer, plane As Integer, width As Integer,
                                    c As Integer, alphaThreshold As Integer) As Double
        For i As Integer = 0 To h
            If x + i >= 0 AndAlso x + i < width Then
                If pSrc(posSrc + 3 + (i * 4)) > alphaThreshold Then
                    Return CDbl(pSrc(posSrc + plane + (i * 4)))
                End If
            End If
        Next

        For i As Integer = 0 To -h Step -1
            If x + i >= 0 AndAlso x + i < width Then
                If pSrc(posSrc + 3 + (i * 4)) > alphaThreshold Then
                    Return CDbl(pSrc(posSrc + plane + (i * 4)))
                End If
            End If
        Next

        Return CDbl(pSrc(posSrc + plane + (c * 4)))
    End Function

    Private Function IsDataToProcess(pSrc() As Byte, posSrc As Integer, length As Integer) As Boolean
        Dim doIt As Boolean = False

        For c As Integer = 0 To length - 1
            If CInt(pSrc(posSrc + 3 + (c * 4))) > 0 Then
                doIt = True
                Exit For
            End If
        Next

        Return doIt
    End Function

    Private Function IsDataToProcess(pSrc() As Single, posSrc As Integer, length As Integer) As Boolean
        Dim doIt As Boolean = False

        For c As Integer = 0 To length - 1
            If CInt(pSrc(posSrc + 3 + (c * 4))) > 0 Then
                doIt = True
                Exit For
            End If
        Next

        Return doIt
    End Function

    Private Function GetExpImage(p() As Single, bias As Integer) As Byte()
        Dim nL As Integer = p.Length
        RaiseEvent ProgressPlus(Me, New ProgressEventArgs(2, 1))
        Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
        Parallel.For(0, nL, Sub(i)
                                p(i) = CSng(Math.Exp(p(i) / f) - 1)
                                'If Double.IsNaN(p(i)) Then
                                '    p(i) = 0
                                'End If
                            End Sub)
        RaiseEvent ProgressPlus(Me, New ProgressEventArgs(1, 1))
        Dim pByte(p.Length - 1) As Byte
        Parallel.For(0, nL, Sub(i)
                                pByte(i) = CByte(Math.Max(Math.Min(p(i) + bias, 255), 0))
                            End Sub)
        Return pByte
    End Function

    Private Sub GetLogImage(p() As Single, pSrc() As Single)
        Dim nL As Integer = p.Length
        RaiseEvent ProgressPlus(Me, New ProgressEventArgs(2, 1))
        Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
        Parallel.For(0, nL, Sub(i)
                                p(i) = CSng(f * Math.Log(1 + p(i)))
                            End Sub)
        RaiseEvent ProgressPlus(Me, New ProgressEventArgs(1, 1))
        Dim nLSrc As Integer = pSrc.Length
        Parallel.For(0, nLSrc, Sub(i)
                                   pSrc(i) = CSng(f * Math.Log(1 + pSrc(i)))
                               End Sub)
    End Sub

    Public Function ConvolveH_par_SigmaAsDistance(b As Bitmap, Kernel As Double(), AddVals As Double(), DistanceWeights As Double(), Bias As Integer, Sigma As Integer, DoTrans As Boolean,
        MaxVal As Integer, SrcOnSigma As Boolean, pe As ProgressEventArgs, bgw As System.ComponentModel.BackgroundWorker, logarithmic As Boolean) As Boolean
        If Not AvailMem.AvailMem.checkAvailRam(b.Width * b.Height * 44L) Then
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

            If Not logarithmic Then
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

                                                       Dim distance As Double = Math.Sqrt((CInt(pSrc(posSrc + (l * 4))) - CInt(pSrc(posSrc + (cc * 4)))) * (CInt(pSrc(posSrc + (l * 4))) - CInt(pSrc(posSrc + (cc * 4)))) +
                                                                                          (CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) * (CInt(pSrc(posSrc + 1 + (l * 4))) - CInt(pSrc(posSrc + 1 + (cc * 4)))) +
                                                                                          (CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) * (CInt(pSrc(posSrc + 2 + (l * 4))) - CInt(pSrc(posSrc + 2 + (cc * 4)))) +
                                                                                          (CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))) * (CInt(pSrc(posSrc + 3 + (l * 4))) - CInt(pSrc(posSrc + 3 + (cc * 4)))))

                                                       Dim d As Integer = Math.Max(Math.Min(CInt(Fix(distance)), DistanceWeights.Length - 1), 0)

                                                       If Not SrcOnSigma OrElse distance <= Sigma * Math.Sqrt(3) Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count += z

                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count2 += z

                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count3 += z

                                                           If DoTrans Then
                                                               Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                               count4 += z
                                                           End If
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If c = h Then
                                                           KSum += (Kernel(c) + AddVals(l)) * DistanceWeights(d)
                                                       Else
                                                           KSum += Kernel(c) * DistanceWeights(d)
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

                                                       Dim d As Integer = Math.Max(Math.Min(CInt(Fix(distance)), DistanceWeights.Length - 1), 0)

                                                       If Not SrcOnSigma OrElse distance <= Sigma * Math.Sqrt(3) Then
                                                           Sum += (CDbl(pSrc(posSrc + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                           count += z

                                                           Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                           count2 += z

                                                           Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                           count3 += z

                                                           If DoTrans Then
                                                               Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                               count4 += z
                                                           End If
                                                       Else
                                                           ignore = True
                                                       End If

                                                       KSum += Kernel(c) * DistanceWeights(d)
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

                                                       Dim d As Integer = Math.Max(Math.Min(CInt(Fix(distance)), DistanceWeights.Length - 1), 0)

                                                       If Not SrcOnSigma OrElse distance <= Sigma * Math.Sqrt(3) Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count += z

                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count2 += z

                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count3 += z

                                                           If DoTrans Then
                                                               Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                               count4 += z
                                                           End If
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If c = h Then
                                                           KSum += (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)
                                                       Else
                                                           KSum += Kernel(c) * DistanceWeights(d)
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

                p = Nothing
                pSrc = Nothing
            Else
                Dim p0((bmData.Stride * bmData.Height) - 1) As Byte
                Dim nL As Integer = p0.Length
                Marshal.Copy(bmData.Scan0, p0, 0, p0.Length)
                Dim p((bmData.Stride * bmData.Height) - 1) As Single
                Parallel.For(0, nL, Sub(i)
                                        p(i) = CSng(p0(i))
                                    End Sub)
                p0 = Nothing

                Dim pSrc0((bmSrc.Stride * bmSrc.Height) - 1) As Byte
                Dim nLSrc As Integer = pSrc0.Length
                Marshal.Copy(bmSrc.Scan0, pSrc0, 0, pSrc0.Length)
                Dim pSrc((bmSrc.Stride * bmSrc.Height) - 1) As Single
                Parallel.For(0, nLSrc, Sub(i)
                                           pSrc(i) = CSng(pSrc0(i))
                                       End Sub)
                pSrc0 = Nothing

                GetLogImage(p, pSrc)

                Dim f As Double = (255.0 / Math.Log(1 + 255 * Math.Sqrt(2)))
                Dim Sigma2 As Double = Math.Log(1 + Sigma * Math.Sqrt(3)) * f

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

                                                       Dim distance As Double = Math.Sqrt((CDbl(pSrc(posSrc + (l * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) * (CDbl(pSrc(posSrc + (l * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) + (CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) * (CDbl(pSrc(posSrc + 1 + (l * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) + (CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) * (CDbl(pSrc(posSrc + 2 + (l * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) + (CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) * (CDbl(pSrc(posSrc + 3 + (l * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))))

                                                       Dim d As Integer = Math.Max(Math.Min(CInt(Fix(distance)), DistanceWeights.Length - 1), 0)

                                                       If Not SrcOnSigma OrElse distance <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count += z

                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count2 += z

                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count3 += z

                                                           If DoTrans Then
                                                               Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(l)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                               count4 += z
                                                           End If
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If c = h Then
                                                           KSum += (Kernel(c) + AddVals(l)) * DistanceWeights(d)
                                                       Else
                                                           KSum += Kernel(c) * DistanceWeights(d)
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
                                                   p(pos) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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
                                                       Dim distance As Double = Math.Sqrt((CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (c * 4)))) * (CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (c * 4)))) + (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) * (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (c * 4)))) + (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) * (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (c * 4)))) + (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))) * (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (c * 4)))))

                                                       Dim d As Integer = Math.Max(Math.Min(CInt(Fix(distance)), DistanceWeights.Length - 1), 0)

                                                       If Not SrcOnSigma OrElse distance <= Sigma2 Then
                                                           Sum += (CDbl(pSrc(posSrc + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                           count += z

                                                           Sum2 += (CDbl(pSrc(posSrc + 1 + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                           count2 += z

                                                           Sum3 += (CDbl(pSrc(posSrc + 2 + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                           count3 += z

                                                           If DoTrans Then
                                                               Sum4 += (CDbl(pSrc(posSrc + 3 + (c * 4))) * Kernel(c) * DistanceWeights(d))
                                                               count4 += z
                                                           End If
                                                       Else
                                                           ignore = True
                                                       End If

                                                       KSum += Kernel(c) * DistanceWeights(d)
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
                                                   p(pos) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                                                       Dim distance As Double = Math.Sqrt((CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) * (CDbl(pSrc(posSrc + (h * 4))) - CDbl(pSrc(posSrc + (cc * 4)))) + (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) * (CDbl(pSrc(posSrc + 1 + (h * 4))) - CDbl(pSrc(posSrc + 1 + (cc * 4)))) + (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) * (CDbl(pSrc(posSrc + 2 + (h * 4))) - CDbl(pSrc(posSrc + 2 + (cc * 4)))) + (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))) * (CDbl(pSrc(posSrc + 3 + (h * 4))) - CDbl(pSrc(posSrc + 3 + (cc * 4)))))

                                                       Dim d As Integer = Math.Max(Math.Min(CInt(Fix(distance)), DistanceWeights.Length - 1), 0)

                                                       If Not SrcOnSigma OrElse distance <= Sigma2 Then
                                                           Sum += If((c = h), (CDbl(pSrc(posSrc + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count += z

                                                           Sum2 += If((c = h), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 1 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count2 += z

                                                           Sum3 += If((c = h), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 2 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                           count3 += z

                                                           If DoTrans Then
                                                               Sum4 += If((c = h), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)), (CDbl(pSrc(posSrc + 3 + (cc * 4))) * Kernel(c) * DistanceWeights(d)))
                                                               count4 += z
                                                           End If
                                                       Else
                                                           ignore = True
                                                       End If

                                                       If c = h Then
                                                           KSum += (Kernel(c) + AddVals(Kernel.Length - (nWidth - l) + h)) * DistanceWeights(d)
                                                       Else
                                                           KSum += Kernel(c) * DistanceWeights(d)
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
                                                   p(pos) = CType((Sum / KSum) / CDbl(count), Single)
                                                   p(pos + 1) = CType((Sum2 / KSum) / CDbl(count2), Single)
                                                   p(pos + 2) = CType((Sum3 / KSum) / CDbl(count3), Single)
                                                   If DoTrans Then
                                                       p(pos + 3) = CType((Sum4 / KSum) / CDbl(count4), Single)
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

                pSrc = Nothing
                Dim p4() As Byte = GetExpImage(p, Bias)
                p = Nothing

                Marshal.Copy(p4, 0, bmData.Scan0, p4.Length)

                p0 = Nothing
                pSrc0 = Nothing
            End If

            b.UnlockBits(bmData)
            bSrc.UnlockBits(bmSrc)
            bSrc.Dispose()

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

End Class
