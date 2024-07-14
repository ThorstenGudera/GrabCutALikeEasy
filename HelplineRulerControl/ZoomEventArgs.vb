Option Strict On

Public Class ZoomEventArgs
    Inherits EventArgs

    Public Property Zoom As Single
    Public Property ZoomWidth As Boolean

    Public Sub New(zoom As Single, zoomWidth As Boolean)
        Me.Zoom = zoom
        Me.ZoomWidth = zoomWidth
    End Sub
End Class
