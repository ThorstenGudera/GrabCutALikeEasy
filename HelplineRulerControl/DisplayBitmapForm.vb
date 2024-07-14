Option Strict On

Imports System.Windows.Forms
Imports System.Drawing

Partial Public Class DisplayBitmapForm
    Inherits Form

    Private _bmpW As Bitmap = Nothing
    Private _bmp As Bitmap = Nothing
    Private _w As Boolean

    Public Sub New()
        InitializeComponent()
        Me.clickPanel1.BackColor = Color.FromArgb(255, 0, 0, 41)
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape OrElse keyData = Keys.Q OrElse keyData = (Keys.Q Or Keys.Control) OrElse keyData = (Keys.Enter Or Keys.Control) Then
            Me.Close()
            Return True
        End If

        If keyData = Keys.Down Then
            clickPanel1.AutoScrollPosition = New Point(-clickPanel1.AutoScrollPosition.X, -clickPanel1.AutoScrollPosition.Y + 4)
            Return True
        End If
        If keyData = Keys.Up Then
            clickPanel1.AutoScrollPosition = New Point(-clickPanel1.AutoScrollPosition.X, -clickPanel1.AutoScrollPosition.Y - 4)
            Return True
        End If
        If keyData = Keys.Left Then
            clickPanel1.AutoScrollPosition = New Point(-clickPanel1.AutoScrollPosition.X - 4, -clickPanel1.AutoScrollPosition.Y)
            Return True
        End If
        If keyData = Keys.Right Then
            clickPanel1.AutoScrollPosition = New Point(-clickPanel1.AutoScrollPosition.X + 4, -clickPanel1.AutoScrollPosition.Y)
            Return True
        End If
        If keyData = Keys.PageDown Then
            clickPanel1.AutoScrollPosition = New Point(-clickPanel1.AutoScrollPosition.X, -clickPanel1.AutoScrollPosition.Y + clickPanel1.ClientSize.Height)
            Return True
        End If
        If keyData = Keys.PageUp Then
            clickPanel1.AutoScrollPosition = New Point(-clickPanel1.AutoScrollPosition.X, -clickPanel1.AutoScrollPosition.Y - clickPanel1.ClientSize.Height)
            Return True
        End If
        If keyData = Keys.Home Then
            clickPanel1.AutoScrollPosition = New Point(-Int16.MaxValue, -clickPanel1.AutoScrollPosition.Y)
            Return True
        End If
        If keyData = Keys.[End] Then
            clickPanel1.AutoScrollPosition = New Point(Int16.MaxValue, -clickPanel1.AutoScrollPosition.Y)
            Return True
        End If
        If keyData = (Keys.Home Or Keys.Control) Then
            clickPanel1.AutoScrollPosition = New Point(-Int16.MaxValue, -Int16.MaxValue)
            Return True
        End If
        If keyData = (Keys.[End] Or Keys.Control) Then
            clickPanel1.AutoScrollPosition = New Point(Int16.MaxValue, Int16.MaxValue)
            Return True
        End If

        If keyData = Keys.B Then
            If Me.clickPanel1.BackColor.ToArgb.Equals(Color.FromArgb(255, 0, 0, 41).ToArgb) Then
                Me.pictureBox1.BackColor = Color.Teal
                Me.clickPanel1.BackColor = Color.Teal
            ElseIf Me.clickPanel1.BackColor.ToArgb.Equals(Color.Teal.ToArgb) Then
                Me.pictureBox1.BackColor = Color.Black
                Me.clickPanel1.BackColor = Color.Black
            ElseIf Me.clickPanel1.BackColor.ToArgb.Equals(Color.Black.ToArgb) Then
                Me.pictureBox1.BackColor = SystemColors.Control
                Me.clickPanel1.BackColor = SystemColors.Control
            ElseIf Me.clickPanel1.BackColor.ToArgb.Equals(SystemColors.Control.ToArgb) Then
                Me.pictureBox1.BackColor = Color.FromArgb(255, 0, 0, 41)
                Me.clickPanel1.BackColor = Color.FromArgb(255, 0, 0, 41)
            End If
            Return True
        End If

        If keyData = Keys.F10 Then
            ChangeZoom()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub pictureBox1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles pictureBox1.MouseDoubleClick
        If e.Button = System.Windows.Forms.MouseButtons.Left Then
            Me.Close()
        End If
        If e.Button = System.Windows.Forms.MouseButtons.Right Then
            ChangeZoom()
        End If
    End Sub

    Private Sub ChangeZoom()
        Me.SuspendLayout()
        Me.pictureBox1.Visible = False

        If Not _bmp Is Nothing Then
            Me.pictureBox1.Image = _bmp

            If Me.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize AndAlso Not _w Then
                Me.pictureBox1.Dock = DockStyle.Fill
                Me.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom
                Me.AutoScroll = False
            Else
                Me.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize
                Me.pictureBox1.Dock = DockStyle.None

                If Not _w AndAlso Not Me._bmpW Is Nothing Then
                    _w = True
                    Me.pictureBox1.Image = _bmpW
                    Me.pictureBox1.Location = If(Me.pictureBox1.Image.Width < Me.ClientSize.Width OrElse Me.pictureBox1.Image.Height < Me.ClientSize.Height,
                        New Point((Me.ClientSize.Width - Me.pictureBox1.Image.Width) \ 2,
                                   (Me.ClientSize.Height - Me.pictureBox1.Image.Height) \ 2), Me.AutoScrollPosition)
                Else
                    _w = False
                    Me.pictureBox1.Image = _bmp

                    If Me.pictureBox1.Image.Width < Me.ClientSize.Width OrElse Me.pictureBox1.Image.Height < Me.ClientSize.Height Then
                        Me.AutoScrollPosition = New Point(0, 0)
                    End If

                    Me.pictureBox1.Location = If(Me.pictureBox1.Image.Width < Me.ClientSize.Width OrElse Me.pictureBox1.Image.Height < Me.ClientSize.Height,
                        New Point(Math.Max((Me.ClientSize.Width - Me.pictureBox1.Image.Width) \ 2, 0),
                                  Math.Max((Me.ClientSize.Height - Me.pictureBox1.Image.Height) \ 2, 0)), Me.AutoScrollPosition)
                End If

                Me.AutoScroll = True
            End If
        End If
        Me.pictureBox1.Visible = True
        Me.ResumeLayout()
    End Sub

    Private Sub pictureBox1_Click(sender As Object, e As EventArgs) Handles pictureBox1.Click
        Me.clickPanel1.Focus()
    End Sub

    Private Sub DisplayBitmapForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim x As Integer = 0
        Dim y As Integer = 0
        If Not Me.pictureBox1.Image Is Nothing Then
            If Me.pictureBox1.Image.Width < Screen.GetWorkingArea(Me).Width Then
                x = (Screen.GetWorkingArea(Me).Width - Me.pictureBox1.Image.Width) \ 2
            End If
            If Me.pictureBox1.Image.Height < Screen.GetWorkingArea(Me).Height Then
                y = (Screen.GetWorkingArea(Me).Height - Me.pictureBox1.Image.Height) \ 2
            End If
            Me.pictureBox1.Location = New Point(x, y)
            Me.clickPanel1.Focus()
        End If
    End Sub

    Private Sub DisplayBitmapForm_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If Not Me.pictureBox1.Image Is Nothing Then
            Dim bmp As Bitmap = Nothing

            Try
                Dim f As Double = Me.ClientSize.Width / Me.pictureBox1.Image.Width
                bmp = New Bitmap(Me.ClientSize.Width, CInt(Me.pictureBox1.Image.Height * f))

                Using g As Graphics = Graphics.FromImage(bmp)
                    g.DrawImage(Me.pictureBox1.Image, 0, 0, bmp.Width, bmp.Height)
                End Using

                Me._bmpW = bmp
                Me._bmp = CType(Me.pictureBox1.Image, Bitmap)
            Catch ex As Exception
                If Not bmp Is Nothing Then
                    bmp.Dispose()
                    bmp = Nothing
                End If
            End Try
        End If
    End Sub
End Class