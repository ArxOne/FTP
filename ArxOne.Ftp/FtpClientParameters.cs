#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;


    /// <summary>
    /// Parameters to initialize FtpClient instance
    /// </summary>
    public class FtpClientParameters
    {


        private TimeSpan m_connectTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        /// <value>The connect timeout.</value>
        public TimeSpan ConnectTimeout
        {
            get
            {
                return this.m_connectTimeout;
            }
            set
            {
                this.m_connectTimeout = value;
            }
        }


        private TimeSpan m_readWriteTimeout = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the read write timeout.
        /// </summary>
        /// <value>The read write timeout.</value>
        public TimeSpan ReadWriteTimeout
        {
            get
            {
                return this.m_readWriteTimeout;
            }
            set
            {
                this.m_readWriteTimeout = value;
            }
        }



        private TimeSpan m_sessionTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets the session timeout.
        /// </summary>
        /// <value>
        /// The session timeout.
        /// </value>

        public TimeSpan SessionTimeout
        {
            get
            {
                return this.m_sessionTimeout;
            }
            set
            {
                this.m_sessionTimeout = value;
            }
        }



        private bool m_passive = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FtpClientParameters"/> is passive.
        /// </summary>
        /// <value><c>true</c> if passive; otherwise, <c>false</c>.</value>
        public bool Passive
        {
            get
            {
                return this.m_passive;
            }
            set
            {
                this.m_passive = value;
            }
        }




        /// <summary>
        /// Gets or sets the active transfer host.
        /// When specified, this address is used with PORT/EPRT commands
        /// </summary>
        /// <value>
        /// The active transfer host.
        /// </value>
        public IPAddress ActiveTransferHost { get; set; }



        private string m_anonymousPassword = "user@" + Environment.MachineName;

        /// <summary>
        /// Gets or sets the anonymous password.
        /// </summary>
        /// <value>The anonymous password.</value>
        public string AnonymousPassword
        {
            get
            {
                return this.m_anonymousPassword;
            }
            set
            {
                this.m_anonymousPassword = value;
            }
        }



        private System.Text.Encoding m_defaultEncoding = System.Text.Encoding.UTF8;

        /// <summary>
        /// Gets or sets the default encoding.
        /// </summary>
        /// <value>The default encoding.</value>
        public System.Text.Encoding DefaultEncoding
        {
            get
            {
                return this.m_defaultEncoding;
            }
            set
            {
                this.m_defaultEncoding = value;
            }
        }



        /// <summary>
        /// Gets or sets the proxy connector.
        /// Arg1: host
        /// Arg2: port
        /// Arg3: true for control stream, false for data stream
        /// </summary>
        /// <value>The proxy.</value>
        public Func<EndPoint, Socket> ProxyConnect { get; set; }

        /// <summary>
        /// Gets or sets the channel protection.
        /// Leave to null to use default value according to protocol
        /// (false for FTP, true for FTPS and FTPES)
        /// </summary>
        /// <value>
        /// The channel protection.
        /// </value>
        public FtpProtection? ChannelProtection { get; set; }

        /// <summary>
        /// Gets or sets the SSL protocols.
        /// </summary>
        /// <value>
        /// The SSL protocols.
        /// </value>
        public SslProtocols? SslProtocols { get; set; }

        /// <summary>
        /// Gets or sets the client certificates.
        /// </summary>
        /// <value>
        /// The client certificate.
        /// </value>
        public X509CertificateCollection ClientCertificates { get; set; }
    }


}
