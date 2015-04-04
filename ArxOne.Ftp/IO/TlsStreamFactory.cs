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
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;

    /// <summary>
    /// Allows to create TlsStream, this MF internal class!
    /// </summary>
    public class TlsStreamFactory
    {
        private static Type _tlsStreamType;

        /// <summary>
        /// Gets the type of the TLS stream.
        /// </summary>
        /// <value>The type of the TLS stream.</value>
        internal /*test*/ static Type TlsStreamType
        {
            get
            {
                if (_tlsStreamType == null)
                    _tlsStreamType = typeof(SslStream).Assembly.GetType("System.Net.TlsStream");
                return _tlsStreamType;
            }
        }

        /// <summary>
        /// Creates the TLS stream.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="underlyingStream">The underlying stream.</param>
        /// <returns></returns>
        public static NetworkStream CreateTlsStream(Uri uri, Stream underlyingStream)
        {
            // public TlsStream(string destinationHost, NetworkStream networkStream, 
            //                  X509CertificateCollection clientCertificates, ServicePoint servicePoint, 
            //                  object initiatingRequest, ExecutionContext executionContext)
            var networkStream = (NetworkStream)underlyingStream;
            var servicePoint = ServicePointManager.FindServicePoint(uri);
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            return (NetworkStream)Activator.CreateInstance(TlsStreamType, uri.Host, networkStream, null, servicePoint, null, null);
        }
    }
}
