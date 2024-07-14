Option Strict On

Public Class ConvMatrix
    Implements ICloneable

    Public TopLeft As Integer = 0, TopMid As Integer = 0, TopRight As Integer = 0
    Public MidLeft As Integer = 0, Pixel As Integer = 1, MidRight As Integer = 0
    Public BottomLeft As Integer = 0, BottomMid As Integer = 0, BottomRight As Integer = 0

    Public Factor As Integer = 1
    Public Offset As Integer = 0

    Public Sub SetAll(nVal As Integer)
        TopLeft = InlineAssignHelper(TopMid, InlineAssignHelper(TopRight, InlineAssignHelper(MidLeft, InlineAssignHelper(Pixel, InlineAssignHelper(MidRight, InlineAssignHelper(BottomLeft, InlineAssignHelper(BottomMid, InlineAssignHelper(BottomRight, nVal))))))))
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Dim cm As New ConvMatrix()
        cm.BottomLeft = Me.BottomLeft
        cm.BottomMid = Me.BottomMid
        cm.BottomRight = Me.BottomRight
        cm.Factor = Me.Factor
        cm.MidLeft = Me.MidLeft
        cm.MidRight = Me.MidRight
        cm.Offset = Me.Offset
        cm.Pixel = Me.Pixel
        cm.TopLeft = Me.TopLeft
        cm.TopMid = Me.TopMid
        cm.TopRight = Me.TopRight
        Return cm
    End Function
End Class
