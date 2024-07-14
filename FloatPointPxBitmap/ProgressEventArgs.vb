Option Strict On

Public Class ProgressEventArgs
    Inherits EventArgs

    Private _prgInterval As Integer = 1
    Public Property ImgWidthHeight() As Integer
        Get
            Return m_ImgWidthHeight
        End Get
        Set(value As Integer)
            m_ImgWidthHeight = value
        End Set
    End Property
    Private m_ImgWidthHeight As Integer
    Public Property CurrentProgress() As Double
        Get
            Return m_CurrentProgress
        End Get
        Set(value As Double)
            m_CurrentProgress = value
        End Set
    End Property
    Private m_CurrentProgress As Double
    Public Property PrgInterval() As Integer
        Get
            Return _prgInterval
        End Get
        Set(value As Integer)
            _prgInterval = Math.Max(value, 1)
        End Set
    End Property

    Public Sub New(ImageWidthHeight As Integer, StartValue As Integer)
        ImgWidthHeight = ImageWidthHeight
        CurrentProgress = StartValue
    End Sub

    Public Sub New(ImageWidthHeight As Integer, StartValue As Double, Interval As Integer)
        ImgWidthHeight = ImageWidthHeight
        CurrentProgress = StartValue
        PrgInterval = Interval
    End Sub
End Class
