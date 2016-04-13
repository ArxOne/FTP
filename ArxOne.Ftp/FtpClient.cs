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
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;
    using Platform;

    /// <summary>
    /// FTP client core. 
    /// Exposes only basic mechanisms
    /// Extended mechanisms and command support are in FtpClient class
    /// </summary>
    public class FtpClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the root URI.
        /// </summary>
        /// <value>The URI.</value>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FtpClient"/> is in passive mode.
        /// </summary>
        /// <value><c>true</c> if passive; otherwise, <c>false</c>.</value>
        public bool Passive { get; private set; }

        /// <summary>
        /// Gets the active transfer host (set by configuration).
        /// </summary>
        /// <value>
        /// The active transfer host.
        /// </value>
        public IPAddress ActiveTransferHost { get; private set; }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        /// <value>The connect timeout.</value>
        public TimeSpan ConnectTimeout { get; private set; }

        /// <summary>
        /// Gets or sets the read write timeout.
        /// </summary>
        /// <value>The read write timeout.</value>
        public TimeSpan ReadWriteTimeout { get; private set; }

        /// <summary>
        /// Gets the session timeout.
        /// </summary>
        public TimeSpan SessionTimeout { get; private set; }

        /// <summary>
        /// Gets or sets the proxy.
        /// </summary>
        /// <value>The proxy.</value>
        public Func<EndPoint, Socket> ProxyConnect { get; private set; }

        /// <summary>
        /// Gets or sets the credential.
        /// </summary>
        /// <value>The credential.</value>
        public NetworkCredential Credential { get; private set; }

        /// <summary>
        /// Gets or sets the anonymous password.
        /// </summary>
        /// <value>The anonymous password.</value>
        public string AnonymousPassword { get; private set; }

        private FtpServerFeatures _serverFeatures;

        /// <summary>
        /// Gets the server features.
        /// </summary>
        /// <value>
        /// The server features.
        /// </value>
        public FtpServerFeatures ServerFeatures => GetServerFeatures(null);

        /// <summary>
        /// Gets or sets the default encoding.
        /// Leave this null to let the automatic detection do the job
        /// (Unfortunately the current debian stable pure-ftpd version doesn't support protocol change)
        /// </summary>
        /// <value>The default encoding.</value>
        public Encoding DefaultEncoding { get; private set; }

        private static FtpClientParameters _defaultParameters;
        /// <summary>
        /// Gets the default parameters.
        /// </summary>
        /// <value>The default parameters.</value>
        public static FtpClientParameters DefaultParameters
        {
            get
            {
                if (_defaultParameters == null)
                    _defaultParameters = new FtpClientParameters();
                return _defaultParameters;
            }
        }

        /// <summary>
        /// Gets a value indicating whether data channel protection is active.
        /// </summary>
        /// <value>
        /// <c>true</c> if [_data channel protection]; otherwise, <c>false</c>.
        /// </value>
        public FtpProtection ChannelProtection { get; private set; }

        /// <summary>
        /// Gets the SSL protocols.
        /// </summary>
        /// <value>
        /// The SSL protocols.
        /// </value>
        public SslProtocols SslProtocols { get; private set; }

        private string _system;

        /// <summary>
        /// Gets the system.
        /// </summary>
        /// <value>The system.</value>
        public string System => GetSystem(null);

        /// <summary>
        /// Gets the system.
        /// </summary>
        /// <param name="session">The FTP session.</param>
        /// <returns></returns>
        public string GetSystem(FtpSession session)
        {
            if (_system == null)
            {
                var systemReply = Process(s => s.Expect(s.SendCommand("SYST"), 215), session);
                _system = systemReply.Lines[0];
            }
            return _system;
        }

        private FtpServerType? _serverType;
        /// <summary>
        /// Gets the type of the server.
        /// </summary>
        /// <value>
        /// The type of the server.
        /// </value>
        public FtpServerType ServerType => GetServerType(null);

        /// <summary>
        /// Gets the type of the server.
        /// </summary>
        /// <param name="session">The FTP session.</param>
        /// <returns></returns>
        private FtpServerType GetServerType(FtpSession session)
        {
            if (!_serverType.HasValue)
            {
                var system = GetSystem(session);
                if (system.StartsWith("unix", StringComparison.InvariantCultureIgnoreCase))
                    _serverType = FtpServerType.Unix;
                else if (system.StartsWith("windows", StringComparison.InvariantCultureIgnoreCase))
                    _serverType = FtpServerType.Windows;
                else
                    _serverType = FtpServerType.Unknown;
            }
            return _serverType.Value;
        }

        private FtpPlatform _platform;

        /// <summary>
        /// Gets the FTP platform.
        /// </summary>
        /// <value>
        /// The FTP platform.
        /// </value>
        public FtpPlatform Platform => GetPlatform(null);

        /// <summary>
        /// Gets the platform.
        /// </summary>
        /// <param name="session">The FTP session.</param>
        /// <returns></returns>
        public FtpPlatform GetPlatform(FtpSession session)
        {
            if (_platform == null)
                _platform = GetFtpPlatform(GetServerType(session), GetSystem(session));
            return _platform;
        }

        /// <summary>
        /// Gets the host address.
        /// </summary>
        /// <value>
        /// The host address.
        /// </value>
        internal IPAddress HostAddress => ActiveTransferHost ?? ActualActiveTransferHost;

        /// <summary>
        /// Gets or sets the actual active transfer host.
        /// </summary>
        /// <value>
        /// The actual active transfer host.
        /// </value>
        internal IPAddress ActualActiveTransferHost { get; set; }

        /// <summary>
        /// Occurs when [check certificate].
        /// </summary>
        public event EventHandler<CheckCertificateEventArgs> CheckCertificate;

        /// <summary>
        /// Occurs when [session initialized].
        /// </summary>
        public event EventHandler ConnectionInitialized;

        /// <summary>
        /// Occurs when [sending request].
        /// </summary>
        public event EventHandler<ProtocolMessageEventArgs> Request;

        /// <summary>
        /// Occurs when [received reply].
        /// </summary>
        public event EventHandler<ProtocolMessageEventArgs> Reply;

        /// <summary>
        /// Occurs when [IO error].
        /// </summary>
        public event EventHandler<ProtocolMessageEventArgs> IOError;

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The _port.</value>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <value>
        /// The _protocol.
        /// </value>
        public FtpProtocol Protocol { get; private set; }

        private class DatedFtpConnection
        {
            public DateTime Date;
            public FtpConnection Connection;
        }

        private readonly Queue<DatedFtpConnection> _availableConnections = new Queue<DatedFtpConnection>();
        private readonly object _connectionsLock = new object();
        private readonly Thread _connectionsThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClient"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        public FtpClient(FtpProtocol protocol, string host, int port, NetworkCredential credential, FtpClientParameters parameters = null)
        {
            Credential = credential;
            Protocol = protocol;
            Port = port;
            var uriBuilder = new UriBuilder { Scheme = GetScheme(protocol), Host = host, Port = port };
            Uri = uriBuilder.Uri;
            InitializeParameters(parameters);
            _connectionsThread = new Thread(ConnectionThread) { IsBackground = true, Name = "FtpClient.SessionThread" };
            _connectionsThread.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClient"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        public FtpClient(Uri uri, NetworkCredential credential, FtpClientParameters parameters = null)
            : this(GetProtocol(uri), uri.Host, GetPort(uri), credential, parameters)
        {
        }

        /// <summary>
        /// Initializes the parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        private void InitializeParameters(FtpClientParameters parameters)
        {
            if (parameters == null)
                parameters = DefaultParameters;
            if (!parameters.Passive && parameters.ProxyConnect != null)
                throw new InvalidOperationException("Active transfer mode only works without proxy server");
            Passive = parameters.Passive;
            ActiveTransferHost = parameters.ActiveTransferHost;
            ConnectTimeout = parameters.ConnectTimeout;
            ReadWriteTimeout = parameters.ReadWriteTimeout;
            SessionTimeout = parameters.SessionTimeout;
            AnonymousPassword = parameters.AnonymousPassword;
            DefaultEncoding = parameters.DefaultEncoding;
            ProxyConnect = parameters.ProxyConnect;
            ChannelProtection = parameters.ChannelProtection ?? GetDefaultDataChannelProtection(Uri);
            SslProtocols = parameters.SslProtocols ?? SslProtocols.Ssl3 | SslProtocols.Tls;
        }

        /// <summary>
        /// Gets the FTP platform.
        /// </summary>
        /// <param name="serverType">Type of the server.</param>
        /// <param name="system">The full platform.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">serverType;null</exception>
        private static FtpPlatform GetFtpPlatform(FtpServerType serverType, string system)
        {
            switch (serverType)
            {
                case FtpServerType.Unknown:
                    return new FtpPlatform();
                case FtpServerType.Unix:
                    if (system.IndexOf("FileZilla", StringComparison.InvariantCultureIgnoreCase) >= 0)
                        return new WindowsFileZillaFtpPlatform();
                    return new UnixFtpPlatform();
                case FtpServerType.Windows:
                    return new WindowsFtpPlatform();
                default:
                    throw new ArgumentOutOfRangeException(nameof(serverType), serverType, null);
            }
        }

        /// <summary>
        /// Gets the default data channel protection.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private static FtpProtection GetDefaultDataChannelProtection(Uri uri)
        {
            switch (GetProtocol(uri))
            {
                case FtpProtocol.Ftp:
                    return FtpProtection.Ftp;
                case FtpProtocol.FtpS:
                    return FtpProtection.FtpS;
                case FtpProtocol.FtpES:
                    return FtpProtection.FtpES;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connectionsThread.Interrupt();
            lock (_connectionsLock)
            {
                foreach (var availableSession in _availableConnections)
                    availableSession.Connection.Dispose();
                _availableConnections.Clear();
            }
        }

        /// <summary>
        /// Called when [sending request].
        /// </summary>
        /// <param name="e">The <see cref="ArxOne.Ftp.ProtocolMessageEventArgs"/> instance containing the event data.</param>
        internal void OnRequest(ProtocolMessageEventArgs e)
        {
            var onSendingRequest = Request;
            if (onSendingRequest != null)
                onSendingRequest(this, e);
        }

        /// <summary>
        /// Called when [received reply].
        /// </summary>
        /// <param name="e">The <see cref="ArxOne.Ftp.ProtocolMessageEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        internal void OnReply(ProtocolMessageEventArgs e)
        {
            var onReceivedReply = Reply;
            if (onReceivedReply != null)
                onReceivedReply(this, e);
        }

        /// <summary>
        /// Raises the IOError event.
        /// </summary>
        /// <param name="e">The <see cref="ArxOne.Ftp.ProtocolMessageEventArgs"/> instance containing the event data.</param>
        internal void OnIOError(ProtocolMessageEventArgs e)
        {
            var onIOError = IOError;
            if (onIOError != null)
                onIOError(this, e);
        }

        /// <summary>
        /// Gets the scheme.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <returns></returns>
        private static string GetScheme(FtpProtocol protocol)
        {
            switch (protocol)
            {
                case FtpProtocol.Ftp:
                    return Uri.UriSchemeFtp;
                case FtpProtocol.FtpS:
                    return "ftps";
                case FtpProtocol.FtpES:
                    return "ftpes";
                default:
                    throw new ArgumentOutOfRangeException(nameof(protocol));
            }
        }

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        private FtpServerFeatures GetServerFeatures(FtpSession session)
        {
            if (_serverFeatures == null)
            {
                if (session != null)
                    _serverFeatures = LoadServerFeatures(session);
                else
                    using (var newSession = Session())
                        _serverFeatures = LoadServerFeatures(newSession);
            }
            return _serverFeatures;
        }

        /// <summary>
        /// Loads the features.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        private static FtpServerFeatures LoadServerFeatures(FtpSession session)
        {
            var featuresReply = session.SendCommand("FEAT");
            if (featuresReply.Code == 211)
            {
                var featuresQuery = from line in featuresReply.Lines.Skip(1).Take(featuresReply.Lines.Length - 2)
                                    select line.Trim();
                return new FtpServerFeatures(featuresQuery);
            }
            return new FtpServerFeatures(new string[0]);
        }

        /// <summary>
        /// Determines whether the specified feature has a requested feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="session">The session.</param>
        /// <returns>
        ///   <c>true</c> if the specified feature has feature; otherwise, <c>false</c>.
        /// </returns>
        internal bool HasServerFeature(string feature, FtpSession session)
        {
            return GetServerFeatures(session).HasFeature(feature);
        }

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private static FtpProtocol GetProtocol(Uri uri)
        {
            if (string.Equals(uri.Scheme, Uri.UriSchemeFtp, StringComparison.InvariantCultureIgnoreCase))
                return FtpProtocol.Ftp;
            if (string.Equals(uri.Scheme, "ftps", StringComparison.InvariantCultureIgnoreCase))
                return FtpProtocol.FtpS;
            if (string.Equals(uri.Scheme, "ftpes", StringComparison.InvariantCultureIgnoreCase))
                return FtpProtocol.FtpES;
            throw new ArgumentException("Unhandled scheme " + uri.Scheme);
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private static int GetPort(Uri uri)
        {
            if (uri.Port > 0)
                return uri.Port;
            switch (GetProtocol(uri))
            {
                case FtpProtocol.Ftp:
                case FtpProtocol.FtpES:
                    return 21;
                case FtpProtocol.FtpS:
                    return 990;
                default:
                    throw new ArgumentException("Unhandled protocol");
            }
        }

        /// <summary>
        /// Raises the <see cref="ConnectionInitialized"/> event.
        /// </summary>
        public void OnConnectionInitialized()
        {
            var connectionInitialized = ConnectionInitialized;
            if (connectionInitialized != null)
                connectionInitialized(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="CheckCertificate"/> event.
        /// </summary>
        /// <param name="eventArgs">The <see cref="ArxOne.Ftp.CheckCertificateEventArgs"/> instance containing the event data.</param>
        public void OnCheckCertificate(CheckCertificateEventArgs eventArgs)
        {
            var checkCertificate = CheckCertificate;
            if (checkCertificate != null)
                checkCertificate(this, eventArgs);
        }

        /// <summary>
        /// Creates the session.
        /// </summary>
        /// <returns></returns>
        private FtpConnection CreateConnection()
        {
            return new FtpConnection(this);
        }

        /// <summary>
        /// Pops the available session.
        /// </summary>
        /// <returns></returns>
        private FtpConnection PopAvailableConnection()
        {
            if (_availableConnections.Count == 0)
                return null;
            return _availableConnections.Dequeue().Connection;
        }

        /// <summary>
        /// Finds the or create session.
        /// </summary>
        /// <returns></returns>
        private FtpConnection FindOrCreateConnection()
        {
            lock (_connectionsLock)
            {
                var session = PopAvailableConnection() ?? CreateConnection();
                return session;
            }
        }

        /// <summary>
        /// Uses a session.
        /// </summary>
        /// <returns></returns>
        public FtpSession Session()
        {
            return new FtpSession(FindOrCreateConnection());
        }

        /// <summary>
        /// Releases the session.
        /// </summary>
        /// <param name="connection">The session.</param>
        internal void ReleaseConnection(FtpConnection connection)
        {
            lock (_connectionsLock)
            {
                _availableConnections.Enqueue(new DatedFtpConnection { Date = DateTime.UtcNow, Connection = connection });
            }
        }

        /// <summary>
        /// Sessions thread, to cleanup.
        /// </summary>
        private void ConnectionThread()
        {
            try
            {
                for (;;)
                {
                    Thread.Sleep(SessionTimeout);
                    CleanupConnections();
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        /// <summary>
        /// Cleanups the (old) unused sessions.
        /// </summary>
        private void CleanupConnections()
        {
            lock (_connectionsLock)
            {
                var now = DateTime.UtcNow;
                var span = SessionTimeout;
                var availableSessions = new List<DatedFtpConnection>(_availableConnections);
                // since the _availableSessions is a queue
                // it is cleared and rebuild
                _availableConnections.Clear();
                foreach (var availableSession in availableSessions)
                {
                    // still young, keep it
                    if (now - availableSession.Date < span)
                        _availableConnections.Enqueue(availableSession);
                    else
                        availableSession.Connection.Dispose();
                }
            }
        }

        /// <summary>
        /// Sends the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public FtpReply SendSingleCommand(string command, params string[] parameters)
        {
            return Process(session => session.SendCommand(command, parameters));
        }

        /// <summary>
        /// Processes the specified action with a session.
        /// </summary>
        /// <typeparam name="TResult">The type of the ret.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="session">An existing session or null to create a new one.</param>
        /// <returns></returns>
        public TResult Process<TResult>(Func<FtpSession, TResult> action, FtpSession session = null)
        {
            if (session != null)
                return action(session);
            using (var newSession = Session())
                return action(newSession);
        }

        /// <summary>
        /// Processes the specified action with a session.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="session">An existing session or null to create a new one.</param>
        public void Process(Action<FtpSession> action, FtpSession session = null)
        {
            if (session != null)
                action(session);
            else
            {
                using (var newSession = Session())
                    action(newSession);
            }
        }
    }
}
