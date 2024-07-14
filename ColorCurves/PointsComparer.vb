Option Strict On

Imports System
Imports System.Drawing
Imports System.Collections

Public Class PointsComparer
    Implements IComparer

    Public Sub New()

    End Sub

    Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer _
      Implements IComparer.Compare

        Return CType(x, Point).X.CompareTo( _
         CType(y, Point).X)

    End Function
End Class
