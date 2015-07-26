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
    internal class FtpActiveStream : FtpStream
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

        public FtpActiveStream(Socket socket, TimeSpan connectTimeout, FtpSession session)
            : base(session)
        {
            _connectTimeout = connectTimeout;
            socket.BeginAccept(OnSocketAccept, socket);
        }

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