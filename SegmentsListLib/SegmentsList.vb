Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D

Public Class SegmentsList
    Implements IEnumerable
    Private _segments As List(Of CurveSegment)
    Public Property Segments() As List(Of CurveSegment)
        Get
            Return _segments
        End Get
        Set(value As List(Of CurveSegment))
            _segments = value
        End Set
    End Property

    Public Property Multiplier() As Integer
        Get
            Return m_Multiplier
        End Get
        Set(value As Integer)
            m_Multiplier = value
        End Set
    End Property
    Private m_Multiplier As Integer

    Public Sub New()
        Me.Multiplier = 1000
    End Sub

    Public Sub New(list As List(Of CurveSegment))
        Me._segments = list
    End Sub

    Public Sub Add(s As CurveSegment)
        If _segments Is Nothing Then
            _segments = New List(Of CurveSegment)()
        End If

        _segments.Add(s)

        OrderByX()
    End Sub

    Public ReadOnly Property Count() As Integer
        Get
            Return _segments.Count
        End Get
    End Property

    Default Public Property Item(index As Integer) As CurveSegment
        Get
            If _segments.Count > index Then
                Return _segments(index)
            End If

            Return (_segments(_segments.Count - 1))
        End Get
        Set(value As CurveSegment)
            If _segments Is Nothing Then
                _segments = New List(Of CurveSegment)()
            End If

            _segments(index) = value
        End Set
    End Property

    Public Sub RemoveAt(idx As Integer)
        If _segments Is Nothing Then
            Return
        End If

        _segments.RemoveAt(idx)
    End Sub

    Public Function OrderBy(keySelector As Func(Of CurveSegment, Integer)) As IEnumerable(Of CurveSegment)
        Return _segments.OrderBy(keySelector)
    End Function

    Public Sub Draw(ByRef g As Graphics, p As Pen, w As Integer)
        g.SmoothingMode = SmoothingMode.AntiAlias

        Using gP As GraphicsPath = GetPath(w)
            g.DrawPath(p, gP)
        End Using

        For Each sg As CurveSegment In _segments
            g.DrawRectangle(p, New Rectangle(sg.StartPoint.X - 2, sg.StartPoint.Y - 2, 4, 4))
        Next

        g.DrawRectangle(p, New Rectangle(_segments(_segments.Count - 1).EndPoint.X - 2, _segments(_segments.Count - 1).EndPoint.Y - 2, 4, 4))
    End Sub

    Public Sub AddSegment(pt As Point)
        If _segments Is Nothing Then
            _segments = New List(Of CurveSegment)()
        End If

        If _segments.Count > 0 AndAlso _segments(0).StartPoint.X < pt.X Then
            Dim idxPtStrt As Integer = -1

            For i As Integer = 0 To _segments.Count - 1
                If (_segments(i).EndPoint.X > pt.X) Then
                    idxPtStrt = i

                    Exit For
                End If
            Next

            If idxPtStrt > -1 AndAlso _segments.Count > idxPtStrt Then
                Dim tens As Single = _segments(idxPtStrt).Tension
                Dim ptE As Point = _segments(idxPtStrt).EndPoint
                _segments.Add(New CurveSegment() With { _
                 .StartPoint = _segments(idxPtStrt).StartPoint, _
                 .EndPoint = pt, _
                 .Tension = tens _
                })
                _segments.RemoveAt(idxPtStrt)
                _segments.Add(New CurveSegment() With { _
                 .StartPoint = pt, _
                 .EndPoint = ptE, _
                 .Tension = tens _
                })
            End If
            Me.OrderByX()
        End If
    End Sub

    Public Sub RemoveSegment(pt As Point)
        Dim idxPtStrt As Integer = -1

        Me.OrderByX()

        For i As Integer = 0 To _segments.Count - 1
            If (_segments(i).EndPoint.X > pt.X) Then
                idxPtStrt = i

                Exit For
            End If
        Next

        If idxPtStrt > -1 AndAlso _segments.Count > idxPtStrt Then
            Dim ptE As Point = _segments(idxPtStrt).EndPoint

            If _segments.Count > 1 AndAlso idxPtStrt > 0 Then
                _segments(idxPtStrt - 1).EndPoint = ptE
            End If
            _segments.RemoveAt(idxPtStrt)
        End If
    End Sub

    Public Function Exists(pt As Point) As Boolean
        For i As Integer = 0 To _segments.Count - 1
            If (_segments(i).StartPoint.X = pt.X) Then
                Return True
            End If
        Next

        If (_segments(_segments.Count - 1).EndPoint.X = pt.X) Then
            Return True
        End If

        Return False
    End Function

    Private Function GetPath(w As Integer) As GraphicsPath
        Dim gp As New GraphicsPath()
        AddSegmentsToPath(gp, w)
        Return gp
    End Function

    Public Sub AddSegmentsToPath(fPath As GraphicsPath, w As Integer)
        If _segments.Count > 0 Then
            Me.OrderByX()
            If _segments(0).StartPoint.X <> 0 Then
                fPath.AddLine(0, _segments(0).StartPoint.Y, _segments(0).StartPoint.X, _segments(0).StartPoint.Y)
            End If
            If _segments.Count > 0 Then
                Dim start As Point = _segments(0).StartPoint

                For i As Integer = 0 To _segments.Count - 1
                    _segments(i).AddToPath(fPath, Me, start, i)
                    start = _segments(i).EndPoint
                Next
            End If
            If _segments(_segments.Count - 1).EndPoint.X <> w Then
                fPath.AddLine(_segments(_segments.Count - 1).EndPoint.X, _segments(_segments.Count - 1).EndPoint.Y, w, _segments(_segments.Count - 1).EndPoint.Y)
            End If
        Else
            fPath.AddLine(0, 0, w, w)
        End If
    End Sub

    Public Function AddSegmentsToPathAndGetPoints(fPath As GraphicsPath, w As Integer, useYMax As Boolean) As List(Of PointF)
        Dim allPoints As New List(Of PointF)()
        If _segments.Count > 0 Then
            Me.OrderByX()
            If _segments(0).StartPoint.X <> 0 Then
                fPath.AddLine(0, _segments(0).StartPoint.Y, _segments(0).StartPoint.X, _segments(0).StartPoint.Y)
            End If
            If _segments.Count > 0 Then
                Dim start As Point = _segments(0).StartPoint

                For i As Integer = 0 To _segments.Count - 1
                    _segments(i).AddToPathAndGetPoints(fPath, Me, start, i, allPoints, useYMax)
                    start = _segments(i).EndPoint
                Next
            End If
            If _segments(_segments.Count - 1).EndPoint.X <> w Then
                fPath.AddLine(_segments(_segments.Count - 1).EndPoint.X, _segments(_segments.Count - 1).EndPoint.Y, w, _segments(_segments.Count - 1).EndPoint.Y)
            End If
        Else
            fPath.AddLine(0, 0, w, w)
        End If

        If allPoints.Count < w + 1 Then
            While allPoints.Count < w + 1
                allPoints.Add(allPoints(allPoints.Count - 1))
            End While
        End If

        If allPoints.Count > w + 1 Then
            While allPoints.Count > w + 1
                allPoints.Remove(allPoints(allPoints.Count - 1))
            End While
        End If

        Return allPoints
    End Function

    Public Function GetAllPoints(w As Integer, useYMax As Boolean, yMax As Integer) As List(Of PointF)
        Dim allPoints As New List(Of PointF)()
        If _segments.Count > 0 Then
            Me.OrderByX()

            If _segments(0).StartPoint.X <> 0 Then
            End If
            If _segments.Count > 0 Then
                Dim start As Point = _segments(0).StartPoint

                For i As Integer = 0 To _segments.Count - 1
                    _segments(i).GetPoints(Me, start, i, allPoints, useYMax, yMax)
                    start = _segments(i).EndPoint
                Next
            End If

            If _segments(_segments.Count - 1).EndPoint.X <> w Then
            End If

        Else
        End If

        If allPoints.Count > 0 AndAlso allPoints.Count < w + 1 Then
            While allPoints.Count < w + 1
                allPoints.Add(allPoints(allPoints.Count - 1))
            End While
        End If

        If allPoints.Count > 0 AndAlso allPoints.Count > w + 1 Then
            While allPoints.Count > w + 1
                allPoints.Remove(allPoints(allPoints.Count - 1))
            End While
        End If

        Return allPoints
    End Function

    Public Function GetSegment(pt As Point) As CurveSegment
        For i As Integer = 0 To _segments.Count - 1
            If (_segments(i).StartPoint.X = pt.X) Then
                Return _segments(i)
            End If
        Next

        Return Nothing
    End Function

    Public Sub ChangeTensionForConnectedSegments(sg As CurveSegment)
        Dim idx As Integer = -1
        For i As Integer = 0 To _segments.Count - 1
            If _segments(i).Equals(sg) Then
                idx = i

                Exit For
            End If
        Next

        If idx > -1 Then
            Dim i As Integer = idx - 1
            While i >= 0
                If _segments(i).[GetType]().Equals(GetType(CurveSegment)) AndAlso _segments(i).IsNewCurveSegment = False Then
                    _segments(i).Tension = sg.Tension
                Else

                    Exit While
                End If
                i += -1
            End While

            For i = idx + 1 To _segments.Count - 1
                If _segments(i).[GetType]().Equals(GetType(CurveSegment)) Then
                    _segments(i).Tension = sg.Tension

                    If _segments(i).IsNewCurveSegment Then

                        Exit For
                    End If
                Else

                    Exit For
                End If
            Next
        End If
    End Sub

    Public Function IsEndPoint(pt As Point) As Boolean
        If Not _segments Is Nothing AndAlso _segments.Count > 0 Then
            Return _segments(_segments.Count - 1).EndPoint.Equals(pt)
        End If

        Return False
    End Function

    Public Sub OrderByX()
        If _segments Is Nothing Then
            _segments = New List(Of CurveSegment)()
        End If

        '_segments.Sort(Function(a, b)
        '                   Return a.StartPoint.X.CompareTo(b.StartPoint.X)
        '               End Function)

        _segments = _segments.OrderBy(Function(a) a.StartPoint.X).ThenBy(Function(b) b.StartPoint.Y).ToList()

        If _segments.Count > 1 Then
            For i As Integer = 1 To _segments.Count - 1
                If _segments(i - 1).EndPoint.Equals(_segments(i).StartPoint) = False Then
                    _segments(i - 1).EndPoint = _segments(i).StartPoint
                End If
            Next
        End If
    End Sub

    Public Sub Clear()
        _segments.Clear()
    End Sub

    Public Sub Remove(sgR As CurveSegment)
        RemoveSegment(sgR.StartPoint)
    End Sub

    Public Function IsInRange(idx As Integer, pt As Point, isEnd As Boolean) As Boolean
        If isEnd Then
            Return _segments(_segments.Count - 1).StartPoint.X < pt.X
        End If
        If idx > 0 And idx < _segments.Count Then
            Return _segments(idx - 1).StartPoint.X < pt.X And _segments(idx).EndPoint.X > pt.X
        ElseIf idx = 0 Then
            Return _segments(idx).EndPoint.X > pt.X
        End If

        Return False
    End Function

    Public Function Eat(idx As Integer, pt As Point, isEndPoint As Boolean) As Integer
        Dim ret As Integer = 0
        If isEndPoint = False Then
            If _segments.Count > 1 AndAlso _segments(_segments.Count - 1).EndPoint.X > pt.X Then
                If pt.X < _segments(idx).StartPoint.X Then
                    Dim startPt As Integer = 0
                    Dim i As Integer = idx - 1
                    While i >= 0
                        If _segments(i).StartPoint.X < pt.X Then
                            startPt = i

                            Exit While
                        End If
                        i += -1
                    End While

                    _segments(startPt).EndPoint = New Point(_segments(idx).StartPoint.X, _segments(idx).StartPoint.Y)

                    i = idx - 1
                    While i >= startPt + 1
                        _segments.RemoveAt(i)
                        ret -= 1
                        i += -1
                    End While
                ElseIf pt.X > _segments(idx).EndPoint.X Then
                    Dim endPt As Integer = 0
                    For i As Integer = idx + 1 To _segments.Count - 1
                        If _segments(i).EndPoint.X > pt.X Then
                            endPt = i

                            Exit For
                        End If
                    Next

                    _segments(idx).EndPoint = New Point(_segments(endPt).EndPoint.X, _segments(endPt).EndPoint.Y)

                    For i As Integer = idx + 1 To endPt
                        _segments.RemoveAt(i)
                    Next
                End If
            End If
        Else
            Dim startPt As Integer = 0
            Dim i As Integer = idx - 2
            While i >= 0
                If _segments(i).StartPoint.X < pt.X Then
                    startPt = i

                    Exit While
                End If
                i += -1
            End While

            _segments(startPt).EndPoint = New Point(_segments(idx - 1).EndPoint.X, _segments(idx - 1).EndPoint.Y)

            i = idx - 1
            While i >= startPt + 1
                _segments.RemoveAt(i)
                ret -= 1
                i += -1
            End While
        End If

        Return ret
    End Function

    Public Function Find(pt As Point) As Point
        For i As Integer = 0 To _segments.Count - 1
            If _segments(i).StartPoint.X > pt.X - 2 AndAlso _segments(i).StartPoint.X < pt.X + 2 AndAlso _segments(i).StartPoint.Y > pt.Y - 2 AndAlso _segments(i).StartPoint.Y < pt.Y + 2 Then
                Return _segments(i).StartPoint
            End If
        Next
        Return New Point(-1, -1)
    End Function

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return _segments.GetEnumerator()
    End Function

    Public Function RemapPoints(factor As Integer, newWidth As Integer, checkEndPoints__1 As Boolean) As SegmentsList
        Dim l As SegmentsList = Nothing

        If _segments IsNot Nothing Then
            l = New SegmentsList()
            For i As Integer = 0 To _segments.Count - 1
                l.Add(New CurveSegment() With { _
                 .StartPoint = New Point(CInt(_segments(i).StartPoint.X * factor), CInt(_segments(i).StartPoint.Y * factor)), _
                 .EndPoint = New Point(CInt(_segments(i).EndPoint.X * factor), CInt(_segments(i).EndPoint.Y * factor)), _
                 .IsNewCurveSegment = _segments(i).IsNewCurveSegment, _
                 .Tension = _segments(i).Tension _
                })
            Next

            If checkEndPoints__1 Then
                CheckEndPoints(l, newWidth)
            End If
        End If

        Return l
    End Function

    Public Function RemapPointsX(factor As Integer, newWidth As Integer) As SegmentsList
        Dim l As SegmentsList = Nothing

        If _segments IsNot Nothing Then
            l = New SegmentsList()
            For i As Integer = 0 To _segments.Count - 1
                l.Add(New CurveSegment() With { _
                 .StartPoint = New Point(CInt(_segments(i).StartPoint.X * factor), CInt(_segments(i).StartPoint.Y)), _
                 .EndPoint = New Point(CInt(_segments(i).EndPoint.X * factor), CInt(_segments(i).EndPoint.Y)), _
                 .IsNewCurveSegment = _segments(i).IsNewCurveSegment, _
                 .Tension = _segments(i).Tension _
                })
            Next
        End If

        Return l
    End Function

    Public Shared Sub CheckEndPoints(l As SegmentsList, width As Integer)
        If l IsNot Nothing AndAlso l.Count > 0 Then
            If l(0).StartPoint.Equals(New Point(0, 0)) = False Then
                l(0).StartPoint = New Point(0, 0)
            End If

            If l(l.Count - 1).EndPoint.Equals(New Point(width - 1, width - 1)) = False Then
                l(l.Count - 1).EndPoint = New Point(width - 1, width - 1)
            End If
        End If
    End Sub

    Public Function CloneList() As List(Of CurveSegment)
        Dim l As New List(Of CurveSegment)()
        For i As Integer = 0 To Segments.Count - 1
            l.Add(DirectCast(Segments(i).Clone(), CurveSegment))
        Next

        Return l
    End Function
End Class
