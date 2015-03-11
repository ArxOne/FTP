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
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;

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
                if (_ftpClient.ProxyConnect != null)
                {
                    var stream = _ftpClient.ProxyConnect(_host, _port, true);
                    if (stream != null)
                        return stream;
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
            // TODO: enumerate AddressFamily members
            var transportSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //var ipAddresses = (from ipAddress in DnsEx.GetHostEntry(_host, TimeSpan.FromSeconds(1)).AddressList
            //                   orderby ipAddress.AddressFamily
            //                   select ipAddress).ToArray();
            transportSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            transportSocket.SendTimeout = transportSocket.ReceiveTimeout = (int)readWriteTimeout.TotalMilliseconds;
            transportSocket.Connect(_host, _port, connectTimeout);
            if (!transportSocket.Connected)
            {
                message = "Not connected";
                return null;
            }

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
            var sslStream = new SslStream(_protocolStream, true, CheckCertificateHandler);
            sslStream.AuthenticateAsClient(_host, null, SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls, false);
            return sslStream;
        }
    }
}
