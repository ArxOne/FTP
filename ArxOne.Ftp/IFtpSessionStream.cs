#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    /// <summary>
    /// Allows to access stream's session
    /// </summary>
    internal interface IFtpSessionStream
    {
        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <value>The session.</value>
        FtpSession Session { get; }

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        void Abort();
    }
}
