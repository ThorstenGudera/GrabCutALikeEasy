Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class ChainCodeF
    Public Property start() As Point
        Get
            Return m_start
        End Get
        Set(value As Point)
            m_start = value
        End Set
    End Property
    Private m_start As Point
    Private _coord As New List(Of PointF)()
    Private _chain As New List(Of Integer)()

    Public Property Coord() As List(Of PointF)
        Get
            Return _coord
        End Get
        Set(value As List(Of PointF))
            _coord = value
        End Set
    End Property
    Public Property Chain() As List(Of Integer)
        Get
            Return _chain
        End Get
        Set(value As List(Of Integer))
            _chain = value
        End Set
    End Property

    Public Property Area() As Integer
        Get
            Return m_Area
        End Get
        Set(value As Integer)
            m_Area = value
        End Set
    End Property
    Private m_Area As Integer
    Public ReadOnly Property Perimeter() As Integer
        Get
            Return _chain.Count
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return "x = " + start.X.ToString() + "; y = " + start.Y.ToString() + "; count = " + _coord.Count.ToString()
    End Function
End Class
