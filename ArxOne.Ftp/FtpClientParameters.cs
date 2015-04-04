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
    using System.Text;

    /// <summary>
    /// Parameters to initialize FtpClient instance
    /// </summary>
    public class FtpClientParameters
    {
        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        /// <value>The connect timeout.</value>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the read write timeout.
        /// </summary>
        /// <value>The read write timeout.</value>
        public TimeSpan ReadWriteTimeout { get; set; }

        /// <summary>
        /// Gets or sets the session timeout.
        /// </summary>
        /// <value>
        /// The session timeout.
        /// </value>
        public TimeSpan SessionTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FtpClientParameters"/> is passive.
        /// </summary>
        /// <value><c>true</c> if passive; otherwise, <c>false</c>.</value>
        public bool Passive { get; set; }

        /// <summary>
        /// Gets or sets the active transfer host.
        /// When specified, this address is used with PORT/EPRT commands
        /// </summary>
        /// <value>
        /// The active transfer host.
        /// </value>
        public IPAddress ActiveTransferHost { get; set; }

        /// <summary>
        /// Gets or sets the anonymous password.
        /// </summary>
        /// <value>The anonymous password.</value>
        public string AnonymousPassword { get; set; }

        /// <summary>
        /// Gets or sets the default encoding.
        /// </summary>
        /// <value>The default encoding.</value>
        public Encoding DefaultEncoding { get; set; }

        /// <summary>
        /// Gets or sets the proxy connector.
        /// Arg1: host
        /// Arg2: port
        /// Arg3: true for control stream, false for data stream
        /// </summary>
        /// <value>The proxy.</value>
        public Func<string, int, bool, Stream> ProxyConnect { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientParameters"/> class.
        /// </summary>
        public FtpClientParameters()
        {
            ConnectTimeout = TimeSpan.FromSeconds(10);
            ReadWriteTimeout = TimeSpan.FromMinutes(10);
            SessionTimeout = TimeSpan.FromMinutes(2);
            Passive = true;
            AnonymousPassword = "user@" + Environment.MachineName;
            DefaultEncoding = Encoding.UTF8;
        }
    }
}
