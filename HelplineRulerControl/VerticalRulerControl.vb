Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms


Partial Public Class VerticalRulerControl
    Inherits Panel
    Private _zoom As Single = 1.0F
    Private _tickFrequencySmall As Single = 1.0F

    Public Property TickFrequencyLarge() As Single
        Get
            Return m_TickFrequencyLarge
        End Get
        Set(value As Single)
            m_TickFrequencyLarge = value
        End Set
    End Property
    Private m_TickFrequencyLarge As Single
    Public Property TickFrequencySmall() As Single
        Get
            Return _tickFrequencySmall
        End Get
        Set(value As Single)
            _tickFrequencySmall = value
            _tickFrequencySmallBU = value
        End Set
    End Property
    Private _drawStart As Single = 0.0F
    Public Property DrawStart() As Single
        Get
            Return _drawStart
        End Get
        Set(value As Single)
            _drawStart = value
            CalcIterationFactor()
        End Set
    End Property
    Public Property DrawEnd() As Single
        Get
            Return m_DrawEnd
        End Get
        Set(value As Single)
            m_DrawEnd = value
        End Set
    End Property
    Private m_DrawEnd As Single
    Public Property PenWidth() As Integer
        Get
            Return m_PenWidth
        End Get
        Set(value As Integer)
            m_PenWidth = value
        End Set
    End Property
    Private m_PenWidth As Integer
    Public Property TickHeightSmall() As Single
        Get
            Return m_TickHeightSmall
        End Get
        Set(value As Single)
            m_TickHeightSmall = value
        End Set
    End Property
    Private m_TickHeightSmall As Single
    Public Property TickHeightLarge() As Single
        Get
            Return m_TickHeightLarge
        End Get
        Set(value As Single)
            m_TickHeightLarge = value
        End Set
    End Property
    Private m_TickHeightLarge As Single
    Public Property ForeColorLargeTick() As Color
        Get
            Return m_ForeColorLargeTick
        End Get
        Set(value As Color)
            m_ForeColorLargeTick = value
        End Set
    End Property
    Private m_ForeColorLargeTick As Color
    Public Property TextColorLargeTick() As Color
        Get
            Return m_TextColorLargeTick
        End Get
        Set(value As Color)
            m_TextColorLargeTick = value
        End Set
    End Property
    Private m_TextColorLargeTick As Color
    Public Property RecalcHeightsOnResize() As Boolean
        Get
            Return m_RecalcHeightsOnResize
        End Get
        Set(value As Boolean)
            m_RecalcHeightsOnResize = value
        End Set
    End Property
    Private m_RecalcHeightsOnResize As Boolean
    Public Property ShowText() As Boolean
        Get
            Return m_ShowText
        End Get
        Set(value As Boolean)
            m_ShowText = value
        End Set
    End Property
    Private m_ShowText As Boolean
    Public Property Zoom() As Single
        Get
            Return _zoom
        End Get
        Set(value As Single)
            _zoom = value
            CalcIterationFactor()
        End Set
    End Property

    Public Property ScaleFactor() As Single
        Get
            Return m_ScaleFactor
        End Get
        Set(value As Single)
            m_ScaleFactor = value
        End Set
    End Property
    Private m_ScaleFactor As Single

    Private Property TickRelationSmall() As Double
        Get
            Return m_TickRelationSmall
        End Get
        Set(value As Double)
            m_TickRelationSmall = value
        End Set
    End Property
    Private m_TickRelationSmall As Double
    Private Property TickRelationLarge() As Double
        Get
            Return m_TickRelationLarge
        End Get
        Set(value As Double)
            m_TickRelationLarge = value
        End Set
    End Property
    Private m_TickRelationLarge As Double
    Private _iterationFactor As Integer = 1
    Private _tickFrequencySmallBU As Single = 1.0F

    Private _backBitmap As Bitmap = Nothing
    Private _minZoomFactor As Single = 0.001F

    Public Sub New()
        InitializeComponent()

        Me.DoubleBuffered = True

        Me.Zoom = 1.0F
        Me.ScaleFactor = 1.0F

        Me.TickFrequencyLarge = 100
        Me.TickFrequencySmall = 10
        Me.DrawStart = 0
        Me.DrawEnd = Me.Height
        Me.PenWidth = 1

        Me.ForeColorLargeTick = Me.ForeColor
        Me.TextColorLargeTick = Me.ForeColor

        Me.TickHeightSmall = Math.Max(Me.ClientSize.Width \ 5, 1)
        Me.TickHeightLarge = Math.Max(Me.ClientSize.Width \ 4, 1)

        Me.TickRelationSmall = CDbl(Me.TickHeightSmall) / Me.ClientSize.Width
        Me.TickRelationLarge = CDbl(Me.TickHeightLarge) / Me.ClientSize.Width

        Me.RecalcHeightsOnResize = True
        Me.ShowText = True
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        If Me._backBitmap IsNot Nothing Then
            e.Graphics.DrawImage(Me._backBitmap, 0, 0)
        End If
    End Sub

    Private Sub CalcIterationFactor()
        _iterationFactor = 1

        Me._tickFrequencySmall = Me._tickFrequencySmallBU

        If Me.DrawEnd > Me.DrawStart AndAlso Zoom <> 1.0F AndAlso Zoom > _minZoomFactor Then
            Using g As Graphics = Me.CreateGraphics()
                Dim tmp1 As SizeF = g.MeasureString((Me.DrawEnd / Zoom).ToString("N0"), Me.Font)
                Dim x1 As Single = Me.DrawEnd - Me.TickFrequencyLarge * Zoom
                Dim tmp2 As SizeF = g.MeasureString((x1 / Zoom).ToString("N0"), Me.Font)

                While Me.DrawEnd - (tmp1.Width / 2.0F) < x1 + (tmp2.Width / 2.0F)
                    _iterationFactor += 1

                    x1 = Me.DrawEnd - Me.TickFrequencyLarge * Zoom * _iterationFactor
                    tmp2 = g.MeasureString((x1 / Zoom).ToString("N0"), Me.Font)
                End While

                If _iterationFactor > 1 Then
                    'direkt auf Wert zugreifen, nicht auf die Property, wg. BU
                    Me._tickFrequencySmall *= _iterationFactor - (_iterationFactor Mod 2)
                End If
            End Using
        End If

        DrawBackgroundBitmap()
    End Sub

    Private Sub DrawBackgroundBitmap()
        If Me.ClientSize.Width > 0 AndAlso Me.ClientSize.Height > 0 Then
            Dim bmp As Bitmap = Nothing

            Try
                If AvailMem.AvailMem.checkAvailRam(Me.ClientSize.Width * Me.ClientSize.Height * 4L) Then
                    bmp = New Bitmap(Me.ClientSize.Width, Me.ClientSize.Height)
                Else
                    Return
                End If

                Using g As Graphics = Graphics.FromImage(bmp)
                    g.Transform = New Matrix(-1, 0, 0, 1, Me.ClientSize.Width, 0)
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit

                    'Brush backBrush = new SolidBrush(this.BackColor);
                    Dim forePenSmall As New Pen(Me.ForeColor, Me.PenWidth)
                    Dim forePenLarge As New Pen(Me.ForeColorLargeTick, Me.PenWidth)
                    Dim textBrush As Brush = New SolidBrush(Me.TextColorLargeTick)

                    'g.FillRectangle(backBrush, this.ClientRectangle);

                    Dim i As Single = Me.DrawStart Mod (Me.TickFrequencySmall * Zoom)
                    While i < Me.DrawEnd
                        g.DrawLine(forePenSmall, New PointF(0, i), New PointF(Me.TickHeightSmall, i))
                        i += Me.TickFrequencySmall * Zoom
                    End While

                    i = Me.DrawStart Mod (Me.TickFrequencyLarge * Zoom)
                    While i < Me.DrawEnd
                        g.DrawLine(forePenLarge, New PointF(0, i), New PointF(Me.TickHeightLarge, i))
                        i += Me.TickFrequencyLarge * Zoom
                    End While

                    g.ResetTransform()

                    If Me.ShowText Then
                        Dim sf As New StringFormat()
                        sf.FormatFlags = StringFormatFlags.DirectionVertical

                        i = Me.DrawStart Mod (Me.TickFrequencyLarge * Zoom)
                        While i < Me.DrawEnd

                            Dim textSize As SizeF = g.MeasureString(((-Me.DrawStart + i) / Zoom).ToString("N0"), Me.Font)
                            g.DrawString(((-Me.DrawStart + i) / Zoom * ScaleFactor).ToString("N0"), Me.Font, textBrush, New PointF(Me.ClientSize.Width - Me.TickHeightLarge - textSize.Height - 3, i - (textSize.Width / 2.0F)), sf)
                            i += Me.TickFrequencyLarge * Zoom * _iterationFactor
                        End While
                        sf.Dispose()
                    End If

                    'backBrush.Dispose();
                    forePenSmall.Dispose()
                    forePenLarge.Dispose()
                    textBrush.Dispose()
                End Using

                Me.SetBitmap(Me._backBitmap, bmp)
            Catch
                If Not bmp Is Nothing Then
                    bmp.Dispose()
                End If
            End Try
        End If
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        Me.DrawEnd = Me.Height

        If Me.RecalcHeightsOnResize Then
            Me.TickHeightSmall = CSng(Me.ClientSize.Width * Me.TickRelationSmall)
            Me.TickHeightLarge = CSng(Me.ClientSize.Width * Me.TickRelationLarge)
        End If
    End Sub

    Private Sub SetBitmap(ByRef bitmapToSet As Bitmap, ByRef bitmapToBeSet As Bitmap)
        Dim bOld As Bitmap = bitmapToSet

        bitmapToSet = bitmapToBeSet

        If bOld IsNot Nothing AndAlso bOld.Equals(bitmapToBeSet) = False Then
            bOld.Dispose()
        End If
    End Sub
End Class
