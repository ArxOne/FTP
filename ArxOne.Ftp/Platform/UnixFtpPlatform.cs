#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp.Platform
{
    /// <summary>
    /// Default implementation for Unix platforms
    /// </summary>
    public class UnixFtpPlatform : FtpPlatform
    {
        /// <summary>
        /// Escapes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public override string EscapePath(string path)
        {
            return EscapePath(path, "[]()");
        }
    }
}