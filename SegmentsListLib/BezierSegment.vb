Option Strict On

Imports System.Drawing


Public Class BezierSegment
    Public Property P1() As PointF
        Get
            Return m_P1
        End Get
        Set(value As PointF)
            m_P1 = value
        End Set
    End Property
    Private m_P1 As PointF
    Public Property P2() As PointF
        Get
            Return m_P2
        End Get
        Set(value As PointF)
            m_P2 = value
        End Set
    End Property
    Private m_P2 As PointF
    Public Property P3() As PointF
        Get
            Return m_P3
        End Get
        Set(value As PointF)
            m_P3 = value
        End Set
    End Property
    Private m_P3 As PointF
    Public Property P4() As PointF
        Get
            Return m_P4
        End Get
        Set(value As PointF)
            m_P4 = value
        End Set
    End Property
    Private m_P4 As PointF
End Class
