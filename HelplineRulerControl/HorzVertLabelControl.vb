Option Strict On

Imports System.Windows.Forms
Imports System.Drawing

Partial Public Class HorzVertLabelControl
    Inherits UserControl
    Private _direction As TextDirection = TextDirection.DirectionHorizontal
    Private _focus As Boolean = False

    Public Property Direction() As TextDirection
        Get
            Return _direction
        End Get
        Set(value As TextDirection)
            _direction = value
            SetlayoutDirection(value)
        End Set
    End Property
    Private _brush As Brush = Nothing

    Public Sub New()
        InitializeComponent()
        Me.DoubleBuffered = True
        Me._brush = New SolidBrush(Me.ForeColor)
    End Sub

    Private Sub SetlayoutDirection(value As TextDirection)
        Me.Invalidate()
    End Sub

    Protected Overrides Sub OnTextChanged(e As EventArgs)
        MyBase.OnTextChanged(e)

        Me.Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)

        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit

        If _focus Then
            Dim r As Rectangle = Me.ClientRectangle
            r.Inflate(-1, -1)
            e.Graphics.DrawRectangle(Pens.Maroon, r)
        End If

        If Me.Direction = TextDirection.DirectionHorizontal Then
            e.Graphics.DrawString(Me.Text, Me.Font, Me._brush, New PointF(0, 0))
        Else
            Dim sf As New StringFormat()
            sf.FormatFlags = StringFormatFlags.DirectionVertical
            e.Graphics.DrawString(Me.Text, Me.Font, Me._brush, New PointF(0, 0), sf)
            sf.Dispose()
        End If
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)

        If Me.Parent IsNot Nothing Then
            Me.Parent.BringToFront()
        End If

        Me._focus = True
        Me.Invalidate()
    End Sub

    Protected Overrides Sub OnLostFocus(e As EventArgs)
        MyBase.OnLostFocus(e)

        Me._focus = False
        Me.Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)

        Me._focus = True
        Me.Invalidate()
    End Sub

    Private Sub HorzVertLabelControl_MouseHover(sender As Object, e As EventArgs) Handles MyBase.MouseHover

    End Sub

    'protected override void OnMouseUp(MouseEventArgs e)
    '{
    '    base.OnMouseUp(e);

    '    this._focus = false;
    '    this.Invalidate();
    '}
End Class
