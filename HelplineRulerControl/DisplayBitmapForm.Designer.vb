<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class DisplayBitmapForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If Not Me._bmpW Is Nothing Then
                _bmpW.Dispose()
                _bmpW = Nothing
            End If
        Catch ex As Exception

        End Try
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.clickPanel1 = New System.Windows.Forms.Panel()
        Me.pictureBox1 = New System.Windows.Forms.PictureBox()
        Me.clickPanel1.SuspendLayout()
        CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'clickPanel1
        '
        Me.clickPanel1.AutoScroll = True
        Me.clickPanel1.Controls.Add(Me.pictureBox1)
        Me.clickPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.clickPanel1.Location = New System.Drawing.Point(0, 0)
        Me.clickPanel1.Name = "clickPanel1"
        Me.clickPanel1.Size = New System.Drawing.Size(717, 543)
        Me.clickPanel1.TabIndex = 2
        '
        'pictureBox1
        '
        Me.pictureBox1.Location = New System.Drawing.Point(0, 0)
        Me.pictureBox1.Name = "pictureBox1"
        Me.pictureBox1.Size = New System.Drawing.Size(100, 50)
        Me.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pictureBox1.TabIndex = 0
        Me.pictureBox1.TabStop = False
        '
        'DisplayBitmapForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(717, 543)
        Me.ControlBox = False
        Me.Controls.Add(Me.clickPanel1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "DisplayBitmapForm"
        Me.Text = "DisplayBitmapForm"
        Me.clickPanel1.ResumeLayout(False)
        Me.clickPanel1.PerformLayout()
        CType(Me.pictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Private WithEvents clickPanel1 As System.Windows.Forms.Panel
    Public WithEvents pictureBox1 As System.Windows.Forms.PictureBox
End Class
