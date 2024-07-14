Option Strict On

Public Class FpBmpInfo
    Public Property UseFPBmp() As Boolean
        Get
            Return m_UseFPBmp
        End Get
        Set(value As Boolean)
            m_UseFPBmp = Value
        End Set
    End Property
    Private m_UseFPBmp As Boolean
    Public Property DoubleSize() As Boolean
        Get
            Return m_DoubleSize
        End Get
        Set(value As Boolean)
            m_DoubleSize = Value
        End Set
    End Property

    Public Property BicubB As Double
    Public Property BicubC As Double

    Private m_DoubleSize As Boolean

    Public Sub New(useFPBmp__1 As Boolean, doDoubleSize As Boolean, b As Double, c As Double)
        UseFPBmp = useFPBmp__1
        DoubleSize = doDoubleSize
        BicubB = b
        BicubC = c
    End Sub
End Class
