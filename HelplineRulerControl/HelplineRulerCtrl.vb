Option Strict On

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms.VisualStyles.VisualStyleElement


Partial Public Class HelplineRulerCtrl
    Inherits UserControl
    Private _menuItemMousePoint As New Point(0, 0)
    Private _helpLines As New List(Of HelpLineControl)()
    Private _curLoc As Point
    Private _dragging As Boolean
    Private _statusFrm As New frmStatus

    Private _zoom As Single = 1.0F
    Dim _dontDoLayout As Boolean
    Public Property ZoomSetManually As Boolean

    Public Property DontDoLayout As Boolean
        Get
            Return _dontDoLayout
        End Get
        Set(value As Boolean)
            _dontDoLayout = value
        End Set
    End Property

    Public Property IgnoreZoom As Boolean

    Public Property Zoom As Single
        Get
            Return _zoom
        End Get
        Set(value As Single)
            _zoom = value

            If value > 0.0F Then
                Try
                    SetZoomToRulers()
                    SetZoomLocationPictureSizeToHelpLines()
                Catch
                End Try
            End If
        End Set
    End Property

    Private _bmpTmp As Bitmap = Nothing
    Public ReadOnly Property BmpTmp As Bitmap
        Get
            Return _bmpTmp
        End Get
    End Property

    Private _bmp As Bitmap = Nothing
    Public Property Bmp() As Bitmap
        Get
            Return _bmp
        End Get
        Set(value As Bitmap)
            _bmp = value

            If value IsNot Nothing Then
                SetZoomToRulers()
                SetZoomLocationPictureSizeToHelpLines()
            End If
        End Set
    End Property

    Public Property DontPaintBaseImg As Boolean

    Public Property MoveHelpLinesOnResize As Boolean
        Get
            Return m_MoveHelpLinesOnResize
        End Get
        Set(value As Boolean)
            m_MoveHelpLinesOnResize = value
        End Set
    End Property
    Private m_MoveHelpLinesOnResize As Boolean
    Public Property DontProcDoubleClick As Boolean
        Get
            Return m_DontProcDoubleClick
        End Get
        Set(value As Boolean)
            m_DontProcDoubleClick = value
        End Set
    End Property
    Private m_DontProcDoubleClick As Boolean

    Public Property DontHandleDoubleClick As Boolean
    Public Shared Property MaxBmpLength As Integer = Int32.MaxValue
    Private _autoScrollPoint As Point
    Public Property SetZoomOnlyByMethodCall As Boolean

    Public ReadOnly Property Helplines As List(Of HelpLineControl)
        Get
            Return _helpLines
        End Get
    End Property

    Public Event ContentPanelDoubleClicked As EventHandler
    Public Event HelpLineAdded As EventHandler

    Public Delegate Sub PostPaintEventHandler(sender As Object, e As PaintEventArgs)
    Public Event PostPaint As PostPaintEventHandler

    Public Delegate Sub DblClickedEventHandler(sender As Object, e As ZoomEventArgs)
    Public Event DBPanelDblClicked As DblClickedEventHandler

    Public Event HelpLineLocationChanged As EventHandler(Of HelpLineLocationEventArgs)

    Public Event HelpLineMouseDown As EventHandler(Of MouseEventArgs)
    Public Event HelpLineMouseMove As EventHandler(Of MouseEventArgs)
    Public Event HelpLineMouseUp As EventHandler(Of MouseEventArgs)

    Private _shiftKey As Boolean

    Private _zoomWidth As Boolean
    Private _cnt As Integer

    Public Sub New()
        InitializeComponent()
        AddHandler Me.dbPanel1.MouseWheel, AddressOf _contentPanel_MouseWheel
    End Sub

    Public Sub AddDefaultHelplines()
        AddHelpLine("horzHelpLine1", LayoutOrientation.OrientationHorizontal, True)
        AddHelpLine("vertHelpLine1", LayoutOrientation.OrientationVertical, True)
    End Sub

    Protected Overrides Sub OnLayout(e As LayoutEventArgs)
        MyBase.OnLayout(e)

        If Not Me.IsDisposed Then
            If Me.dbPanel1 IsNot Nothing Then
                For Each h As HelpLineControl In Me._helpLines
                    If h IsNot Nothing AndAlso Not h.IsDisposed Then
                        If h.Orientation = LayoutOrientation.OrientationHorizontal Then
                            h.Size = New Size(Me.leftPanel.Width + Me.dbPanel1.ClientRectangle.Width, h.Height)
                            h.ParentSize = New Size(Me.leftPanel.Width + Me.dbPanel1.ClientRectangle.Width, h.Height)
                        Else
                            h.Size = New Size(h.Width, Me.topPanel.Height + Me.dbPanel1.ClientRectangle.Height)
                            h.ParentSize = New Size(h.Width, Me.topPanel.Height + Me.dbPanel1.ClientRectangle.Height)
                        End If

                        h.Zoom = Me.Zoom

                        If Me.MoveHelpLinesOnResize = False Then
                            h.SetText()
                        End If
                    End If
                Next
            End If
        End If
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)

        SetZoomToRulers()
        'SetZoomLocationPictureSizeToHelpLines(); 

        'new code on 06/08/2023, remove, when problems occur
        If Me.Bmp IsNot Nothing Then
            Me.Enabled = False
            Me.dbPanel1.AutoScrollMinSize = New Size(0, 0) 'helper
            Me.dbPanel1.AutoScrollMinSize = New Size(CInt(Me.Bmp.Width * Me.Zoom), CInt(Math.Floor(Me.Bmp.Height * Me.Zoom)))
            Me.Enabled = True
            'Me.Refresh()

            Console.WriteLine(_cnt.ToString() & " - " & Me.dbPanel1.AutoScrollMinSize.ToString())
            _cnt += 1
        End If
    End Sub

    Private Sub _contentPanel_Scroll(sender As Object, e As ScrollEventArgs) Handles dbPanel1.Scroll
        SetHelpLinePositions(e)
        SetRulersText(e)
    End Sub

    Private Sub _contentPanel_MouseWheel(sender As Object, e As MouseEventArgs)
        Me.topPanel.DrawStart = Me.dbPanel1.AutoScrollPosition.X
        Me.topPanel.Invalidate()
        Me.leftPanel.DrawStart = Me.dbPanel1.AutoScrollPosition.Y
        Me.leftPanel.Invalidate()
        SetHelpLinePositions(Me.dbPanel1.AutoScrollPosition.Y - Me._autoScrollPoint.Y)
        Me._autoScrollPoint = Me.dbPanel1.AutoScrollPosition
    End Sub

#Region "Rulers"

    Private Sub SetRulersText(e As ScrollEventArgs)
        If e.ScrollOrientation = ScrollOrientation.HorizontalScroll Then
            Me.topPanel.DrawStart += (e.OldValue - e.NewValue)
            Me.topPanel.Invalidate()
        Else
            Me.leftPanel.DrawStart += (e.OldValue - e.NewValue)
            Me.leftPanel.Invalidate()
        End If
    End Sub

    Private Sub SetZoomToRulers()
        If Me.dbPanel1 IsNot Nothing AndAlso Me.Zoom > 0.0F Then
            Me.topPanel.Zoom = Me.Zoom
            Me.leftPanel.Zoom = Me.Zoom
            Me.topPanel.DrawStart = Me.dbPanel1.AutoScrollPosition.X
            Me.leftPanel.DrawStart = Me.dbPanel1.AutoScrollPosition.Y
            Me._autoScrollPoint = Me.dbPanel1.AutoScrollPosition
            Me.topPanel.Invalidate()
            Me.leftPanel.Invalidate()
        End If
    End Sub

    Public Sub SetRulersBackColor(color As Color)
        Me.topPanel.BackColor = color
        Me.leftPanel.BackColor = color
    End Sub

#End Region

#Region "HelpLines"

    Private Sub AddHelpLine(FName As String, Direction As LayoutOrientation, initial As Boolean)
        If Me.dbPanel1 IsNot Nothing Then
            If Direction = LayoutOrientation.OrientationHorizontal Then
                Dim horzHelpLine As New HelpLineControl()
                horzHelpLine.ParentSize = New Size(Me.ClientSize.Width, 100)
                horzHelpLine.LabelSize = New Size(Me.leftPanel.Width, 50)
                horzHelpLine.Orientation = LayoutOrientation.OrientationHorizontal
                horzHelpLine.Location = New Point(0, _menuItemMousePoint.Y + Me.topPanel.Height - horzHelpLine.horzVertLabel1.Height)
                horzHelpLine.LayoutCorrection = -Me.topPanel.Height + horzHelpLine.horzVertLabel1.Height
                horzHelpLine.Name = FName
                AddHandler horzHelpLine.horzVertLabel1.MouseDown, AddressOf horzHelpLine_horzVertLabel1_MouseDown
                AddHandler horzHelpLine.horzVertLabel1.MouseMove, AddressOf horzHelpLine_horzVertLabel1_MouseMove
                AddHandler horzHelpLine.horzVertLabel1.MouseUp, AddressOf horzHelpLine_horzVertLabel1_MouseUp
                AddHandler horzHelpLine.horzVertLabel2.MouseDown, AddressOf horzHelpLine_horzVertLabel2_MouseDown
                AddHandler horzHelpLine.horzVertLabel2.MouseMove, AddressOf horzHelpLine_horzVertLabel2_MouseMove
                AddHandler horzHelpLine.horzVertLabel2.MouseUp, AddressOf horzHelpLine_horzVertLabel2_MouseUp
                AddHandler horzHelpLine.panel1.MouseDown, AddressOf horzHelpLine_panel1_MouseDown
                AddHandler horzHelpLine.panel1.MouseMove, AddressOf horzHelpLine_panel1_MouseMove
                AddHandler horzHelpLine.panel1.MouseUp, AddressOf horzHelpLine_panel1_MouseUp
                AddHandler horzHelpLine.removeToolStripMenuItem.Click, AddressOf removeToolStripMenuItem_Click
                AddHandler horzHelpLine.switchColorToolStripMenuItem.Click, AddressOf switchColorToolStripMenuItem_Click
                AddHandler horzHelpLine.RemoveHelpLine, AddressOf horzHelpLine_RemoveThis
                AddHandler horzHelpLine.LocationUpdated, AddressOf horzHelpLine_LocationUpdated

                Me._helpLines.Add(horzHelpLine)
                Me.Controls.Add(horzHelpLine)
                horzHelpLine.BringToFront()
                horzHelpLine.Zoom = Me.Zoom
                SetParentAutoScrollPosition(horzHelpLine)
                If Me.Bmp IsNot Nothing Then
                    Try
                        horzHelpLine.SetText()
                        horzHelpLine.Picturesize = Me.Bmp.Size

                        If Not initial Then
                            _dragging = True
                            horzHelpLine_horzVertLabel1_MouseMove(horzHelpLine.horzVertLabel1, New MouseEventArgs(MouseButtons.Left, 1, horzHelpLine.Location.X, 0, 0))
                            _dragging = False
                        End If

                    Catch
                    End Try
                End If
            Else
                Dim vertHelpLine As New HelpLineControl()
                vertHelpLine.ParentSize = New Size(100, Me.ClientSize.Height)
                vertHelpLine.LabelSize = New Size(50, Me.topPanel.Height)
                vertHelpLine.Orientation = LayoutOrientation.OrientationVertical
                vertHelpLine.Location = New Point(_menuItemMousePoint.X + Me.leftPanel.Width - vertHelpLine.horzVertLabel1.Width, 0)
                vertHelpLine.LayoutCorrection = -Me.leftPanel.Width + vertHelpLine.horzVertLabel1.Width
                vertHelpLine.Name = FName
                AddHandler vertHelpLine.horzVertLabel1.MouseDown, AddressOf horzHelpLine_horzVertLabel1_MouseDown
                AddHandler vertHelpLine.horzVertLabel1.MouseMove, AddressOf horzHelpLine_horzVertLabel1_MouseMove
                AddHandler vertHelpLine.horzVertLabel1.MouseUp, AddressOf horzHelpLine_horzVertLabel1_MouseUp
                AddHandler vertHelpLine.horzVertLabel2.MouseDown, AddressOf horzHelpLine_horzVertLabel2_MouseDown
                AddHandler vertHelpLine.horzVertLabel2.MouseMove, AddressOf horzHelpLine_horzVertLabel2_MouseMove
                AddHandler vertHelpLine.horzVertLabel2.MouseUp, AddressOf horzHelpLine_horzVertLabel2_MouseUp
                AddHandler vertHelpLine.panel1.MouseDown, AddressOf horzHelpLine_panel1_MouseDown
                AddHandler vertHelpLine.panel1.MouseMove, AddressOf horzHelpLine_panel1_MouseMove
                AddHandler vertHelpLine.panel1.MouseUp, AddressOf horzHelpLine_panel1_MouseUp
                AddHandler vertHelpLine.removeToolStripMenuItem.Click, AddressOf removeToolStripMenuItem_Click
                AddHandler vertHelpLine.switchColorToolStripMenuItem.Click, AddressOf switchColorToolStripMenuItem_Click
                AddHandler vertHelpLine.RemoveHelpLine, AddressOf horzHelpLine_RemoveThis
                AddHandler vertHelpLine.LocationUpdated, AddressOf vertHelpLine_LocationUpdated

                Me._helpLines.Add(vertHelpLine)
                Me.Controls.Add(vertHelpLine)
                vertHelpLine.BringToFront()
                vertHelpLine.Zoom = Me.Zoom
                SetParentAutoScrollPosition(vertHelpLine)
                If Me.Bmp IsNot Nothing Then
                    Try
                        vertHelpLine.SetText()
                        vertHelpLine.Picturesize = Me.Bmp.Size

                        If Not initial Then
                            _dragging = True
                            horzHelpLine_horzVertLabel1_MouseMove(vertHelpLine.horzVertLabel1, New MouseEventArgs(MouseButtons.Left, 1, vertHelpLine.Location.X, 0, 0))
                            _dragging = False
                        End If

                    Catch
                    End Try
                End If
            End If
        End If
    End Sub

    Private Sub switchColorToolStripMenuItem_Click(sender As Object, e As EventArgs)
        Dim t As ToolStripMenuItem = DirectCast(sender, ToolStripMenuItem)
        Dim pl As HelpLineControl = FindHelpLine(t, 1)
        If pl IsNot Nothing Then
            If pl.Color.ToArgb().Equals(Color.Cyan.ToArgb()) Then
                pl.Color = Color.Red
            Else
                pl.Color = Color.Cyan
            End If
        End If
    End Sub

    Public Sub ResetAllHelpLineLabelsColor()
        For Each h As HelpLineControl In Me._helpLines
            h.horzVertLabel1.BackColor = SystemColors.Control
            h.horzVertLabel2.BackColor = SystemColors.Control
        Next
    End Sub

    Private Sub horzHelpLine_RemoveThis(sender As Object, e As EventArgs)
        Dim pl As HelpLineControl = DirectCast(sender, HelpLineControl)
        Me._helpLines.Remove(pl)
        Me.Controls.Remove(pl)
        pl.Dispose()
    End Sub

    Private Sub horzHelpLine_panel1_MouseDown(sender As Object, e As MouseEventArgs)
        If Me.dbPanel1 IsNot Nothing Then
            Dim p As Point = DirectCast(sender, Control).PointToScreen(e.Location)
            'contentPanel_MouseDown(this.dbPanel1, new MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta))
            Dim pt As Point = Me.dbPanel1.PointToClient(p)
            RaiseEvent HelpLineMouseDown(sender, New MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta))
        End If
    End Sub

    Private Sub horzHelpLine_panel1_MouseMove(sender As Object, e As MouseEventArgs)
        If Me.dbPanel1 IsNot Nothing Then
            Dim p As Point = DirectCast(sender, Control).PointToScreen(e.Location)
            'contentPanel_MouseMove(this.dbPanel1, new MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta));
            Dim pt As Point = Me.dbPanel1.PointToClient(p)
            RaiseEvent HelpLineMouseMove(sender, New MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta))
        End If
    End Sub

    Private Sub horzHelpLine_panel1_MouseUp(sender As Object, e As MouseEventArgs)
        If Me.dbPanel1 IsNot Nothing Then
            Dim p As Point = DirectCast(sender, Control).PointToScreen(e.Location)
            'contentPanel_MouseUp(this.dbPanel1, new MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta));
            Dim pt As Point = Me.dbPanel1.PointToClient(p)
            RaiseEvent HelpLineMouseUp(sender, New MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta))
        End If
    End Sub

    Private Sub removeToolStripMenuItem_Click(sender As Object, e As EventArgs)
        Dim t As ToolStripMenuItem = DirectCast(sender, ToolStripMenuItem)
        Dim pl As HelpLineControl = FindHelpLine(t)
        If pl IsNot Nothing Then
            Me._helpLines.Remove(pl)
            Me.Controls.Remove(pl)
            pl.Dispose()
        End If
    End Sub

    Private Function FindHelpLine(t As ToolStripMenuItem) As HelpLineControl
        For Each pl As HelpLineControl In Me._helpLines
            If pl.contextMenuStrip1.Items(0).Equals(t) Then
                Return pl
            End If
        Next
        Return Nothing
    End Function

    Private Function FindHelpLine(t As ToolStripMenuItem, index As Integer) As HelpLineControl
        For Each pl As HelpLineControl In Me._helpLines
            If pl.contextMenuStrip1.Items(index).Equals(t) Then
                Return pl
            End If
        Next
        Return Nothing
    End Function

    Private Sub horzHelpLine_horzVertLabel1_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = System.Windows.Forms.MouseButtons.Left Then
            _dragging = True
            Dim h As HelplineRulerControl.HorzVertLabelControl = DirectCast(sender, HelplineRulerControl.HorzVertLabelControl)
            _curLoc = New Point(-e.X, -e.Y)
        End If
    End Sub

    Private Sub horzHelpLine_horzVertLabel1_MouseMove(sender As Object, e As MouseEventArgs)
        If Me.dbPanel1 IsNot Nothing Then
            If e.Button = System.Windows.Forms.MouseButtons.Left AndAlso Me._dragging AndAlso Me.Bmp IsNot Nothing Then
                Dim h As HelplineRulerControl.HorzVertLabelControl = DirectCast(sender, HelplineRulerControl.HorzVertLabelControl)
                Dim pl As HelpLineControl = DirectCast(h.Parent, HelpLineControl)

                SetParentAutoScrollPosition(pl)

                Try
                    If pl.Orientation = LayoutOrientation.OrientationHorizontal Then
                        Dim mousePos As Point = Me.PointToClient(Control.MousePosition)
                        mousePos.Offset(0, _curLoc.Y)

                        Dim f As Integer = 0

                        If Not Me.IgnoreZoom Then
                            f = CInt(Math.Ceiling(Me.Bmp.Height * Zoom)) + Me.leftPanel.Width + Me.dbPanel1.AutoScrollPosition.Y
                        Else
                            f = CInt(Math.Ceiling(Me.Bmp.Height)) + Me.leftPanel.Width + Me.dbPanel1.AutoScrollPosition.Y
                        End If

                        If ((mousePos.Y > Me.leftPanel.Width - 1) OrElse (Me.dbPanel1.AutoScrollPosition.Y <> 0)) AndAlso mousePos.Y < f Then
                            pl.Location = New Point(h.Location.X, mousePos.Y - h.ClientSize.Height)
                        Else
                            If mousePos.Y < Me.leftPanel.Width Then
                                pl.Location = New Point(h.Location.X, Me.leftPanel.Width - h.ClientSize.Height)
                            Else
                                pl.Location = New Point(h.Location.X, f - h.ClientSize.Height)
                            End If
                        End If

                        If pl.Mode <> LayoutMode.LayoutMode2 AndAlso pl.Location.Y > f - h.ClientSize.Height * 2 Then
                            pl.Mode = LayoutMode.LayoutMode2
                            pl.PerformLayout()
                        End If

                        If pl.Mode <> LayoutMode.LayoutMode1 AndAlso pl.Location.Y < f - h.ClientSize.Height * 2 Then
                            pl.Mode = LayoutMode.LayoutMode1
                            pl.PerformLayout()
                        End If
                    Else
                        Dim mousePos As Point = Me.PointToClient(Control.MousePosition)
                        mousePos.Offset(_curLoc.X, 0)
                        Dim f As Integer = 0

                        If Not Me.IgnoreZoom Then
                            f = CInt(Math.Ceiling(Me.Bmp.Width * Zoom)) + Me.topPanel.Height + Me.dbPanel1.AutoScrollPosition.X
                        Else
                            f = CInt(Math.Ceiling(Me.Bmp.Width)) + Me.topPanel.Height + Me.dbPanel1.AutoScrollPosition.X
                        End If

                        If ((mousePos.X > Me.topPanel.Height - 1) OrElse (Me.dbPanel1.AutoScrollPosition.X <> 0)) AndAlso mousePos.X < f Then
                            pl.Location = New Point(mousePos.X - h.ClientSize.Width, h.Location.Y)
                        Else
                            If mousePos.X < Me.topPanel.Height Then
                                pl.Location = New Point(Me.topPanel.Height - h.ClientSize.Width, h.Location.Y)
                            Else
                                pl.Location = New Point(f - h.ClientSize.Width, h.Location.Y)

                            End If
                        End If

                        If pl.Mode <> LayoutMode.LayoutMode2 AndAlso pl.Location.X > f - h.ClientSize.Width * 2 Then
                            pl.Mode = LayoutMode.LayoutMode2
                            pl.PerformLayout()
                        End If

                        If pl.Mode <> LayoutMode.LayoutMode1 AndAlso pl.Location.X < f - h.ClientSize.Width * 2 Then
                            pl.Mode = LayoutMode.LayoutMode1
                            pl.PerformLayout()
                        End If
                    End If

                Catch
                End Try
            End If
        End If
    End Sub

    Private Sub horzHelpLine_horzVertLabel1_MouseUp(sender As Object, e As MouseEventArgs)
        _dragging = False

        Dim h As HelplineRulerControl.HorzVertLabelControl = DirectCast(sender, HelplineRulerControl.HorzVertLabelControl)
        Dim pl As HelpLineControl = DirectCast(h.Parent, HelpLineControl)

        SetParentAutoScrollPosition(pl)
        pl.SetText()

        'RaiseEvent HelpLineMouseUp(Me, New HelpLineMouseEventArgs() With {.Location = pl.CurPos, .Zoom = pl.Zoom})
    End Sub

    Private Sub horzHelpLine_horzVertLabel2_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = System.Windows.Forms.MouseButtons.Left Then
            _dragging = True
            Dim h As HelplineRulerControl.HorzVertLabelControl = DirectCast(sender, HelplineRulerControl.HorzVertLabelControl)
            _curLoc = New Point(-e.X, -e.Y)
        End If
    End Sub

    Private Sub horzHelpLine_horzVertLabel2_MouseMove(sender As Object, e As MouseEventArgs)
        If Me.dbPanel1 IsNot Nothing Then
            If e.Button = System.Windows.Forms.MouseButtons.Left AndAlso Me._dragging AndAlso Me.Bmp IsNot Nothing Then
                Try
                    Dim h As HelplineRulerControl.HorzVertLabelControl = DirectCast(sender, HelplineRulerControl.HorzVertLabelControl)
                    Dim pl As HelpLineControl = DirectCast(h.Parent, HelpLineControl)

                    SetParentAutoScrollPosition(pl)

                    If pl.Orientation = LayoutOrientation.OrientationHorizontal Then
                        Dim mousePos As Point = Me.PointToClient(Control.MousePosition)
                        mousePos.Offset(0, _curLoc.Y)

                        Dim f As Integer = 0
                        If Not Me.IgnoreZoom Then
                            f = CInt(Math.Ceiling(Me.Bmp.Height * Zoom)) + Me.leftPanel.Width + Me.dbPanel1.AutoScrollPosition.Y
                        Else
                            f = CInt(Math.Ceiling(Me.Bmp.Height)) + Me.leftPanel.Width + Me.dbPanel1.AutoScrollPosition.Y
                        End If

                        If ((mousePos.Y > Me.leftPanel.Width - 1) OrElse (Me.dbPanel1.AutoScrollPosition.Y <> 0)) AndAlso mousePos.Y < f - h.ClientSize.Height Then
                            pl.Location = New Point(h.Location.X, mousePos.Y)
                        Else
                            If mousePos.Y < Me.leftPanel.Width Then
                                pl.Location = New Point(h.Location.X, Me.leftPanel.Width)
                            Else
                                pl.Location = New Point(h.Location.X, f - h.ClientSize.Height)
                            End If
                        End If
                    Else
                        Dim mousePos As Point = Me.PointToClient(Control.MousePosition)
                        mousePos.Offset(_curLoc.X, 0)

                        Dim f As Integer = 0

                        If Not Me.IgnoreZoom Then
                            f = CInt(Math.Ceiling(Me.Bmp.Width * Zoom)) + Me.topPanel.Height + Me.dbPanel1.AutoScrollPosition.X
                        Else
                            f = CInt(Math.Ceiling(Me.Bmp.Width)) + Me.topPanel.Height + Me.dbPanel1.AutoScrollPosition.X
                        End If

                        If ((mousePos.X > Me.topPanel.Height - 1) OrElse (Me.dbPanel1.AutoScrollPosition.X <> 0)) AndAlso mousePos.X < f - h.ClientSize.Width Then
                            pl.Location = New Point(mousePos.X, h.Location.Y)
                        Else
                            If mousePos.X < Me.topPanel.Height Then
                                pl.Location = New Point(Me.topPanel.Height, h.Location.Y)
                            Else
                                pl.Location = New Point(f - h.ClientSize.Width, h.Location.Y)

                            End If
                        End If
                    End If

                Catch
                End Try
            End If
        End If
    End Sub

    Private Sub horzHelpLine_horzVertLabel2_MouseUp(sender As Object, e As MouseEventArgs)
        _dragging = False

        If Me.dbPanel1 IsNot Nothing Then
            Dim h As HelplineRulerControl.HorzVertLabelControl = DirectCast(sender, HelplineRulerControl.HorzVertLabelControl)
            Dim pl As HelpLineControl = DirectCast(h.Parent, HelpLineControl)

            SetParentAutoScrollPosition(pl)

            If pl.Orientation = LayoutOrientation.OrientationHorizontal Then
                Dim f As Integer = CInt(Math.Ceiling(Me.Bmp.Height * Zoom)) + Me.leftPanel.Width + Me.dbPanel1.AutoScrollPosition.Y

                If pl.Mode <> LayoutMode.LayoutMode2 AndAlso pl.Location.Y > f - h.ClientSize.Height * 2 Then
                    pl.Mode = LayoutMode.LayoutMode2
                End If

                If pl.Mode <> LayoutMode.LayoutMode1 AndAlso pl.Location.Y < f - h.ClientSize.Height * 2 Then
                    pl.Mode = LayoutMode.LayoutMode1
                End If
            Else
                Dim f As Integer = CInt(Math.Ceiling(Me.Bmp.Width * Zoom)) + Me.topPanel.Height + Me.dbPanel1.AutoScrollPosition.X

                If pl.Mode <> LayoutMode.LayoutMode2 AndAlso pl.Location.X > f - h.ClientSize.Width * 2 Then
                    pl.Mode = LayoutMode.LayoutMode2
                End If

                If pl.Mode <> LayoutMode.LayoutMode1 AndAlso pl.Location.X < f - h.ClientSize.Width * 2 Then
                    pl.Mode = LayoutMode.LayoutMode1
                End If
            End If

            pl.SetText()
            pl.PerformLayout()

            'RaiseEvent HelpLineMouseUp(Me, New HelpLineMouseEventArgs() With {.Location = pl.CurPos, .Zoom = pl.Zoom})
        End If
    End Sub

    Private Sub SetParentAutoScrollPosition(pl As HelpLineControl)
        If Me.dbPanel1 IsNot Nothing Then
            pl.ParentAutoScrollPosition = Me.dbPanel1.AutoScrollPosition
        End If
    End Sub

    Private Sub addHelpLineToolStripMenuItem1_MouseDown(sender As Object, e As MouseEventArgs) Handles addHelpLineToolStripMenuItem1.MouseDown
        _menuItemMousePoint = topPanel.PointToClient(Control.MousePosition)
        _menuItemMousePoint.Offset(-Me.leftPanel.Width, 0)
    End Sub

    Private Sub addHelpLineToolStripMenuItem_MouseDown(sender As Object, e As MouseEventArgs) Handles addHelpLineToolStripMenuItem.MouseDown
        _menuItemMousePoint = leftPanel.PointToClient(Control.MousePosition)
        _menuItemMousePoint.Offset(0, -Me.topPanel.Height)
    End Sub

    Private Sub SetHelpLinePositions(Delta As Integer)
        For Each pl As HelpLineControl In Me._helpLines
            If pl.Orientation = LayoutOrientation.OrientationHorizontal Then
                SetParentAutoScrollPosition(pl)
                pl.Location = New Point(pl.Location.X, pl.Location.Y + Delta)
                'if (pl.Orientation == HelpLineControl.LayoutOrientation.OrientationVertical)
                '{
                '    SetParentAutoScrollPosition(pl);
                '    pl.Location = new Point(pl.Location.X + Delta, pl.Location.Y);
                '}
            End If
        Next
    End Sub

    Private Sub SetHelpLinePositions(e As ScrollEventArgs)
        For Each pl As HelpLineControl In Me._helpLines
            If pl.Orientation = LayoutOrientation.OrientationHorizontal AndAlso e.ScrollOrientation = ScrollOrientation.VerticalScroll Then
                SetParentAutoScrollPosition(pl)
                pl.Location = New Point(pl.Location.X, pl.Location.Y + (e.OldValue - e.NewValue))
            End If
            If pl.Orientation = LayoutOrientation.OrientationVertical AndAlso e.ScrollOrientation = ScrollOrientation.HorizontalScroll Then
                SetParentAutoScrollPosition(pl)
                pl.Location = New Point(pl.Location.X + (e.OldValue - e.NewValue), pl.Location.Y)
            End If
        Next
    End Sub

    Private Sub addHelpLineToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles addHelpLineToolStripMenuItem1.Click
        'vert
        Dim f As String = Me._helpLines.Count.ToString()
        AddHelpLine(Convert.ToString("vertHelpLine") & f, LayoutOrientation.OrientationVertical, False)
        ResetAllHelpLineLabelsColor()
        OnHelpLineAdded(Me._helpLines(Me._helpLines.Count - 1))
    End Sub

    Private Sub addHelpLineToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles addHelpLineToolStripMenuItem.Click
        'horz
        Dim f As String = Me._helpLines.Count.ToString()
        AddHelpLine(Convert.ToString("horzHelpLine") & f, LayoutOrientation.OrientationHorizontal, False)
        ResetAllHelpLineLabelsColor()
        OnHelpLineAdded(Me._helpLines(Me._helpLines.Count - 1))
    End Sub

    Private Sub OnHelpLineAdded(helpLineControl As HelpLineControl)
        'Dim handler As EventHandler = Me.HelpLineAdded

        'If handler IsNot Nothing Then
        RaiseEvent HelpLineAdded(helpLineControl, New EventArgs())
        'End If
    End Sub

    Private Sub SetZoomLocationPictureSizeToHelpLines()
        If Me.dbPanel1 IsNot Nothing AndAlso Me.Bmp IsNot Nothing Then
            For Each h As HelpLineControl In Me._helpLines
                Try
                    h.Zoom = Me.Zoom
                    h.Picturesize = Me.Bmp.Size

                    If h.Orientation = LayoutOrientation.OrientationHorizontal Then
                        SetParentAutoScrollPosition(h)
                        h.Location = New Point(0, CInt(Math.Ceiling(h.CurPos * h.Zoom)) - h.LayoutCorrection + h.ParentAutoScrollPosition.Y)
                    Else
                        SetParentAutoScrollPosition(h)
                        h.Location = New Point(CInt(Math.Ceiling(h.CurPos * h.Zoom)) - h.LayoutCorrection + h.ParentAutoScrollPosition.X, 0)
                    End If
                Catch
                End Try
            Next
        End If
    End Sub

    Public Sub SetHelplineScaleFactor(h As HelpLineControl)
        h.ScaleFactor = Me.topPanel.ScaleFactor
    End Sub

#End Region

    Public Sub SetRulersAndHelplinesScaleFactor(p As Single)
        Me.topPanel.ScaleFactor = p
        Me.leftPanel.ScaleFactor = p

        For Each h As HelpLineControl In Me._helpLines
            h.ScaleFactor = p
        Next
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        'this._controlKey = false;  
        Me._shiftKey = False

        'if ((keyData & Keys.Control) == Keys.Control)
        '    this._controlKey = true;    

        If (keyData And Keys.Shift) = Keys.Shift Then
            Me._shiftKey = True
        End If

        If keyData = (Keys.Enter Or Keys.Shift) Then
            If Me.Bmp IsNot Nothing Then
                OnContentPanelDoubleClicked()
                Using frm As New DisplayBitmapForm()
                    Me.DontProcDoubleClick = True
                    frm.pictureBox1.Image = Me.Bmp
                    frm.WindowState = FormWindowState.Maximized
                    frm.ShowDialog()
                End Using

                Me._shiftKey = False
            End If

            Return True
        End If

        If Me.dbPanel1 IsNot Nothing Then
            If keyData = Keys.Down Then
                dbPanel1.AutoScrollPosition = New Point(-dbPanel1.AutoScrollPosition.X, -dbPanel1.AutoScrollPosition.Y + 4)
                Return True
            End If
            If keyData = Keys.Up Then
                dbPanel1.AutoScrollPosition = New Point(-dbPanel1.AutoScrollPosition.X, -dbPanel1.AutoScrollPosition.Y - 4)
                Return True
            End If
            If keyData = Keys.Left Then
                dbPanel1.AutoScrollPosition = New Point(-dbPanel1.AutoScrollPosition.X - 4, -dbPanel1.AutoScrollPosition.Y)
                Return True
            End If
            If keyData = Keys.Right Then
                dbPanel1.AutoScrollPosition = New Point(-dbPanel1.AutoScrollPosition.X + 4, -dbPanel1.AutoScrollPosition.Y)
                Return True
            End If
            If keyData = Keys.PageDown Then
                dbPanel1.AutoScrollPosition = New Point(-dbPanel1.AutoScrollPosition.X, -dbPanel1.AutoScrollPosition.Y + dbPanel1.ClientSize.Height)
                Return True
            End If
            If keyData = Keys.PageUp Then
                dbPanel1.AutoScrollPosition = New Point(-dbPanel1.AutoScrollPosition.X, -dbPanel1.AutoScrollPosition.Y - dbPanel1.ClientSize.Height)
                Return True
            End If
            If keyData = Keys.Home Then
                dbPanel1.AutoScrollPosition = New Point(-Int16.MaxValue, -dbPanel1.AutoScrollPosition.Y)
                Return True
            End If
            If keyData = Keys.[End] Then
                dbPanel1.AutoScrollPosition = New Point(Int16.MaxValue, -dbPanel1.AutoScrollPosition.Y)
                Return True
            End If
            If keyData = (Keys.Home Or Keys.Control) Then
                dbPanel1.AutoScrollPosition = New Point(-Int16.MaxValue, -Int16.MaxValue)
                Return True
            End If
            If keyData = (Keys.[End] Or Keys.Control) Then
                dbPanel1.AutoScrollPosition = New Point(Int16.MaxValue, Int16.MaxValue)
                Return True
            End If
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Public Sub _contentPanel_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles dbPanel1.MouseDoubleClick
        If e.Button = System.Windows.Forms.MouseButtons.Left Then
            If Me.Bmp IsNot Nothing AndAlso Me._shiftKey Then
                Me._shiftKey = False
                OnContentPanelDoubleClicked()
                Using frm As New DisplayBitmapForm()
                    Me.DontProcDoubleClick = True
                    frm.pictureBox1.Image = Me.Bmp
                    frm.WindowState = FormWindowState.Maximized
                    frm.ShowDialog()
                End Using
            End If
        End If

        _dontDoLayout = True
        If Not Me.SetZoomOnlyByMethodCall AndAlso Not Me.DontHandleDoubleClick AndAlso _bmp IsNot Nothing AndAlso Not Me.DontProcDoubleClick Then
            Dim zoom As Single = Me.Zoom
            If zoom <> 1.0F AndAlso Not _zoomWidth Then
                zoom = 1.0F
            ElseIf zoom = 1.0F Then
                zoom = CSng(CDbl(Me.dbPanel1.ClientSize.Width) / CDbl(Me._bmp.Width))
                Me._zoomWidth = True
            ElseIf zoom <> 1.0F AndAlso _zoomWidth Then
                Dim faktor As Double = CDbl(dbPanel1.Width) / CDbl(dbPanel1.Height)
                Dim multiplier As Double = CDbl(_bmp.Width) / CDbl(_bmp.Height)
                If multiplier >= faktor Then
                    zoom = CSng(CDbl(dbPanel1.Width) / CDbl(_bmp.Width))
                Else
                    zoom = CSng(CDbl(dbPanel1.Height) / CDbl(_bmp.Height))
                End If
                Me._zoomWidth = False
            End If

            Me.dbPanel1.AutoScrollMinSize = New Size(CInt(_bmp.Width * zoom), CInt(_bmp.Height * zoom))
            Me.Zoom = zoom
            Me.ZoomSetManually = False

            MakeBitmap(_bmp)

            Me.dbPanel1.Invalidate()
        End If

        Me.DontProcDoubleClick = False
        Me._dontDoLayout = False

        RaiseEvent DBPanelDblClicked(Me, New ZoomEventArgs(Zoom, _zoomWidth))
    End Sub

    Private Sub SetHRControlVars()
        Me.Bmp = _bmp
    End Sub

    Public Sub MakeBitmap(bmp As Bitmap)
        If Zoom <> 0.0 AndAlso Not Me.DontHandleDoubleClick AndAlso bmp IsNot Nothing Then
            Dim bOLd As Bitmap = _bmpTmp
            Dim w2 As Integer = CInt(Math.Ceiling(bmp.Width * Me.Zoom))
            Dim h2 As Integer = CInt(Math.Ceiling(bmp.Height * Me.Zoom))

            If AvailMem.AvailMem.checkAvailRam(w2 * h2 * 4L) Then
                _bmpTmp = New Bitmap(w2, h2)
            Else
                Return
            End If

            Using g As Graphics = Graphics.FromImage(Me._bmpTmp)
                g.DrawImage(bmp, 0, 0, CInt(Math.Ceiling(bmp.Width * Me.Zoom)), CInt(Math.Ceiling(bmp.Height * Me.Zoom)))
            End Using

            If bOLd IsNot Nothing Then
                bOLd.Dispose()
            End If

            SetHRControlVars()
        End If
    End Sub

    Protected Overridable Sub OnContentPanelDoubleClicked()
        'Dim handler As EventHandler = Me.ContentPanelDoubleClicked
        'If handler IsNot Nothing Then
        RaiseEvent ContentPanelDoubleClicked(Me.dbPanel1, New EventArgs())
        'End If
    End Sub

    Private Sub dbPanel1_Paint(sender As Object, e As PaintEventArgs) Handles dbPanel1.Paint
        If Not Me.DontPaintBaseImg Then
            Dim pz As DBPanel = DirectCast(sender, DBPanel)
            Dim g As Graphics = e.Graphics

            Dim pzX As Single = -pz.AutoScrollPosition.X
            Dim pzY As Single = -pz.AutoScrollPosition.Y

            If Me._bmpTmp IsNot Nothing Then
                g.DrawImage(Me._bmpTmp, New RectangleF(0, 0, Math.Min(pz.ClientRectangle.Width, _bmpTmp.Width), Math.Min(pz.ClientRectangle.Height, _bmpTmp.Height)),
                        New RectangleF(pzX, pzY, Math.Min(pz.ClientRectangle.Width, _bmpTmp.Width), Math.Min(pz.ClientRectangle.Height, _bmpTmp.Height)), GraphicsUnit.Pixel)
            ElseIf _bmp IsNot Nothing Then
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.PixelOffsetMode = PixelOffsetMode.Half
                g.DrawImage(_bmp, New RectangleF(0, 0, pz.ClientRectangle.Width, pz.ClientRectangle.Height), New RectangleF(-pz.AutoScrollPosition.X / Me.Zoom, -pz.AutoScrollPosition.Y / Me.Zoom, pz.ClientRectangle.Width / Me.Zoom, pz.ClientRectangle.Height / Me.Zoom), GraphicsUnit.Pixel)
            End If
        End If

        RaiseEvent PostPaint(Me, e)
    End Sub

    Private Sub dbPanel1_Click(sender As Object, e As EventArgs) Handles dbPanel1.Click
        'added set Autoscrollposition 10.11.2016
        Dim pt As Point = Me.AutoScrollPosition
        Me.dbPanel1.Focus()
        Me.AutoScrollPosition = New Point(-pt.X, -pt.Y)
    End Sub

    Private Sub DisposeBitmapData()
        If Me._bmpTmp IsNot Nothing Then
            Me._bmpTmp.Dispose()
            Me._bmpTmp = Nothing
        End If

        If Me._bmp IsNot Nothing Then
            Me._bmp.Dispose()
            Me._bmp = Nothing
        End If

        If Me._helpLines IsNot Nothing AndAlso Me._helpLines.Count > 0 Then
            'Testing
            'For Each h As HelpLineControl In Me._helpLines
            '    h.Dispose()
            '    h = Nothing
            'Next
            For i As Integer = 0 To Me._helpLines.Count - 1
                Me._helpLines(i).Dispose()
                Me._helpLines(i) = Nothing
            Next
        End If
    End Sub

    Public Sub CalculateZoom()
        If Me.Bmp IsNot Nothing Then
            If InvokeRequired Then
                Me.Invoke(New Action(Sub()
                                         Dim zoom As Single = Me.Zoom

                                         If zoom <> 1.0F AndAlso _zoomWidth Then
                                             zoom = CSng(CDbl(Me.dbPanel1.ClientSize.Width) / CDbl(Me.Bmp.Width))
                                             Me._zoomWidth = True
                                         ElseIf zoom <> 1.0F AndAlso Not _zoomWidth Then
                                             Dim faktor As Double = CDbl(dbPanel1.Width) / CDbl(dbPanel1.Height)
                                             Dim multiplier As Double = CDbl(Me.Bmp.Width) / CDbl(Me.Bmp.Height)
                                             If multiplier >= faktor Then
                                                 zoom = CSng(CDbl(dbPanel1.Width) / CDbl(Me.Bmp.Width))
                                             Else
                                                 zoom = CSng(CDbl(dbPanel1.Height) / CDbl(Me.Bmp.Height))
                                             End If
                                             Me._zoomWidth = False
                                         End If

                                         Me.dbPanel1.AutoScrollMinSize = New Size(CInt(Me.Bmp.Width * zoom), CInt(Me.Bmp.Height * zoom))
                                         Me.Zoom = zoom

                                         Me.MakeBitmap(Me.Bmp)

                                         Me.dbPanel1.Invalidate()

                                     End Sub))
            Else
                Dim zoom As Single = Me.Zoom

                If zoom <> 1.0F AndAlso _zoomWidth Then
                    zoom = CSng(CDbl(Me.dbPanel1.ClientSize.Width) / CDbl(Me.Bmp.Width))
                    Me._zoomWidth = True
                ElseIf zoom <> 1.0F AndAlso Not _zoomWidth Then
                    Dim faktor As Double = CDbl(dbPanel1.Width) / CDbl(dbPanel1.Height)
                    Dim multiplier As Double = CDbl(Me.Bmp.Width) / CDbl(Me.Bmp.Height)
                    If multiplier >= faktor Then
                        zoom = CSng(CDbl(dbPanel1.Width) / CDbl(Me.Bmp.Width))
                    Else
                        zoom = CSng(CDbl(dbPanel1.Height) / CDbl(Me.Bmp.Height))
                    End If
                    Me._zoomWidth = False
                End If

                Me.dbPanel1.AutoScrollMinSize = New Size(CInt(Me.Bmp.Width * zoom), CInt(Me.Bmp.Height * zoom))
                Me.Zoom = zoom

                Me.MakeBitmap(Me.Bmp)

                Me.dbPanel1.Invalidate()
            End If
        End If
    End Sub

    Private Sub dbPanel1_Resize(sender As System.Object, e As System.EventArgs) Handles dbPanel1.Resize
        If Me.Visible AndAlso Me._dontDoLayout = False Then
            If Not Me.ZoomSetManually Then
                CalculateZoom()
            End If
        End If
    End Sub

    Public Sub DrawHelpLineDistances(graphics As Graphics, offsetX As Integer, offsetY As Integer)
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit

        Using f As New Font(Me.Font.FontFamily, Me.Font.Size + 2, FontStyle.Bold)
            For Each pl As HelpLineControl In Me._helpLines
                If pl.Orientation = LayoutOrientation.OrientationHorizontal Then
                    Dim l As String = (_bmp.Height - pl.CurPos).ToString()
                    Dim sz As SizeF = graphics.MeasureString(l, f)
                    Dim l2 As [String] = " |" & vbLf & "V "
                    Dim sz2 As SizeF = graphics.MeasureString(l2, f)
                    If pl.Mode = LayoutMode.LayoutMode1 Then
                        graphics.FillRectangle(Brushes.LightGreen, New RectangleF(New PointF(71 + sz2.Width, -offsetY + pl.CurPos * _zoom + sz.Height), New SizeF(sz.Width + sz2.Height / 8.0F, sz.Height)))
                        graphics.DrawString(l2 & l, f, Brushes.Black, New PointF(71, -offsetY + pl.CurPos * _zoom))
                    Else
                        graphics.FillRectangle(Brushes.LightGreen, New RectangleF(New PointF(71 + sz2.Width, -offsetY + pl.CurPos * _zoom + sz.Height - pl.horzVertLabel1.ClientSize.Height), New SizeF(sz.Width + sz2.Height / 8.0F, sz.Height)))
                        graphics.DrawString(l2 & l, f, Brushes.Black, New PointF(71, -offsetY + pl.CurPos * _zoom - pl.horzVertLabel1.ClientSize.Height))
                    End If
                Else
                    Dim l As String = (_bmp.Width - pl.CurPos).ToString()
                    Dim sz As SizeF = graphics.MeasureString(l, f)
                    Dim l2 As [String] = " --> "
                    Dim sz2 As SizeF = graphics.MeasureString(l2, f)
                    If pl.Mode = LayoutMode.LayoutMode1 Then
                        graphics.FillRectangle(Brushes.LightSalmon, New RectangleF(New PointF(-offsetX + pl.CurPos * _zoom + sz2.Width + 2, 14), New SizeF(sz.Width + sz2.Height / 8.0F, sz.Height)))
                        graphics.DrawString(l2 & l, f, Brushes.Black, New PointF(-offsetX + pl.CurPos * _zoom, 12))
                    Else
                        graphics.FillRectangle(Brushes.LightSalmon, New RectangleF(New PointF(-offsetX + pl.CurPos * _zoom + sz2.Width - pl.horzVertLabel1.ClientSize.Width + 2, 14), New SizeF(sz.Width + sz2.Height / 8.0F, sz.Height)))
                        graphics.DrawString(l2 & l, f, Brushes.Black, New PointF(-offsetX + pl.CurPos * _zoom - pl.horzVertLabel1.ClientSize.Width, 12))
                    End If
                End If
            Next
        End Using
    End Sub

    Private Sub horzHelpLine_LocationUpdated(sender As Object, e As SingleEventArgs)
        If _bmp IsNot Nothing Then
            Me.dbPanel1.Invalidate(New Rectangle(50, 0, 100, Me.dbPanel1.ClientSize.Height))
            RaiseEvent HelpLineLocationChanged(sender, New HelpLineLocationEventArgs() With {.Location = e.Val})
        End If
    End Sub

    Private Sub vertHelpLine_LocationUpdated(sender As Object, e As SingleEventArgs)
        If _bmp IsNot Nothing Then
            Me.dbPanel1.Invalidate(New Rectangle(0, 0, Me.dbPanel1.ClientSize.Width, Me.dbPanel1.ClientSize.Height))
            RaiseEvent HelpLineLocationChanged(sender, New HelpLineLocationEventArgs() With {.Location = e.Val})
        End If
    End Sub

    Public Sub SetZoom(zoomString As String)
        _dontDoLayout = True
        If Not Me.DontHandleDoubleClick AndAlso _bmp IsNot Nothing AndAlso Not Me.DontProcDoubleClick Then
            Dim zoom As Single = Me.Zoom
            If zoomString = "1" Then
                zoom = 1.0F
            ElseIf zoomString.ToLower = "fit_width" Then
                zoom = CSng(CDbl(Me.dbPanel1.ClientSize.Width) / CDbl(Me._bmp.Width))
                Me._zoomWidth = True
            ElseIf zoomString.ToLower = "fit" Then
                Dim faktor As Double = CDbl(dbPanel1.Width) / CDbl(dbPanel1.Height)
                Dim multiplier As Double = CDbl(_bmp.Width) / CDbl(_bmp.Height)
                If multiplier >= faktor Then
                    zoom = CSng(CDbl(dbPanel1.Width) / CDbl(_bmp.Width))
                Else
                    zoom = CSng(CDbl(dbPanel1.Height) / CDbl(_bmp.Height))
                End If
                Me._zoomWidth = False
            Else
                Dim j As Single = 0
                If Me._statusFrm Is Nothing OrElse Me._statusFrm.IsDisposed Then
                    Me._statusFrm = New frmStatus()
                End If

                If Single.TryParse(zoomString, j) Then
                    Me.Cursor = Cursors.WaitCursor
                    If j > 1.0F Then
                        Me._statusFrm.Show(j)
                    End If
                    zoom = j
                End If
            End If

            Me.dbPanel1.AutoScrollMinSize = New Size(CInt(_bmp.Width * zoom), CInt(_bmp.Height * zoom))
            Me.Zoom = zoom

            MakeBitmap(_bmp)

            Me.dbPanel1.Invalidate()
            Me._statusFrm.Hide()
            Me.Cursor = Cursors.Default
        End If

        Me.DontProcDoubleClick = False
        Me._dontDoLayout = False
    End Sub

    Public Sub Reset()
        Me.DisposeBitmapData()
        Me.dbPanel1.Invalidate()
    End Sub
End Class
