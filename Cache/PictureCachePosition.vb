Option Strict On

Public Class PictureCachePosition
    Public Property Number() As Integer
        Get
            Return m_Number
        End Get
        Set(value As Integer)
            m_Number = Value
        End Set
    End Property
    Private m_Number As Integer
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set(value As String)
            m_Name = Value
        End Set
    End Property
    Private m_Name As String

    Public Sub New(no As Integer, name As String)
        Me.Number = no
        Me.Name = name
    End Sub

    Public Overrides Function ToString() As String
        Return Name.ToString() + "; " + Number.ToString()
    End Function
End Class
