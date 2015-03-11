#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;

    /// <summary>
    /// EventArgs for protocol message
    /// </summary>
    public class ProtocolMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        /// <value>The session ID.</value>
        public int SessionID { get; private set; }

        /// <summary>
        /// Gets or sets the request message.
        /// </summary>
        /// <value>The message.</value>
        public string RequestCommand { get; private set; }

        /// <summary>
        /// Gets or sets the request parameters.
        /// </summary>
        /// <value>The request parameters.</value>
        public string[] RequestParameters { get; private set; }

        /// <summary>
        /// Gets or sets the reply.
        /// </summary>
        /// <value>The reply.</value>
        public FtpReply Reply { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolMessageEventArgs"/> class.
        /// </summary>
        /// <param name="sessionID">The session ID.</param>
        /// <param name="requestCommand">The request command.</param>
        /// <param name="requestParameters">The request parameters.</param>
        /// <param name="reply">The reply.</param>
        public ProtocolMessageEventArgs(int sessionID, string requestCommand = null, string[] requestParameters = null, FtpReply reply = null)
        {
            SessionID = sessionID;
            RequestCommand = requestCommand;
            RequestParameters = requestParameters;
            Reply = reply;
        }
    }
}
