Option Strict On

Imports System.Net.Security
Imports System.Windows.Forms

Public Class DBPanel
    Inherits Panel

    Public Sub New()
        MyBase.New()

        Me.DoubleBuffered = True
    End Sub

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)

        If Me.Parent IsNot Nothing Then
            If TypeOf Me.Parent Is UserControl Then
                Dim ctrl As UserControl = CType(Me.Parent, UserControl)
                If TypeOf ctrl Is HelplineRulerCtrl Then
                    Dim hlc As HelplineRulerCtrl = CType(ctrl, HelplineRulerCtrl)
                    AddHandler hlc.HelpLineMouseDown, AddressOf hlc_MouseDown
                    AddHandler hlc.HelpLineMouseMove, AddressOf hlc_MouseMove
                    AddHandler hlc.HelpLineMouseUp, AddressOf hlc_MouseUp
                End If
                'ElseIf TypeOf Me.Parent Is SplitterPanel Then
                '    Dim ctrl As SplitterPanel = CType(Me.Parent, SplitterPanel)
            End If
        End If
    End Sub

    Private Sub hlc_MouseDown(sender As Object, e As MouseEventArgs)
        OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
    End Sub

    Private Sub hlc_MouseMove(sender As Object, e As MouseEventArgs)
        OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
    End Sub

    Private Sub hlc_MouseUp(sender As Object, e As MouseEventArgs)
        OnMouseUp(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
    End Sub
End Class
