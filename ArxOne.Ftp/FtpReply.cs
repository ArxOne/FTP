#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

    /// <summary>
    /// FTP Reply line
    /// </summary>
    [DebuggerDisplay("FTP {Code.Code} {Lines[0]}")]
    public class FtpReply
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public FtpReplyCode Code { get; private set; }

        /// <summary>
        /// Gets or sets the lines.
        /// </summary>
        /// <value>The lines.</value>
        public string[] Lines { get; private set; }

        private static readonly Regex FirstLine = new Regex(@"^(?<code>\d{3})\-(?<line>.*)", RegexOptions.Compiled);
        private static readonly Regex LastLine = new Regex(@"^(?<code>\d{3})\ (?<line>.*)", RegexOptions.Compiled);

        /// <summary>
        /// Parses the line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        internal bool ParseLine(string line)
        {
            var lastLineMatch = LastLine.Match(line);
            if (lastLineMatch.Success)
            {
                Code = new FtpReplyCode(int.Parse(lastLineMatch.Groups["code"].Value));
                AppendLine(lastLineMatch.Groups["line"].Value);
                return false;
            }

            var firstLineMatch = FirstLine.Match(line);
            if (firstLineMatch.Success)
                AppendLine(firstLineMatch.Groups["line"].Value);
            else
                AppendLine(line);
            return true;
        }

        /// <summary>
        /// Appends the line.
        /// </summary>
        /// <param name="line">The line.</param>
        private void AppendLine(string line)
        {
            var lines = new List<string>();
            if (Lines != null)
                lines.AddRange(Lines);
            lines.Add(line);
            Lines = lines.ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpReply"/> class.
        /// </summary>
        public FtpReply()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpReply"/> class.
        /// </summary>
        /// <param name="lines">The lines.</param>
        public FtpReply(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if(!ParseLine(line))
                    break;
            }
        }
    }
}
