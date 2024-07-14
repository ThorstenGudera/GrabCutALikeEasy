Option Strict On
Imports System.Drawing

Public Class GraphicsPathData
    Public Property AllPoints As List(Of List(Of PointF))
    Public Property AllTypes As List(Of List(Of Byte))

    Public Sub New()

    End Sub

    Public Sub New(allPts As List(Of List(Of PointF)), allTps As List(Of List(Of Byte)))
        Me.AllPoints = allPts
        Me.AllTypes = allTps
    End Sub

    Public Sub Add(shiftedPts As List(Of PointF), shiftedTypes As List(Of Byte))
        If Me.AllPoints Is Nothing Then
            Me.AllPoints = New List(Of List(Of PointF))
        End If

        If Me.AllTypes Is Nothing Then
            Me.AllTypes = New List(Of List(Of Byte))
        End If

        Me.AllPoints.Add(shiftedPts)
        Me.AllTypes.Add(shiftedTypes)
    End Sub

    Public Overrides Function ToString() As String
        Dim z As String = MyBase.ToString()
        If Not Me.AllPoints Is Nothing AndAlso Me.AllPoints.Count > 0 Then
            z += " , PointsList count:" & Me.AllPoints.Count.ToString()
        End If
        If Not Me.AllTypes Is Nothing AndAlso Me.AllTypes.Count > 0 Then
            z += " , TypesList count:" & Me.AllTypes.Count.ToString()
        End If
        Return z
    End Function
End Class
