Option Strict On
Imports System.Windows.Forms

Public Class frmSetRamFreeableAmount
    Public Property BaseForm As Form

    Public Sub New(baseForm As Form)
        InitializeComponent()

        Me.BaseForm = baseForm
        Me.NumericUpDown1.Value = CDec(AvailMem.assumeFreeable)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.NumericUpDown1.Value = Me.NumericUpDown2.Value * Me.NumericUpDown3.Value * Me.NumericUpDown4.Value * Me.NumericUpDown5.Value
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        AvailMem.assumeFreeable = Convert.ToUInt64(Me.NumericUpDown1.Value)

        If Me.CheckBox1.Checked Then
            If Me.Timer1.Enabled Then
                Me.Timer1.Stop()
            End If

            Me.Timer1.Start()
        End If
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Me.Timer1.Stop()
        AvailMem.Reset()
        Me.NumericUpDown1.Value = CDec(AvailMem.assumeFreeable)
    End Sub

    'Private Sub frmSetRamFreeableAmount_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
    '    If e.CloseReason = CloseReason.UserClosing Then
    '        Dim frm As New frmSetRamFreeableAmount(Me.BaseForm)
    '        frm.Show()
    '    End If
    'End Sub
End Class