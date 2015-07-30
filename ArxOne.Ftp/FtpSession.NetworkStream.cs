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
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using IO;

    partial class FtpSession
    {
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
                if (FtpClient.ProxyConnect != null)
                {
                    var socket = FtpClient.ProxyConnect(new DnsEndPoint(_host, _port));
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
            transportSocket.Connect(_host, _port, connectTimeout);
            if (!transportSocket.Connected)
            {
                message = "Not connected";
                return null;
            }
            _activeTransferHost = ((IPEndPoint)transportSocket.LocalEndPoint).Address;
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
            sslStream.AuthenticateAsClient(_host, null, SslProtocols.Ssl3 | SslProtocols.Tls, false);
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
            if (FtpClient.ChannelProtection.HasFlag(FtpProtection.DataChannel))
                stream = UpgradeToSsl(stream);
            return stream;
        }
    }
}
