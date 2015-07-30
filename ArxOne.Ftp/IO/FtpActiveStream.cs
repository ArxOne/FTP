#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp.IO
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Exceptions;

    /// <summary>
    /// FTP active transfer stream
    /// </summary>
    internal class FtpActiveStream : FtpPassiveStream
    {
        private readonly TimeSpan _connectTimeout;
        private Socket _socket;
        private readonly EventWaitHandle _socketSet = new ManualResetEvent(false);
        private IOException _exception;

        protected override Stream InnerStream
        {
            get
            {
                EnsureConnection();
                return base.InnerStream;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpActiveStream"/> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="session">The session.</param>
        public FtpActiveStream(Socket socket, TimeSpan connectTimeout, FtpSessionHandle session)
            : base(session)
        {
            _connectTimeout = connectTimeout;
            socket.BeginAccept(OnSocketAccept, socket);
        }

        /// <summary>
        /// Ensures there is a valid connection.
        /// </summary>
        /// <exception cref="FtpTransportException">Active stream did not get connection</exception>
        private void EnsureConnection()
        {
            if (_socket != null)
                return;

            if (!_socketSet.WaitOne(_connectTimeout))
                throw new FtpTransportException("Active stream did not get connection");

            if (_exception != null)
                throw _exception;
        }

        private void OnSocketAccept(IAsyncResult ar)
        {
            var socket = (Socket)ar.AsyncState;
            _socket = socket.EndAccept(ar);
            try
            {
                SetSocket(_socket);
            }
            catch (IOException e)
            {
                _exception = e;
            }
            _socketSet.Set();
        }
    }
}