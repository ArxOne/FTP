#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    /// <summary>
    /// Error class
    /// </summary>
    public enum FtpReplyCodeClass
    {
        /// <summary>
        /// These replies refer to syntax errors,
        /// syntactically correct commands that don't fit any
        /// functional category, unimplemented or superfluous
        /// commands.
        /// </summary>
        Syntax = 00,

        /// <summary>
        /// These are replies to requests for
        /// information, such as status or help.
        /// </summary>
        Information = 10,

        /// <summary>
        /// Replies referring to the control and
        /// data connections.
        /// </summary>
        Connections = 20,

        /// <summary>
        /// Replies for the login
        /// process and accounting procedures.
        /// </summary>
        AuthenticationAndAccounting = 30,

        /// <summary>
        /// These replies indicate the status of the
        /// Server file system vis-a-vis the requested transfer or
        /// other file system action.
        /// </summary>
        Filesystem = 50,
    }
}