Option Strict On

Imports System.Drawing
Imports System.Windows.Forms


<Serializable()> _
Public Class CurveSegment
    Implements ICloneable
    Public Property StartPoint() As Point
        Get
            Return m_StartPoint
        End Get
        Set(value As Point)
            m_StartPoint = value
        End Set
    End Property
    Private m_StartPoint As Point
    Public Property EndPoint() As Point
        Get
            Return m_EndPoint
        End Get
        Set(value As Point)
            m_EndPoint = value
        End Set
    End Property
    Private m_EndPoint As Point
    Public Property Tension() As Single
        Get
            Return m_Tension
        End Get
        Set(value As Single)
            m_Tension = value
        End Set
    End Property
    Private m_Tension As Single
    Public Property IsNewCurveSegment() As Boolean
        Get
            Return m_IsNewCurveSegment
        End Get
        Set(value As Boolean)
            m_IsNewCurveSegment = value
        End Set
    End Property
    Private m_IsNewCurveSegment As Boolean

    Public Sub MapPoints(pts As List(Of PointF), mappings As Byte())
        If pts.Count <> mappings.Length Then
            Return
        End If
        For i As Integer = 0 To pts.Count - 1
            If Single.IsInfinity(pts(i).Y) OrElse Single.IsNaN(pts(i).Y) Then
                mappings(i) = Convert.ToByte(Math.Max(Math.Min(i, 255), 0))
            Else
                mappings(i) = Convert.ToByte(Math.Max(Math.Min(pts(i).Y, 255), 0))
            End If
        Next
    End Sub

    Public Sub MapPoints(pts As List(Of PointF), mappings As Integer())
        If pts.Count <> mappings.Length Then
            Return
        End If
        For i As Integer = 0 To pts.Count - 1
            If Single.IsInfinity(pts(i).Y) OrElse Single.IsNaN(pts(i).Y) Then
                mappings(i) = Convert.ToInt32(Math.Max(Math.Min(i, 255), 0))
            Else
                mappings(i) = Convert.ToInt32(Math.Max(Math.Min(pts(i).Y, 255), 0))
            End If
        Next
    End Sub

    'orig from: http://floris.briolas.nl/floris/2009/04/addcurve-for-wpf-cardinal-spline/      

    'from http://www.peterbuettner.de/develop/javasnippets/bezier/index.html
    'The Bézier curve is defined by a set of 3 points that specify a cubic parametric curve to be drawn from the most recently specified point. The curve is interpolated by the parametric control equation in the range (t=[0..1]) using the most recently specified (current) point (CP), the two control points (P1, P2) and the final point (P3):P(t) = B(3,0)*CP + B(3,1)*P1 + B(3,2)*P2 + B(3,3)*P3 ; 0 <=t<=1 
    'mth coefficient of nth degree Bernstein polynomial:
    '    B(n,m) = C(n,m) * t^(m) * (1 - t)^(n-m) 
    '    C(n,m) = n! / (m! * (n-m)!)

    ' which gives (Points: CP, P1, P2, P3) 
    '    P(t) = (1-t)^3 * CP + 3(1-t)^2 t * P1 + 3(1-t)t^2 * P2 + t^3*P3
    '    P'(t) = [-3 (1 - t)^2] *CP + [3 - 12 t + 9 t^2] * P1 + [3 (2 - 3 t) t] * P2  [3 t^2] *P3
    '    P''(t) = [6(1-t)] * CP + [-6(2-3t)]*P1 + [6(1-3t)]*P2 + [6t]*P3

    ' Maybe we can calculate the control points from the derivates of our known function.
    '    P'(t=0)  = P'  = -3*CP + 3*P1         
    '    P''(t=0) = P'' =  6*CP -12*P1 + 6*P2  
    '                   = -6*CP - 4*P' + 6*P2 
    '    =>
    '    P1= P'/3 +CP
    '    P2 = P''/6 +2P1 -CP= P''/6 +2/3P' +CP

    'we use here only the first derivative, the second Control-Point 
    'is a mirror of the first one of the following segment.

    'Each intermediate point's controlPoint is computed by only the slope of the point and the tension.
    'The slope will give a line on which the controlpoint is found, a directional vector, since its a tangent of the curve
    'at the curve's point. The tension defines the lenght of that vector.

    'Now because an intermediate point's slope is defined by nothing else than the position of the point before and the point after,
    '(and the influence of the point before is calculated by differenciating p1-p0, and the influence of the point after is calculated by
    'differenciating p2-p1), we can differenciate this larger section p2-p0 to get the slope (and the offset(s) for the ControlPoint) of the intermediate point.
    Public Function CalcBezierSegments(points As Point(), tens As Double) As List(Of BezierSegment)
        Dim sgmts As New List(Of BezierSegment)()

        If points.Length >= 2 Then
            Dim tensionthird As Double = tens / 3.0

            Dim bz As New BezierSegment()
            bz.P1 = points(0)
            bz.P2 = New PointF(Convert.ToSingle((tensionthird * (points(1).X - points(0).X) + points(0).X)), Convert.ToSingle((tensionthird * (points(1).Y - points(0).Y) + points(0).Y)))

            sgmts.Add(bz)

            For i As Integer = 0 To points.Length - 3
                sgmts.Add(New BezierSegment())

                Dim dX As Double = 0
                Dim dY As Double = 0
                dX = points(i + 2).X - points(i).X
                dY = points(i + 2).Y - points(i).Y

                sgmts(i).P3 = New PointF(Convert.ToSingle(points(i + 1).X - tensionthird * dX), Convert.ToSingle(points(i + 1).Y - tensionthird * dY))

                sgmts(i).P4 = points(i + 1)
                sgmts(i + 1).P1 = points(i + 1)

                sgmts(i + 1).P2 = New PointF(Convert.ToSingle(points(i + 1).X + tensionthird * dX), Convert.ToSingle(points(i + 1).Y + tensionthird * dY))
            Next

            sgmts(sgmts.Count - 1).P3 = New PointF(Convert.ToSingle((tensionthird * (points(points.Length - 2).X - points(points.Length - 1).X) + points(points.Length - 1).X)), Convert.ToSingle((tensionthird * (points(points.Length - 2).Y - points(points.Length - 1).Y) + points(points.Length - 1).Y)))

            sgmts(sgmts.Count - 1).P4 = points(points.Length - 1)
        End If

        Return sgmts
    End Function

    Private Function GetAllPointsForOneSegment(segment As BezierSegment, startX As Integer, endX As Integer) As List(Of PointF)
        Dim pts As New List(Of PointF)()

        Try
            If segment.P4.X - segment.P1.X > 0.0F Then
                Dim cx As Double = 3 * (segment.P2.X - segment.P1.X)
                Dim bx As Double = 3 * (segment.P3.X - segment.P2.X) - cx
                Dim ax As Double = segment.P4.X - segment.P1.X - cx - bx

                Dim cy As Double = 3 * (segment.P2.Y - segment.P1.Y)
                Dim by As Double = 3 * (segment.P3.Y - segment.P2.Y) - cy
                Dim ay As Double = segment.P4.Y - segment.P1.Y - cy - by

                Dim prevT As Double = 0.0

                For x As Integer = startX To endX - 1
                    Dim t As Double = Solve(x, ax, bx, cx, segment.P1.X, prevT)
                    prevT = t

                    If t < 0.0 Then
                        t = 0.0
                    End If
                    If t > 1.0 Then
                        t = 1.0
                    End If

                    Dim y As Double = ay * t * t * t + by * t * t + cy * t + segment.P1.Y

                    pts.Add(New PointF(Convert.ToSingle(x), Convert.ToSingle(y)))

                    'Dim t As Double = 0.0
                    ''you could also use an int-counter, if the round-off errors increase too much....
                    'Dim prevX As Integer = 0
                    'If st > 0.0 AndAlso Double.IsInfinity(st) = False AndAlso Double.IsNaN(st) = False Then
                    '    While t <= 1.0 + 0.0001 'give a little tolerance
                    '        Dim xt As Double = ax * t * t * t + bx * t * t + cx * t + segment.P1.X
                    '        Dim yt As Double = ay * t * t * t + by * t * t + cy * t + segment.P1.Y

                    '        If CType(xt, Integer) > prevX Then
                    '            pts.Add(New PointF(CType(xt, Single), CType(yt, Single)))
                    '        End If

                    '        prevX = CType(xt, Integer)
                    '        t += st
                    '    End While
                    'End If

                Next
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try

        Return pts
    End Function

    Private Function Solve(x As Double, a As Double, b As Double, c As Double, d As Double, prevT As Double) As Double
        Dim d0 As Double = d - x

        Dim p As Double = ((3 * a * c) - (b * b)) / (3 * a * a)
        Dim q1 As Double = (2 * b * b * b) - (9 * a * b * c) + (27 * a * a * d0)
        Dim q2 As Double = 27 * a * a * a
        Dim q As Double = q1 / q2

        Dim l As Double = Math.Pow(q / 2.0, 2.0) + Math.Pow(p / 3.0, 3.0)

        Dim t1 As Double = 0
        Dim t2 As Double = 0
        Dim t3 As Double = 0

        Dim tl1 As Double = 0
        Dim tl2 As Double = 0
        Dim tl3 As Double = 0

        If l < 0.0 Then
            Dim theta As Double = Math.Acos((-q / 2.0) * Math.Sqrt(-27 / (p * p * p))) / 3.0
            Dim z As Double = Math.Sqrt(-(4.0 / 3.0) * p)

            t1 = z * Math.Cos(theta)
            t2 = -z * Math.Cos(theta + (Math.PI / 3.0))
            t3 = -z * Math.Cos(theta - (Math.PI / 3.0))
        ElseIf l > 0 Then
            Dim u1 As Double = (-q / 2.0) + Math.Sqrt(l)
            Dim v1 As Double = (-q / 2.0) - Math.Sqrt(l)

            Dim u As Double = 0
            Dim v As Double = 0

            If u1 >= 0 Then
                u = Math.Pow(u1, 1.0 / 3.0)

                If v1 >= 0 Then
                    v = Math.Pow(v1, 1.0 / 3.0)
                    t1 = u + v
                Else
                    v = Math.Pow(-v1, 1.0 / 3.0)
                    t1 = u - v
                End If
            Else
                u = Math.Pow(-u1, 1.0 / 3.0)

                If v1 >= 0 Then
                    v = Math.Pow(v1, 1.0 / 3.0)
                    t1 = -u + v
                Else
                    v = Math.Pow(-v1, 1.0 / 3.0)
                    t1 = -u - v
                End If
            End If

            'complex solutions ignored, we only need the real one...
            '(-(u + v) / 2.0) +- ((u - v) / 2.0) * Math.Sqrt(3) * i
            t2 = t1
            t3 = t1
        Else
            t1 = (3 * q) / p
            t2 = -(3 * q) / (2 * p)
            t3 = t2
        End If

        tl1 = t1 - (b / (3 * a))

        If tl1 >= 0 AndAlso tl1 <= 1.0 Then
            Return tl1
        End If

        tl2 = t2 - (b / (3 * a))

        If tl2 >= 0 AndAlso tl2 <= 1.0 Then
            Return tl2
        End If

        tl3 = t3 - (b / (3 * a))

        If tl3 >= 0 AndAlso tl3 <= 1.0 Then
            Return tl3
        End If

        Return Nearest0To1(prevT, tl1, tl2, tl3)
    End Function

    Private Function Nearest0To1(prevT As Double, tl1 As Double, tl2 As Double, tl3 As Double) As Double
        Dim l As New List(Of Double)()
        l.Add(Math.Abs(tl1 - prevT))
        l.Add(Math.Abs(tl2 - prevT))
        l.Add(Math.Abs(tl3 - prevT))

        Dim z As Double() = {tl1, tl2, tl3}

        Return z(l.IndexOf(l.Min()))
    End Function

    Private Sub CheckPts(pts As List(Of PointF), numberOfPoints As Integer, minY As Single, maxY As Single)
        Try
            If (pts IsNot Nothing) AndAlso pts.Count > 0 Then
                For i As Integer = 0 To pts.Count - 1
                    If pts(i).Y < minY Then
                        pts(i) = New PointF(pts(i).X, minY)
                    End If
                    If pts(i).Y > maxY Then
                        pts(i) = New PointF(pts(i).X, maxY)
                    End If
                Next

                Dim pts2 As New List(Of PointF)()
                pts2.AddRange(pts.ToArray())

                Dim prevX As Integer = Convert.ToInt32(pts2(0).X)

                For i As Integer = 1 To pts2.Count - 1
                    Dim z As Integer = Convert.ToInt32(pts2(i).X)
                    If z > prevX + 1 Then
                        Dim dist As Integer = z - Convert.ToInt32(pts2(i - 1).X)
                        Dim l As Double = (pts2(i).Y - pts2(i - 1).Y) / (pts2(i).X - pts2(i - 1).X)

                        For j As Integer = 1 To dist - 1
                            pts.Add(New PointF(z + j, Convert.ToSingle(l * j + pts2(i - 1).Y)))
                        Next
                    End If
                    prevX = z
                Next

                If pts.Count < numberOfPoints Then
                    While pts.Count < numberOfPoints
                        pts.Add(pts(pts.Count - 1))
                    End While
                End If

                If pts.Count > numberOfPoints Then
                    While pts.Count > numberOfPoints
                        pts.Remove(pts(pts.Count - 1))
                    End While
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    'public List<PointF> GetAllPoints(List<BezierSegment> segments)
    '{
    '    List<PointF> pts = new List<PointF>();
    '    try
    '    {
    '        if ((segments != null) && segments.Count > 0)
    '        {
    '            for (int i = 0; i <= segments.Count - 1; i++)
    '            {
    '                int dist = Convert.ToInt32(segments[i].P4.X - segments[i].P1.X);

    '                if (dist != 0.0)
    '                {
    '                    int startX = Convert.ToInt32(segments[i].P1.X);
    '                    int endX = segments.Count > i + 1 ? Convert.ToInt32(segments[i + 1].P1.X) : Convert.ToInt32(segments[i].P4.X) + 1;
    '                    List<PointF> pt = GetAllPointsForOneSegment(segments[i], startX, endX);

    '                    if (pt.Count > 0)
    '                    {
    '                        pts.AddRange(pt.ToArray());

    '                        if ((i < segments.Count - 1) && pt.Count > dist)
    '                        {
    '                            pts.RemoveAt(pts.Count - 1);
    '                        }
    '                    }
    '                }
    '            }
    '        }

    '        CheckPts(pts, (int)segments[segments.Count - 1].P4.X + 1, 0, (int)segments[segments.Count - 1].P4.X);

    '        pts = pts.OrderBy(a => (int)a.X).ThenBy(b => b.Y).ToList();
    '    }
    '    catch (Exception ex)
    '    {
    '        MessageBox.Show(ex.Message);
    '    }
    '    return pts;
    '}

    Public Function GetAllPoints(segments As List(Of BezierSegment), offset As Integer) As List(Of PointF)
        Dim pts As New List(Of PointF)()

        If (segments IsNot Nothing) AndAlso segments.Count > 0 Then
            For i As Integer = 0 To segments.Count - 1
                Dim dist As Double = Convert.ToInt32(segments(i).P4.X - segments(i).P1.X)
                If dist <> 0.0 Then
                    Dim startX As Integer = CInt(segments(i).P1.X)
                    Dim endX As Integer = If((segments.Count > i + 1), CInt(segments(i + 1).P1.X), CInt(segments(i).P4.X) + 1)
                    Dim pt As List(Of PointF) = GetAllPointsForOneSegment(segments(i), startX, endX, offset, 255 + offset)

                    If pt.Count > 0 Then
                        pts.AddRange(pt.ToArray())

                        If (i < segments.Count - 1) AndAlso pt.Count > dist Then
                            pts.RemoveAt(pts.Count - 1)
                        End If
                    End If
                End If
            Next

            CheckPts(pts, 256, offset, 255 + offset)

            pts = pts.OrderBy(Function(a) CInt(a.X)).ThenBy(Function(b) b.Y).ToList()
        End If

        Return pts
    End Function

    Public Function GetAllPointsForOneSegment(segment As BezierSegment, startX As Integer, endX As Integer, minVal As Integer, maxVal As Integer) As List(Of PointF)
        Dim pts As New List(Of PointF)()

        Try
            If segment.P4.X - segment.P1.X > 0F Then
                Dim cx As Double = 3 * (segment.P2.X - segment.P1.X)
                Dim bx As Double = 3 * (segment.P3.X - segment.P2.X) - cx
                Dim ax As Double = segment.P4.X - segment.P1.X - cx - bx

                Dim cy As Double = 3 * (segment.P2.Y - segment.P1.Y)
                Dim by As Double = 3 * (segment.P3.Y - segment.P2.Y) - cy
                Dim ay As Double = segment.P4.Y - segment.P1.Y - cy - by

                Dim prevT As Double = 0.0
                Dim fl As Double = segment.P1.X - CInt(segment.P1.X)

                For x As Integer = startX To endX - 1
                    'if(x == 12567)
                    '    x = x;
                    Dim t As Double = Solve(x + fl, ax, bx, cx, segment.P1.X, prevT)
                    prevT = t

                    If t < 0.0 Then
                        If x < endX - 1 Then
                            t = 0.0
                        Else
                            t = 1.0
                        End If
                    End If
                    If t > 1.0 Then
                        If x < endX - 1 Then
                            t = 0.0
                        Else
                            t = 1.0
                        End If
                    End If

                    Dim y As Double = ay * t * t * t + by * t * t + cy * t + segment.P1.Y

                    pts.Add(New PointF(CSng(x), Math.Max(Math.Min(CSng(y), maxVal), minVal)))

                    '/you could also use an int-counter, if the round-off errors increase too much....
                    'double t = 0.0;
                    '/give a little tolerance
                    'int prevX = 0;

                    'if (st > 0.0 && double.IsInfinity(st) == false && double.IsNaN(st) == false)
                    '{
                    '    while (t <= 1.0 + 0.0001) //a little tolerance
                    '    {
                    '        double xt = ax * t * t * t + bx * t * t + cx * t + segment.P1.X;
                    '        double yt = ay * t * t * t + by * t * t + cy * t + segment.P1.Y;

                    '        if ((int)xt > prevX)
                    '            pts.Add(new PointF(Convert.ToSingle(xt), Convert.ToSingle(yt)));

                    '        prevX = (int)xt;

                    '        t += st;
                    '    }
                    '}
                Next
            End If
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
        End Try

        Return pts
    End Function

    Public Function GetAllPoints(segments As List(Of BezierSegment), min As Single, max As Single) As List(Of PointF)
        Dim pts As New List(Of PointF)()
        Try
            If (segments IsNot Nothing) AndAlso segments.Count > 0 Then
                For i As Integer = 0 To segments.Count - 1
                    Dim dist As Integer = Convert.ToInt32(segments(i).P4.X - segments(i).P1.X)

                    If dist <> 0.0 Then
                        Dim startX As Integer = Convert.ToInt32(segments(i).P1.X)
                        Dim endX As Integer = If(segments.Count > i + 1, Convert.ToInt32(segments(i + 1).P1.X), Convert.ToInt32(segments(i).P4.X) + 1)
                        Dim pt As List(Of PointF) = GetAllPointsForOneSegment(segments(i), startX, endX)

                        If pt.Count > 0 Then
                            pts.AddRange(pt.ToArray())

                            If (i < segments.Count - 1) AndAlso pt.Count > dist Then
                                pts.RemoveAt(pts.Count - 1)
                            End If
                        End If
                    End If
                Next
            End If

            CheckPts(pts, CInt(segments(segments.Count - 1).P4.X) + 1, min, max)

            pts = pts.OrderBy(Function(a) CInt(a.X)).ThenBy(Function(b) b.Y).ToList()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
        Return pts
    End Function

    Public Function GetAllPoints(segments As List(Of BezierSegment), numberOfPoints As Integer, min As Single, max As Single) As List(Of PointF)
        Dim pts As New List(Of PointF)()
        Try
            If (segments IsNot Nothing) AndAlso segments.Count > 0 Then
                For i As Integer = 0 To segments.Count - 1
                    Dim dist As Integer = Convert.ToInt32(segments(i).P4.X - segments(i).P1.X)

                    If dist <> 0.0 Then
                        Dim startX As Integer = Convert.ToInt32(segments(i).P1.X)
                        Dim endX As Integer = If(segments.Count > i + 1, Convert.ToInt32(segments(i + 1).P1.X), Convert.ToInt32(segments(i).P4.X) + 1)
                        Dim pt As List(Of PointF) = GetAllPointsForOneSegment(segments(i), startX, endX)

                        If pt.Count > 0 Then
                            pts.AddRange(pt.ToArray())

                            If (i < segments.Count - 1) AndAlso pt.Count > dist Then
                                pts.RemoveAt(pts.Count - 1)
                            End If
                        End If
                    End If
                Next
            End If

            CheckPts(pts, numberOfPoints, min, max)

            pts = pts.OrderBy(Function(a) CInt(a.X)).ThenBy(Function(b) b.Y).ToList()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
        Return pts
    End Function

    Public Sub AddToPath(fPath As System.Drawing.Drawing2D.GraphicsPath, list As SegmentsList, st As Point, ByRef indx As Integer)
        Dim pts As New List(Of Point)()
        pts.Add(st)

        Dim fsg As Integer = indx

        Dim tension As Single = list(indx).Tension

        While indx < list.Count AndAlso list(indx).[GetType]().Equals(GetType(CurveSegment)) AndAlso (list(indx).IsNewCurveSegment = False Or fsg = indx)
            pts.Add(list(indx).EndPoint)
            indx += 1
        End While

        fPath.AddCurve(pts.ToArray(), tension)

        indx -= 1
    End Sub

    Public Sub AddToPathAndGetPoints(fPath As System.Drawing.Drawing2D.GraphicsPath, list As SegmentsList, st As Point, ByRef indx As Integer, ByRef allPoints As List(Of PointF), useYMax As Boolean)
        Dim pts As New List(Of Point)()
        pts.Add(st)

        Dim fsg As Integer = indx

        Dim tension As Single = list(indx).Tension

        While indx < list.Count AndAlso list(indx).[GetType]().Equals(GetType(CurveSegment)) AndAlso (list(indx).IsNewCurveSegment = False Or fsg = indx)
            pts.Add(list(indx).EndPoint)
            indx += 1
        End While

        fPath.AddCurve(pts.ToArray(), tension)
        Dim bz As List(Of BezierSegment) = CalcBezierSegments(pts.ToArray(), tension)
        Dim allCurvePoints As List(Of PointF) = GetAllPoints(bz, 0, (If(useYMax, CInt(bz(bz.Count - 1).P4.Y), CInt(bz(bz.Count - 1).P4.X))))

        If (allCurvePoints IsNot Nothing) AndAlso allCurvePoints.Count > 0 Then
            Dim dist As Integer = Convert.ToInt32(bz(bz.Count - 1).P4.X - bz(0).P1.X)

            If allCurvePoints.Count < dist + 1 Then
                While allCurvePoints.Count < dist
                    allCurvePoints.Add(allCurvePoints(allCurvePoints.Count - 1))
                End While
            End If

            If list.Count > indx Then
                allCurvePoints.RemoveAt(allCurvePoints.Count - 1)
            Else
                If allCurvePoints.Count < dist + 1 Then
                    While allCurvePoints.Count < dist + 1
                        allCurvePoints.Add(allCurvePoints(allCurvePoints.Count - 1))
                    End While
                End If

                If allCurvePoints.Count > dist + 1 Then
                    While allCurvePoints.Count > dist + 1
                        allCurvePoints.Remove(allCurvePoints(allCurvePoints.Count - 1))
                    End While
                End If
            End If

            allPoints.AddRange(allCurvePoints.ToArray())
        End If

        indx -= 1
    End Sub

    Public Sub GetPoints(list As SegmentsList, st As Point, ByRef indx As Integer, ByRef allPoints As List(Of PointF), useYMax As Boolean, yMax As Integer)
        Dim pts As New List(Of Point)()
        pts.Add(st)

        Dim fsg As Integer = indx

        Dim tension As Single = list(indx).Tension

        While indx < list.Count AndAlso list(indx).[GetType]().Equals(GetType(CurveSegment)) AndAlso (list(indx).IsNewCurveSegment = False Or fsg = indx)
            pts.Add(list(indx).EndPoint)
            indx += 1
        End While

        Dim bz As List(Of BezierSegment) = CalcBezierSegments(pts.ToArray(), tension)
        Dim allCurvePoints As List(Of PointF) = GetAllPoints(bz, 0, (If(useYMax, yMax, CInt(bz(bz.Count - 1).P4.X))))

        If (allCurvePoints IsNot Nothing) AndAlso allCurvePoints.Count > 0 Then
            Dim dist As Integer = Convert.ToInt32(bz(bz.Count - 1).P4.X - bz(0).P1.X)

            If allCurvePoints.Count < dist + 1 Then
                While allCurvePoints.Count < dist
                    allCurvePoints.Add(allCurvePoints(allCurvePoints.Count - 1))
                End While
            End If

            If list.Count > indx Then
                allCurvePoints.RemoveAt(allCurvePoints.Count - 1)
            Else
                If allCurvePoints.Count < dist + 1 Then
                    While allCurvePoints.Count < dist + 1
                        allCurvePoints.Add(allCurvePoints(allCurvePoints.Count - 1))
                    End While
                End If

                If allCurvePoints.Count > dist + 1 Then
                    While allCurvePoints.Count > dist + 1
                        allCurvePoints.Remove(allCurvePoints(allCurvePoints.Count - 1))
                    End While
                End If
            End If

            allPoints.AddRange(allCurvePoints.ToArray())
        End If

        indx -= 1
    End Sub

    Public Overrides Function ToString() As String
        Return StartPoint.ToString()
    End Function

    Public Function Clone() As Object Implements ICloneable.Clone
        Return New CurveSegment() With { _
         .StartPoint = Me.StartPoint, _
         .EndPoint = Me.EndPoint, _
         .Tension = Me.Tension, _
         .IsNewCurveSegment = Me.IsNewCurveSegment _
        }
    End Function
End Class
