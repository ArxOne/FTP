#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    /// <summary>
    /// Type for FtpEntry
    /// </summary>
    public enum FtpEntryType
    {
        /// <summary>
        /// Regular file
        /// </summary>
        File,
        /// <summary>
        /// Directory
        /// </summary>
        Directory,
        /// <summary>
        /// Symlink
        /// </summary>
        Link,
    }
}