#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System.Diagnostics;

    /// <summary>
    /// FTP reply code
    /// </summary>
    [DebuggerDisplay("FTP code {Code}")]
    public class FtpReplyCode
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public int Code { get; private set; }

        /// <summary>
        /// Gets the severity.
        /// </summary>
        /// <value>The severity.</value>
        public FtpReplyCodeSeverity Severity { get { return (FtpReplyCodeSeverity)((Code / 100) * 100); } }

        /// <summary>
        /// Gets the class.
        /// </summary>
        /// <value>The class.</value>
        public FtpReplyCodeClass Class { get { return (FtpReplyCodeClass)(((Code / 10) % 10) * 10); } }

        /// <summary>
        /// Gets a value indicating whether this instance is success.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is success; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccess
        {
            get { return Code < (int)FtpReplyCodeSeverity.TransientNegativeCompletion; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpReplyCode"/> class.
        /// </summary>
        /// <param name="code">The code.</param>
        public FtpReplyCode(int code)
        {
            Code = code;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ArxOne.Ftp.FtpReplyCode"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="replyCode">The reply code.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator int(FtpReplyCode replyCode)
        {
            return replyCode.Code;
        }

        /// <summary>
        /// Converts to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Code.ToString();
        }
    }
}
