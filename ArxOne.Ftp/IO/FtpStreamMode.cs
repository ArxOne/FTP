#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp.IO
{
    /// <summary>
    /// Read/write mode for <see cref="FtpStream"/>
    /// </summary>
    public enum FtpStreamMode
    {
        /// <summary>
        /// Stream is used for reading
        /// </summary>
        Read,
        /// <summary>
        /// Stream is used for writing
        /// </summary>
        Write,
    }
}
