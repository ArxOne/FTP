
namespace ArxOne.Ftp.Platform
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Base for platform-specific operations
    /// </summary>
    public class FtpPlatform
    {
        private static readonly Regex UnixListEx = new Regex(
            @"(?<xtype>[-dlDL])[A-Za-z\-]{9}\s+"
            + @"\d*\s+"
            + @"(?<owner>\S*)\s+"
            + @"(?<group>\S*)\s+"
            + @"(?<size>\d*)\s+"
            + @"(?<month>[a-zA-Z]{3})\s+"
            + @"(?<day>\d{1,2})\s+"
            + @"(((?<hour>\d{2})\:(?<minute>\d{2}))|(?<year>\d{4}))\s+"
            + @"(?<name>.*)"
            , RegexOptions.CultureInvariant | RegexOptions.Compiled);


        /// <summary>
        /// Parses the specified directory line.
        /// </summary>
        /// <param name="directoryLine">The directory line.</param>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        public virtual FtpEntry Parse(string directoryLine, FtpPath parent)
        {
            return ParseUnix(directoryLine, parent);
        }

        /// <summary>
        /// Parses the line.
        /// </summary>
        /// <param name="directoryLine">The directory line.</param>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        internal /* test */ static FtpEntry ParseUnix(string directoryLine, FtpPath parent)
        {
            var match = UnixListEx.Match(directoryLine);
            if (!match.Success)
                return null;

            var literalType = match.Groups["xtype"].Value;
            var name = match.Groups["name"].Value;
            var type = FtpEntryType.File;
            string target = null;
            if (string.Equals(literalType, "d", StringComparison.InvariantCultureIgnoreCase))
                type = FtpEntryType.Directory;
            else if (string.Equals(literalType, "l", StringComparison.InvariantCultureIgnoreCase))
            {
                type = FtpEntryType.Link;
                const string separator = " -> ";
                var linkIndex = name.IndexOf(separator, StringComparison.InvariantCultureIgnoreCase);
                if (linkIndex >= 0)
                {
                    target = name.Substring(linkIndex + separator.Length);
                    name = name.Substring(0, linkIndex);
                }
            }
            var ftpEntry = new FtpEntry(parent, name, long.Parse(match.Groups["size"].Value), type, ParseDateTime(match, DateTime.Now), target);
            return ftpEntry;
        }


        /// <summary>
        /// Parses the date time.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <param name="now">The now.</param>
        /// <returns></returns>
        protected static DateTime ParseDateTime(Match match, DateTime now)
        {
            return ParseDateTime(match.Groups["year"].Value, match.Groups["month"].Value, match.Groups["day"].Value,
                                 match.Groups["hour"].Value, match.Groups["minute"].Value, match.Groups["am"].Value, match.Groups["pm"].Value,
                                 now);
        }


        /// <summary>
        /// Parses the date time.
        /// </summary>
        /// <param name="literalYear">The literal year.</param>
        /// <param name="literalMonth">The literal month.</param>
        /// <param name="literalDay">The literal day.</param>
        /// <param name="literalHour">The literal hour.</param>
        /// <param name="literalMinute">The literal minute.</param>
        /// <param name="am">The am.</param>
        /// <param name="pm">PM.</param>
        /// <param name="now">The now.</param>
        /// <returns></returns>
        private static DateTime ParseDateTime(string literalYear, string literalMonth, string literalDay,
                                              string literalHour, string literalMinute, string am, string pm, DateTime now)
        {
            var month = ParseMonth(literalMonth);
            var day = int.Parse(literalDay);
            int year;
            if (string.IsNullOrEmpty(literalYear))
            {
                year = now.Year;
                var guessDate = new DateTime(year, month, day);
                if (guessDate > now.Date)
                    year--;
            }
            else
                year = int.Parse(literalYear);
            if (year < 100)
            {
                var century = (now.Year / 100) * 100;
                year += century;
            }
            var hour = string.IsNullOrEmpty(literalHour) ? 0 : int.Parse(literalHour);
            if (!string.IsNullOrEmpty(pm))
            {
                if (hour < 12) // because 12PM is 12 so 12 stays 12
                    hour += 12;
            }
            else if (!string.IsNullOrEmpty(am))
            {
                // 12AM is 0
                if (hour == 12)
                    hour = 0;
            }
            var minute = string.IsNullOrEmpty(literalMinute) ? 0 : int.Parse(literalMinute);
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);
        }

        private static readonly string[] LiteralMonths = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

        /// <summary>
        /// Gets the month.
        /// </summary>
        /// <param name="literalMonth">The literal month.</param>
        /// <returns></returns>
        private static int ParseMonth(string literalMonth)
        {
            int month;
            if (int.TryParse(literalMonth, out month))
                return month;
            month = Array.IndexOf(LiteralMonths, literalMonth.ToLower()) + 1;
            return month;
        }

        /// <summary>
        /// Escapes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public virtual string EscapePath(string path)
        {
            return path;
        }

        /// <summary>
        /// Escapes the path.
        /// Provided at this level for convenience
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="escapeCharacters">The escape characters.</param>
        /// <returns></returns>
        protected string EscapePath(string path, string escapeCharacters)
        {
            // first, see if any chaaracter to be escaped is contained
            if (!escapeCharacters.Any(path.Contains))
                return path;
            // otherwise use a StringBuilder and escape with \
            var pathBuilder = new StringBuilder();
            foreach (var c in path)
            {
                if (escapeCharacters.Contains(c))
                    pathBuilder.Append('\\');
                pathBuilder.Append(c);
            }
            return pathBuilder.ToString();
        }
    }
}
