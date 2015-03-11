#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using Exceptions;

    /// <summary>
    /// FTP client
    /// </summary>
    public class FtpClient : FtpClientCore
    {
        private string _system;

        /// <summary>
        /// Gets the system.
        /// </summary>
        /// <value>The system.</value>
        public string System
        {
            get
            {
                if (_system == null)
                {
                    var systemReply = Expect(SendSingleCommand("SYST"), 215);
                    _system = systemReply.Lines[0];
                }
                return _system;
            }
        }

        private bool? _isUnix;

        /// <summary>
        /// Gets a value indicating whether this instance is unix.
        /// Important: using this propery may send a command to server, so use outside command sequences
        /// </summary>
        /// <value><c>true</c> if this instance is unix; otherwise, <c>false</c>.</value>
        public bool IsUnix
        {
            get
            {
                if (!_isUnix.HasValue)
                    _isUnix = System.StartsWith("unix", StringComparison.InvariantCultureIgnoreCase);
                return _isUnix.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientCore"/> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        public FtpClient(FtpProtocol protocol, string host, int port, NetworkCredential credential, FtpClientParameters parameters = null)
            : base(protocol, host, port, credential, parameters)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientCore"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        public FtpClient(Uri uri, NetworkCredential credential, FtpClientParameters parameters = null)
            : base(uri, credential, parameters)
        {
        }

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
            , RegexOptions.Compiled);

        /// <summary>
        /// Parses the unix ls line.
        /// </summary>
        /// <param name="directoryLine">The directory line.</param>
        /// <returns></returns>
        internal /*test*/ static FtpEntry ParseUnix(string directoryLine)
        {
            var match = UnixListEx.Match(directoryLine);
            if (!match.Success)
                return null;

            var literalType = match.Groups["xtype"].Value;
            var name = match.Groups["name"].Value;
            var type = FtpEntryType.File;
            string target = null;
            if (string.Compare(literalType, "d", true) == 0)
                type = FtpEntryType.Directory;
            else if (string.Compare(literalType, "l", true) == 0)
            {
                type = FtpEntryType.Link;
                const string separator = " -> ";
                var linkIndex = name.IndexOf(separator);
                if (linkIndex >= 0)
                {
                    target = name.Substring(linkIndex + separator.Length);
                    name = name.Substring(0, linkIndex);
                }
            }
            var ftpEntry = new FtpEntry(name,
                                        long.Parse(match.Groups["size"].Value),
                                        type,
                                        ParseDateTime(match, DateTime.Now),
                                        target);
            return ftpEntry;
        }

        /// <summary>
        /// Parses the date time.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <param name="now">The now.</param>
        /// <returns></returns>
        private static DateTime ParseDateTime(Match match, DateTime now)
        {
            return ParseDateTime(match.Groups["year"].Value, match.Groups["month"].Value, match.Groups["day"].Value,
                                 match.Groups["hour"].Value, match.Groups["minute"].Value, now);
        }

        /// <summary>
        /// Parses the date time.
        /// </summary>
        /// <param name="literalYear">The literal year.</param>
        /// <param name="literalMonth">The literal month.</param>
        /// <param name="literalDay">The literal day.</param>
        /// <param name="literalHour">The literal hour.</param>
        /// <param name="literalMinute">The literal minute.</param>
        /// <param name="now">The now.</param>
        /// <returns></returns>
        private static DateTime ParseDateTime(string literalYear, string literalMonth, string literalDay,
                                              string literalHour, string literalMinute, DateTime now)
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
            var hour = string.IsNullOrEmpty(literalHour) ? 0 : int.Parse(literalHour);
            var minute = string.IsNullOrEmpty(literalMinute) ? 0 : int.Parse(literalMinute);
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        private static readonly string[] LiteralMonths = new[]
                                                              {
                                                                  "jan", "feb", "mar", "apr", "may", "jun",
                                                                  "jul", "aug", "sep", "oct", "nov", "dec"
                                                              };

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
        /// Retries the specified action.
        /// </summary>
        /// <typeparam name="TResult">The type of the ret.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public TResult Process<TResult>(Func<FtpSessionHandle, TResult> action)
        {
            using (var handle = Session())
            {
                return action(handle);
            }
        }

        /// <summary>
        /// Sends LIST command.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IList<string> List(string path)
        {
            return Process(handle => ProcessList(handle, path));
        }

        /// <summary>
        /// Processes the list.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private IList<string> ProcessList(FtpSessionHandle handle, string path)
        {
            // Then data channel
            using (var dataStream = OpenDataStream(handle, FtpTransferMode.Binary))
            using (var streamReader = new StreamReader(dataStream,
                                                    ((IFtpStream)dataStream).ProtocolEncoding))
            {
                // then command is sent
                var reply = Expect(SendCommand(handle, "LIST", path), 125, 150, 425);
                if (!reply.Code.IsSuccess)
                {
                    Abort(dataStream);
                    if (reply.Code.Class == FtpReplyCodeClass.Connections)
                        throw new IOException();
                }
                var list = new List<string>();
                for (; ; )
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        break;
                    list.Add(line);
                }
                return list;
            }
        }

        /// <summary>
        /// Lists the entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IEnumerable<FtpEntry> ListEntries(string path)
        {
            // extracted here, because the IsUnix property may send a command
            var isUnix = IsUnix;
            foreach (var line in List(path))
            {
                FtpEntry ftpEntry;
                if (isUnix)
                    ftpEntry = ParseUnix(line);
                else
                    throw new FtpException("Unhandled server type");
                if (ftpEntry == null)
                    throw new FtpProtocolException("Impossible to parse line: " + line, new FtpReplyCode(553));

                yield return ftpEntry;
            }
        }

        /// <summary>
        /// Lists the entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IEnumerable<FtpEntry> StatEntries(string path)
        {
            // extracted here, because the IsUnix property may send a command
            var isUnix = IsUnix;
            var replyLines = Stat(path);
            return EnumerateEntries(replyLines, isUnix);
        }

        /// <summary>
        /// Sends a STAT command.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IEnumerable<string> Stat(string path)
        {
            var reply = Process(session =>
                  {
                      CheckProtection(session, true);
                      return Expect(SendCommand(session, "STAT", path), 213);
                  });
            return reply.Lines.Skip(1).Take(reply.Lines.Length - 2);
        }

        /// <summary>
        /// Enumerates the entries.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="isUnix">The is unix.</param>
        /// <returns></returns>
        public IEnumerable<FtpEntry> EnumerateEntries(IEnumerable<string> lines, bool? isUnix = null)
        {
            if (!isUnix.HasValue)
                isUnix = IsUnix;
            foreach (var line in lines)
            {
                FtpEntry ftpEntry;
                if (isUnix.Value)
                    ftpEntry = ParseUnix(line);
                else
                    throw new FtpException("Unhandled server type");
                if (ftpEntry == null)
                    throw new FtpProtocolException("Impossible to parse line: " + line, new FtpReplyCode(553));

                if (ftpEntry.Name == "." || ftpEntry.Name == "..")
                    continue;

                yield return ftpEntry;
            }
        }

        /// <summary>
        /// Sends RETR command.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public Stream Retr(string path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            return Process(handle => ProcessRetr(handle, path));
        }

        /// <summary>
        /// Processes the retr.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private Stream ProcessRetr(FtpSessionHandle handle, string path)
        {
            var stream = OpenDataStream(handle, FtpTransferMode.Binary);
            var reply = Expect(SendCommand(handle, "RETR", path), 125, 150, 425, 550);
            if (!reply.Code.IsSuccess)
            {
                Abort(stream);
                if (reply.Code.Class == FtpReplyCodeClass.Connections)
                    throw new IOException();
                return null;
            }
            return stream;
        }

        /// <summary>
        /// Send STOR command.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public Stream Stor(string path)
        {
            return Process(handle => ProcessStor(handle, path));
        }

        /// <summary>
        /// Processes the stor.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private Stream ProcessStor(FtpSessionHandle handle, string path)
        {
            var stream = OpenDataStream(handle, FtpTransferMode.Binary);
            var reply = Expect(SendCommand(handle, "STOR", path), 125, 150, 425, 550);
            if (!reply.Code.IsSuccess)
            {
                Abort(stream);
                if (reply.Code.Class == FtpReplyCodeClass.Connections)
                    throw new IOException();
                return null;
            }
            return stream;
        }

        /// <summary>
        /// Sends a RMD command (ReMove Directory).
        /// </summary>
        /// <param name="path">The path.</param>
        public bool Rmd(string path)
        {
            var reply = Process(handle => Expect(SendCommand(handle, "RMD", path), 250, 550));
            return reply.Code.IsSuccess;
        }

        /// <summary>
        /// Sends a DELE command (DELEte file).
        /// </summary>
        /// <param name="path">The path.</param>
        public bool Dele(string path)
        {
            var reply = Process(handle => Expect(SendSingleCommand("DELE", path), 250, 550));
            return reply.Code.IsSuccess;
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">The is directory.</param>
        /// <returns></returns>
        private bool Delete(string path, bool? isDirectory)
        {
            // if we don't know, try to delete as a directory
            if (!isDirectory.HasValue || isDirectory.Value)
            {
                var deleted = Rmd(path);
                // if we wanted to delete a directory or if we actually deleted something, then consider the operation as complete
                if (deleted || isDirectory.HasValue)
                    return deleted;
            }
            // otherwise, either it is a directory, or the type is unknown and the file delete failed
            return Dele(path);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns></returns>
        public bool Delete(string path, bool isDirectory)
        {
            return Delete(path, (bool?)isDirectory);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool Delete(string path)
        {
            return Delete(path, null);
        }

        /// <summary>
        /// Sends a MKD command (MaKe Directory).
        /// </summary>
        /// <param name="path">The path.</param>
        public void Mkd(string path)
        {
            Process(handle => Expect(SendCommand(handle, "MKD", path), 257));
        }

        /// <summary>
        /// Sends RNFR / RNTO pair.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void RnfrTo(string from, string to)
        {
            Process(delegate(FtpSessionHandle handle)
                      {
                          Expect(SendCommand(handle, "RNFR", from), 350);
                          Expect(SendCommand(handle, "RNTO", to), 250);
                          return 0;
                      });
        }

        /// <summary>
        /// Sends the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public FtpReply SendSingleCommand(string command, params string[] parameters)
        {
            return Process(handle => handle.Session.SendCommand(command, parameters));
        }
    }
}
