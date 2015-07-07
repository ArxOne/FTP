#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp.Platform
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Windows IIS FTP specific platform
    /// </summary>
    public class WindowsFtpPlatform : FtpPlatform
    {

        private static readonly Regex WindowsListEx = new Regex(
            @"\s*"
            + @"(?<month>\d{2})\-"
            + @"(?<day>\d{2})\-"
            + @"(?<year>\d{2,4})\s+"
            + @"(?<hour>\d{2})\:"
            + @"(?<minute>\d{2})"
            + @"((?<am>AM)|(?<pm>PM))\s+"
            + @"((?<dir>\<DIR\>)|(?<size>\d+))\s+"
            + @"(?<name>.*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Parses Windows formatted list line.
        /// </summary>
        /// <param name="directoryLine">The directory line.</param>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        public override FtpEntry Parse(string directoryLine, FtpPath parent)
        {
            return ParseLine(directoryLine, parent);
        }

        /// <summary>
        /// Parses the line.
        /// </summary>
        /// <param name="directoryLine">The directory line.</param>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        internal /* test */ static FtpEntry ParseLine(string directoryLine, FtpPath parent)
        {
            var match = WindowsListEx.Match(directoryLine);
            if (!match.Success)
                return null;

            var name = match.Groups["name"].Value;
            var date = ParseDateTime(match, DateTime.Now);
            var literalSize = match.Groups["size"].Value;
            long? size = null;
            var type = FtpEntryType.File;
            if (!string.IsNullOrEmpty(literalSize))
                size = long.Parse(literalSize);
            else
                type = FtpEntryType.Directory;
            return new FtpEntry(parent, name, size, type, date, null);
        }
    }
}