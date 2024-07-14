Option Strict On

Imports System.Drawing
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports Cache

Public Class UndoOPCache
    Implements IDisposable
    Public Property CurrentPosition() As Integer
        Get
            Return m_CurrentPosition
        End Get
        Set(value As Integer)
            m_CurrentPosition = value
        End Set
    End Property
    Private m_CurrentPosition As Integer
    Private _curPosMode As New List(Of FpBmpInfo)()
    'use FPBmp and do doubleSize
    Public ReadOnly Property CurPosMode() As List(Of FpBmpInfo)
        Get
            Return _curPosMode
        End Get
    End Property
    'public Bitmap BOut { get; set; }
    Public Property Processing() As Boolean
        Get
            Return m_Processing
        End Get
        Set(value As Boolean)
            m_Processing = value
        End Set
    End Property
    Private m_Processing As Boolean
    Public Property Count() As Integer
        Get
            Return m_Count
        End Get
        Set(value As Integer)
            m_Count = value
        End Set
    End Property
    Private m_Count As Integer

    Public ReadOnly Property ThreadCount() As Integer
        Get
            Return If(Cache Is Nothing, 0, Cache.ThreadCount)
        End Get
    End Property

    Private _cache As Cache = Nothing
    Public ReadOnly Property Cache() As Cache
        Get
            Return _cache
        End Get
    End Property
    Private _controlMutex As New Mutex(True, "TG_DogEarsTest")

    Public Property ImageBackupPointer() As Integer
        Get
            Return m_ImageBackupPointer
        End Get
        Set(value As Integer)
            m_ImageBackupPointer = value
        End Set
    End Property
    Private m_ImageBackupPointer As Integer

    Private _lockObject As New Object()

    Public Sub New(t As Type)
        _cache = New Cache(_controlMutex, t)
        _cache.ClearCompleteCache()
        _cache.SetupCache()
    End Sub

    Public Sub New(t As Type, baseFolderAddition As String)
        _cache = New Cache(_controlMutex, t, baseFolderAddition)
        _cache.ClearCompleteCache()
        _cache.SetupCache()
    End Sub

    Public Sub New(t As Type, baseFolderAddition As String, subFolder As String)
        _cache = New Cache(_controlMutex, t, baseFolderAddition, subFolder)
        _cache.ClearCompleteCache()
        _cache.SetupCache()
    End Sub

    Public Sub New(t As Type, baseFolderAddition As String, clear As Boolean)
        _cache = New Cache(_controlMutex, t, baseFolderAddition)
        If clear Then
            _cache.ClearCompleteCache()
        End If
        _cache.SetupCache()
    End Sub

    Public Sub New(t As Type, baseFolderAddition As String, addition As String, firstInstance As Boolean)
        _cache = New Cache(_controlMutex, t, baseFolderAddition, addition)

        If firstInstance Then
            _cache.ClearCompleteCache()
        End If

        _cache.SetupCache()
    End Sub

    Public Sub New(cachePathForShape As String, position As Integer)
        _cache = New Cache(_controlMutex, cachePathForShape)
        If cachePathForShape.EndsWith("\") Then
            cachePathForShape = cachePathForShape.Substring(0, cachePathForShape.Length - 2)
        End If
        _cache.SetupCacheForMappedCache(cachePathForShape.Substring(cachePathForShape.LastIndexOf("\") + 1))

        Me.CurrentPosition = GetCurrentPosition(Me.GetCachePath, position)
    End Sub

    Private Function GetCurrentPosition(cachePath As String, position As Integer) As Integer
        Dim dir As New IO.DirectoryInfo(cachePath)
        Dim fi() As IO.FileInfo = dir.GetFiles("*.png")

        Dim i As Integer = 0

        While IO.File.Exists(IO.Path.Combine(dir.FullName, i.ToString() & ".png"))
            Me.CurrentPosition += 1
            i += 1
        End While

        Count = i

        If position < i Then
            i = position
        End If

        Return i
    End Function

    Public Sub SetCount()
        Dim dir As New IO.DirectoryInfo(Me.GetCachePath())
        Dim fi() As IO.FileInfo = dir.GetFiles("*.png")

        Dim i As Integer = 0

        While IO.File.Exists(IO.Path.Combine(dir.FullName, i.ToString() & ".png"))
            i += 1
        End While

        Count = i
    End Sub

    Public Property FPUsed As Boolean

    Public Sub Add(bmp As Bitmap)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            _cache.AddToCache(bmp, Me.CurrentPosition)

            Try
                If CurPosMode.Count - CurrentPosition >= 0 Then
                    CurPosMode.RemoveRange(CurrentPosition, CurPosMode.Count - CurrentPosition)
                Else
                    Console.WriteLine("Cache RemoveRange: " & (CurPosMode.Count - CurrentPosition).ToString())
                End If
            Catch
            End Try

            CurrentPosition += 1
            CurPosMode.Add(New FpBmpInfo(False, False, 0, 0))
            Count = CurrentPosition
        End If
    End Sub

    Public Sub Add(bmp As Bitmap, doDoubleSize As Boolean)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            _cache.AddToCache(bmp, Me.CurrentPosition)

            Try
                CurPosMode.RemoveRange(CurrentPosition, CurPosMode.Count - CurrentPosition)
            Catch
            End Try

            CurrentPosition += 1
            CurPosMode.Add(New FpBmpInfo(False, doDoubleSize, 0, 0.5))
            Count = CurrentPosition
        End If
    End Sub

    Public Function DoUndo() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If CurrentPosition > 1 Then
                CurrentPosition -= 1
                bOut = _cache.LoadFromCache(CurrentPosition - 1)
            End If
        End If

        Return bOut
    End Function

    Public Function DoUndo(PreviewMode As Boolean) As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If CurrentPosition > 1 Then
                CurrentPosition -= 1
                If PreviewMode Then
                    If CurrentPosition > ImageBackupPointer Then
                        bOut = _cache.LoadFromCache(CurrentPosition - 1)
                    End If
                Else
                    bOut = _cache.LoadFromCache(CurrentPosition - 1)
                End If
            End If
        End If

        Return bOut
    End Function

    Public Function DoUndoAll() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            CurrentPosition = 1
            bOut = _cache.LoadFromCache(CurrentPosition - 1)
        End If

        Return bOut
    End Function

    Public Function DoUndoAll(PreviewMode As Boolean) As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then

            If PreviewMode Then
                CurrentPosition = ImageBackupPointer + 1
                'if (CurrentPosition > ImageBackupPointer)
                bOut = _cache.LoadFromCache(CurrentPosition - 1)
            Else
                CurrentPosition = 1
                bOut = _cache.LoadFromCache(CurrentPosition - 1)
            End If
        End If

        Return bOut
    End Function

    Public Function DoRedo() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If Count > CurrentPosition Then
                If CurrentPosition >= 0 Then
                    bOut = _cache.LoadFromCache(CurrentPosition)
                End If
            End If

            CurrentPosition += 1
        End If

        Return bOut
    End Function

    Public Function DoRedo(PreviewMode As Boolean) As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If PreviewMode Then
                If Count > CurrentPosition AndAlso CurrentPosition > ImageBackupPointer Then
                    If CurrentPosition >= 0 Then
                        bOut = _cache.LoadFromCache(CurrentPosition)
                    End If
                End If
            Else
                If Count > CurrentPosition Then
                    If CurrentPosition >= 0 Then
                        bOut = _cache.LoadFromCache(CurrentPosition)
                    End If
                End If
            End If

            CurrentPosition += 1
        End If

        Return bOut
    End Function

    Public Sub Reset(resetCount As Boolean)
        CurrentPosition = 1

        'CurPosMode.Clear()
        ''first pic in cache is always a bmp not a FPBmp so add doDoubleSize as false
        'CurPosMode.Add(New FpBmpInfo(False, False))

        If resetCount Then
            Count = CurrentPosition
        End If
    End Sub

    Public Sub Clear(bmp As Bitmap)
        CurrentPosition = 0
        CurPosMode.Clear()
        Count = 0

        Add(bmp)
    End Sub

    Public Sub CheckRedoButton(ct As Control)
        If CurrentPosition < Count Then
            ct.Enabled = True
        Else
            ct.Enabled = False
        End If
    End Sub

    Public Sub SetImgBackupPointer(firstPreviewImage As Bitmap)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            Me.ImageBackupPointer = CurrentPosition
            Me.FPUsed = False
            If Not Me._curPosMode Is Nothing AndAlso Me._curPosMode.Count > Me.CurrentPosition AndAlso Me._curPosMode(CurrentPosition).UseFPBmp Then
                Me.FPUsed = True
            End If
            Me.Add(firstPreviewImage)
        End If
    End Sub

    Public Function GetBackupBitmapAndReset() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            CurrentPosition = Me.ImageBackupPointer
            bOut = _cache.LoadFromCache(CurrentPosition - 1)

            ClearToImageBackupPointer()
        End If

        If Not CurPosMode Is Nothing AndAlso CurPosMode.Count > CurrentPosition Then
            If Me.FPUsed Then
                CurPosMode(CurrentPosition).UseFPBmp = True
            End If
            If Count = Me.CurrentPosition Then
                Count += 1
            End If
        End If

        Return bOut
    End Function

    Public Function GetBackupBitmap() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            CurrentPosition = Me.ImageBackupPointer
            bOut = _cache.LoadFromCache(CurrentPosition)
            CurrentPosition += 1
        End If

        Return bOut
    End Function

    Private Sub ClearToImageBackupPointer()
        Count = Me.CurrentPosition
        ResetImgBackupPointer()
    End Sub

    Public Function GetFirstPreviewBitmap() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            CurrentPosition = Me.ImageBackupPointer
            bOut = _cache.LoadFromCache(CurrentPosition)
        End If

        Return bOut
    End Function

    Public Sub ResetImgBackupPointer()
        Me.ImageBackupPointer = 0
    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' So ermitteln Sie überflüssige Aufrufe

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' Verwalteten Zustand löschen (verwaltete Objekte).
                If _controlMutex IsNot Nothing Then
                    If Not _cache Is Nothing Then
                        _cache.Dispose()
                    End If
                    Try
                        Try
                            Dim b As Boolean = _controlMutex.SafeWaitHandle.IsClosed

                            If Not b AndAlso _controlMutex.WaitOne(0, False) = True Then
                                _controlMutex.ReleaseMutex()
                            End If
                        Catch

                        End Try
                        _controlMutex.Dispose()
                        _controlMutex = Nothing
                    Catch
                    End Try
                End If
            End If

            ' Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalize() unten überschreiben.
            ' Große Felder auf NULL festlegen.

        End If
        Me.disposedValue = True
    End Sub

    ' Finalize() nur überschreiben, wenn Dispose(ByVal disposing As Boolean) oben über Code zum Freigeben von nicht verwalteten Ressourcen verfügt.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region

    Public Function GetCachePath() As String
        Return _cache.GetCachePath()
    End Function

    Public Sub IncrementManually()
        Me.CurrentPosition += 1
    End Sub

    Public Sub AddFPBmp(fpBmp As FloatPointPxBitmap.FloatPointPxBitmap, doDoubleSize As Boolean, b As Double, c As Double)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            'create a copy since further operations on the Pixels array might be in process
            Dim fCopy As FloatPointPxBitmap.FloatPointPxBitmap = Nothing
            SyncLock Me._lockObject
                fCopy = CType(fpBmp.Clone(), FloatPointPxBitmap.FloatPointPxBitmap)
            End SyncLock
            'will be disposed in bgw of cache
            _cache.AddToCache(fCopy, Me.CurrentPosition)

            Try
                CurPosMode.RemoveRange(CurrentPosition, CurPosMode.Count - CurrentPosition)
            Catch
            End Try

            CurrentPosition += 1
            CurPosMode.Add(New FpBmpInfo(True, doDoubleSize, b, c))
            Count = CurrentPosition
        End If
    End Sub

    Public Function PeekUndo(PreviewMode As Boolean) As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive AndAlso CurrentPosition >= 2 Then
            If PreviewMode Then
                If CurrentPosition > ImageBackupPointer Then
                    bOut = _cache.LoadFromCache(CurrentPosition - 2)
                End If
            Else
                bOut = _cache.LoadFromCache(CurrentPosition - 2)
            End If
        End If

        Return bOut
    End Function

    Public Function UndoFPBmp(bgw As System.ComponentModel.BackgroundWorker) As FloatPointPxBitmap.FloatPointPxBitmap
        Dim bOut As FloatPointPxBitmap.FloatPointPxBitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            CurrentPosition -= 1
            bOut = _cache.LoadFPBmpFromCache(CurrentPosition - 1, bgw)
        End If

        Return bOut
    End Function

    Public Function RedoFPBmp(bgw As System.ComponentModel.BackgroundWorker) As FloatPointPxBitmap.FloatPointPxBitmap
        Dim bOut As FloatPointPxBitmap.FloatPointPxBitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If Count > CurrentPosition Then
                If CurrentPosition >= 0 Then
                    bOut = _cache.LoadFPBmpFromCache(CurrentPosition, bgw)
                End If
            End If

            CurrentPosition += 1
        End If

        Return bOut
    End Function

    Public Function GetBackupFPBitmapAndReset(bgw As System.ComponentModel.BackgroundWorker) As FloatPointPxBitmap.FloatPointPxBitmap
        Dim bOut As FloatPointPxBitmap.FloatPointPxBitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            CurrentPosition = Me.ImageBackupPointer
            bOut = _cache.LoadFPBmpFromCache(CurrentPosition - 1, bgw)

            ClearToImageBackupPointer()
        End If

        Return bOut
    End Function

    Sub ResetPreviewMode()
        CurrentPosition = Me.ImageBackupPointer
        ClearToImageBackupPointer()
        If Not CurPosMode Is Nothing AndAlso CurPosMode.Count > CurrentPosition Then
            If Me.FPUsed Then
                CurPosMode(CurrentPosition).UseFPBmp = True
            End If
            If Count = Me.CurrentPosition Then
                Count += 1
            End If
        End If
    End Sub

    Sub ShiftPngNames(dirUp As Boolean)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            _cache.ShiftPngNames(dirUp, Me.CurrentPosition)
        End If
    End Sub

    Public Function DoUndoWithoutDecrementing() As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If CurrentPosition > 0 Then
                bOut = _cache.LoadFromCache(CurrentPosition - 1)
            Else
                If Me.Count > 0 Then
                    MessageBox.Show("Incorrect position, loading pic 0, use undo and redo buttons to navigate")
                    bOut = _cache.LoadFromCache(0)
                End If
            End If
        End If

        Return bOut
    End Function

    Public Sub Save(bmp As Bitmap, fileNumber As Integer)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            'changed from _cache.AddToCache(bmp, fileNumber) for testing
            _cache.AddToCacheDirect(bmp, fileNumber)
        End If
    End Sub

    Public Function Load(fileNumber As Integer) As Bitmap
        Dim bOut As Bitmap = Nothing
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            bOut = _cache.LoadFromCache(fileNumber)
        End If
        Return bOut
    End Function

    Public Sub CheckFiles(length As Integer, skipNumers As List(Of Boolean))
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            Dim i As Integer = 0
            Dim maxRetry As Integer = 100
            For position As Integer = 0 To length - 1
                If Not skipNumers(position) Then
                    While i < maxRetry AndAlso Not File.Exists(Path.Combine(Me.Cache.GetCachePath(), position.ToString() + ".png"))
                        i += 1
                        Thread.Sleep(100)
                    End While

                    If i >= maxRetry Then
                        Throw New Exception("Cannot load pic no. " & position.ToString())
                    End If
                End If
            Next
        Else
            Throw New Exception("Cache is null")
        End If
    End Sub
End Class
