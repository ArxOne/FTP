#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Authentication exception
    /// </summary>
    [Serializable]
    public class FtpAuthenticationException : FtpProtocolException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FtpAuthenticationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        public FtpAuthenticationException(string message, FtpReplyCode code)
            : base(message, code)
        {
        }

        #region Serialization

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpAuthenticationException"/> class.
        /// </summary>
        [Obsolete("Serialization-only ctor")]
        protected FtpAuthenticationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpAuthenticationException"/> class.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        protected FtpAuthenticationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}