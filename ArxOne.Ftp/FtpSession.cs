#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;
    using Exceptions;
    using IO;

    /// <summary>
    /// Session lock: holds a session when used
    /// </summary>
    public class FtpSession : IDisposable
    {
        /// <summary>
        /// Gets or sets the FTP session.
        /// </summary>
        /// <value>The FTP session.</value>
        public FtpSessionConnection SessionConnection { get; private set; }

        /// <summary>
        /// Gets the transport stream.
        /// </summary>
        /// <value>The transport stream.</value>
        public Stream ProtocolStream
        {
            get
            {
                if (SessionConnection.ProtocolStream == null)
                {
                    //if (_transportSocket != null)
                    //    Trace.WriteLine("ftp #" + _id + ": disconnecting");
                    SessionConnection.Disconnect();
                    Connect(SessionConnection.Client.ConnectTimeout, SessionConnection.Client.ReadWriteTimeout);
                }
                return SessionConnection.ProtocolStream;
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public FtpSessionState State
        {
            get
            {
                if (SessionConnection.State == null)
                    SessionConnection.State = new FtpSessionState(this);
                return SessionConnection.State;
            }
        }

        /// <summary>
        /// FTP End of line
        /// </summary>
        private const string Eol = "\r\n";

        private static byte[] _eolB;

        private static byte[] EolB
        {
            get
            {
                if (_eolB == null)
                    _eolB = Encoding.UTF8.GetBytes(Eol);
                return _eolB;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpSession"/> class.
        /// </summary>
        /// <param name="ftpSessionConnection">The FTP session.</param>
        internal FtpSession(FtpSessionConnection ftpSessionConnection)
        {
            SessionConnection = ftpSessionConnection;
            SessionConnection.AddReference();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            SessionConnection.Release();
        }


        /// <summary>
        /// Executes the given function in a context where exceptions will be translated.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="func">The func.</param>
        /// <param name="commandDescription">The command description.</param>
        /// <param name="requestCommand">The request command.</param>
        /// <param name="requestParameters">The request parameters.</param>
        /// <returns></returns>
        internal TResult Process<TResult>(Func<TResult> func, string commandDescription, string requestCommand = null,
            string[] requestParameters = null)
        {
            try
            {
                return func();
            }
            catch (FtpProtocolException)
            {
                throw;
            }
            catch (SocketException se)
            {
                SessionConnection.Client.OnIOError(new ProtocolMessageEventArgs(SessionConnection.ID, requestCommand, requestParameters));
                SessionConnection.Disconnect();
                throw new FtpTransportException("Socket Exception while " + commandDescription, se);
            }
            catch (IOException ioe)
            {
                SessionConnection.Client.OnIOError(new ProtocolMessageEventArgs(SessionConnection.ID, requestCommand, requestParameters));
                SessionConnection.Disconnect();
                throw new FtpTransportException("IO Exception while " + commandDescription, ioe);
            }
        }

        /// <summary>
        /// Connects this instance.
        /// </summary>
        /// <param name="connectTimeout">The timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        private void Connect(TimeSpan connectTimeout, TimeSpan readWriteTimeout)
        {
            Process(() => ProcessConnect(connectTimeout, readWriteTimeout), "connecting to FTP server", "(Connect)");
        }

        /// <summary>
        /// Processes the connect.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <returns></returns>
        private bool ProcessConnect(TimeSpan connectTimeout, TimeSpan readWriteTimeout)
        {
            InitializeTransport(connectTimeout, readWriteTimeout, SessionConnection.Protocol == FtpProtocol.FtpS);
            InitializeProtocol();
            InitializeSession();
            return true;
        }

        /// <summary>
        /// Initializes the transport.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <param name="ssl">if set to <c>true</c> [SSL].</param>
        /// <exception cref="FtpTransportException">Socket not connected to  + _host + , message= + message</exception>
        private void InitializeTransport(TimeSpan connectTimeout, TimeSpan readWriteTimeout, bool ssl)
        {
            // first of all, set defaults...
            // ... encoding
            SessionConnection.Encoding = SessionConnection.Client.DefaultEncoding ?? Encoding.ASCII;
            // ... transfer mode ('type')
            SessionConnection.TransferMode = null;

            string message;
            var protocolStream = ConnectTransport(connectTimeout, readWriteTimeout, out message);
            if (protocolStream == null)
                throw new FtpTransportException("Socket not connected to " + SessionConnection.Client.Uri.Host + ", message=" + message);
            SessionConnection.ProtocolStream = protocolStream;

            if (ssl)
                EnterSslProtocol();

            // on connection a 220 message is expected
            // (the dude says hello)
            Expect(ReadReply(), 220);

            InitializeTransportEncoding();
        }

        /// <summary>
        /// Connects the transport.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private Stream ConnectTransport(TimeSpan connectTimeout, TimeSpan readWriteTimeout, out string message)
        {
            message = null;
            try
            {
                // try to use a proxy
                if (SessionConnection.Client.ProxyConnect != null)
                {
                    var socket = SessionConnection.Client.ProxyConnect(new DnsEndPoint(SessionConnection.Client.Uri.Host, SessionConnection.Client.Port));
                    if (socket != null)
                        return new NetworkStream(socket);
                }
                return DirectConnectTransport(readWriteTimeout, connectTimeout, ref message);
            }
            // may be thrown by dns resolution
            catch (SocketException se)
            {
                message = se.ToString();
                return null;
            }
            // may be thrown by proxy connexion
            catch (IOException se)
            {
                message = se.ToString();
                return null;
            }
        }

        /// <summary>
        /// Direct transport connection.
        /// </summary>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private Stream DirectConnectTransport(TimeSpan readWriteTimeout, TimeSpan connectTimeout, ref string message)
        {
            Socket transportSocket;
            try
            {
                transportSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (SocketException)
            {
                transportSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            }
            transportSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            transportSocket.SendTimeout = transportSocket.ReceiveTimeout = (int)readWriteTimeout.TotalMilliseconds;
            transportSocket.Connect(SessionConnection.Client.Uri.Host, SessionConnection.Client.Port, connectTimeout);
            if (!transportSocket.Connected)
            {
                message = "Not connected";
                return null;
            }
            SessionConnection.ActiveTransferHost = ((IPEndPoint)transportSocket.LocalEndPoint).Address;
            return new NetworkStream(transportSocket, FileAccess.ReadWrite, true);
        }

        /// <summary>
        /// Upgrades the stream to SSL
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        private Stream UpgradeToSsl(Stream stream)
        {
            if (stream is SslStream)
                return stream;
            var sslStream = new SslStream(stream, false, CheckCertificateHandler);
            sslStream.AuthenticateAsClient(SessionConnection.Client.Uri.Host, null, SslProtocols.Ssl3 | SslProtocols.Tls, false);
            return sslStream;
        }

        /// <summary>
        /// Creates a data stream, optionnally secured.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <returns></returns>
        /// <exception cref="IOException">The <paramref name="socket" /> parameter is not connected.-or- The <see cref="P:System.Net.Sockets.Socket.SocketType" /> property of the <paramref name="socket" /> parameter is not <see cref="F:System.Net.Sockets.SocketType.Stream" />.-or- The <paramref name="socket" /> parameter is in a nonblocking state. </exception>
        internal Stream CreateDataStream(Socket socket)
        {
            Stream stream = new NetworkStream(socket, true);
            if (SessionConnection.Client.ChannelProtection.HasFlag(FtpProtection.DataChannel))
                stream = UpgradeToSsl(stream);
            return stream;
        }

        /// <summary>
        /// Initializes the protocol.
        /// To be strict, SSL/TLS is not on the protocol level, 
        /// </summary>
        private void InitializeProtocol()
        {
            // setting up the protocol socket depends on the used protocol
            switch (SessionConnection.Protocol)
            {
                // FTP is straightforward, it is clear data
                case FtpProtocol.Ftp:
                    LeaveSslProtocol();
                    break;
                // FTPS is also straightforward, it is in a simple SSL tunnel
                case FtpProtocol.FtpS:
                    EnterSslProtocol();
                    break;
                // FTPES informs first over a clear channel that it switches to 
                case FtpProtocol.FtpES:
                    LeaveSslProtocol();
                    Expect(SendCommand(SessionConnection.ProtocolStream, "AUTH", "TLS"), 234);
                    EnterSslProtocol();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Initializes the session, logs the user in
        /// </summary>
        private void InitializeSession()
        {
            var credential = SessionConnection.Client.Credential != null && !string.IsNullOrEmpty(SessionConnection.Client.Credential.UserName)
                ? SessionConnection.Client.Credential
                : new NetworkCredential("anonymous", SessionConnection.Client.AnonymousPassword);
            var userResult = Expect(SendCommand(ProtocolStream, "USER", credential.UserName), 331, 530);
            if (userResult.Code == 530)
                throw new FtpAuthenticationException("No anonymous user allowed", userResult.Code);
            var passResult = SendCommand(ProtocolStream, "PASS", credential.Password);
            if (passResult.Code != 230)
                throw new FtpAuthenticationException("Authentication failed for user " + credential.UserName, passResult.Code);

            SessionConnection.Client.OnSessionInitialized();
        }

        /// <summary>
        /// Initializes the transport encoding.
        /// </summary>
        private void InitializeTransportEncoding()
        {
            // try to switch to UTF8 if not already the case
            if (SessionConnection.Client.DefaultEncoding == null && SessionConnection.Client.HasServerFeature("UTF8", this))
            {
                Expect(SendCommand(ProtocolStream, "OPTS", "UTF8", "ON"), 200);
                SessionConnection.Encoding = Encoding.UTF8;
            }
        }

        /// <summary>
        /// Enters the SSL protocol.
        /// </summary>
        public void EnterSslProtocol()
        {
            SessionConnection.ProtocolStream = UpgradeToSsl(ProtocolStream);
        }

        /// <summary>
        /// Checks the certificate.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslpolicyerrors">The sslpolicyerrors.</param>
        /// <returns></returns>
        private bool CheckCertificateHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            var e = new CheckCertificateEventArgs(certificate, chain, sslpolicyerrors);
            SessionConnection.Client.OnCheckCertificate(e);
            return e.IsValid;
        }

        /// <summary>
        /// Leaves the SSL protocol.
        /// </summary>
        public bool LeaveSslProtocol()
        {
            // not exactly supported yet :)
            return false;
        }

        /// <summary>
        /// Expects the specified reply.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <param name="codes">The codes.</param>
        /// <returns></returns>
        public static FtpReply Expect(FtpReply reply, params int[] codes)
        {
            if (!codes.Any(code => code == reply.Code))
                FtpClient.ThrowException(reply);
            return reply;
        }

        /// <summary>
        /// Expects the specified code.
        /// </summary>
        /// <param name="codes">The codes.</param>
        public void Expect(params int[] codes)
        {
            var reply = ReadReply(ProtocolStream);
            Expect(reply, codes);
        }

        /// <summary>
        /// Sends the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public FtpReply SendCommand(string command, params string[] parameters)
        {
            return SendCommand(ProtocolStream, command, parameters);
        }

        private static readonly string[] CensoredParameters = { "****" };

        /// <summary>
        /// Sends the command.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public FtpReply SendCommand(Stream stream, string command, params string[] parameters)
        {
            return Process(() => ProcessSendCommand(stream, command, parameters), "sending FTP request",
                command, parameters);
        }

        /// <summary>
        /// Processes the send command.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        private FtpReply ProcessSendCommand(Stream stream, string command, string[] parameters)
        {
            SessionConnection.Client.OnRequest(new ProtocolMessageEventArgs(SessionConnection.ID, command, command == "PASS" ? CensoredParameters : parameters));
            var commandLine = GetCommandLine(command, parameters);
            WriteLine(stream, commandLine);
            var reply = ReadReply(stream);
            return reply;
        }

        /// <summary>
        /// Reads the reply.
        /// </summary>
        /// <returns></returns>
        public FtpReply ReadReply()
        {
            return ReadReply(ProtocolStream);
        }

        /// <summary>
        /// Reads the reply.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public FtpReply ReadReply(Stream stream)
        {
            return Process(() => ProcessReadReply(stream), "reading FTP reply", "(ReadReply)");
        }

        /// <summary>
        /// Processes the read reply.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        private FtpReply ProcessReadReply(Stream stream)
        {
            var reply = new FtpReply();
            {
                for (;;)
                {
                    var line = ReadLine(() => ReadByte(stream));
                    if (line == null)
                    {
                        SessionConnection.Disconnect();
                        throw new FtpTransportException(String.Format("Error while reading reply ({0})",
                            reply.Lines != null ? String.Join("//", reply.Lines) : "null"));
                    }
                    if (!reply.ParseLine(line))
                        break;
                }
            }
            SessionConnection.Client.OnReply(new ProtocolMessageEventArgs(SessionConnection.ID, null, null, reply));
            return reply;
        }

        private readonly byte[] _byteBuffer = new byte[1];
        private int ReadByte(Stream stream)
        {
            var bytesRead = stream.Read(_byteBuffer, 0, 1);
            if (bytesRead == 0)
                return -1;
            return _byteBuffer[0];
        }

        /// <summary>
        /// Gets the command line.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public static string GetCommandLine(string command, params string[] parameters)
        {
            var lineBuilder = new StringBuilder(command);
            foreach (var parameter in parameters)
            {
                lineBuilder.Append(' ');
                lineBuilder.Append(parameter);
            }
            return lineBuilder.ToString();
        }

        private readonly byte[] _readLineBuffer = new byte[10 << 10];

        /// <summary>
        /// Reads the line.
        /// </summary>
        /// <param name="byteReader">The byte reader.</param>
        /// <returns></returns>
        private string ReadLine(Func<int> byteReader)
        {
            var eolB = EolB;
            var index = 0;
            var buffer = _readLineBuffer;
            for (;;)
            {
                var b = byteReader();
                if (b == -1)
                    return null;

                buffer[index++] = (byte)b;
                if (EndsWith(buffer, index, eolB) || index >= buffer.Length)
                    return SessionConnection.Encoding.GetString(buffer, 0, index - eolB.Length);
            }
        }

        private static bool EndsWith(byte[] buffer, int bufferLength, byte[] end)
        {
            if (bufferLength < end.Length)
                return false;

            for (int index = 0; index < end.Length; index++)
            {
                if (buffer[bufferLength - end.Length + index] != end[index])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="line">The line.</param>
        public void WriteLine(Stream stream, string line)
        {
            Write(stream, line + Eol);
        }

        /// <summary>
        /// Writes the string.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="line">The line.</param>
        private void Write(Stream stream, string line)
        {
            var lineBytes = SessionConnection.Encoding.GetBytes(line);
            stream.Write(lineBytes, 0, lineBytes.Length);
        }

        /// <summary>
        /// Opens the data stream.
        /// </summary>
        /// <param name="passive">if set to <c>true</c> [passive].</param>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public FtpStream OpenDataStream(bool passive, TimeSpan connectTimeout, TimeSpan readWriteTimeout, FtpTransferMode mode)
        {
            SetTransferMode(mode);
            FtpStream stream;
            if (passive)
                stream = OpenPassiveDataStream(connectTimeout, readWriteTimeout);
            else
                stream = OpenActiveDataStream(connectTimeout, readWriteTimeout);
            return stream;
        }

        private static readonly Regex EpsvEx = new Regex(@".*?\(\|\|\|(?<port>\d*)\|\)", RegexOptions.Compiled);
        private static readonly Regex PasvEx = new Regex(@".*?\(\s*(?<ip1>\d*)\s*,\s*(?<ip2>\d*)\s*,\s*(?<ip3>\d*)\s*,\s*(?<ip4>\d*)\s*,\s*(?<portHi>\d*)\s*,\s*(?<portLo>\d*)\s*\)", RegexOptions.Compiled);

        /// <summary>
        /// Opens the passive data stream.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <returns></returns>
        private FtpStream OpenPassiveDataStream(TimeSpan connectTimeout, TimeSpan readWriteTimeout)
        {
            string host;
            int port;
            if (SessionConnection.Client.HasServerFeature("EPSV", this))
            {
                var reply = Expect(SendCommand("EPSV"), 229);
                var match = EpsvEx.Match(reply.Lines[0]);
                host = SessionConnection.Client.Uri.Host;
                port = Int32.Parse(match.Groups["port"].Value);
            }
            else
            {
                var reply = Expect(SendCommand("PASV"), 227);
                var match = PasvEx.Match(reply.Lines[0]);
                host = String.Format("{0}.{1}.{2}.{3}",
                    match.Groups["ip1"], match.Groups["ip2"],
                                     match.Groups["ip3"], match.Groups["ip4"]);
                port = Int32.Parse(match.Groups["portHi"].Value) * 256 + Int32.Parse(match.Groups["portLo"].Value);
            }

            if (SessionConnection.Client.ProxyConnect != null)
            {
                var socket = SessionConnection.Client.ProxyConnect(new DnsEndPoint(host, port));
                if (socket != null)
                    return new FtpPassiveStream(socket, this);
            }

            return OpenDirectPassiveDataStream(host, port, connectTimeout, readWriteTimeout);
        }

        /// <summary>
        /// Opens the direct passive data stream.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <returns></returns>
        private FtpStream OpenDirectPassiveDataStream(string host, int port, TimeSpan connectTimeout, TimeSpan readWriteTimeout)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = socket.ReceiveTimeout = (int)readWriteTimeout.TotalMilliseconds;
            socket.Connect(host, port, connectTimeout);
            if (!socket.Connected)
                throw new FtpTransportException("Socket error to " + host);

            return new FtpPassiveStream(socket, this);
        }

        /// <summary>
        /// Opens the active data stream.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="readWriteTimeout">The read write timeout.</param>
        /// <returns></returns>
        private FtpStream OpenActiveDataStream(TimeSpan connectTimeout, TimeSpan readWriteTimeout)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = socket.ReceiveTimeout = (int)readWriteTimeout.TotalMilliseconds;
            socket.Bind(new IPEndPoint(SessionConnection.HostAddress, 0));
            var port = ((IPEndPoint)socket.LocalEndPoint).Port;
            if (SessionConnection.Client.HasServerFeature("EPRT", this))
                Expect(SendCommand(String.Format("EPRT |{0}|{1}|{2}|", SessionConnection.HostAddress.AddressFamily == AddressFamily.InterNetwork ? 1 : 2, SessionConnection.HostAddress, port)), 200);
            else
            {
                var addressBytes = SessionConnection.HostAddress.GetAddressBytes();
                Expect(SendCommand(String.Format("PORT {0},{1},{2},{3},{4},{5}", addressBytes[0], addressBytes[1], addressBytes[2], addressBytes[3], port / 256, port % 256)), 200);
            }

            socket.Listen(1);
            return new FtpActiveStream(socket, connectTimeout, this);
        }

        private readonly IDictionary<string, string> _sessionState = new Dictionary<string, string>();

        /// <summary>
        /// Gets the session parameter.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns></returns>
        public string GetSessionParameter(string parameterName)
        {
            string parameterValue;
            lock (_sessionState)
                _sessionState.TryGetValue(parameterName, out parameterValue);
            return parameterValue;
        }

        /// <summary>
        /// Checks the session parameter has the existing value.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">The parameter value.</param>
        public void CheckSessionParameter(string parameterName, string parameterValue)
        {
            // First of all, check that feature exists
            if (!SessionConnection.Client.HasServerFeature(parameterName, this))
                return;
            // Then see if it is required
            lock (_sessionState)
            {
                string currentParameterValue;
                _sessionState.TryGetValue(parameterName, out currentParameterValue);
                if (currentParameterValue == parameterValue)
                    return;

                _sessionState[parameterName] = parameterValue;
            }
            Expect(SendCommand(parameterName, parameterValue), 200);
        }

        /// <summary>
        /// Sets the transfer mode.
        /// </summary>
        /// <param name="value">The value.</param>
        internal void SetTransferMode(FtpTransferMode value)
        {
            if (value != SessionConnection.TransferMode)
            {
                Expect(SendCommand("TYPE", ((char)value).ToString()), 200);
                SessionConnection.TransferMode = value;
            }
        }
    }
}
