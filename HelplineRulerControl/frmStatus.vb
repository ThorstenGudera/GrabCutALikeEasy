Option Strict On

Public Class frmStatus
    Public Overloads Sub Show(zoom As Single)
        Me.Label1.Text = "Generating Pic of zoomfactor " & zoom.ToString() & " ..."
        Me.Label1.Refresh()
        MyBase.Show()
        Windows.Forms.Application.DoEvents()
    End Sub
End Class