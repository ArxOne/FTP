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
    using Exceptions;

    /// <summary>
    /// FTP active transfer stream
    /// </summary>
    internal class FtpActiveStream : FtpStream
    {
        private readonly TimeSpan _connectTimeout;
        private Socket _socket;
        private readonly IAsyncResult _acceptResult;

        protected override Stream InnerStream
        {
            get
            {
                EnsureConnection();
                return base.InnerStream;
            }
        }

        public FtpActiveStream(Socket socket, TimeSpan connectTimeout, FtpSession session)
            : base(session)
        {
            _connectTimeout = connectTimeout;
            _acceptResult = socket.BeginAccept(OnSocketAccept, socket);
        }

        private void EnsureConnection()
        {
            if (_socket != null)
                return;

            if (!_acceptResult.AsyncWaitHandle.WaitOne(_connectTimeout))
                throw new FtpTransportException("Active stream did not get connection");
        }

        private void OnSocketAccept(IAsyncResult ar)
        {
            var socket = (Socket)ar.AsyncState;
            _socket = socket.EndAccept(ar);
            SetSocket(_socket);
        }
    }
}