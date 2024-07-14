Option Strict On

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Partial Public Class HelpLineControl
    Inherits UserControl
    Private _gPath As New GraphicsPath()
    Private _orientation As LayoutOrientation = LayoutOrientation.OrientationHorizontal
    Public Property ParentSize() As Size
        Get
            Return m_ParentSize
        End Get
        Set(value As Size)
            m_ParentSize = value
        End Set
    End Property
    Private m_ParentSize As Size
    Public Property LabelSize() As Size
        Get
            Return m_LabelSize
        End Get
        Set(value As Size)
            m_LabelSize = value
        End Set
    End Property
    Private m_LabelSize As Size
    Public Property LayoutCorrection() As Integer
        Get
            Return m_LayoutCorrection
        End Get
        Set(value As Integer)
            m_LayoutCorrection = value
        End Set
    End Property
    Private m_LayoutCorrection As Integer
    Public Property Zoom() As Single
        Get
            Return m_Zoom
        End Get
        Set(value As Single)
            m_Zoom = value
        End Set
    End Property
    Private m_Zoom As Single
    Public Property Mode() As LayoutMode
        Get
            Return m_Mode
        End Get
        Set(value As LayoutMode)
            m_Mode = value
        End Set
    End Property
    Private m_Mode As LayoutMode
    Public Property ParentAutoScrollPosition() As Point
        Get
            Return m_ParentAutoScrollPosition
        End Get
        Set(value As Point)
            m_ParentAutoScrollPosition = value
        End Set
    End Property
    Private m_ParentAutoScrollPosition As Point
    Public Property CurPos() As Integer
        Get
            Return m_CurPos
        End Get
        Set(value As Integer)
            m_CurPos = value
        End Set
    End Property
    Private m_CurPos As Integer
    Public Property Picturesize() As Size
        Get
            Return m_Picturesize
        End Get
        Set(value As Size)
            m_Picturesize = value
        End Set
    End Property
    Private m_Picturesize As Size
    Public Property ScaleFactor() As Single
        Get
            Return m_ScaleFactor
        End Get
        Set(value As Single)
            m_ScaleFactor = value
        End Set
    End Property
    Private m_ScaleFactor As Single

    Public Property Color() As Color
        Get
            Return Me.panel1.BackColor
        End Get
        Set(value As Color)
            Me.panel1.BackColor = value
        End Set
    End Property

    Public Property Orientation() As LayoutOrientation
        Get
            Return _orientation
        End Get
        Set(value As LayoutOrientation)
            _orientation = value
            SetlayoutDirection(value)
        End Set
    End Property

    Public Delegate Sub RemoveEventHandler(sender As Object, e As EventArgs)
    Public Event RemoveHelpLine As RemoveEventHandler

    Public Delegate Sub LocationUpdatedEventHandler(sender As Object, e As SingleEventArgs)
    Public Event LocationUpdated As LocationUpdatedEventHandler

    Public Sub New()
        InitializeComponent()
        Me.ParentSize = New Size(500, 100)
        Me.LabelSize = Me.horzVertLabel1.Size
        Me.LayoutCorrection = -20 + Me.horzVertLabel1.Height
        Me.Zoom = 1.0F
        Me.ScaleFactor = 1.0F
        Me.Mode = LayoutMode.LayoutMode1
        Me.Picturesize = New Size(1, 1)
    End Sub

    Private Sub SetlayoutDirection(value As LayoutOrientation)
        Me._gPath.Reset()
        Me.SuspendLayout()

        If value = LayoutOrientation.OrientationHorizontal Then
            Me.Size = Me.ParentSize

            Me.horzVertLabel1.Size = Me.LabelSize
            Me.panel1.Size = New Size(Me.ClientSize.Width, 1)
            Me.horzVertLabel2.Location = New Point(0, 0)
            Me.horzVertLabel1.Location = New Point(0, 50)
            Me.horzVertLabel1.Height = Me.ClientSize.Height \ 2
            Me.horzVertLabel2.Height = Me.ClientSize.Height \ 2
            Me.horzVertLabel1.Width = Me.LabelSize.Width
            Me.horzVertLabel2.Width = Me.LabelSize.Width
            Me.horzVertLabel1.Direction = TextDirection.DirectionVertical
            Me.horzVertLabel2.Direction = TextDirection.DirectionVertical
            If Me.Mode = LayoutMode.LayoutMode2 Then
                _gPath.AddRectangle(New Rectangle(Me.horzVertLabel2.Location, Me.horzVertLabel2.ClientSize))
            End If
            _gPath.AddRectangle(New Rectangle(Me.horzVertLabel1.Location, Me.horzVertLabel1.ClientSize))
            Me.panel1.Location = New Point(Me.horzVertLabel1.Width, 50)
            _gPath.AddRectangle(New Rectangle(Me.panel1.Location, Me.panel1.ClientSize))
            Dim rOld As Region = Me.Region
            Me.Region = New Region(Me._gPath)
            If rOld IsNot Nothing Then
                rOld.Dispose()

            End If
        Else
            Me.Size = Me.ParentSize

            Me.horzVertLabel1.Size = Me.LabelSize
            Me.panel1.Size = New Size(1, Me.ClientSize.Height)
            Me.horzVertLabel1.Location = New Point(50, 0)
            Me.horzVertLabel2.Location = New Point(0, 0)
            Me.horzVertLabel1.Height = Me.LabelSize.Height
            Me.horzVertLabel2.Height = Me.LabelSize.Height
            Me.horzVertLabel1.Width = Me.ClientSize.Width \ 2
            Me.horzVertLabel2.Width = Me.ClientSize.Width \ 2
            Me.horzVertLabel1.Direction = TextDirection.DirectionHorizontal
            Me.horzVertLabel2.Direction = TextDirection.DirectionHorizontal
            If Me.Mode = LayoutMode.LayoutMode2 Then
                _gPath.AddRectangle(New Rectangle(Me.horzVertLabel2.Location, Me.horzVertLabel2.ClientSize))
            End If
            _gPath.AddRectangle(New Rectangle(Me.horzVertLabel1.Location, Me.horzVertLabel1.ClientSize))
            Me.panel1.Location = New Point(50, Me.horzVertLabel1.Height)
            _gPath.AddRectangle(New Rectangle(Me.panel1.Location, Me.panel1.ClientSize))
            Dim rOld As Region = Me.Region
            Me.Region = New Region(Me._gPath)
            If rOld IsNot Nothing Then
                rOld.Dispose()
            End If
        End If

        Me.ResumeLayout()
    End Sub

    Protected Overrides Sub OnLocationChanged(e As EventArgs)
        MyBase.OnLocationChanged(e)

        If Me.Zoom > 0.0F Then
            Dim newValue As Single = 10.0F

            If Me.Mode = LayoutMode.LayoutMode1 Then
                If Me.Orientation = LayoutOrientation.OrientationHorizontal Then
                    Me.CurPos = CInt((Me.Location.Y + Me.LayoutCorrection - Me.ParentAutoScrollPosition.Y) / Zoom)
                    newValue = Me.CurPos * Me.ScaleFactor
                    Me.horzVertLabel1.Text = newValue.ToString()
                Else
                    Me.CurPos = CInt((Me.Location.X + Me.LayoutCorrection - Me.ParentAutoScrollPosition.X) / Zoom)
                    newValue = Me.CurPos * Me.ScaleFactor
                    Me.horzVertLabel1.Text = newValue.ToString()
                End If
            Else
                Me.horzVertLabel1.Text = ""

                If Me.Orientation = LayoutOrientation.OrientationHorizontal Then
                    Me.CurPos = CInt((Me.Location.Y + Me.LayoutCorrection - Me.ParentAutoScrollPosition.Y) / Zoom)
                    newValue = Me.CurPos * Me.ScaleFactor
                    Me.horzVertLabel2.Text = newValue.ToString()
                Else
                    Me.CurPos = CInt((Me.Location.X + Me.LayoutCorrection - Me.ParentAutoScrollPosition.X) / Zoom)
                    newValue = Me.CurPos * Me.ScaleFactor
                    Me.horzVertLabel2.Text = newValue.ToString()
                End If
            End If

            OnLocationUpdated(newValue)
        End If
    End Sub

    Protected Overridable Sub OnLocationUpdated(newValue As Single)
        Dim handler As LocationUpdatedEventHandler = LocationUpdatedEvent
        If handler IsNot Nothing Then
            RaiseEvent LocationUpdated(Me, New SingleEventArgs() With {.Val = newValue})
        End If
    End Sub

    Protected Overrides Sub OnLayout(e As LayoutEventArgs)
        MyBase.OnLayout(e)

        If Me.Orientation = LayoutOrientation.OrientationHorizontal Then
            Me.panel1.ClientSize = New Size(Me.ParentSize.Width, 1)
        Else
            Me.panel1.ClientSize = New Size(1, Me.ParentSize.Height)
        End If

        If _gPath IsNot Nothing Then
            _gPath.Reset()
            If Me.Mode = LayoutMode.LayoutMode2 Then
                _gPath.AddRectangle(New Rectangle(Me.horzVertLabel2.Location, Me.horzVertLabel2.ClientSize))
            End If
            _gPath.AddRectangle(New Rectangle(Me.horzVertLabel1.Location, Me.horzVertLabel1.ClientSize))
            _gPath.AddRectangle(New Rectangle(Me.panel1.Location, Me.panel1.ClientSize))
            Dim rOld As Region = Me.Region
            Me.Region = New Region(Me._gPath)
            If rOld IsNot Nothing Then
                rOld.Dispose()
            End If
        End If
    End Sub

    Public Sub SetText()
        If Me.Zoom > 0.0F Then
            If Me.Mode = LayoutMode.LayoutMode1 Then
                If Me.Orientation = LayoutOrientation.OrientationHorizontal Then
                    Me.CurPos = CInt((Me.Location.Y + Me.LayoutCorrection - Me.ParentAutoScrollPosition.Y) / Zoom)
                    Me.horzVertLabel1.Text = (Me.CurPos * Me.ScaleFactor).ToString()
                Else
                    Me.CurPos = CInt((Me.Location.X + Me.LayoutCorrection - Me.ParentAutoScrollPosition.X) / Zoom)
                    Me.horzVertLabel1.Text = (Me.CurPos * Me.ScaleFactor).ToString()
                End If
            Else
                Me.horzVertLabel1.Text = ""

                If Me.Orientation = LayoutOrientation.OrientationHorizontal Then
                    Me.CurPos = CInt((Me.Location.Y + Me.LayoutCorrection - Me.ParentAutoScrollPosition.Y) / Zoom)
                    Me.horzVertLabel2.Text = (Me.CurPos * Me.ScaleFactor).ToString()
                Else
                    Me.CurPos = CInt((Me.Location.X + Me.LayoutCorrection - Me.ParentAutoScrollPosition.X) / Zoom)
                    Me.horzVertLabel2.Text = (Me.CurPos * Me.ScaleFactor).ToString()
                End If
            End If
        End If
    End Sub

    Public Sub SetLocation()
        OnLocationChanged(New EventArgs())
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If Me.horzVertLabel1.Focused OrElse Me.horzVertLabel2.Focused Then
            If keyData = Keys.Right Then
                If Me.Orientation = LayoutOrientation.OrientationVertical AndAlso Me.CurPos < Picturesize.Width Then
                    Me.Location = New Point(Me.Location.X + 1, Me.Location.Y)
                    If LocationUpdatedEvent IsNot Nothing Then
                        SetLocation()
                    End If
                End If
                Return True
            End If

            If keyData = Keys.Left Then
                If Me.Orientation = LayoutOrientation.OrientationVertical AndAlso Me.CurPos > 0 Then
                    Me.Location = New Point(Me.Location.X - 1, Me.Location.Y)
                    If LocationUpdatedEvent IsNot Nothing Then
                        SetLocation()
                    End If
                End If
                Return True
            End If

            If keyData = Keys.Up Then
                If Me.Orientation = LayoutOrientation.OrientationHorizontal AndAlso Me.CurPos > 0 Then
                    Me.Location = New Point(Me.Location.X, Me.Location.Y - 1)
                    If LocationUpdatedEvent IsNot Nothing Then
                        SetLocation()
                    End If
                End If
                Return True
            End If

            If keyData = Keys.Down Then
                If Me.Orientation = LayoutOrientation.OrientationHorizontal AndAlso Me.CurPos < Picturesize.Height Then
                    Me.Location = New Point(Me.Location.X, Me.Location.Y + 1)
                    If LocationUpdatedEvent IsNot Nothing Then
                        SetLocation()
                    End If
                End If
                Return True
            End If

            If keyData = Keys.Delete Then
                OnRemoveThis()
                Return True
            End If
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Protected Overridable Sub OnRemoveThis()
        'Dim handler As RemoveEventHandler = Me.RemoveHelpLine
        'If handler IsNot Nothing Then
        RaiseEvent RemoveHelpLine(Me, New EventArgs())
        'End If
    End Sub

    Public Sub RefreshControl()
        For Each c As Control In Me.Controls
            c.Invalidate()
        Next
    End Sub
End Class
