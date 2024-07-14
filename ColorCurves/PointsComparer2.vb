Imports System.Drawing

Friend Class PointsComparer2
    Implements IComparer(Of Point)

    Public Function Compare(x As Point, y As Point) As Integer Implements IComparer(Of Point).Compare
        Return CType(x, Point).X.CompareTo(CType(y, Point).X)
    End Function
End Class
