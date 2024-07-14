Option Strict On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Collections
Imports System.Collections.Generic
Imports SegmentsListLib

Public Class frmColorCurves
    Inherits System.Windows.Forms.Form

    Private redMappings(255) As Byte
    Private greenMappings(255) As Byte
    Private blueMappings(255) As Byte

    Private ptsred As Point() = New Point() {New Point(0, 0), New Point(255, 255)}
    Private ptsgreen As Point() = New Point() {New Point(0, 0), New Point(255, 255)}
    Private ptsblue As Point() = New Point() {New Point(0, 0), New Point(255, 255)}

    Private ptsred2 As Point() = New Point() {New Point(0, 0), New Point(255, 255)}
    Private ptsgreen2 As Point() = New Point() {New Point(0, 0), New Point(255, 255)}
    Private ptsblue2 As Point() = New Point() {New Point(0, 0), New Point(255, 255)}

    Private lumMappings(255) As Integer
    Private ptslum As Point() = New Point() {New Point(0, 0), New Point(255, 255)}
    Dim ptslum4 As Point() = New Point() {New Point(0, 0), New Point(255, 255)}
    Dim ptslum2 As Point() = New Point() {New Point(0, 0), New Point(255, 255)}

    Dim _rgbMode As Boolean = True
    Dim _multiplicator As Integer = 1000

    Dim _lums As Boolean
    Dim _lump As Integer

    Private _bitmap As Bitmap = Nothing
    Private _bSrc As Bitmap = Nothing

    Private _tracking As Boolean = False
    Private _tracking2 As Boolean = False

    Private _rs As Boolean = False
    Private _gs As Boolean = False
    Private _bs As Boolean = False

    Private _rp As Integer = -1
    Private _gp As Integer = -1
    Private _bp As Integer = -1

    Private _loaded As Boolean = False

    Private _left As Integer = 0
    Private _top As Integer = 0
    Private _x As Integer = 0
    Private _y As Integer = 0

    Private _ix As Integer = 0
    Private _iy As Integer = 0

    Public Property Multiplicator() As Integer
        Get
            Return _multiplicator
        End Get

        Set(ByVal value As Integer)
            _multiplicator = value
        End Set
    End Property

    Public Sub New(ByVal imgIn As Bitmap, ByVal Farbe As String, ByVal Tolerance As Byte)
        MyBase.New()

        Me.InitializeComponent()

        If AvailMem.AvailMem.checkAvailRam(imgIn.Width * imgIn.Height * 8L) Then
            _bitmap = makeBitmap(imgIn)
            _bSrc = CType(imgIn.Clone, Bitmap)
        Else
            MessageBox.Show("Not enough Memory")
            Return
        End If

        pictureBox1.Image = CType(_bitmap.Clone, Bitmap)

        Me.comboBox1.SelectedItem = Me.comboBox1.Items(0)
        display(Me.listBox1, ptsred)
        display(Me.listBox2, ptsgreen)
        display(Me.listBox3, ptsblue)

        display(Me.ListBox4, ptslum)

        Me.ComboBox2.SelectedItem = Me.ComboBox2.Items(0)
        button10.Enabled = False

        Me.TextBox1.Text = Farbe
        Me.NumericUpDown10.Value = CType(Tolerance, Decimal)
    End Sub

    Public Property FBitmap() As Bitmap
        Get
            Return _bitmap
        End Get

        Set(ByVal value As Bitmap)
            _bitmap = value
        End Set
    End Property

    Public ReadOnly Property MappingsRed() As Byte()
        Get
            Return redMappings
        End Get
    End Property

    Public ReadOnly Property MappingsGreen() As Byte()
        Get
            Return greenMappings
        End Get
    End Property

    Public ReadOnly Property MappingsBlue() As Byte()
        Get
            Return blueMappings
        End Get
    End Property

    Public ReadOnly Property MappingsLuminance() As Integer()
        Get
            Return lumMappings
        End Get
    End Property

    Public Property LumPoints2() As Point()
        Get
            Return ptslum
        End Get

        Set(ByVal value As Point())
            ptslum4 = value
        End Set
    End Property

    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles checkBox1.CheckedChanged
        If CType(sender, CheckBox).Checked Then
            Me.comboBox1.Enabled = True
        Else
            Me.comboBox1.Enabled = False
        End If

        Me.panel3.Invalidate()
    End Sub

    Private Sub Panel3_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles panel3.Paint
        Using _fPen As New Pen(SystemColors.ControlText, 1)
            Dim mx As Matrix = New Matrix(1, 0, 0, -1, 0, panel3.ClientSize.Height)
            e.Graphics.Transform = mx

            e.Graphics.DrawLine(_fPen, 30, 50, 285, 50)
            e.Graphics.DrawLine(_fPen, 30, 50, 30, 305)

            Dim x As Integer

            For x = 0 To 5

                e.Graphics.DrawLine(_fPen, 25, 50 + (50 * x), 30, 50 + (50 * x))
                e.Graphics.DrawLine(_fPen, 30 + (50 * x), 45, 30 + (50 * x), 50)

            Next

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            For x = 1 To 9
                Dim fpen As Pen = New Pen(Color.Black, 1)
                fpen.DashStyle = DashStyle.Custom
                fpen.DashPattern = New Single() {1, 4}
                e.Graphics.DrawLine(fpen, 30, 50 + (50 * x), 30 + (50 * x), 50)
                fpen.Dispose()
            Next

            Dim gs As GraphicsState = e.Graphics.Save()

            Dim mx1 As Matrix = mx.Clone()
            mx1.Translate(30, 50)
            e.Graphics.Transform = mx1

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            Dim b As Integer = 0

            If Me._rgbMode Then
                If Me.comboBox1.Enabled Then
                    Select Case Me.comboBox1.SelectedItem.ToString
                        Case "red"
                            If ptsred.Length > 0 Then
                                e.Graphics.DrawLine(Pens.DarkRed, 0, ptsred(0).Y, ptsred(0).X, ptsred(0).Y)
                            End If
                            If ptsred.Length > 1 Then
                                e.Graphics.DrawCurve(Pens.Red, ptsred, CType(numericUpDown7.Value, Single))
                            Else
                                b += 1
                            End If
                            If ptsred.Length > 0 Then
                                e.Graphics.DrawLine(Pens.DarkRed, ptsred(ptsred.Length - 1).X, ptsred(ptsred.Length - 1).Y, 255, ptsred(ptsred.Length - 1).Y)
                            End If
                            For Each ptf As Point In ptsred
                                e.Graphics.DrawRectangle(Pens.Red, New Rectangle(ptf.X - 2, ptf.Y - 2, 4, 4))
                            Next
                        Case "green"
                            If ptsgreen.Length > 0 Then
                                e.Graphics.DrawLine(Pens.DarkGreen, 0, ptsgreen(0).Y, ptsgreen(0).X, ptsgreen(0).Y)
                            End If
                            If ptsgreen.Length > 1 Then
                                e.Graphics.DrawCurve(Pens.Green, ptsgreen, CType(numericUpDown8.Value, Single))
                            Else
                                b += 2
                            End If
                            If ptsgreen.Length > 0 Then
                                e.Graphics.DrawLine(Pens.DarkGreen, ptsgreen(ptsgreen.Length - 1).X, ptsgreen(ptsgreen.Length - 1).Y, 255, ptsgreen(ptsgreen.Length - 1).Y)
                            End If
                            For Each ptf As Point In ptsgreen
                                e.Graphics.DrawRectangle(Pens.Green, New Rectangle(ptf.X - 2, ptf.Y - 2, 4, 4))
                            Next
                        Case "blue"
                            If ptsblue.Length > 0 Then
                                e.Graphics.DrawLine(Pens.DarkBlue, 0, ptsblue(0).Y, ptsblue(0).X, ptsblue(0).Y)
                            End If
                            If ptsblue.Length > 1 Then
                                e.Graphics.DrawCurve(Pens.Blue, ptsblue, CType(numericUpDown9.Value, Single))
                            Else
                                b += 4
                            End If
                            If ptsblue.Length > 0 Then
                                e.Graphics.DrawLine(Pens.DarkBlue, ptsblue(ptsblue.Length - 1).X, ptsblue(ptsblue.Length - 1).Y, 255, ptsblue(ptsblue.Length - 1).Y)
                            End If
                            For Each ptf As Point In ptsblue
                                e.Graphics.DrawRectangle(Pens.Blue, New Rectangle(ptf.X - 2, ptf.Y - 2, 4, 4))
                            Next
                    End Select
                Else
                    If ptsred.Length > 0 Then
                        e.Graphics.DrawLine(Pens.DarkRed, 0, ptsred(0).Y, ptsred(0).X, ptsred(0).Y)
                    End If
                    If ptsred.Length > 1 Then
                        e.Graphics.DrawCurve(Pens.Red, ptsred, CType(numericUpDown7.Value, Single))
                    Else
                        b += 1
                    End If
                    If ptsred.Length > 0 Then
                        e.Graphics.DrawLine(Pens.DarkRed, ptsred(ptsred.Length - 1).X, ptsred(ptsred.Length - 1).Y, 255, ptsred(ptsred.Length - 1).Y)
                    End If

                    If ptsgreen.Length > 0 Then
                        e.Graphics.DrawLine(Pens.DarkGreen, 0, ptsgreen(0).Y, ptsgreen(0).X, ptsgreen(0).Y)
                    End If
                    If ptsgreen.Length > 1 Then
                        e.Graphics.DrawCurve(Pens.Green, ptsgreen, CType(numericUpDown8.Value, Single))
                    Else
                        b += 2
                    End If
                    If ptsgreen.Length > 0 Then
                        e.Graphics.DrawLine(Pens.DarkGreen, ptsgreen(ptsgreen.Length - 1).X, ptsgreen(ptsgreen.Length - 1).Y, 255, ptsgreen(ptsgreen.Length - 1).Y)
                    End If

                    If ptsblue.Length > 0 Then
                        e.Graphics.DrawLine(Pens.DarkBlue, 0, ptsblue(0).Y, ptsblue(0).X, ptsblue(0).Y)
                    End If
                    If ptsblue.Length > 1 Then
                        e.Graphics.DrawCurve(Pens.Blue, ptsblue, CType(numericUpDown9.Value, Single))
                    Else
                        b += 4
                    End If
                    If ptsblue.Length > 0 Then
                        e.Graphics.DrawLine(Pens.DarkBlue, ptsblue(ptsblue.Length - 1).X, ptsblue(ptsblue.Length - 1).Y, 255, ptsblue(ptsblue.Length - 1).Y)
                    End If

                    For Each ptf As Point In ptsred
                        e.Graphics.DrawRectangle(Pens.Red, New Rectangle(ptf.X - 2, ptf.Y - 2, 4, 4))
                    Next

                    For Each ptf As Point In ptsgreen
                        e.Graphics.DrawRectangle(Pens.Green, New Rectangle(ptf.X - 2, ptf.Y - 2, 4, 4))
                    Next

                    For Each ptf As Point In ptsblue
                        e.Graphics.DrawRectangle(Pens.Blue, New Rectangle(ptf.X - 2, ptf.Y - 2, 4, 4))
                    Next
                End If

                e.Graphics.Restore(gs)

                e.Graphics.Transform = New Matrix(1, 0, 0, 1, 40, 0)

                Dim ffont As Font = New Font("Arial", 12)

                If (b And 1) = 1 Then
                    e.Graphics.DrawString("Not enough points for red", ffont, Brushes.Red, 0, 0)
                End If

                If (b And 2) = 2 Then
                    e.Graphics.DrawString("Not enough points for green", ffont, Brushes.Green, 0, 24)
                End If

                If (b And 4) = 4 Then
                    e.Graphics.DrawString("Not enough points for blue", ffont, Brushes.Blue, 0, 48)
                End If

                If Me.comboBox1.Enabled Then
                    Select Case Me.comboBox1.SelectedItem.ToString
                        Case "red"
                            e.Graphics.DrawString("Channels combined at red", ffont, Brushes.Red, -10, 255)
                        Case "green"
                            e.Graphics.DrawString("Channels combined at green", ffont, Brushes.Green, -10, 255)
                        Case "blue"
                            e.Graphics.DrawString("Channels combined at blue", ffont, Brushes.Blue, -10, 255)
                    End Select
                End If

                ffont.Dispose()
            Else
                If ptslum.Length > 0 Then
                    e.Graphics.DrawLine(Pens.DarkGray, 0, ptslum(0).Y, ptslum(0).X, ptslum(0).Y)
                End If
                If ptslum.Length > 1 Then
                    e.Graphics.DrawCurve(Pens.Black, ptslum, CType(NumericUpDown11.Value, Single))
                Else
                    b += 8
                End If
                If ptslum.Length > 0 Then
                    e.Graphics.DrawLine(Pens.DarkGray, ptslum(ptslum.Length - 1).X, ptslum(ptslum.Length - 1).Y, 255, ptslum(ptslum.Length - 1).Y)
                End If

                For Each ptf As Point In ptslum
                    e.Graphics.DrawRectangle(Pens.Black, ptf.X - 2, ptf.Y - 2, 4, 4)
                Next

                e.Graphics.Restore(gs)

                e.Graphics.Transform = New Matrix(1, 0, 0, 1, 40, 0)

                Dim ffont As Font = New Font("Arial", 12)

                If (b And 8) = 8 Then
                    e.Graphics.DrawString("Zuwenig Punkte für Luminace", ffont, Brushes.White, 0, 48)
                End If

                ffont.Dispose()
            End If
        End Using
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button1.Click
        Dim pt As Point = New Point(CType(Me.numericUpDown1.Value, Integer), CType(Me.numericUpDown2.Value, Integer))

        Dim i As Integer = 0
        Dim f As Boolean = False

        For i = 0 To Me.listBox1.Items.Count - 1
            If CType(Me.listBox1.Items(i), Point).X = pt.X Then
                f = True
                Exit For
            End If
        Next

        If f = False Then
            ReDim Preserve ptsred(ptsred.Length)
            ptsred(ptsred.Length - 1) = pt
            sortArray(ptsred)
            display(Me.listBox1, ptsred)
        End If
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button2.Click
        Try
            Me.listBox1.Items.Remove(Me.listBox1.SelectedItem)
            ptsred = New Point() {}
            ReDim ptsred(Me.listBox1.Items.Count - 1)

            Dim i As Integer = 0

            For i = 0 To Me.listBox1.Items.Count - 1
                ptsred(i) = CType(Me.listBox1.Items(i), Point)
            Next
        Catch

        End Try
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button4.Click
        Dim pt As Point = New Point(CType(Me.numericUpDown4.Value, Integer), CType(Me.numericUpDown3.Value, Integer))

        Dim i As Integer = 0
        Dim f As Boolean = False

        For i = 0 To Me.listBox2.Items.Count - 1
            If CType(Me.listBox2.Items(i), Point).X = pt.X Then
                f = True
                Exit For
            End If
        Next

        If f = False Then
            ReDim Preserve ptsgreen(ptsgreen.Length)
            ptsgreen(ptsgreen.Length - 1) = pt
            sortArray(ptsgreen)
            display(Me.listBox2, ptsgreen)
        End If
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button3.Click
        Try
            Me.listBox2.Items.Remove(Me.listBox2.SelectedItem)
            ptsgreen = New Point() {}
            ReDim ptsgreen(Me.listBox2.Items.Count - 1)

            Dim i As Integer

            For i = 0 To Me.listBox2.Items.Count - 1
                ptsgreen(i) = CType(Me.listBox2.Items(i), Point)
            Next
        Catch

        End Try
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button6.Click
        Dim pt As Point = New Point(CType(Me.numericUpDown6.Value, Integer), CType(Me.numericUpDown5.Value, Integer))

        Dim i As Integer = 0
        Dim f As Boolean = False

        For i = 0 To Me.listBox3.Items.Count - 1
            If CType(Me.listBox3.Items(i), Point).X = pt.X Then
                f = True
                Exit For
            End If
        Next

        If f = False Then
            ReDim Preserve ptsblue(ptsblue.Length)
            ptsblue(ptsblue.Length - 1) = pt
            sortArray(ptsblue)
            display(Me.listBox3, ptsblue)
        End If
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button5.Click
        Try
            Me.listBox3.Items.Remove(Me.listBox3.SelectedItem)
            ptsblue = New Point() {}
            ReDim ptsblue(Me.listBox3.Items.Count - 1)

            Dim i As Integer

            For i = 0 To Me.listBox3.Items.Count - 1
                ptsblue(i) = CType(Me.listBox3.Items(i), Point)
            Next
        Catch

        End Try
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub sortArray(ByVal a As Point())
        Array.Sort(a, New PointsComparer())
    End Sub

    Private Sub display(ByVal lb As ListBox, ByVal a As Point())
        lb.Items.Clear()

        Dim i As Integer = 0

        For i = 0 To a.Length - 1
            lb.Items.Add(a(i))
        Next
    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles button9.Click
        If Me._rgbMode Then
            If CheckBox2.Checked Then
                ReDim ptsred2(ptsred.Length - 1)
                ptsred.CopyTo(ptsred2, 0)
                sortArray(ptsred)
                ReDim ptsgreen2(ptsgreen.Length - 1)
                ptsgreen.CopyTo(ptsgreen2, 0)
                sortArray(ptsgreen)
                ReDim ptsblue2(ptsblue.Length - 1)
                ptsblue.CopyTo(ptsblue2, 0)
                sortArray(ptsblue)
            End If

            display(Me.listBox1, ptsred)
            display(Me.listBox2, ptsgreen)
            display(Me.listBox3, ptsblue)
        Else
            If CheckBox2.Checked Then
                ReDim ptslum2(ptslum.Length - 1)
                ptslum.CopyTo(ptslum2, 0)
                sortArray(ptslum2)
            End If

            display(Me.ListBox4, ptslum)
        End If

        panel3.Invalidate()

        DrawPic()
    End Sub

    Private Sub DrawPic()
        Dim bmp2 As Bitmap = Nothing
        If AvailMem.AvailMem.checkAvailRam(_bitmap.Width * _bitmap.Height * 4L) Then
            bmp2 = CType(_bitmap.Clone, Bitmap)
        Else
            Return
        End If
        Dim fPath As GraphicsPath = New GraphicsPath()

        Dim cuSgmt As New CurveSegment

        If Me._rgbMode Then
            Gamma_reset()

            Dim fFarbe As Color = Color.FromArgb(255, 127, 127, 127)
            Dim fTolerance As Byte = 255

            If Me.CheckBox3.Checked Then

                Dim c As Color = fFarbe
                Try
                    Dim val As String() = Split(Me.TextBox1.Text, ";")
                    Dim a, r, g, b As Integer
                    a = CType(val(0), Integer)
                    r = CType(val(1), Integer)
                    g = CType(val(2), Integer)
                    b = CType(val(3), Integer)

                    c = Color.FromArgb(a, r, g, b)
                Catch

                End Try

                fFarbe = c

                Dim val2 As Byte = fTolerance

                Try
                    val2 = CType(Me.NumericUpDown10.Value, Byte)
                Catch

                End Try

                fTolerance = val2
            End If

            If Me.comboBox1.Enabled Then
                Dim b(256) As Byte

                Select Case Me.comboBox1.SelectedItem.ToString
                    Case "red"
                        If ptsred.Length > 0 Then
                            fPath.AddLine(0, ptsred(0).Y, ptsred(0).X, ptsred(0).Y)
                            If ptsred.Length > 1 Then
                                fPath.AddCurve(ptsred, CType(numericUpDown7.Value, Single))
                            End If
                            fPath.AddLine(ptsred(ptsred.Length - 1).X, ptsred(ptsred.Length - 1).Y, 255, ptsred(ptsred.Length - 1).Y)
                        Else
                            fPath.AddLine(0, 0, 255, 255)
                        End If

                        Dim bz As List(Of BezierSegment) = cuSgmt.CalcBezierSegments(ptsred, CType(numericUpDown7.Value, Single))
                        Dim pts As List(Of PointF) = cuSgmt.GetAllPoints(bz, 256, 0, 255)
                        cuSgmt.MapPoints(pts, redMappings)

                        If Me.CheckBox3.Checked Then
                            fipbmp.GradColors(bmp2, redMappings, redMappings, redMappings, fFarbe, fTolerance)
                        Else
                            fipbmp.GradColors(bmp2, redMappings, redMappings, redMappings)
                        End If
                    Case "green"
                        If ptsgreen.Length > 0 Then
                            fPath.AddLine(0, ptsgreen(0).Y, ptsgreen(0).X, ptsgreen(0).Y)
                            If ptsgreen.Length > 1 Then
                                fPath.AddCurve(ptsgreen, CType(numericUpDown8.Value, Single))
                            End If
                            fPath.AddLine(ptsgreen(ptsgreen.Length - 1).X, ptsgreen(ptsgreen.Length - 1).Y, 255, ptsgreen(ptsgreen.Length - 1).Y)
                        Else
                            fPath.AddLine(0, 0, 255, 255)
                        End If

                        Dim bz As List(Of BezierSegment) = cuSgmt.CalcBezierSegments(ptsgreen, CType(numericUpDown7.Value, Single))
                        Dim pts As List(Of PointF) = cuSgmt.GetAllPoints(bz, 256, 0, 255)
                        cuSgmt.MapPoints(pts, greenMappings)

                        If Me.CheckBox3.Checked Then
                            fipbmp.GradColors(bmp2, greenMappings, greenMappings, greenMappings, fFarbe, fTolerance)
                        Else
                            fipbmp.GradColors(bmp2, greenMappings, greenMappings, greenMappings)
                        End If
                    Case "blue"
                        If ptsblue.Length > 0 Then
                            fPath.AddLine(0, ptsblue(0).Y, ptsblue(0).X, ptsblue(0).Y)
                            If ptsblue.Length > 1 Then
                                fPath.AddCurve(ptsblue, CType(numericUpDown9.Value, Single))
                            End If
                            fPath.AddLine(ptsblue(ptsblue.Length - 1).X, ptsblue(ptsblue.Length - 1).Y, 255, ptsblue(ptsblue.Length - 1).Y)
                        Else
                            fPath.AddLine(0, 0, 255, 255)
                        End If

                        Dim bz As List(Of BezierSegment) = cuSgmt.CalcBezierSegments(ptsblue, CType(numericUpDown7.Value, Single))
                        Dim pts As List(Of PointF) = cuSgmt.GetAllPoints(bz, 256, 0, 255)
                        cuSgmt.MapPoints(pts, blueMappings)

                        If Me.CheckBox3.Checked Then
                            fipbmp.GradColors(bmp2, blueMappings, blueMappings, blueMappings, fFarbe, fTolerance)
                        Else
                            fipbmp.GradColors(bmp2, blueMappings, blueMappings, blueMappings)
                        End If
                End Select

                Dim bmpOut2 As Bitmap = Nothing
                If AvailMem.AvailMem.checkAvailRam(bmp2.Width * bmp2.Height * 4L) Then
                    bmpOut2 = CType(bmp2.Clone(), Bitmap)
                End If
                bmp2.Dispose()
                If Not IsNothing(pictureBox1.Image) Then
                    pictureBox1.Image.Dispose()
                End If
                pictureBox1.Image = bmpOut2
            Else
                If ptsblue.Length > 0 Then
                    fPath.AddLine(0, ptsblue(0).Y, ptsblue(0).X, ptsblue(0).Y)
                    If ptsblue.Length > 1 Then
                        fPath.AddCurve(ptsblue, CType(numericUpDown9.Value, Single))
                    End If
                    fPath.AddLine(ptsblue(ptsblue.Length - 1).X, ptsblue(ptsblue.Length - 1).Y, 255, ptsblue(ptsblue.Length - 1).Y)
                Else
                    fPath.AddLine(0, 0, 255, 255)
                End If

                fPath.Reset()

                Dim bz As List(Of BezierSegment) = cuSgmt.CalcBezierSegments(ptsblue, CType(numericUpDown9.Value, Single))
                Dim pts As List(Of PointF) = cuSgmt.GetAllPoints(bz, 256, 0, 255)
                cuSgmt.MapPoints(pts, blueMappings)

                If ptsgreen.Length > 0 Then
                    fPath.AddLine(0, ptsgreen(0).Y, ptsgreen(0).X, ptsgreen(0).Y)
                    If ptsgreen.Length > 1 Then
                        fPath.AddCurve(ptsgreen, CType(numericUpDown8.Value, Single))
                    End If
                    fPath.AddLine(ptsgreen(ptsgreen.Length - 1).X, ptsgreen(ptsgreen.Length - 1).Y, 255, ptsgreen(ptsgreen.Length - 1).Y)
                Else
                    fPath.AddLine(0, 0, 255, 255)
                End If

                fPath.Reset()

                bz = cuSgmt.CalcBezierSegments(ptsgreen, CType(numericUpDown8.Value, Single))
                pts = cuSgmt.GetAllPoints(bz, 256, 0, 255)
                cuSgmt.MapPoints(pts, greenMappings)

                If ptsred.Length > 0 Then
                    fPath.AddLine(0, ptsred(0).Y, ptsred(0).X, ptsred(0).Y)
                    If ptsred.Length > 1 Then
                        fPath.AddCurve(ptsred, CType(numericUpDown7.Value, Single))
                    End If
                    fPath.AddLine(ptsred(ptsred.Length - 1).X, ptsred(ptsred.Length - 1).Y, 255, ptsred(ptsred.Length - 1).Y)
                Else
                    fPath.AddLine(0, 0, 255, 255)
                End If

                bz = cuSgmt.CalcBezierSegments(ptsred, CType(numericUpDown7.Value, Single))
                pts = cuSgmt.GetAllPoints(bz, 256, 0, 255)
                cuSgmt.MapPoints(pts, redMappings)

                If Me.CheckBox3.Checked Then
                    fipbmp.GradColors(bmp2, redMappings, greenMappings, blueMappings, fFarbe, fTolerance)
                Else
                    fipbmp.GradColors(bmp2, redMappings, greenMappings, blueMappings)
                End If

                Dim bmpOut2 As Bitmap = Nothing
                If AvailMem.AvailMem.checkAvailRam(bmp2.Width * bmp2.Height * 4L) Then
                    bmpOut2 = CType(bmp2.Clone(), Bitmap)
                End If
                bmp2.Dispose()
                If Not IsNothing(pictureBox1.Image) Then
                    pictureBox1.Image.Dispose()
                End If
                pictureBox1.Image = bmpOut2
            End If
        Else
            Gamma_reset_Lum()

            If ptslum.Length > 0 Then
                fPath.AddLine(0, ptslum(0).Y, ptslum(0).X, ptslum(0).Y)
                If ptslum.Length > 1 Then
                    fPath.AddCurve(ptslum, CType(NumericUpDown11.Value, Single))
                End If
                fPath.AddLine(ptslum(ptslum.Length - 1).X, ptslum(ptslum.Length - 1).Y, 255, ptslum(ptslum.Length - 1).Y)
            Else
                fPath.AddLine(0, 0, 255, 255)
            End If

            fPath.Reset()

            Dim bz As List(Of BezierSegment) = cuSgmt.CalcBezierSegments(ptslum, CType(numericUpDown7.Value, Single))
            Dim pts As List(Of PointF) = cuSgmt.GetAllPoints(bz, 256, 0, 255)
            cuSgmt.MapPoints(pts, lumMappings)

            fipbmp.gradColors(bmp2, lumMappings, _multiplicator)

            Dim bmpOut2 As Bitmap = Nothing

            If AvailMem.AvailMem.checkAvailRam(bmp2.Width * bmp2.Height * 4L) Then
                bmpOut2 = CType(bmp2.Clone(), Bitmap)
            End If

            bmp2.Dispose()
            If Not pictureBox1.Image Is Nothing Then
                pictureBox1.Image.Dispose()
            End If
            pictureBox1.Image = bmpOut2
        End If

        button10.Enabled = True

    End Sub

    Private Function makeBitmap(ByVal bmp As Bitmap) As Bitmap
        Dim faktor As Double = panel2.Width / panel2.Height
        Dim multiplier As Double = bmp.Width / bmp.Height
        Dim zoom As Single = 0.0F

        If multiplier >= faktor Then
            zoom = CType(panel2.ClientRectangle.Width / bmp.Width, Single)
        Else
            zoom = CType(panel2.ClientRectangle.Height / bmp.Height, Single)
        End If

        Dim bitmap As Bitmap = Nothing
        Dim w2 As Integer = CType(bmp.Width * zoom, Integer)
        Dim h2 As Integer = CType(bmp.Height * zoom, Integer)
        If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 4L) Then
            bitmap = New Bitmap(w2, h2)
        Else
            Return Nothing
        End If
        Dim gx As Graphics = Graphics.FromImage(bitmap)
        gx.SmoothingMode = SmoothingMode.AntiAlias
        gx.InterpolationMode = InterpolationMode.HighQualityBicubic
        'gx.InterpolationMode = InterpolationMode.HighQualityBilinear
        gx.DrawImage(bmp, 0, 0, CType(bmp.Width * zoom, Integer), CType(bmp.Height * zoom, Integer))
        gx.Dispose()

        Return bitmap
    End Function

    Private Sub listBox1_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles listBox1.DoubleClick
        Try
            Dim pt As Point = CType(CType(sender, ListBox).SelectedItem, Point)
            numericUpDown1.Value = CType(pt.X, Decimal)
            numericUpDown2.Value = CType(pt.Y, Decimal)
        Catch

        End Try
    End Sub

    Private Sub listBox2_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles listBox2.DoubleClick
        Try
            Dim pt As Point = CType(CType(sender, ListBox).SelectedItem, Point)
            numericUpDown4.Value = CType(pt.X, Decimal)
            numericUpDown3.Value = CType(pt.Y, Decimal)
        Catch

        End Try
    End Sub

    Private Sub listBox3_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles listBox3.DoubleClick
        Try
            Dim pt As Point = CType(CType(sender, ListBox).SelectedItem, Point)
            numericUpDown6.Value = CType(pt.X, Decimal)
            numericUpDown5.Value = CType(pt.Y, Decimal)
        Catch

        End Try
    End Sub

    Private Sub Gamma_reset()
        For i As Integer = 0 To 255
            redMappings(i) = 0
            greenMappings(i) = 0
            blueMappings(i) = 0
        Next
    End Sub

    Private Sub Gamma_reset_Lum()
        For i As Integer = 0 To 255
            lumMappings(i) = 0
        Next
    End Sub

    Private Sub panel3_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles panel3.MouseMove
        Dim ix As Integer = e.X - 30
        Dim iy As Integer = 254 - (e.Y)

        ix = Math.Max(Math.Min(ix, 255), 0)
        iy = Math.Max(Math.Min(iy, 255), 0)

        Me.ToolStripStatusLabel1.Text = ix.ToString & ";" & iy.ToString

        If _tracking = True AndAlso (e.Button = MouseButtons.Left OrElse e.Button = MouseButtons.Right) Then
            If Me._rgbMode Then
                'If (ix >= 0) And (ix <= 255) And (iy >= 0) And (iy <= 255) Then
                If _rs Then
                    Me.ptsred(_rp) = New Point(ix, iy)
                    'checkX(Me.ptsred, _rp)
                End If

                If _gs Then
                    Me.ptsgreen(_gp) = New Point(ix, iy)
                    'checkX(Me.ptsgreen, _gp)
                End If

                If _bs Then
                    Me.ptsblue(_bp) = New Point(ix, iy)
                    'checkX(Me.ptsblue, _bp)
                End If

                panel3.Invalidate()

                If Timer1.Enabled Then
                    Timer1.Stop()
                End If

                Timer1.Start()
                'End If
            Else
                'If (ix >= 0) And (ix <= 255) And (iy >= 0) And (iy <= 255) Then
                If _lums Then
                    Me.ptslum(_lump) = New Point(ix, iy)
                End If

                If CheckBox2.Checked Then
                    ReDim ptslum2(ptslum.Length - 1)
                    ptslum.CopyTo(ptslum2, 0)
                    sortArray(ptslum2)
                End If

                display(Me.ListBox4, ptslum)

                panel3.Invalidate()

                If Timer1.Enabled Then
                    Timer1.Stop()
                End If

                Me.Timer1.Start()
                'End If
            End If
        End If

    End Sub

    Private Sub panel3_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles panel3.MouseUp
        Dim ix As Integer = e.X - 30
        Dim iy As Integer = 254 - (e.Y)

        ix = Math.Max(Math.Min(ix, 255), 0)
        iy = Math.Max(Math.Min(iy, 255), 0)

        Dim pt As Point = New Point(ix, iy)

        If e.Button = Windows.Forms.MouseButtons.Right Then
            'If (pt.X >= 0) And (pt.X <= 255) And (pt.Y >= 0) And (pt.Y <= 255) Then
            If Me._rgbMode Then
                Select Case ComboBox2.SelectedIndex
                    Case 0
                        Dim i As Integer = 0
                        Dim f As Boolean = False

                        For i = 0 To Me.ptsred.Length - 1
                            If Me.ptsred(i).X = pt.X Then
                                f = True
                                Me.ptsred(i) = pt
                                Exit For
                            End If
                        Next

                        If f = False Then
                            ReDim Preserve ptsred(ptsred.Length)
                            ptsred(ptsred.Length - 1) = pt
                            sortArray(ptsred)
                            display(Me.listBox1, ptsred)
                        End If

                        i = 0
                        f = False

                        For i = 0 To Me.ptsgreen.Length - 1
                            If Me.ptsgreen(i).X = pt.X Then
                                f = True
                                Me.ptsgreen(i) = pt
                                Exit For
                            End If
                        Next

                        If f = False Then
                            ReDim Preserve ptsgreen(ptsgreen.Length)
                            ptsgreen(ptsgreen.Length - 1) = pt
                            sortArray(ptsgreen)
                            display(Me.listBox2, ptsgreen)
                        End If

                        i = 0
                        f = False

                        For i = 0 To Me.ptsblue.Length - 1
                            If Me.ptsblue(i).X = pt.X Then
                                f = True
                                Me.ptsblue(i) = pt
                                Exit For
                            End If
                        Next

                        If f = False Then
                            ReDim Preserve ptsblue(ptsblue.Length)
                            ptsblue(ptsblue.Length - 1) = pt
                            sortArray(ptsblue)
                            display(Me.listBox3, ptsblue)
                        End If
                    Case 1
                        Dim i As Integer = 0
                        Dim f As Boolean = False

                        For i = 0 To Me.ptsred.Length - 1
                            If Me.ptsred(i).X = pt.X Then
                                f = True
                                Me.ptsred(i) = pt
                                Exit For
                            End If
                        Next

                        If f = False Then
                            ReDim Preserve ptsred(ptsred.Length)
                            ptsred(ptsred.Length - 1) = pt
                            sortArray(ptsred)
                            display(Me.listBox1, ptsred)
                        End If
                    Case 2
                        Dim i As Integer = 0
                        Dim f As Boolean = False

                        For i = 0 To Me.ptsgreen.Length - 1
                            If Me.ptsgreen(i).X = pt.X Then
                                f = True
                                Me.ptsgreen(i) = pt
                                Exit For
                            End If
                        Next

                        If f = False Then
                            ReDim Preserve ptsgreen(ptsgreen.Length)
                            ptsgreen(ptsgreen.Length - 1) = pt
                            sortArray(ptsgreen)
                            display(Me.listBox2, ptsgreen)
                        End If
                    Case 3
                        Dim i As Integer = 0
                        Dim f As Boolean = False

                        For i = 0 To Me.ptsblue.Length - 1
                            If Me.ptsblue(i).X = pt.X Then
                                f = True
                                Me.ptsblue(i) = pt
                                Exit For
                            End If
                        Next

                        If f = False Then
                            ReDim Preserve ptsblue(ptsblue.Length)
                            ptsblue(ptsblue.Length - 1) = pt
                            sortArray(ptsblue)
                            display(Me.listBox3, ptsblue)
                        End If
                End Select
            Else
                Dim i As Integer = 0
                Dim f As Boolean = False

                For i = 0 To Me.ptslum.Length - 1
                    If Me.ptslum(i).X = pt.X Then
                        f = True
                        Me.ptslum(i) = pt
                        Exit For
                    End If
                Next

                If f = False Then
                    ReDim Preserve ptslum(ptslum.Length)
                    ptslum(ptslum.Length - 1) = pt
                    sortArray(ptslum)
                    display(Me.ListBox4, ptslum)
                End If
            End If

            Button9_Click(button9, EventArgs.Empty)
        End If
        'End If

        _tracking = False

        Me.panel3.Capture = False
    End Sub

    Private Sub Button12_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button12.Click
        Gamma_reset()
        ptsred = New Point() {New Point(0, 0), New Point(255, 255)}
        ptsgreen = New Point() {New Point(0, 0), New Point(255, 255)}
        ptsblue = New Point() {New Point(0, 0), New Point(255, 255)}

        Gamma_reset_Lum()
        ptslum = New Point() {New Point(0, 0), New Point(255, 255)}

        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub panel3_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles panel3.MouseDown
        Dim ix As Integer = e.X - 30
        Dim iy As Integer = 254 - (e.Y)

        ix = Math.Max(Math.Min(ix, 255), 0)
        iy = Math.Max(Math.Min(iy, 255), 0)

        Dim pt As Point = New Point(ix, iy)

        If Me._rgbMode Then

            _rs = False
            _gs = False
            _bs = False

            If e.Button = Windows.Forms.MouseButtons.Left Then
                If ComboBox2.SelectedIndex = 0 Then
                    For i As Integer = 0 To Me.listBox1.Items.Count - 1
                        Dim ptf As Point = CType(Me.listBox1.Items(i), Point)
                        If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                            (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                            Me.listBox1.SelectedItem = Me.listBox1.Items(i)
                            _rs = True
                            _rp = i
                            _tracking = True
                        End If
                    Next

                    For i As Integer = 0 To Me.listBox2.Items.Count - 1
                        Dim ptf As Point = CType(Me.listBox2.Items(i), Point)
                        If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                            (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                            Me.listBox2.SelectedItem = Me.listBox2.Items(i)
                            _gs = True
                            _gp = i
                            _tracking = True
                        End If
                    Next

                    For i As Integer = 0 To Me.listBox3.Items.Count - 1
                        Dim ptf As Point = CType(Me.listBox3.Items(i), Point)
                        If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                            (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                            Me.listBox3.SelectedItem = Me.listBox3.Items(i)
                            _bs = True
                            _bp = i
                            _tracking = True
                        End If
                    Next
                ElseIf ComboBox2.SelectedIndex = 1 Then
                    For i As Integer = 0 To Me.listBox1.Items.Count - 1
                        Dim ptf As Point = CType(Me.listBox1.Items(i), Point)
                        If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                            (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                            Me.listBox1.SelectedItem = Me.listBox1.Items(i)
                            _rs = True
                            _rp = i
                            _tracking = True
                        End If
                    Next
                ElseIf ComboBox2.SelectedIndex = 2 Then
                    For i As Integer = 0 To Me.listBox2.Items.Count - 1
                        Dim ptf As Point = CType(Me.listBox2.Items(i), Point)
                        If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                            (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                            Me.listBox2.SelectedItem = Me.listBox2.Items(i)
                            _gs = True
                            _gp = i
                            _tracking = True
                        End If
                    Next
                ElseIf ComboBox2.SelectedIndex = 3 Then
                    For i As Integer = 0 To Me.listBox3.Items.Count - 1
                        Dim ptf As Point = CType(Me.listBox3.Items(i), Point)
                        If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                            (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                            Me.listBox3.SelectedItem = Me.listBox3.Items(i)
                            _bs = True
                            _bp = i
                            _tracking = True
                        End If
                    Next
                End If

            End If
        Else
            _lums = False

            If e.Button = Windows.Forms.MouseButtons.Left Then
                For i As Integer = 0 To Me.ListBox4.Items.Count - 1
                    Dim ptf As Point = CType(Me.ListBox4.Items(i), Point)
                    If (pt.X > (ptf.X - 4)) AndAlso (pt.X < (ptf.X + 4)) AndAlso
                        (pt.Y > (ptf.Y - 4)) AndAlso (pt.Y < (ptf.Y + 4)) Then
                        Me.ListBox4.SelectedItem = Me.ListBox4.Items(i)
                        _lums = True
                        _lump = i
                        _tracking = True
                    End If
                Next
            End If
        End If

        If _tracking Then
            Me.panel3.Capture = True
        End If
    End Sub

    Private Sub CheckBox2_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox2.CheckedChanged
        If CType(sender, CheckBox).Checked = False Then
            ptsred = ptsred2
            ptsgreen = ptsgreen2
            ptsblue = ptsblue2
        End If

        If _loaded Then
            Button9_Click(button9, EventArgs.Empty)
        End If
    End Sub

    Private Sub frmGradColors3_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.GroupBox5.Location = Me.GroupBox4.Location
        Me.GroupBox5.SendToBack()
        Me.GroupBox5.Visible = False
        Me.GroupBox6.Location = Me.groupBox1.Location
        _loaded = True
    End Sub

    Private Sub numericUpDown7_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles numericUpDown7.ValueChanged
        If _loaded Then
            Button9_Click(button9, EventArgs.Empty)
        End If
    End Sub

    Private Sub numericUpDown8_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles numericUpDown8.ValueChanged
        If _loaded Then
            Button9_Click(button9, EventArgs.Empty)
        End If
    End Sub

    Private Sub numericUpDown9_ValueChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles numericUpDown9.ValueChanged
        If _loaded Then
            Button9_Click(button9, EventArgs.Empty)
        End If
    End Sub

    Private Sub CheckBoxPic_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBoxPic.CheckedChanged
        If CheckBoxPic.Checked Then
            Me.pictureBox1.SizeMode = PictureBoxSizeMode.Normal
            Me.pictureBox1.Location = New Point(0, 0)
            Me.pictureBox1.Size = New Size(Me.panel2.Width, Me.panel2.Height)
            Me.pictureBox1.Cursor = Cursors.Hand

            _left = 0
            _top = 0

            AddHandler pictureBox1.MouseDown, AddressOf pictureBox1_MouseDown
            AddHandler pictureBox1.MouseMove, AddressOf pictureBox1_MouseMove_2
            AddHandler pictureBox1.MouseUp, AddressOf pictureBox1_MouseUp

            If Not IsNothing(_bitmap) Then
                _bitmap.Dispose()
            End If

            If AvailMem.AvailMem.checkAvailRam(Me.pictureBox1.ClientSize.Width * Me.pictureBox1.ClientSize.Height * 4L) Then
                _bitmap = New Bitmap(Me.pictureBox1.ClientSize.Width, Me.pictureBox1.ClientSize.Height)
            Else
                Return
            End If
            Dim g As Graphics = Graphics.FromImage(_bitmap)
            g.DrawImage(_bSrc, New Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
            New Rectangle(_left, _top, Me.pictureBox1.ClientSize.Width, Me.pictureBox1.ClientSize.Height), GraphicsUnit.Pixel)
            g.Dispose()
        Else
            Me.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize
            Me.pictureBox1.Cursor = Cursors.Default

            RemoveHandler pictureBox1.MouseDown, AddressOf pictureBox1_MouseDown
            RemoveHandler pictureBox1.MouseMove, AddressOf pictureBox1_MouseMove_2
            RemoveHandler pictureBox1.MouseUp, AddressOf pictureBox1_MouseUp

            If Not IsNothing(_bitmap) AndAlso _bitmap.Equals(_bSrc) = False Then
                _bitmap.Dispose()
            End If
            _bitmap = New Bitmap(makeBitmap(_bSrc))
        End If

        If Not IsNothing(Me.pictureBox1.Image) Then
            Me.pictureBox1.Image.Dispose()
        End If
        If AvailMem.AvailMem.checkAvailRam(_bitmap.Width * _bitmap.Height * 4L) Then
            Me.pictureBox1.Image = CType(_bitmap.Clone, Bitmap)
            Button9_Click(button9, EventArgs.Empty)
        End If
    End Sub

    Private Sub pictureBox1_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        If e.Button = Windows.Forms.MouseButtons.Left Then
            _x = e.X
            _y = e.Y
            _tracking2 = True
        End If
    End Sub

    Private Sub pictureBox1_MouseMove_2(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        If _tracking2 Then
            _left = Math.Min(Math.Max(_left + (_x - e.X), 0), _bSrc.Width - Me.pictureBox1.ClientSize.Width)
            _top = Math.Min(Math.Max(_top + (_y - e.Y), 0), _bSrc.Height - Me.pictureBox1.ClientSize.Height)
        End If
    End Sub

    Private Sub pictureBox1_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        If (_tracking2) AndAlso (_bSrc.Width > pictureBox1.Width Or _bSrc.Height > pictureBox1.Height) Then
            If AvailMem.AvailMem.checkAvailRam(Me.pictureBox1.ClientSize.Width * Me.pictureBox1.ClientSize.Height * 4L) Then
                _bitmap = New Bitmap(Me.pictureBox1.ClientSize.Width, Me.pictureBox1.ClientSize.Height)
            Else
                Return
            End If
            Dim g As Graphics = Graphics.FromImage(_bitmap)
            g.DrawImage(_bSrc, New Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
            New Rectangle(_left, _top, Me.pictureBox1.ClientSize.Width, Me.pictureBox1.ClientSize.Height), GraphicsUnit.Pixel)
            g.Dispose()
            If Not IsNothing(Me.pictureBox1.Image) Then
                Me.pictureBox1.Image.Dispose()
            End If
            If AvailMem.AvailMem.checkAvailRam(_bitmap.Width * _bitmap.Height * 4L) Then
                Me.pictureBox1.Image = CType(_bitmap.Clone, Bitmap)
                Button9_Click(button9, EventArgs.Empty)
            End If
            _tracking2 = False
        End If
    End Sub

    Private Sub pictureBox1_MouseMove(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles pictureBox1.MouseMove
        If _tracking = False Then
            If e.X >= 0 AndAlso e.X < _bitmap.Width AndAlso e.Y >= 0 AndAlso e.Y < _bitmap.Height Then
                Dim ix As Integer = e.X
                Dim iy As Integer = e.Y
                _ix = e.X
                _iy = e.Y

                Dim gfcolor As Color = _bitmap.GetPixel(ix, iy)
                Dim gfcolor2 As Color = CType(pictureBox1.Image, Bitmap).GetPixel(ix, iy)
                Me.ToolStripStatusLabel1.Text = "ARGB: " & gfcolor.A.ToString() & "; " & gfcolor.R.ToString() & "; " & gfcolor.G.ToString() & "; " & gfcolor.B.ToString() &
          "; NEW: ARGB: " & gfcolor2.A.ToString() & "; " & gfcolor2.R.ToString() & "; " & gfcolor2.G.ToString() & "; " & gfcolor2.B.ToString()
                Me.ToolStripStatusLabel2.Text = "Lum: " & gfcolor.GetBrightness.ToString("N4")
            End If
        End If
    End Sub

    Private Sub pictureBox1_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles pictureBox1.DoubleClick
        If _tracking = False Then
            If _ix >= 0 AndAlso _ix < _bitmap.Width AndAlso _iy >= 0 AndAlso _iy < _bitmap.Height Then
                Dim gfcolor As Color = _bitmap.GetPixel(_ix, _iy)
                Me.TextBox1.Text = gfcolor.A.ToString() & ";" & gfcolor.R.ToString() & ";" & gfcolor.G.ToString() & ";" & gfcolor.B.ToString()
            End If
        End If
    End Sub

    Private Sub HandleKeyPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles TextBox1.KeyPress
        If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> Char.Parse("-") AndAlso e.KeyChar <> Char.Parse(",") _
         AndAlso e.KeyChar <> Char.Parse(".") AndAlso Not Char.IsControl(e.KeyChar) AndAlso e.KeyChar <> Char.Parse(";") Then
            e.Handled = True
        End If
    End Sub

    Private Sub CheckBox3_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles CheckBox3.CheckedChanged
        If _loaded Then
            Me.Button13.Enabled = Me.CheckBox3.Checked
            DrawPic()
        End If
    End Sub

    Private Sub NumericUpDown10_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles NumericUpDown10.ValueChanged
        If _loaded Then
            DrawPic()
        End If
    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles TextBox1.TextChanged
        If _loaded Then
            DrawPic()
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, ByVal keyData As Keys) As Boolean
        If keyData = Keys.F2 Then
            If CheckBoxPic.Checked Then
                Dim pt As Point = fipbmp.ScrollToPic(_bSrc)
                _left = pt.X
                _top = pt.Y

                If (_bSrc.Width > pictureBox1.Width Or _bSrc.Height > pictureBox1.Height) Then
                    If Not IsNothing(_bitmap) Then
                        _bitmap.Dispose()
                    End If
                    If AvailMem.AvailMem.checkAvailRam(Me.pictureBox1.ClientSize.Width * Me.pictureBox1.ClientSize.Height * 4L) Then
                        _bitmap = New Bitmap(Me.pictureBox1.ClientSize.Width, Me.pictureBox1.ClientSize.Height)
                    Else
                        Return True
                    End If
                    Dim g As Graphics = Graphics.FromImage(_bitmap)
                    g.DrawImage(_bSrc, New Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                     New Rectangle(_left, _top, Me.pictureBox1.ClientSize.Width, Me.pictureBox1.ClientSize.Height), GraphicsUnit.Pixel)
                    g.Dispose()
                    If Not IsNothing(Me.pictureBox1.Image) Then
                        Me.pictureBox1.Image.Dispose()
                    End If
                    If AvailMem.AvailMem.checkAvailRam(_bitmap.Width * _bitmap.Height * 4L) Then
                        Me.pictureBox1.Image = CType(_bitmap.Clone, Bitmap)
                    End If
                End If
            End If

            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub Timer1_Tick(sender As System.Object, e As System.EventArgs) Handles Timer1.Tick
        Timer1.Stop()

        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles CheckBox4.CheckedChanged
        If _loaded Then
            Dim b As Boolean = True
            If Me.CheckBox4.Checked Then

                If MessageBox.Show("All current values will be ignored. Continue?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) = DialogResult.Yes Then
                    b = True
                Else
                    b = False
                End If
            End If

            If b Then
                Me._rgbMode = Not Me.CheckBox4.Checked

                If Me.CheckBox4.Checked Then
                    Me.GroupBox5.Visible = True
                    Me.GroupBox4.Visible = False
                    Me.ComboBox2.Enabled = False
                    Me.GroupBox5.BringToFront()
                Else
                    Me.GroupBox4.Visible = True
                    Me.GroupBox5.Visible = False
                    Me.ComboBox2.Enabled = True
                    Me.GroupBox4.BringToFront()
                End If

                display(Me.ListBox4, ptslum)
                panel3.Invalidate()

                DrawPic()
            End If
        End If
    End Sub

    Private Sub Button8_Click_1(sender As System.Object, e As System.EventArgs) Handles Button8.Click
        Dim pt As Point = New Point(CType(Me.NumericUpDown13.Value, Integer), CType(Me.NumericUpDown12.Value, Integer))

        Dim i As Integer = 0
        Dim f As Boolean = False

        For i = 0 To Me.ListBox4.Items.Count - 1
            If CType(Me.ListBox4.Items(i), Point).X = pt.X Then
                f = True
                Exit For
            End If
        Next

        If f = False Then
            ReDim Preserve ptslum(ptslum.Length)
            ptslum(ptslum.Length - 1) = pt
            sortArray(ptslum)
            display(Me.ListBox4, ptslum)
        End If
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub Button7_Click(sender As System.Object, e As System.EventArgs) Handles Button7.Click
        Try
            Me.ListBox4.Items.Remove(Me.ListBox4.SelectedItem)
            ptslum = New Point() {}
            ReDim ptslum(Me.ListBox4.Items.Count - 1)

            Dim i As Integer = 0

            For i = 0 To Me.ListBox4.Items.Count - 1
                ptslum(i) = CType(Me.ListBox4.Items(i), Point)
            Next
        Catch

        End Try
        Button9_Click(button9, EventArgs.Empty)
    End Sub

    Private Sub NumericUpDown11_ValueChanged(sender As System.Object, e As System.EventArgs) Handles NumericUpDown11.ValueChanged
        If _loaded Then
            Button9_Click(button9, EventArgs.Empty)
        End If
    End Sub

    Function ProcessColors() As Bitmap
        Dim bWork As Bitmap = Nothing

        Try
            If AvailMem.AvailMem.checkAvailRam(Me._bSrc.Width * Me._bSrc.Height * 4L) Then
                bWork = CType(Me._bSrc.Clone, Bitmap)
            End If
            Dim ar(255) As Byte
            Dim ag(255) As Byte
            Dim ab(255) As Byte
            Dim alum(255) As Integer

            If CheckBox4.Checked = False Then
                Array.Copy(MappingsRed, ar, MappingsRed.Length)
                Array.Copy(MappingsGreen, ag, MappingsGreen.Length)
                Array.Copy(MappingsBlue, ab, MappingsBlue.Length)

                If checkBox1.Checked Then
                    Select Case comboBox1.SelectedIndex
                        Case 0
                            ag = ar
                            ab = ar
                        Case 1
                            ar = ag
                            ab = ag
                        Case 2
                            ar = ab
                            ag = ab
                    End Select
                End If

                Dim fFarbe As Color = Color.FromArgb(255, 127, 127, 127)
                Dim fTolerance As Byte = 255

                If CheckBox3.Checked Then
                    Dim c As Color = fFarbe
                    Try
                        Dim val As String() = Split(TextBox1.Text, ";")
                        Dim a, r, g, b As Integer
                        a = CType(val(0), Integer)
                        r = CType(val(1), Integer)
                        g = CType(val(2), Integer)
                        b = CType(val(3), Integer)

                        c = Color.FromArgb(a, r, g, b)
                    Catch

                    End Try

                    fFarbe = c

                    Dim val2 As Byte = fTolerance

                    Try
                        val2 = CType(NumericUpDown10.Value, Byte)
                    Catch

                    End Try

                    fTolerance = val2

                    fipbmp.GradColors(bWork, ar, ag, ab, fFarbe, fTolerance)
                Else
                    fipbmp.GradColors(bWork, ar, ag, ab)
                End If
            Else
                Array.Copy(MappingsLuminance, alum, MappingsLuminance.Length)
                fipbmp.gradColors(bWork, alum, Multiplicator)
            End If
        Catch
            If Not bWork Is Nothing Then
                bWork.Dispose()
                bWork = Nothing
            End If
        End Try
        Return bWork
    End Function

    Private Sub CheckBox12_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox12.CheckedChanged
        If Me.CheckBox12.Checked Then
            Me.pictureBox1.BackColor = SystemColors.ControlDarkDark
        Else
            Me.pictureBox1.BackColor = SystemColors.Control
        End If
    End Sub

    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click
        If Me.ColorDialog1.ShowDialog() = DialogResult.OK Then
            Dim c As Color = Me.ColorDialog1.Color
            Me.TextBox1.Text = c.A & ";" & c.R & ";" & c.G & ";" & c.B
        End If
    End Sub

    Private Sub ListBox4_DoubleClick(sender As Object, e As EventArgs) Handles ListBox4.DoubleClick
        Try
            Dim pt As Point = CType(CType(sender, ListBox).SelectedItem, Point)
            NumericUpDown13.Value = CType(pt.X, Decimal)
            NumericUpDown12.Value = CType(pt.Y, Decimal)
        Catch

        End Try
    End Sub
End Class
