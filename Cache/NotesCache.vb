Option Strict On

Public Class NotesCache
    Public Property Notes As List(Of String) = Nothing
    Public Property NotesCnt As Integer = -1
    Public Property InitialNotesCnt As Integer = -1

    Public Sub New()
        Init(New List(Of String))
    End Sub

    Public Sub Init(notes As List(Of String))
        Me.Notes = notes
        If Not Me.Notes Is Nothing Then
            NotesCnt = notes.Count
            InitialNotesCnt = notes.Count
        End If
    End Sub

    Public Sub Reset()
        Me.NotesCnt = Me.InitialNotesCnt
    End Sub

    Public Sub Increment()
        NotesCnt += 1
    End Sub

    Public Sub Decrement()
        NotesCnt -= 1
    End Sub

    Public Sub SyncAfterOperations()
        If Me.Notes.Count - Me.NotesCnt > 0 Then
            If Me.Notes.Count > (Me.Notes.Count - Me.NotesCnt) Then 'is or is not notescount negative?
                Me.Notes.RemoveRange(Me.NotesCnt, Me.Notes.Count - Me.NotesCnt)
            Else
                Me.NotesCnt = Me.Notes.Count
            End If
        End If
    End Sub

    Public Sub AddAndIncrement(v As String)
        Me.Notes.Add(v)
        Me.Increment()
    End Sub

    Public Sub SyncWithListAfterOperations(notesIn As NotesCache)
        If notesIn IsNot Nothing AndAlso Me.Notes IsNot Nothing Then
            SyncAfterOperations()
            Dim infoNotes As List(Of String) = notesIn.Notes
            infoNotes.AddRange(Me.Notes)
            notesIn.NotesCnt = Me.NotesCnt
        End If
    End Sub

    Public Sub SyncWithShapeAfterOperations(infoNotes As List(Of String))
        infoNotes.AddRange(Me.Notes)
    End Sub
End Class
