Option Strict On

Imports System.Drawing
Imports System.Runtime.Serialization
Imports ChainCodeFinder

<Serializable>
Public Class ChainCode
    Implements ISerializable

    Public Property start() As Point
        Get
            Return m_start
        End Get
        Set(value As Point)
            m_start = value
        End Set
    End Property
    Private m_start As Point

    Private _coord As New List(Of Point)()
    Private _chain As New List(Of Integer)()

    Public Property Coord() As List(Of Point)
        Get
            Return _coord
        End Get
        Set(value As List(Of Point))
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

    Public Sub New()

    End Sub

    Public Overrides Function ToString() As String
        Return "x = " + start.X.ToString() + "; y = " + start.Y.ToString() + "; count = " + _coord.Count.ToString()
    End Function

    Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
        info.AddValue("__start", start)
        info.AddValue("__coord", Coord.ToArray())
        info.AddValue("__chain", Chain.ToArray())
        info.AddValue("__area", Area)
        'info.AddValue("__perimeter", Perimeter)
    End Sub

    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        Me.start = CType(info.GetValue("__start", GetType(Point)), Point)
        Me.Coord = New List(Of Point)()
        Me.Coord.AddRange(CType(info.GetValue("__coord", GetType(Point())), Point()))
        Me.Chain = New List(Of Integer)()
        Me.Chain.AddRange(CType(info.GetValue("__chain", GetType(Int32())), Integer()))
        Me.Area = CType(info.GetValue("__area", GetType(Int32)), Integer)
    End Sub

    Public Function ConvertCoordsToChainF() As ChainCodeF
        Dim cOut As New ChainCodeF

        cOut.Area = Me.Area
        cOut.Chain = Me.Chain
        cOut.start = Me.start

        Dim coordsF As New List(Of PointF)

        For i As Integer = 0 To Me.Coord.Count - 1
            coordsF.Add(New PointF(Me.Coord(i).X, Me.Coord(i).Y))
        Next

        cOut.Coord = coordsF

        Return cOut
    End Function
End Class
