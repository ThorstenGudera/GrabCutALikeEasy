Option Strict On

Imports System.Drawing
Imports System.Threading

Public Class PictureCache
    Implements IDisposable
    Public Property Processing() As Boolean
        Get
            Return m_Processing
        End Get
        Set(value As Boolean)
            m_Processing = value
        End Set
    End Property
    Private m_Processing As Boolean
    Public Property Positions() As List(Of PictureCachePosition)
        Get
            Return m_Positions
        End Get
        Set(value As List(Of PictureCachePosition))
            m_Positions = value
        End Set
    End Property
    Private m_Positions As List(Of PictureCachePosition)

    Private _cache As Cache = Nothing
    Private _controlMutex As New Mutex(True, "TG_ComposeContainerCache")

    Public Sub New(t As Type)
        _cache = New Cache(_controlMutex, t)
        _cache.ClearCompleteCache()
        _cache.SetupCache()

        Positions = New List(Of PictureCachePosition)()
    End Sub

    Public Sub New(t As Type, baseFolderAddition As String)
        _cache = New Cache(_controlMutex, t, baseFolderAddition)
        _cache.ClearCompleteCache()
        _cache.SetupCache()

        Positions = New List(Of PictureCachePosition)()
    End Sub

    Public Sub Add(bmp As Bitmap, currentName As String)
        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            _cache.AddToCache(bmp, Me.Positions.Count)
            Positions.Add(New PictureCachePosition(Me.Positions.Count, currentName))
        End If
    End Sub

    Public Function LoadFromCache(position As Integer) As Bitmap
        Dim bOut As Bitmap = Nothing

        If _cache IsNot Nothing AndAlso _cache.IsActive Then
            If position >= 0 Then
                bOut = _cache.LoadFromCache(position)
            End If
        End If

        Return bOut
    End Function

    Public Sub Remove(id As Integer, name As String)
        Dim p As PictureCachePosition = Me.Positions.Where(Function(a) a.Number = id AndAlso a.Name = name).First()

        If p IsNot Nothing Then
            Me.Positions.Remove(p)
        End If
    End Sub

    Public Sub Remove(p As PictureCachePosition)
        If p IsNot Nothing Then
            Me.Positions.Remove(p)
        End If
    End Sub

    Public Sub Clear(bmp As Bitmap, name As String)
        Add(bmp, name)
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

    Public Function GetName() As String
        Dim i As Integer = 0
        Dim exist As Boolean = Me.Positions.Where(Function(a)
                                                      Return a.Name = i.ToString()
                                                  End Function).Count > 0

        While exist
            i += 1
            exist = Me.Positions.Where(Function(a)
                                           Return a.Name = i.ToString()
                                       End Function).Count > 0
        End While

        Return i.ToString()
    End Function
End Class
