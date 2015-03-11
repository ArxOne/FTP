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
    /// Protocol exceptions
    /// </summary>
    [Serializable]
    public class FtpProtocolException : FtpException
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public FtpReplyCode Code { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        public FtpProtocolException(string message, FtpReplyCode code)
            : base(message)
        {
            Code = code;
        }

        #region Serialization

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class.
        /// </summary>
        [Obsolete("Serialization-only ctor")]
        protected FtpProtocolException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpException"/> class.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        protected FtpProtocolException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Code = new FtpReplyCode(info.GetInt32("FtpReplyCode"));
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/>
        /// </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("FtpReplyCode", Code.Code);
            base.GetObjectData(info, context);
        }

        #endregion
    }
}
