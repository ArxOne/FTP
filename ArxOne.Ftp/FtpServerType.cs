#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp
{
    /// <summary>
    /// Type for FTP server
    /// </summary>
    public enum FtpServerType
    {
        /// <summary>
        /// Afraid of the unknown?
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// UNIX (and Linux)
        /// </summary>
        Unix,
        /// <summary>
        /// Windows
        /// </summary>
        Windows,
		/// <summary>
		/// z/OS and MVS
		/// </summary>
		MVS,
    }
}
