#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp
{
    /// <summary>
    /// Session state
    /// </summary>
    public class FtpSessionState
    {
        private readonly FtpSession _ftpSession;

        /// <summary>
        ///   Gets or sets the parameter. If the value is the current one, then no command is sent
        /// </summary>
        public string this[string name]
        {
            get { return _ftpSession.GetSessionParameter(name); }
            set { _ftpSession.CheckSessionParameter(name, value); }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="FtpSessionState" /> class.
        /// </summary>
        /// <param name="ftpSession"> The FTP session. </param>
        internal FtpSessionState(FtpSession ftpSession)
        {
            _ftpSession = ftpSession;
        }
    }
}
