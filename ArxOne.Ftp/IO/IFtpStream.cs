#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp.IO
{
    using System.Text;

    /// <summary>
    /// FTP stream interface
    /// </summary>
    public interface IFtpStream
    {
        /// <summary>
        /// Gets the protocol encoding.
        /// </summary>
        /// <value>The protocol encoding.</value>
        Encoding ProtocolEncoding { get; }
    }
}
