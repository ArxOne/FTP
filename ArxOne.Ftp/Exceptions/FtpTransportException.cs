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
    /// Transport exceptions
    /// </summary>
    [Serializable]
    public class FtpTransportException : FtpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public FtpTransportException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpTransportException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FtpTransportException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #region Serialization

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class.
        /// </summary>
        [Obsolete("Serialization-only ctor")]
        protected FtpTransportException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        protected FtpTransportException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion
    }
}
