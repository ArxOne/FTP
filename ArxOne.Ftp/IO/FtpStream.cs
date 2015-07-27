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

        /// <summary>
        /// Gets or sets a value indicating whether an end reply is expected.
        /// Use with caution
        /// </summary>
        /// <value>
        ///   <c>true</c> if [expect end reply]; otherwise, <c>false</c>.
        /// </value>
        protected bool ExpectEndReply;

        /// <summary>
        /// Sets the stream as validated (it will required a reply from server when disposing).
        /// </summary>
        /// <returns></returns>
        public FtpStream Validated()
        {
            ExpectEndReply = true;
            return this;
        }
    }
}