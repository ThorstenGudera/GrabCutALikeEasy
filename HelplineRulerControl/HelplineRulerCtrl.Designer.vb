<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class HelplineRulerCtrl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If Not Me._statusFrm Is Nothing Then
                Me._statusFrm.Dispose()
            End If
        Catch ex As Exception

        End Try
        Try
            DisposeBitmapData()
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
        Me.contextMenuStrip1 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.addHelpLineToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.addHelpLineToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.contextMenuStrip2 = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.topLeftPanel1 = New System.Windows.Forms.Panel()
        Me.topPanel1 = New System.Windows.Forms.Panel()
        Me.topPanel = New HelplineRulerControl.HorizontalRulerControl()
        Me.dbPanel1 = New HelplineRulerControl.DBPanel()
        Me.leftPanel = New HelplineRulerControl.VerticalRulerControl()
        Me.contextMenuStrip1.SuspendLayout()
        Me.contextMenuStrip2.SuspendLayout()
        Me.topPanel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'contextMenuStrip1
        '
        Me.contextMenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.addHelpLineToolStripMenuItem1})
        Me.contextMenuStrip1.Name = "contextMenuStrip1"
        Me.contextMenuStrip1.Size = New System.Drawing.Size(144, 26)
        '
        'addHelpLineToolStripMenuItem1
        '
        Me.addHelpLineToolStripMenuItem1.Name = "addHelpLineToolStripMenuItem1"
        Me.addHelpLineToolStripMenuItem1.Size = New System.Drawing.Size(143, 22)
        Me.addHelpLineToolStripMenuItem1.Text = "AddHelpLine"
        '
        'addHelpLineToolStripMenuItem
        '
        Me.addHelpLineToolStripMenuItem.Name = "addHelpLineToolStripMenuItem"
        Me.addHelpLineToolStripMenuItem.Size = New System.Drawing.Size(143, 22)
        Me.addHelpLineToolStripMenuItem.Text = "AddHelpLine"
        '
        'contextMenuStrip2
        '
        Me.contextMenuStrip2.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.addHelpLineToolStripMenuItem})
        Me.contextMenuStrip2.Name = "contextMenuStrip2"
        Me.contextMenuStrip2.Size = New System.Drawing.Size(144, 26)
        '
        'topLeftPanel1
        '
        Me.topLeftPanel1.BackColor = System.Drawing.SystemColors.ControlDarkDark
        Me.topLeftPanel1.Dock = System.Windows.Forms.DockStyle.Left
        Me.topLeftPanel1.Location = New System.Drawing.Point(0, 0)
        Me.topLeftPanel1.Name = "topLeftPanel1"
        Me.topLeftPanel1.Size = New System.Drawing.Size(20, 20)
        Me.topLeftPanel1.TabIndex = 5
        '
        'topPanel1
        '
        Me.topPanel1.BackColor = System.Drawing.SystemColors.Control
        Me.topPanel1.Controls.Add(Me.topPanel)
        Me.topPanel1.Controls.Add(Me.topLeftPanel1)
        Me.topPanel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.topPanel1.Location = New System.Drawing.Point(0, 0)
        Me.topPanel1.Name = "topPanel1"
        Me.topPanel1.Size = New System.Drawing.Size(774, 20)
        Me.topPanel1.TabIndex = 7
        '
        'topPanel
        '
        Me.topPanel.BackColor = System.Drawing.SystemColors.ControlDark
        Me.topPanel.ContextMenuStrip = Me.contextMenuStrip1
        Me.topPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.topPanel.DrawEnd = 754.0!
        Me.topPanel.DrawStart = 0.0!
        Me.topPanel.ForeColorLargeTick = System.Drawing.SystemColors.ControlText
        Me.topPanel.Location = New System.Drawing.Point(20, 0)
        Me.topPanel.Name = "topPanel"
        Me.topPanel.PenWidth = 1
        Me.topPanel.RecalcHeightsOnResize = True
        Me.topPanel.ScaleFactor = 1.0!
        Me.topPanel.ShowText = True
        Me.topPanel.Size = New System.Drawing.Size(754, 20)
        Me.topPanel.TabIndex = 5
        Me.topPanel.TextColorLargeTick = System.Drawing.SystemColors.ControlText
        Me.topPanel.TickFrequencyLarge = 100.0!
        Me.topPanel.TickFrequencySmall = 10.0!
        Me.topPanel.TickHeightLarge = 5.0!
        Me.topPanel.TickHeightSmall = 4.0!
        Me.topPanel.Zoom = 1.0!
        '
        'dbPanel1
        '
        Me.dbPanel1.AllowDrop = True
        Me.dbPanel1.AutoScroll = True
        Me.dbPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.dbPanel1.Location = New System.Drawing.Point(20, 20)
        Me.dbPanel1.Name = "dbPanel1"
        Me.dbPanel1.Size = New System.Drawing.Size(754, 551)
        Me.dbPanel1.TabIndex = 9
        '
        'leftPanel
        '
        Me.leftPanel.BackColor = System.Drawing.SystemColors.ControlDark
        Me.leftPanel.ContextMenuStrip = Me.contextMenuStrip2
        Me.leftPanel.Dock = System.Windows.Forms.DockStyle.Left
        Me.leftPanel.DrawEnd = 551.0!
        Me.leftPanel.DrawStart = 0.0!
        Me.leftPanel.ForeColorLargeTick = System.Drawing.SystemColors.ControlText
        Me.leftPanel.Location = New System.Drawing.Point(0, 20)
        Me.leftPanel.Name = "leftPanel"
        Me.leftPanel.PenWidth = 1
        Me.leftPanel.RecalcHeightsOnResize = True
        Me.leftPanel.ScaleFactor = 1.0!
        Me.leftPanel.ShowText = True
        Me.leftPanel.Size = New System.Drawing.Size(20, 551)
        Me.leftPanel.TabIndex = 8
        Me.leftPanel.TextColorLargeTick = System.Drawing.SystemColors.ControlText
        Me.leftPanel.TickFrequencyLarge = 100.0!
        Me.leftPanel.TickFrequencySmall = 10.0!
        Me.leftPanel.TickHeightLarge = 5.0!
        Me.leftPanel.TickHeightSmall = 4.0!
        Me.leftPanel.Zoom = 1.0!
        '
        'HelplineRulerCtrl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.dbPanel1)
        Me.Controls.Add(Me.leftPanel)
        Me.Controls.Add(Me.topPanel1)
        Me.Name = "HelplineRulerCtrl"
        Me.Size = New System.Drawing.Size(774, 571)
        Me.contextMenuStrip1.ResumeLayout(False)
        Me.contextMenuStrip2.ResumeLayout(False)
        Me.topPanel1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Private WithEvents topPanel As HelplineRulerControl.HorizontalRulerControl
    Private WithEvents contextMenuStrip1 As System.Windows.Forms.ContextMenuStrip
    Private WithEvents addHelpLineToolStripMenuItem1 As System.Windows.Forms.ToolStripMenuItem
    Public WithEvents dbPanel1 As HelplineRulerControl.DBPanel
    Private WithEvents addHelpLineToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Private WithEvents contextMenuStrip2 As System.Windows.Forms.ContextMenuStrip
    Private WithEvents leftPanel As HelplineRulerControl.VerticalRulerControl
    Private WithEvents topLeftPanel1 As System.Windows.Forms.Panel
    Public WithEvents topPanel1 As System.Windows.Forms.Panel

End Class
