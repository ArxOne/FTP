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
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using IO;

    /// <summary>
    /// FTP client core. 
    /// Exposes only basic mechanisms
    /// Extended mechanisms and command support are in FtpClient class
    /// </summary>
    public class FtpClientCore : IDisposable
    {
        /// <summary>
        /// Gets or sets the root URI.
        /// </summary>
        /// <value>The URI.</value>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FtpClientCore"/> is in passive mode.
        /// </summary>
        /// <value><c>true</c> if passive; otherwise, <c>false</c>.</value>
        public bool Passive { get; private set; }
      
        /// <summary>
        /// Gets the active transfer host.
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
        /// Gets the features.
        /// </summary>
        /// <value>The features.</value>
        public IList<string> Features { get; internal set; }

        /// <summary>
        /// Gets or sets the anonymous password.
        /// </summary>
        /// <value>The anonymous password.</value>
        public string AnonymousPassword { get; private set; }

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
        /// Occurs when [check certificate].
        /// </summary>
        public event EventHandler<CheckCertificateEventArgs> CheckCertificate;

        /// <summary>
        /// Occurs when [session initialized].
        /// </summary>
        public event EventHandler SessionInitialized;

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

        private readonly string _host;
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The _port.</value>
        public int Port { get; private set; }
        private readonly FtpProtocol _protocol;

        private class DatedFtpSession
        {
            public DateTime Date;
            public FtpSession Session;
        }

        private readonly Queue<DatedFtpSession> _availableSessions = new Queue<DatedFtpSession>();
        private readonly object _sessionsLock = new object();
        private readonly Thread _sessionThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientCore"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        public FtpClientCore(FtpProtocol protocol, string host, int port, NetworkCredential credential, FtpClientParameters parameters = null)
        {
            Credential = credential;
            _protocol = protocol;
            _host = host;
            Port = port;
            var uriBuilder = new UriBuilder { Scheme = GetScheme(protocol), Host = host, Port = port };
            Uri = uriBuilder.Uri;
            InitializeParameters(parameters);
            _sessionThread = new Thread(SessionThread) { IsBackground = true, Name = "FtpClient.SessionThread" };
            _sessionThread.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientCore"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        public FtpClientCore(Uri uri, NetworkCredential credential, FtpClientParameters parameters = null)
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
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _sessionThread.Interrupt();
            lock (_sessionsLock)
            {
                foreach (var availableSession in _availableSessions)
                    availableSession.Session.Dispose();
                _availableSessions.Clear();
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
                    throw new ArgumentOutOfRangeException("protocol");
            }
        }

        /// <summary>
        /// Determines whether the specified feature has feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns>
        /// 	<c>true</c> if the specified feature has feature; otherwise, <c>false</c>.
        /// </returns>
        public bool HasFeature(string feature)
        {
            return Features.Any(f => string.Equals(f, feature, StringComparison.InvariantCultureIgnoreCase));
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
        /// Raises the <see cref="SessionInitialized"/> event.
        /// </summary>
        public void OnSessionInitialized()
        {
            var sessionInitialized = SessionInitialized;
            if (sessionInitialized != null)
                sessionInitialized(this, EventArgs.Empty);
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
        private FtpSession CreateSession()
        {
            return new FtpSession(this, _protocol, _host, Port);
        }

        /// <summary>
        /// Pops the available session.
        /// </summary>
        /// <returns></returns>
        private FtpSession PopAvailableSession()
        {
            if (_availableSessions.Count == 0)
                return null;
            return _availableSessions.Dequeue().Session;
        }

        /// <summary>
        /// Finds the or create session.
        /// </summary>
        /// <returns></returns>
        private FtpSession FindOrCreateSession()
        {
            lock (_sessionsLock)
            {
                var session = PopAvailableSession() ?? CreateSession();
                return session;
            }
        }

        /// <summary>
        /// Uses a session.
        /// </summary>
        /// <returns></returns>
        public FtpSessionHandle Session()
        {
            return new FtpSessionHandle(FindOrCreateSession());
        }

        /// <summary>
        /// Releases the session.
        /// </summary>
        /// <param name="session">The session.</param>
        internal void ReleaseSession(FtpSession session)
        {
            lock (_sessionsLock)
            {
                _availableSessions.Enqueue(new DatedFtpSession { Date = DateTime.UtcNow, Session = session });
            }
        }

        /// <summary>
        /// Sessions thread, to cleanup.
        /// </summary>
        private void SessionThread()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(SessionTimeout);
                    CleanupSessions();
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        /// <summary>
        /// Cleanups the (old) unused sessions.
        /// </summary>
        private void CleanupSessions()
        {
            lock (_sessionsLock)
            {
                var now = DateTime.UtcNow;
                var span = SessionTimeout;
                var availableSessions = new List<DatedFtpSession>(_availableSessions);
                // since the _availableSessions is a queue
                // it is cleared and rebuild
                _availableSessions.Clear();
                foreach (var availableSession in availableSessions)
                {
                    // still young, keep it
                    if (now - availableSession.Date < span)
                        _availableSessions.Enqueue(availableSession);
                    else
                        availableSession.Session.Dispose();
                }
            }
        }

        /// <summary>
        /// Sends the command.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public FtpReply SendCommand(FtpSessionHandle sequence, string command, params string[] parameters)
        {
            return sequence.Session.SendCommand(command, parameters);
        }

        /// <summary>
        /// Expects the specified reply.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <param name="codes">The codes.</param>
        /// <returns></returns>
        public FtpReply Expect(FtpReply reply, params int[] codes)
        {
            FtpSession.Expect(reply, codes);
            return reply;
        }

        /// <summary>
        /// Checks the protection.
        /// </summary>
        /// <param name="sessionHandle">The session handle.</param>
        /// <param name="protection">if set to <c>true</c> [protection].</param>
        protected static void CheckProtection(FtpSessionHandle sessionHandle, bool protection)
        {
            if (sessionHandle.Session.Protocol == FtpProtocol.FtpES)
                sessionHandle.Session.State["PROT"] = protection ? "P" : "C";
        }

        /// <summary>
        /// Opens a data stream.
        /// </summary>
        /// <param name="handle">The sequence.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        internal Stream OpenDataStream(FtpSessionHandle handle, FtpTransferMode mode)
        {
            CheckProtection(handle, false);
            return handle.Session.OpenDataStream(Passive, ConnectTimeout, ReadWriteTimeout, mode);
        }

        /// <summary>
        /// Gets the protocol encoding.
        /// </summary>
        /// <param name="ftpStream">The FTP stream.</param>
        /// <returns></returns>
        public Encoding GetProtocolEncoding(Stream ftpStream)
        {
            return ((IFtpStream)ftpStream).ProtocolEncoding;
        }

        /// <summary>
        /// Aborts the specified FTP stream.
        /// </summary>
        /// <param name="ftpStream">The FTP stream.</param>
        internal static void Abort(Stream ftpStream)
        {
            var passiveStream = ftpStream as FtpStream;
            if (passiveStream != null)
            {
                passiveStream.Abort();
                return;
            }
            throw new NotSupportedException();
        }
    }
}
