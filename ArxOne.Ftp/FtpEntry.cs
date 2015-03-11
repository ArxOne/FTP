#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents a filesystem entry
    /// </summary>
    [DebuggerDisplay("FTP entry {Name}, type={Type}, size={Size}")]
    public class FtpEntry
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the target (for symlinks, null otherwise).
        /// </summary>
        /// <value>The target.</value>
        public string Target { get; private set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public long? Size { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is directory.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is directory; otherwise, <c>false</c>.
        /// </value>
        public FtpEntryType Type { get; private set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpEntry"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="size">The size.</param>
        /// <param name="type">The type.</param>
        /// <param name="date">The date.</param>
        /// <param name="target">The target.</param>
        public FtpEntry(string name, long? size, FtpEntryType type, DateTime date, string target)
        {
            Name = name;
            Date = date;
            Type = type;
            Target = target;
            Size = size;
        }
    }
}
