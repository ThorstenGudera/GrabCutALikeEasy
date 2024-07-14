Imports System.Drawing
Imports System.Windows.Forms

Public Class frmColorPresets
    Private _dontLoad As Boolean
    Private _lb2Changed As Boolean
    Private _index As Integer
    Public Property DataList As List(Of ColValues)

    Public Sub New(colData As List(Of ColValues))
        InitializeComponent()

        Me.DataList = colData
        If Me.DataList IsNot Nothing AndAlso Me.DataList.Count > 0 Then
            Me._dontLoad = True
        End If
    End Sub

    Private Sub frmColorPresets_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'If Not Me._dontLoad Then
        '    Dim pts As New List(Of Point)

        '    pts.Add(New Point(0, 0))
        '    pts.Add(New Point(255, 255))

        '    Dim ptsG As New List(Of Point)
        '    Dim ptsR As New List(Of Point)

        '    ptsG.AddRange(pts)
        '    ptsR.AddRange(pts)

        '    Me.DataList = New List(Of ColValues)
        '    Me.DataList.Add(New ColValues() With {.BluePts = pts, .GreenPts = ptsG, .RedPts = ptsR})
        'End If

        Me.ListBox1.DataSource = Me.DataList

        If Me.DataList.Count > 0 Then
            Me.ListBox1.SelectedIndex = 0
        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        If Me._lb2Changed Then
            If MessageBox.Show("Update current list first?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) = DialogResult.Yes Then
                Dim pts As New List(Of Point)

                For i As Integer = 0 To Me.ListBox2.Items.Count - 1
                    pts.Add(New Point(Me.ListBox2.Items(i).X, Me.ListBox2.Items(i).y))
                Next

                Dim ptsG As New List(Of Point)
                Dim ptsR As New List(Of Point)

                ptsG.AddRange(pts)
                ptsR.AddRange(pts)

                Me.DataList(Me._index).BluePts = pts
                Me.DataList(Me._index).GreenPts = ptsG
                Me.DataList(Me._index).RedPts = ptsR
            End If

            Me._lb2Changed = False
        End If
        If ListBox1.SelectedIndex > -1 AndAlso Me.DataList.Count > 0 Then
            Dim pts As List(Of Point) = Me.DataList(Me.ListBox1.SelectedIndex).BluePts

            Me.ListBox2.Items.Clear()

            For i As Integer = 0 To pts.Count - 1
                Me.ListBox2.Items.Add(pts(i))
            Next

            If Me.ListBox2.Items.Count > 0 Then
                Me.ListBox2.SelectedIndex = 0
            End If

            Me._index = Me.ListBox1.SelectedIndex
        End If
    End Sub

    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedIndexChanged
        If Me.ListBox2.SelectedIndex > -1 AndAlso Me.ListBox2.Items.Count > 0 Then
            Dim pt As Point = CType(CType(sender, ListBox).SelectedItem, Point)

            Me.numCPX.Value = CDec(pt.X)
            Me.numCPY.Value = CDec(pt.Y)
        End If
    End Sub

    Private Sub btnAddPt_Click(sender As Object, e As EventArgs) Handles btnAddPt.Click
        Dim pt As Point = New Point(CType(Me.numCPX.Value, Integer), CType(Me.numCPY.Value, Integer))

        Dim pts As New List(Of Point)

        For i As Integer = 0 To Me.ListBox2.Items.Count - 1
            pts.Add(New Point(Me.ListBox2.Items(i).X, Me.ListBox2.Items(i).y))
        Next

        Dim f As Boolean = False
        For i As Integer = 0 To Me.ListBox2.Items.Count - 1
            If CType(Me.ListBox2.Items(i), Point).X = pt.X Then
                pts(i) = New Point(Me.numCPX.Value, Me.numCPY.Value)
                f = True
                Exit For
            End If
        Next

        If Not f Then
            pts.Add(New Point(Me.numCPX.Value, Me.numCPY.Value))
        End If

        pts.Sort(New PointsComparer2())

        Me.ListBox2.Items.Clear()
        For i As Integer = 0 To pts.Count - 1
            Me.ListBox2.Items.Add(pts(i))
        Next

        Me._lb2Changed = True
    End Sub

    Private Sub AddPointToArray(ind As Integer, pt As Point)
        Me.DataList(ind).BluePts.Add(pt)
        Me.DataList(ind).GreenPts.Add(pt)
        Me.DataList(ind).RedPts.Add(pt)
    End Sub

    Private Sub SortArray(ind As Integer)
        Me.DataList(ind).BluePts.Sort(New PointsComparer2())
        Me.DataList(ind).GreenPts.Sort(New PointsComparer2())
        Me.DataList(ind).RedPts.Sort(New PointsComparer2())
    End Sub

    Private Sub btnRemPt_Click(sender As Object, e As EventArgs) Handles btnRemPt.Click
        Dim pt As Point = New Point(CType(Me.numCPX.Value, Integer), CType(Me.numCPY.Value, Integer))

        Dim pts As New List(Of Point)

        For i As Integer = 0 To Me.ListBox2.Items.Count - 1
            pts.Add(New Point(Me.ListBox2.Items(i).X, Me.ListBox2.Items(i).y))
        Next

        For i As Integer = 0 To Me.ListBox2.Items.Count - 1
            If CType(Me.ListBox2.Items(i), Point).X = pt.X Then
                pts.RemoveAt(i)
                Exit For
            End If
        Next

        pts.Sort(New PointsComparer2())

        Me.ListBox2.Items.Clear()
        For i As Integer = 0 To pts.Count - 1
            Me.ListBox2.Items.Add(pts(i))
        Next

        Me._lb2Changed = True
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        Dim col As New ColValues()

        If Me.ListBox2.Items.Count > 1 Then
            Dim pts As New List(Of Point)

            Dim pt As Point = CType(Me.ListBox2.Items(0), Point)

            If pt.X <> 0 Then
                pt = New Point(0, 0)
            Else
                If pt.Y <> 0 Then
                    pt = New Point(0, 0)
                End If
            End If

            Me.ListBox2.Items(0) = pt

            Dim pt2 As Point = CType(Me.ListBox2.Items(Me.ListBox2.Items.Count - 1), Point)

            If pt2.X <> 255 Then
                pt2 = New Point(255, 255)
            Else
                If pt2.Y <> 0 Then
                    pt2 = New Point(255, 255)
                End If
            End If

            Me.ListBox2.Items(Me.ListBox2.Items.Count - 1) = pt2

            For i As Integer = 0 To Me.ListBox2.Items.Count - 1
                pts.Add(Me.ListBox2.Items(i))
            Next

            Dim ptsG As New List(Of Point)
            Dim ptsR As New List(Of Point)

            ptsG.AddRange(pts)
            ptsR.AddRange(pts)

            col.BluePts = pts
            col.GreenPts = ptsG
            col.RedPts = ptsR

            Me.DataList.Add(col)
            Me.ListBox1.DataSource = Nothing
            Me.ListBox1.DataSource = Me.DataList

            Me.ListBox1_SelectedIndexChanged(Me.ListBox1, New EventArgs())
        End If
    End Sub

    Private Sub btnRemove_Click(sender As Object, e As EventArgs) Handles btnRemove.Click
        If Me.DataList.Count > 0 AndAlso Me.ListBox1.SelectedIndex > -1 Then
            Me.DataList.RemoveAt(Me.ListBox1.SelectedIndex)
            Me.ListBox1.DataSource = Nothing
            Me.ListBox1.DataSource = Me.DataList

            Me.ListBox1_SelectedIndexChanged(Me.ListBox1, New EventArgs())
        End If
    End Sub
End Class