#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    /// <summary>
    /// FTP Protocol
    /// </summary>
    public enum FtpProtocol
    {
        /// <summary>
        /// Standard FTP, with no SSL
        /// </summary>
        Ftp,
        /// <summary>
        /// Implicit SSL FTP
        /// </summary>
        FtpS,
        /// <summary>
        /// Explicit SSL FTP
        /// </summary>
        FtpES,
    }
}