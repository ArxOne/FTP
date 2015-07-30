#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// Represents a single session
    /// </summary>
    public class FtpSessionConnection : IDisposable
    {
        private static int _currentID;

        internal readonly int ID = ++_currentID;

        /// <summary>
        /// Gets or sets the FTP client.
        /// </summary>
        /// <value>
        /// The _FTP client.
        /// </value>
        public FtpClient Client { get; private set; }

        /// <summary>
        /// Gets the protocol.
        /// </summary>
        public FtpProtocol Protocol { get; private set; }

        /// <summary>
        /// Text encoding mode
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the transfer mode.
        /// </summary>
        /// <value>
        /// The transfer mode.
        /// </value>
        internal FtpTransferMode? TransferMode { get; set; }

        /// <summary>
        /// Gets the transport stream.
        /// </summary>
        /// <value>The transport stream.</value>
        public Stream ProtocolStream { get; set; }

        /// <summary>
        /// Gets the session parameters.
        /// </summary>
        public FtpSessionState State { get; set; }

        /// <summary>
        /// Gets the host address.
        /// </summary>
        /// <value>
        /// The host address.
        /// </value>
        internal IPAddress HostAddress
        {
            get
            {
                return Client.ActiveTransferHost ?? ActiveTransferHost;
            }
        }

        // the session can be held by two different elements: FtpSessionHandle and specific Stream
        // the session handle can be implicit (using the Sequence() method) and needs to be released
        private readonly object _referenceCountLock = new object();
        private int _referenceCount;

        internal IPAddress ActiveTransferHost { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpSessionConnection"/> class.
        /// </summary>
        /// <param name="client">The FTP client.</param>
        /// <param name="protocol">The protocol.</param>
        public FtpSessionConnection(FtpClient client, FtpProtocol protocol)
        {
            Client = client;
            Protocol = protocol;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// Disposes the transport.
        /// </summary>
        internal void Disconnect()
        {
            if (ProtocolStream != null)
            {
                try
                {
                    ProtocolStream.Dispose();
                }
                catch (SocketException)
                {
                }
                catch (IOException)
                {
                }
                ProtocolStream = null;
            }
        }

        /// <summary>
        /// Adds a reference.
        /// </summary>
        internal void AddReference()
        {
            //if (_protocol == FtpProtocol.FtpES && BytesProcessed > 64 << 10)
            //    Disconnect();
            lock (_referenceCountLock)
                ++_referenceCount;
        }

        /// <summary>
        /// Releases this instance.
        /// </summary>
        internal void Release()
        {
            lock (_referenceCountLock)
            {
                if (--_referenceCount == 0)
                    Client.ReleaseSession(this);
            }
        }
    }
}
