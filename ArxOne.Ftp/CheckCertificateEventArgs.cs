#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate check EventArgs
    /// </summary>
    public class CheckCertificateEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>The certificate.</value>
        public X509Certificate Certificate { get; private set; }
        /// <summary>
        /// Gets or sets the chain.
        /// </summary>
        /// <value>The chain.</value>
        public X509Chain Chain { get; private set; }
        /// <summary>
        /// Gets or sets the sslpolicyerrors.
        /// </summary>
        /// <value>The sslpolicyerrors.</value>
        public SslPolicyErrors Sslpolicyerrors { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CheckCertificateEventArgs"/> is valid.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Invalidates this instance.
        /// </summary>
        public void Invalidate()
        {
            IsValid = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckCertificateEventArgs"/> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslpolicyerrors">The sslpolicyerrors.</param>
        public CheckCertificateEventArgs(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            Certificate = certificate;
            Chain = chain;
            Sslpolicyerrors = sslpolicyerrors;
            IsValid = true;
        }
    }
}
