Option Strict On
Imports System.Drawing
Imports System.Windows.Forms

Public Class HelperFunctions
    Public Shared Sub SetFormSizeBig(frm As Form)
        Dim rc As Rectangle = Screen.GetWorkingArea(frm)
        Dim ratio As Double = rc.Width / rc.Height
        If rc.Height > rc.Width Then
            ratio = 1 / ratio
        End If

        Dim ratioMax As Double = 16.0 / 9.0

        Dim h As Integer = Math.Min(rc.Width, rc.Height) - 20
        Dim w As Integer = CInt(Math.Min(rc.Width, rc.Height) * Math.Min(ratio, ratioMax)) - 100

        frm.Width = If(rc.Width > rc.Height, w, h)
        frm.Height = If(rc.Width > rc.Height, h, w)
    End Sub
End Class
