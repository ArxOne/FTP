#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp.IO
{
    using System.IO;

    /// <summary>
    /// FTP stream
    /// </summary>
    public abstract class FtpStream : Stream
    {
        /// <summary>
        /// Aborts this instance.
        /// To be used with caution, since it does not sends information to control channel
        /// (whereas Dispose() does)
        /// </summary>
        public abstract void Abort();
    }
}