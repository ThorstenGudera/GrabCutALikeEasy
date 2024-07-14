<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class HelpLineControl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If Not Me.Region Is Nothing Then
                Me.Region.Dispose()
                Me.Region = Nothing
            End If
            If Not Me._gPath Is Nothing Then
                Me._gPath.Dispose()
                Me._gPath = Nothing
            End If

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
        Me.components = New System.ComponentModel.Container()
        Me.horzVertLabel2 = New HelplineRulerControl.HorzVertLabelControl()
        Me.contextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.removeToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.switchColorToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.panel1 = New System.Windows.Forms.Panel()
        Me.horzVertLabel1 = New HelplineRulerControl.HorzVertLabelControl()
        Me.contextMenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'horzVertLabel2
        '
        Me.horzVertLabel2.ContextMenuStrip = Me.contextMenuStrip1
        Me.horzVertLabel2.Direction = HelplineRulerControl.TextDirection.DirectionVertical
        Me.horzVertLabel2.Location = New System.Drawing.Point(0, 0)
        Me.horzVertLabel2.Name = "horzVertLabel2"
        Me.horzVertLabel2.Size = New System.Drawing.Size(20, 50)
        Me.horzVertLabel2.TabIndex = 6
        '
        'contextMenuStrip1
        '
        Me.contextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.removeToolStripMenuItem, Me.switchColorToolStripMenuItem})
        Me.contextMenuStrip1.Name = "contextMenuStrip1"
        Me.contextMenuStrip1.Size = New System.Drawing.Size(142, 48)
        '
        'removeToolStripMenuItem
        '
        Me.removeToolStripMenuItem.Name = "removeToolStripMenuItem"
        Me.removeToolStripMenuItem.Size = New System.Drawing.Size(141, 22)
        Me.removeToolStripMenuItem.Text = "Remove"
        '
        'switchColorToolStripMenuItem
        '
        Me.switchColorToolStripMenuItem.Name = "switchColorToolStripMenuItem"
        Me.switchColorToolStripMenuItem.Size = New System.Drawing.Size(141, 22)
        Me.switchColorToolStripMenuItem.Text = "Switch Color"
        '
        'panel1
        '
        Me.panel1.BackColor = System.Drawing.Color.Cyan
        Me.panel1.Location = New System.Drawing.Point(20, 50)
        Me.panel1.Name = "panel1"
        Me.panel1.Size = New System.Drawing.Size(478, 1)
        Me.panel1.TabIndex = 4
        '
        'horzVertLabel1
        '
        Me.horzVertLabel1.ContextMenuStrip = Me.contextMenuStrip1
        Me.horzVertLabel1.Direction = HelplineRulerControl.TextDirection.DirectionVertical
        Me.horzVertLabel1.Location = New System.Drawing.Point(0, 50)
        Me.horzVertLabel1.Name = "horzVertLabel1"
        Me.horzVertLabel1.Size = New System.Drawing.Size(20, 50)
        Me.horzVertLabel1.TabIndex = 5
        '
        'HelpLineControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.horzVertLabel2)
        Me.Controls.Add(Me.panel1)
        Me.Controls.Add(Me.horzVertLabel1)
        Me.Name = "HelpLineControl"
        Me.Size = New System.Drawing.Size(498, 100)
        Me.contextMenuStrip1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Public WithEvents horzVertLabel2 As HelplineRulerControl.HorzVertLabelControl
    Public WithEvents contextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
    Public WithEvents removeToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents switchColorToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents panel1 As System.Windows.Forms.Panel
    Public WithEvents horzVertLabel1 As HelplineRulerControl.HorzVertLabelControl

End Class
