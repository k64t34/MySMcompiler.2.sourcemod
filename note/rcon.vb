'http://msdn.microsoft.com/en-us/library/system.net.ipaddress.aspx
'Try
'                m_ip = Net.IPAddress.Parse(value)
'            Catch ex As Exception
'                Try
'                    Dim a_IP() As Net.IPAddress
'                    a_IP = Net.Dns.GetHostAddresses(value)
'                    m_ip = a_IP(0)
'                Catch ex_inner As Exception
'                    Throw New Exception("Invalid IP address or DNS name.", ex_inner)
'                End Try
'End Try
'http://forums.steampowered.com/forums/showthread.php?t=1722388            
Public Class RCON
    Const SERVERDATA_EXECCOMMAND As Integer = 2
    Const SERVERDATA_AUTH As Integer = 3
    Const SERVERDATA_AUTH_RESPONSE As Integer = 2
    Const SERVERDATA_RESPONSE_VALUE As Integer = 0

    Dim m_ip As Net.IPAddress
    Dim m_port As Integer
    Dim m_password As String
    Dim m_socket As Net.Sockets.Socket
    Dim m_id As Integer = 0
	'--------------------------------------------------------
    Sub New(ByVal ip As Net.IPAddress, ByVal port As Integer)
        SetServer(ip, port)
    End Sub
	'--------------------------------------------------------
    Private Sub SetServer(ByVal ip As Net.IPAddress, ByVal port As Integer)
        m_ip = ip
        m_port = port
        initSocket()
    End Sub
    '--------------------------------------------------------
    '->
    Private Sub TestConnect()
        m_socket = New Net.Sockets.Socket(Net.Sockets.AddressFamily.InterNetwork, Net.Sockets.SocketType.Stream, Net.Sockets.ProtocolType.Tcp)
        m_socket.Connect(m_ip, m_port)
    End Sub

	'--------------------------------------------------------
    'Initialise the connection socket
    Private Sub initSocket()
        m_socket = New Net.Sockets.Socket(Net.Sockets.AddressFamily.InterNetwork, Net.Sockets.SocketType.Stream, Net.Sockets.ProtocolType.Tcp)
        m_socket.Connect(m_ip, m_port)
        m_socket.SendTimeout = 4000
        m_socket.ReceiveTimeout = 4000
    End Sub
	'--------------------------------------------------------
    Sub SetPassword(ByVal password As String)
        m_password = password
    End Sub
	'--------------------------------------------------------
    'Reconnect to the server
    Sub refreshSocket()
        m_socket.Disconnect(True)
        initSocket()
        Auth()
    End Sub
	'--------------------------------------------------------
    'Authenticate RCON password
    Function Auth() As Boolean
        _Write(SERVERDATA_AUTH, m_password)
        'initSocket()
        Dim ret As Byte() = Nothing
        Do While ret Is Nothing
            Threading.Thread.Sleep(100)
            Try
                ret = _PacketRead()
            Catch ex As Exception
                Throw ex
            End Try
        Loop

        If ret(18) = Byte.Parse("255") Then
            Return False
        Else
            Return True
        End If

    End Function
	'--------------------------------------------------------
    Function SendCommand(ByVal command As String, Optional ByVal ignorenoreply As Boolean = False) As String
        _Write(SERVERDATA_EXECCOMMAND, command)
        If Not ignorenoreply Then
            Dim server_response As String = ""
            Do While server_response = ""
                Threading.Thread.Sleep(100)
                server_response = _Read(SERVERDATA_EXECCOMMAND)
            Loop
            Return server_response
        Else
            Return ""
        End If

    End Function
	'--------------------------------------------------------
    'Package and send command to server
    Private Sub _Write(ByVal cmd As Integer, ByVal s1 As String, Optional ByVal s2 As String = "")
        m_id += 1
        Dim sendData As Byte()
        Dim sendData_ID As Byte() = BitConverter.GetBytes(m_id)
        Dim sendData_CMD As Byte() = BitConverter.GetBytes(cmd)
        Dim sendData_s1 As Byte() = (New Text.ASCIIEncoding()).GetBytes(s1)
        Dim sendData_s2 As Byte() = (New Text.ASCIIEncoding()).GetBytes(s2)
        Dim packetlength As Integer = sendData_ID.Length + sendData_CMD.Length + sendData_s1.Length + 2
        ReDim sendData(packetlength - 1 + BitConverter.GetBytes(packetlength).Length)

        Dim J As Integer = 0
        Dim packetlength_bytes As Byte() = BitConverter.GetBytes(packetlength)
        For i As Integer = 0 To packetlength_bytes.Length - 1
            sendData(J) = packetlength_bytes(i)
            J = J + 1
        Next
        For i As Integer = 0 To sendData_ID.Length - 1
            sendData(J) = sendData_ID(i)
            J = J + 1
        Next
        For i As Integer = 0 To sendData_CMD.Length - 1
            sendData(J) = sendData_CMD(i)
            J = J + 1
        Next
        For i As Integer = 0 To sendData_s1.Length - 1
            sendData(J) = sendData_s1(i)
            J = J + 1
        Next
        sendData(J) = &H0
        J += 1

        m_socket.Send(sendData, sendData.Length, Net.Sockets.SocketFlags.None)

        Return
    End Sub
   	'--------------------------------------------------------
    'Read packets from the server
    Private Function _PacketRead(Optional ByVal ignorereturn As Boolean = False) As Byte()
        Dim buffer() As Byte
        'm_socket.Receive(buffer, 16, Net.Sockets.SocketFlags.Partial)
        Dim ip As Net.IPAddress = m_ip
        Dim RemoteEndPoint As New System.Net.IPEndPoint(m_ip, 0)
        m_socket.Poll(500, Net.Sockets.SelectMode.SelectRead)
        Dim available_bytes As Integer = m_socket.Available
        If Not available_bytes = 0 Then
            ReDim buffer(available_bytes)
            m_socket.ReceiveFrom(buffer, available_bytes, Net.Sockets.SocketFlags.None, RemoteEndPoint)
            Dim size As Integer = BitConverter.ToInt32(buffer, 0)

            Return buffer
        Else
            If Not ignorereturn Then Throw New Exception("You have been banned from RCON, or the server is not fully started.") Else Return Nothing
            Exit Function
        End If

    End Function

    'Read and unpack response from the server
    Private Function _Read(ByVal cmd As Integer, Optional ByVal ignorereturn As Boolean = False) As String
        Dim packets As Byte() = _PacketRead(ignorereturn)

        If ignorereturn And packets Is Nothing Then Return Nothing

        Dim returnstring As String = ""
        If cmd = SERVERDATA_EXECCOMMAND And packets.Length > 2 Then

            Dim d As System.Text.Decoder = System.Text.Encoding.ASCII.GetDecoder
            Dim chars(0) As Char
            Dim nextIndex As Integer = 0 'startindex

            Dim packetsize As Integer = BitConverter.ToInt32(packets, nextIndex)
            nextIndex += 4
            Dim requestID As Integer = BitConverter.ToInt32(packets, nextIndex)
            nextIndex += 4
            Dim serverdata As Integer = BitConverter.ToInt32(packets, nextIndex)
            nextIndex += 4

            ReDim chars(getNextIndex(packets, nextIndex) - nextIndex - 2)
            d.GetChars(packets, nextIndex, chars.Length, chars, 0)
            Dim response As String = ""
            For i = 0 To chars.Length - 2
                response &= chars(i)
            Next

            returnstring = response
        End If
        Return returnstring
    End Function

    'Get the starting index for the value after a string
    Private Function getNextIndex(ByVal data As Byte(), ByVal startingIndex As Integer) As Integer
        Dim i As Integer = startingIndex
        Dim x As Byte
        Dim y As Byte

        Do
            i = i + 1
            x = data(i)
            y = data(i + 1)
        Loop Until (x = Byte.Parse("0") And y = Byte.Parse("0"))

        getNextIndex = i + 1
    End Function

End Class