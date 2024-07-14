Option Strict On
Public Class ProgEventArgs
    Inherits EventArgs

    Public Property Y() As Integer
        Get
            Return m_Y
        End Get
        Set(value As Integer)
            m_Y = value
        End Set
    End Property
    Private m_Y As Integer
    Public Property Height() As Integer
        Get
            Return m_Height
        End Get
        Set(value As Integer)
            m_Height = value
        End Set
    End Property
    Private m_Height As Integer
    Public Sub New(y__1 As Integer, height__2 As Integer)
        Y = y__1
        Height = height__2
    End Sub
End Class
