Option Strict On

Imports System.Threading
Imports System.IO
Imports System.ComponentModel
Imports System.Drawing

Public Class Cache
    Implements IDisposable

    Private _cachePath As String = ""
    Private _cacheBasePath As String = ""

    Private _appDataApplicationsGroup As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Thorsten_Gudera")

    Private _isActive As Boolean = False
    Public ReadOnly Property IsActive() As Boolean
        Get
            Return _isActive
        End Get
    End Property

    Private _rnd As New Random()
    Private _controlMutex As Mutex = Nothing
    Private _threadCount As Integer = 0
    Private _lockObject As New Object()

    Public ReadOnly Property ThreadCount As Integer
        Get
            Return _threadCount
        End Get
    End Property

    Public Delegate Sub ProgressEventHandler(sender As Object, e As ProgressChangedEventArgs)
    Public Event ProgressChanged As ProgressEventHandler

    Public Delegate Sub NotificationEventHandler(sender As Object, e As NotificationEventArgs)
    Public Event Notify As NotificationEventHandler

    Public Delegate Sub DataSavedEventHandler(sender As Object, e As DataSavedEventArgs)
    Public Event DataSaved As DataSavedEventHandler

    Public Sub New(mutex As Mutex, t As Type)
        _controlMutex = mutex
        GetCacheBasePath(t)
    End Sub

    Public Sub New(mutex As Mutex, t As Type, baseFolderAddition As String)
        _controlMutex = mutex
        GetCacheBasePath(t, baseFolderAddition)
    End Sub

    Public Sub New(mutex As Mutex, t As Type, baseFolderAddition As String, addition As String)
        _controlMutex = mutex
        GetCacheBasePath(t, baseFolderAddition, addition)
    End Sub

    Public Sub New(mutex As Mutex, path As String)
        _controlMutex = mutex
        GetCacheBasePath(path)
    End Sub

    Public Function SetupCache() As Boolean
        Try
            Dim i As Integer = _rnd.[Next](0, Int32.MaxValue)

            While Directory.Exists(Path.Combine(_cacheBasePath, i.ToString()))
                i = _rnd.[Next](0, Int32.MaxValue)
            End While

            _cachePath = Path.Combine(_cacheBasePath, i.ToString())
            Directory.CreateDirectory(_cachePath)

            Me._isActive = True

            Return True

        Catch
        End Try

        Return False
    End Function

    Public Function SetupCacheForMappedCache(cacheNumber As String) As Boolean
        Try
            _cachePath = Path.Combine(_cacheBasePath, cacheNumber)

            Me._isActive = True
            Return True
        Catch
        End Try

        Return False
    End Function

    Private Sub GetCacheBasePath(t As Type)
        _cacheBasePath = Path.Combine(Path.Combine(_appDataApplicationsGroup, t.Assembly.GetName().Name), "Cache")
    End Sub

    Private Sub GetCacheBasePath(t As Type, basePathAddition As String)
        _cacheBasePath = Path.Combine(Path.Combine(Path.Combine(_appDataApplicationsGroup, basePathAddition), t.Assembly.GetName().Name), "Cache")
    End Sub

    Private Sub GetCacheBasePath(t As Type, basePathAddition As String, addition As String)
        _cacheBasePath = Path.Combine(Path.Combine(Path.Combine(_appDataApplicationsGroup, basePathAddition), String.Concat(t.Assembly.GetName().Name, addition)), "Cache")
    End Sub

    Private Sub GetCacheBasePath(path As String)
        _cacheBasePath = IO.Path.GetDirectoryName(path)
    End Sub

    Public Function ClearCachedFiles() As Boolean
        Try
            Dim dir As New DirectoryInfo(_cachePath)
            Dim fi As FileInfo() = dir.GetFiles()

            For i As Integer = 0 To fi.Length - 1
                Try
                    fi(i).Delete()

                Catch
                End Try
            Next

            Return True

        Catch
        End Try
        Return False
    End Function

    Public Function ClearCachedFiles(removeDirectory As Boolean) As Boolean
        Try
            Dim dir As New DirectoryInfo(_cachePath)
            Dim fi As FileInfo() = dir.GetFiles()

            For i As Integer = 0 To fi.Length - 1
                Try
                    fi(i).Delete()

                Catch
                End Try
            Next

            If removeDirectory Then
                Directory.Delete(_cachePath)
            End If

            Return True

        Catch
        End Try
        Return False
    End Function

    Public Function ClearCachedFiles(dir As DirectoryInfo) As Boolean
        Try
            Dim fi As FileInfo() = dir.GetFiles()

            For i As Integer = 0 To fi.Length - 1
                Try
                    fi(i).Delete()

                Catch
                End Try
            Next

            Return True

        Catch
        End Try
        Return False
    End Function

    Public Function ClearCompleteCache() As Boolean
        Try
            If _controlMutex.WaitOne(0, False) = True Then
                Try
                    If _cacheBasePath <> "" AndAlso Directory.Exists(_cacheBasePath) Then
                        Try
                            Dim dirs As DirectoryInfo() = New DirectoryInfo(_cacheBasePath).GetDirectories()
                            For i As Integer = dirs.Length - 1 To 0 Step -1
                                ClearCachedFiles(dirs(i))
                                dirs(i).Delete()
                            Next

                        Catch
                        End Try
                    End If
                Catch
                End Try

                _controlMutex.ReleaseMutex()
                Return True
            End If
        Catch ex As AbandonedMutexException
            'System.Windows.Forms.MessageBox.Show(ex.Message)
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
        End Try

        Return False
    End Function

    Public Function ClearFileCache(addPath As String) As Boolean
        Try
            If _controlMutex.WaitOne(0, False) = True Then
                Try
                    Dim cp As String = Me.GetCachePath()
                    If cp.EndsWith("\") Then
                        cp = cp.Substring(0, cp.Length - 2)
                    End If
                    cp = Path.GetDirectoryName(Path.GetDirectoryName(cp))
                    Dim detc As String = Path.Combine(cp, addPath)

                    If detc <> "" AndAlso Directory.Exists(detc) Then
                        Try
                            Dim dirs As DirectoryInfo() = New DirectoryInfo(detc).GetDirectories()
                            For i As Integer = dirs.Length - 1 To 0 Step -1
                                ClearCachedFiles(dirs(i))
                                dirs(i).Delete()
                            Next

                        Catch
                        End Try
                    End If
                Catch
                End Try

                _controlMutex.ReleaseMutex()
                Return True
            End If
        Catch ex As AbandonedMutexException
            'System.Windows.Forms.MessageBox.Show(ex.Message)
        Catch ex As Exception
            System.Windows.Forms.MessageBox.Show(ex.Message)
        End Try

        Return False
    End Function

    Public Sub AddToCache(bmp As Bitmap, position As Integer)
        OnProgressChanged(0)

        'You also could use the ThreadPool here, for that, create a class that 
        'holds the path, the clone of the bmp and the Undo object.
        'Create an instance of this class "t_data" and queue the thread like
        'ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork, t_data);
        'where DoWork is the method that does the work, you'll have to write it.
        'To update the progress-control use a delegate and invoke.
        'To do the work at finish, also use a delegate and invoke.

        Dim b As New BackgroundWorker()

        b.WorkerReportsProgress = True
        AddHandler b.ProgressChanged, AddressOf b_ProgressChanged
        AddHandler b.RunWorkerCompleted, AddressOf b_RunWorkerCompleted
        AddHandler b.DoWork, AddressOf b_DoWork

        OnProgressChanged(50)

        Me._threadCount += 1

        'dont use Clone() here (may block)
        Dim f As String = Path.Combine(_cachePath, position.ToString() + ".png")

        If Not bmp Is Nothing Then
            If AvailMem.AvailMem.checkAvailRam(bmp.Width * bmp.Height * 4L) Then
                b.RunWorkerAsync(New Object() {f, New Bitmap(bmp)})
            Else
                Windows.Forms.MessageBox.Show("Not enough Memory")

                RemoveHandler b.RunWorkerCompleted, AddressOf b_RunWorkerCompleted
                RemoveHandler b.ProgressChanged, AddressOf b_ProgressChanged
                RemoveHandler b.DoWork, AddressOf b_DoWork

                Me._threadCount -= 1

                OnNotify(f, -1)
                SetResult(f, -1)

                b.Dispose()
            End If
        Else
            RemoveHandler b.RunWorkerCompleted, AddressOf b_RunWorkerCompleted
            RemoveHandler b.ProgressChanged, AddressOf b_ProgressChanged
            RemoveHandler b.DoWork, AddressOf b_DoWork

            Me._threadCount -= 1

            OnNotify(f, -1)
            SetResult(f, -1)

            b.Dispose()
        End If
    End Sub

    Public Sub AddToCacheDirect(bmp As Bitmap, position As Integer)
        Dim f As String = Path.Combine(_cachePath, position.ToString() + ".png")

        Dim dir As String = Path.GetDirectoryName(f)
        If Directory.Exists(dir) = False Then
            Directory.CreateDirectory(dir)
        End If

        If Not bmp Is Nothing Then
            bmp.Save(f, System.Drawing.Imaging.ImageFormat.Png)
        End If
    End Sub

    Private Sub b_DoWork(sender As Object, e As DoWorkEventArgs)
        If TypeOf e.Argument Is Object() Then
            Dim o As Object() = DirectCast(e.Argument, Object())
            If o.Length = 2 Then
                If TypeOf o(0) Is String AndAlso TypeOf o(1) Is Bitmap Then
                    SyncLock _lockObject
                        Dim b As BackgroundWorker = DirectCast(sender, BackgroundWorker)
                        If b.WorkerReportsProgress Then
                            b.ReportProgress(75)
                        End If

                        Dim bmp As Bitmap = Nothing

                        Try
                            Dim dir As String = Path.GetDirectoryName(o(0).ToString())
                            If Directory.Exists(dir) = False Then
                                Directory.CreateDirectory(dir)
                            End If
                            bmp = DirectCast(o(1), Bitmap)
                            bmp.Save(o(0).ToString(), System.Drawing.Imaging.ImageFormat.Png)
                            e.Result = New Object() {o(0), 0}
                        Catch
                            e.Result = New Object() {o(0), -1}
                        End Try

                        If bmp IsNot Nothing Then
                            bmp.Dispose()
                        End If

                        If b.WorkerReportsProgress Then
                            b.ReportProgress(100)
                        End If
                    End SyncLock
                ElseIf TypeOf o(0) Is String AndAlso TypeOf o(1) Is FloatPointPxBitmap.FloatPointPxBitmap Then
                    SyncLock _lockObject
                        Dim b As BackgroundWorker = DirectCast(sender, BackgroundWorker)
                        If b.WorkerReportsProgress Then
                            b.ReportProgress(51)
                        End If

                        Dim fpBmp As FloatPointPxBitmap.FloatPointPxBitmap = Nothing

                        Try
                            Dim dir As String = Path.GetDirectoryName(o(0).ToString())
                            If Directory.Exists(dir) = False Then
                                Directory.CreateDirectory(dir)
                            End If
                            fpBmp = DirectCast(o(1), FloatPointPxBitmap.FloatPointPxBitmap)
                            fpBmp.Save(o(0).ToString(), b)
                            If Not fpBmp Is Nothing Then
                                fpBmp.Dispose()
                            End If
                            e.Result = New Object() {o(0), 0}
                        Catch
                            e.Result = New Object() {o(0), -1}
                        End Try

                        If b.WorkerReportsProgress Then
                            b.ReportProgress(100)
                        End If
                    End SyncLock
                End If
            End If
        End If
    End Sub

    Private Sub b_ProgressChanged(sender As Object, e As ProgressChangedEventArgs)
        OnProgressChanged(e.ProgressPercentage)
    End Sub

    Private Sub b_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        RemoveHandler DirectCast(sender, BackgroundWorker).RunWorkerCompleted, AddressOf b_RunWorkerCompleted
        RemoveHandler DirectCast(sender, BackgroundWorker).ProgressChanged, AddressOf b_ProgressChanged
        RemoveHandler DirectCast(sender, BackgroundWorker).DoWork, AddressOf b_DoWork

        Me._threadCount -= 1

        Dim o As Object() = DirectCast(e.Result, Object())

        OnNotify(o(0).ToString(), CInt(o(1)))
        SetResult(o(0).ToString(), CInt(o(1)))

        DirectCast(sender, BackgroundWorker).Dispose()
    End Sub

    Private Sub SetResult(f As String, res As Integer)
        OnDataSaved(f)
    End Sub

    Public Function LoadFromCache(position As Integer) As Bitmap
        Dim bmp As Bitmap = Nothing
        Dim img As Image = Nothing
        Try
            Dim i As Integer = 0
            While Me._threadCount > 0 AndAlso i < 40
                Thread.Sleep(100)
                System.Windows.Forms.Application.DoEvents()
                i += 1
            End While

            If position >= 0 Then
                Dim f As String = Path.Combine(_cachePath, position.ToString() + ".png")
                If File.Exists(f) Then
                    img = Image.FromFile(f)
                    If AvailMem.AvailMem.checkAvailRam(img.Width * img.Height * 4L) Then
                        bmp = New Bitmap(img)
                    Else
                        Windows.Forms.MessageBox.Show("Not enough Memory")
                    End If
                    img.Dispose()
                End If
            End If

            Return bmp
        Catch
            If img IsNot Nothing Then
                img.Dispose()
                img = Nothing
            End If
            If bmp IsNot Nothing Then
                bmp.Dispose()
                bmp = Nothing
            End If
        End Try

        Return Nothing
    End Function

    Protected Overridable Sub OnDataSaved(fileName As String)
        'Dim handler As DataSavedEventHandler = Me.DataSaved
        'If handler IsNot Nothing Then
        RaiseEvent DataSaved(Me, New DataSavedEventArgs() With {
         .FileName = fileName
        })
        'End If
    End Sub

    Protected Overridable Sub OnNotify(fileName As String, result As Integer)
        'Dim handler As NotificationEventHandler = Me.Notify
        'If handler IsNot Nothing Then
        RaiseEvent Notify(Me, New NotificationEventArgs() With {
         .FileName = fileName,
         .Result = result
        })
        'End If
    End Sub

    Protected Overridable Sub OnProgressChanged(percentage As Integer)
        'Dim handler As ProgressEventHandler = Me.ProgressChanged
        'If handler IsNot Nothing Then
        RaiseEvent ProgressChanged(Me, New ProgressChangedEventArgs(percentage, Nothing))
        'End If
    End Sub

    Friend Function GetCachePath() As String
        Return _cachePath
    End Function

    'Friend Sub AddToCache(fpBmp As BitmapFloatPointPixels.FloatPointPxBitmap, position As Integer)
    '    OnProgressChanged(0)

    '    'You also could use the ThreadPool here, for that, create a class that 
    '    'holds the path, the clone of the bmp and the Undo object.
    '    'Create an instance of this class "t_data" and queue the thread like
    '    'ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork, t_data);
    '    'where DoWork is the method that does the work, you'll have to write it.
    '    'To update the progress-control use a delegate and invoke.
    '    'To do the work at finish, also use a delegate and invoke.

    '    Dim b As New BackgroundWorker()

    '    b.WorkerReportsProgress = True
    '    b.ProgressChanged += New ProgressChangedEventHandler(AddressOf b_ProgressChanged)
    '    b.RunWorkerCompleted += New RunWorkerCompletedEventHandler(AddressOf b_RunWorkerCompleted)
    '    b.DoWork += New DoWorkEventHandler(AddressOf b_DoWork)

    '    OnProgressChanged(50)

    '    Me._threadCount += 1
    '    'dont use Clone() here (may block)
    '    b.RunWorkerAsync(New Object() {Path.Combine(_cachePath, position.ToString() + ".fpBmp"), fpBmp})
    'End Sub

    'Friend Function LoadFPBmpFromCache(position As Integer) As BitmapFloatPointPixels.FloatPointPxBitmap
    '    Dim fpBmp As BitmapFloatPointPixels.FloatPointPxBitmap = Nothing

    '    Try
    '        Dim i As Integer = 0
    '        While Me._threadCount > 0 AndAlso i < 40
    '            Thread.Sleep(100)
    '            System.Windows.Forms.Application.DoEvents()
    '            i += 1
    '        End While

    '        If position >= 0 Then
    '            Dim fileName As String = Path.Combine(_cachePath, position.ToString() + ".fpBmp")
    '            fpBmp = BitmapFloatPointPixels.FloatPointPxBitmap.Open(fileName)
    '        End If

    '        Return fpBmp
    '    Catch ex As Exception
    '        If fpBmp IsNot Nothing Then
    '            fpBmp.Pixels = Nothing
    '        End If

    '        System.Windows.Forms.MessageBox.Show(ex.Message)
    '    End Try

    '    Return Nothing
    'End Function

    'Friend Function LoadFPBmpFromCache(position As Integer, bgw As System.ComponentModel.BackgroundWorker) As BitmapFloatPointPixels.FloatPointPxBitmap
    '    Dim fpBmp As BitmapFloatPointPixels.FloatPointPxBitmap = Nothing

    '    Try
    '        Dim i As Integer = 0
    '        While Me._threadCount > 0 AndAlso i < 40
    '            Thread.Sleep(100)
    '            System.Windows.Forms.Application.DoEvents()
    '            i += 1
    '        End While

    '        If position >= 0 Then
    '            Dim fileName As String = Path.Combine(_cachePath, position.ToString() + ".fpBmp")
    '            fpBmp = BitmapFloatPointPixels.FloatPointPxBitmap.Open(fileName, bgw)
    '        End If

    '        Return fpBmp
    '    Catch ex As Exception
    '        If fpBmp IsNot Nothing Then
    '            fpBmp.Pixels = Nothing
    '        End If

    '        System.Windows.Forms.MessageBox.Show(ex.Message)
    '    End Try

    '    Return Nothing
    'End Function


    Friend Sub AddToCache(fpBmp As FloatPointPxBitmap.FloatPointPxBitmap, position As Integer)
        OnProgressChanged(0)

        'You also could use the ThreadPool here, for that, create a class that 
        'holds the path, the clone of the bmp and the Undo object.
        'Create an instance of this class "t_data" and queue the thread like
        'ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork, t_data);
        'where DoWork is the method that does the work, you'll have to write it.
        'To update the progress-control use a delegate and invoke.
        'To do the work at finish, also use a delegate and invoke.

        Dim b As New BackgroundWorker()

        b.WorkerReportsProgress = True
        AddHandler b.ProgressChanged, AddressOf b_ProgressChanged
        AddHandler b.RunWorkerCompleted, AddressOf b_RunWorkerCompleted
        AddHandler b.DoWork, AddressOf b_DoWork

        OnProgressChanged(50)

        Me._threadCount += 1
        'dont use Clone() here (may block)
        b.RunWorkerAsync(New Object() {Path.Combine(_cachePath, position.ToString() + ".fpBmp"), fpBmp})
    End Sub

    Friend Function LoadFPBmpFromCache(position As Integer) As FloatPointPxBitmap.FloatPointPxBitmap
        Dim fpBmp As FloatPointPxBitmap.FloatPointPxBitmap = Nothing

        Try
            Dim i As Integer = 0
            While Me._threadCount > 0 AndAlso i < 40
                Thread.Sleep(100)
                System.Windows.Forms.Application.DoEvents()
                i += 1
            End While

            If position >= 0 Then
                Dim fileName As String = Path.Combine(_cachePath, position.ToString() + ".fpBmp")
                fpBmp = FloatPointPxBitmap.FloatPointPxBitmap.Open(fileName)
            End If

            Return fpBmp
        Catch ex As Exception
            If fpBmp IsNot Nothing Then
                fpBmp.Dispose()
                fpBmp = Nothing
            End If

            System.Windows.Forms.MessageBox.Show(ex.Message)
        End Try

        Return Nothing
    End Function

    Friend Function LoadFPBmpFromCache(position As Integer, bgw As System.ComponentModel.BackgroundWorker) As FloatPointPxBitmap.FloatPointPxBitmap
        Dim fpBmp As FloatPointPxBitmap.FloatPointPxBitmap = Nothing

        Try
            Dim i As Integer = 0
            While Me._threadCount > 0 AndAlso i < 40
                Thread.Sleep(100)
                System.Windows.Forms.Application.DoEvents()
                i += 1
            End While

            If position >= 0 Then
                Dim fileName As String = Path.Combine(_cachePath, position.ToString() + ".fpBmp")
                fpBmp = FloatPointPxBitmap.FloatPointPxBitmap.Open(fileName, bgw)
            End If

            Return fpBmp
        Catch ex As Exception
            If fpBmp IsNot Nothing Then
                fpBmp.Dispose()
                fpBmp = Nothing
            End If

            System.Windows.Forms.MessageBox.Show(ex.Message)
        End Try

        Return Nothing
    End Function

    Sub ShiftPngNames(dirUp As Boolean, curPos As Integer)
        Dim f() As FileInfo = New DirectoryInfo(_cachePath).GetFiles("*.png")
        If dirUp Then
            For i As Integer = f.Length - 1 To curPos Step -1
                f(i).MoveTo(Path.Combine(_cachePath, (i + 1).ToString + ".png"))
            Next
        Else
            f(curPos - 1).Delete()
            For i As Integer = curPos To f.Length - 1
                f(i).MoveTo(Path.Combine(_cachePath, (i - 1).ToString + ".png"))
            Next
        End If
    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                If Not _controlMutex Is Nothing Then
                    Try
                        Dim b As Boolean = _controlMutex.SafeWaitHandle.IsClosed

                        If Not b AndAlso _controlMutex.WaitOne(0, False) = True Then
                            _controlMutex.ReleaseMutex()
                        End If
                    Catch

                    End Try
                    _controlMutex.Dispose()
                    _controlMutex = Nothing
                End If
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class
