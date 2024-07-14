Option Strict On
Imports Microsoft.VisualBasic.Devices

Public Class AvailMem
    Public Shared Property assumeFreeable As ULong = 0L '1024L * 1024 * 512
    Public Shared Property freeAddition As ULong = Convert.ToUInt64(1024 * 1024 * 100)

    Public Shared Property NoMemCheck As Boolean = False

    Public Shared Function checkAvailRam(picSize As Long) As Boolean
        If NoMemCheck Then
            Return True
        End If

        Dim c As New ComputerInfo()
        Dim t As Boolean = c.AvailablePhysicalMemory > (Convert.ToUInt64(picSize) + freeAddition - Math.Min(assumeFreeable, Convert.ToUInt64(picSize) + freeAddition))

        Return t
    End Function

    Public Shared Sub Reset()
        assumeFreeable = 0
    End Sub
End Class