#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;

    /// <summary>
    /// Session lock: holds a session when used
    /// </summary>
    public class FtpSessionHandle : IDisposable
    {
        /// <summary>
        /// Gets or sets the FTP session.
        /// </summary>
        /// <value>The FTP session.</value>
        public FtpSession Session { get; private set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public FtpSessionState State { get { return Session.State; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpSessionHandle"/> class.
        /// </summary>
        /// <param name="ftpSession">The FTP session.</param>
        internal FtpSessionHandle(FtpSession ftpSession)
        {
            Session = ftpSession;
            Session.AddReference();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            Session.Release();
        }
    }
}
