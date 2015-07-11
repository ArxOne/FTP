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
    /// Protection levels for FTP
    /// </summary>
    [Flags]
    public enum FtpProtection
    {
        /// <summary>
        /// Protection on control channel
        /// </summary>
        ControlChannel = 0x01,
        /// <summary>
        /// Protection on data channel
        /// </summary>
        DataChannel = 0x02,

        /// <summary>
        /// All channels protection
        /// </summary>
        AllChannels = ControlChannel | DataChannel,

        /// <summary>
        /// The FTP default protection
        /// </summary>
        Ftp = 0,
        /// <summary>
        /// The FTPES default protection
        /// </summary>
        FtpES = AllChannels,
        /// <summary>
        /// The FTPS default protection
        /// </summary>
        FtpS = AllChannels,
    }
}
