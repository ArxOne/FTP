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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Exceptions;
    using IO;

    /// <summary>
    /// FTP client
    /// </summary>
    public static class FtpClientUtility
    {
        /// <summary>
        /// Checks the protection.
        /// </summary>
        /// <param name="sessionHandle">The session handle.</param>
        /// <param name="requiredChannelProtection">The required channel protection.</param>
        private static void CheckProtection(FtpSessionHandle sessionHandle, FtpProtection requiredChannelProtection)
        {
            // for FTP, don't even bother
            if (sessionHandle.Session.Protocol == FtpProtocol.Ftp)
                return;
            var prot = sessionHandle.Session.FtpClient.ChannelProtection.HasFlag(requiredChannelProtection) ? "P" : "C";
            sessionHandle.State["PROT"] = prot;
        }

        /// <summary>
        /// Opens a data stream.
        /// </summary>
        /// <param name="sessionHandle">The session handle.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        internal static FtpStream OpenDataStream(FtpSessionHandle sessionHandle, FtpTransferMode mode)
        {
            CheckProtection(sessionHandle, FtpProtection.DataChannel);
            return sessionHandle.OpenDataStream(sessionHandle.Session.FtpClient.Passive, sessionHandle.Session.FtpClient.ConnectTimeout, sessionHandle.Session.FtpClient.ReadWriteTimeout, mode);
        }

        /// <summary>
        /// Sends LIST command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IList<string> List(this FtpClient ftpClient, FtpPath path)
        {
            return ftpClient.Process(handle => ProcessList(handle, path));
        }

        /// <summary>
        /// Processes the list.
        /// </summary>
        /// <param name="sessionHandle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static IList<string> ProcessList(FtpSessionHandle sessionHandle, FtpPath path)
        {
            // Open data channel
            using (var dataStream = OpenDataStream(sessionHandle, FtpTransferMode.Binary))
            {
                // then command is sent
                var reply = sessionHandle.Session.FtpClient.Expect(sessionHandle.SendCommand("LIST", sessionHandle.Session.FtpClient.FtpPlatform.EscapePath(path.ToString())), 125, 150, 425);
                if (!reply.Code.IsSuccess)
                {
                    dataStream.Abort();
                    FtpClient.ThrowException(reply);
                }
                using (var streamReader = new StreamReader(dataStream.Validated(), sessionHandle.Session.Encoding))
                {
                    var list = new List<string>();
                    for (;;)
                    {
                        var line = streamReader.ReadLine();
                        if (line == null)
                            break;
                        list.Add(line);
                    }
                    return list;
                }
            }
        }

        /// <summary>
        /// Lists the entries.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IEnumerable<FtpEntry> ListEntries(this FtpClient ftpClient, FtpPath path)
        {
            return EnumerateEntries(ftpClient, path, List(ftpClient, path));
        }

        /// <summary>
        /// Lists the entries.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IEnumerable<FtpEntry> StatEntries(this FtpClient ftpClient, FtpPath path)
        {
            return EnumerateEntries(ftpClient, path, Stat(ftpClient, path));
        }

        /// <summary>
        /// Sends a STAT command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IEnumerable<string> Stat(this FtpClient ftpClient, FtpPath path)
        {
            var reply = ftpClient.Process(session =>
                  {
                      CheckProtection(session, FtpProtection.ControlChannel);
                      return ftpClient.Expect(ftpClient.SendCommand(session, "STAT", ftpClient.FtpPlatform.EscapePath(path.ToString())), 213);
                  });
            return reply.Lines.Skip(1).Take(reply.Lines.Length - 2);
        }

        /// <summary>
        /// Enumerates the entries.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="lines">The lines.</param>
        /// <param name="ignoreSpecialEntries">if set to <c>true</c> [ignore special entries].</param>
        /// <returns></returns>
        /// <exception cref="FtpProtocolException">Impossible to parse line:  + line;new FtpReplyCode(553)</exception>
        /// <exception cref="FtpReplyCode">553</exception>
        /// <exception cref="FtpException">Unhandled server type</exception>
        private static IEnumerable<FtpEntry> EnumerateEntries(FtpClient ftpClient, FtpPath parent, IEnumerable<string> lines, bool ignoreSpecialEntries = true)
        {
            foreach (var line in lines)
            {
                var ftpEntry = ftpClient.FtpPlatform.Parse(line, parent);
                if (ftpEntry == null)
                    throw new FtpProtocolException("Impossible to parse line: " + line, new FtpReplyCode(553));

                if (ignoreSpecialEntries && (ftpEntry.Name == "." || ftpEntry.Name == ".."))
                    continue;

                yield return ftpEntry;
            }
        }

        /// <summary>
        /// Sends RETR command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static Stream Retr(this FtpClient ftpClient, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            return ftpClient.Process(handle => ProcessRetr(handle, path, mode));
        }

        /// <summary>
        /// Processes the retr.
        /// </summary>
        /// <param name="sessionHandle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private static Stream ProcessRetr(FtpSessionHandle sessionHandle, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            var stream = OpenDataStream(sessionHandle, mode);
            var reply = sessionHandle.Session.FtpClient.Expect(sessionHandle.SendCommand("RETR", path.ToString()), 125, 150, 425, 550);
            if (!reply.Code.IsSuccess)
            {
                stream.Abort();
                FtpClient.ThrowException(reply);
                return null;
            }
            return stream.Validated();
        }

        /// <summary>
        /// Send STOR command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static Stream Stor(this FtpClient ftpClient, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            return ftpClient.Process(handle => ProcessStor(handle, path, mode));
        }

        /// <summary>
        /// Processes the stor.
        /// </summary>
        /// <param name="sessionHandle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private static Stream ProcessStor(FtpSessionHandle sessionHandle, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            var stream = OpenDataStream(sessionHandle, mode);
            var reply = sessionHandle.Session.FtpClient.Expect(sessionHandle.SendCommand("STOR", path.ToString()), 125, 150, 425, 550);
            if (!reply.Code.IsSuccess)
            {
                stream.Abort();
                FtpClient.ThrowException(reply);
                return null;
            }
            return stream.Validated();
        }

        /// <summary>
        /// Sends a RMD command (ReMove Directory).
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static bool Rmd(this FtpClient ftpClient, FtpPath path)
        {
            var reply = ftpClient.Process(handle => handle.Session.FtpClient.Expect(handle.SendCommand("RMD", path.ToString()), 250, 550));
            return reply.Code.IsSuccess;
        }

        /// <summary>
        /// Sends a DELE command (DELEte file).
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static bool Dele(this FtpClient ftpClient, FtpPath path)
        {
            var reply = ftpClient.Process(handle => handle.Session.FtpClient.Expect(handle.Session.FtpClient.SendSingleCommand("DELE", path.ToString()), 250, 550));
            return reply.Code.IsSuccess;
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">The is directory.</param>
        /// <returns></returns>
        private static bool Delete(this FtpClient ftpClient, FtpPath path, bool? isDirectory)
        {
            // if we don't know, try to delete as a directory
            if (!isDirectory.HasValue || isDirectory.Value)
            {
                var deleted = Rmd(ftpClient, path);
                // if we wanted to delete a directory or if we actually deleted something, then consider the operation as complete
                if (deleted || isDirectory.HasValue)
                    return deleted;
            }
            // otherwise, either it is a directory, or the type is unknown and the file delete failed
            return Dele(ftpClient, path);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns></returns>
        public static bool Delete(this FtpClient ftpClient, FtpPath path, bool isDirectory)
        {
            return Delete(ftpClient, path, (bool?)isDirectory);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static bool Delete(this FtpClient ftpClient, FtpPath path)
        {
            return Delete(ftpClient, path, null);
        }

        /// <summary>
        /// Sends a MKD command (MaKe Directory).
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        public static void Mkd(this FtpClient ftpClient, FtpPath path)
        {
            ftpClient.Process(handle => handle.Session.FtpClient.Expect(handle.SendCommand("MKD", path.ToString()), 257));
        }

        /// <summary>
        /// Sends RNFR / RNTO pair.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public static void RnfrTo(this FtpClient ftpClient, string from, string to)
        {
            ftpClient.Process(delegate (FtpSessionHandle handle)
                      {
                          ftpClient.Expect(ftpClient.SendCommand(handle, "RNFR", from), 350);
                          ftpClient.Expect(ftpClient.SendCommand(handle, "RNTO", to), 250);
                          return 0;
                      });
        }

        /// <summary>
        /// Gets a <see cref="FtpEntry" /> about given path.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns>
        /// The entry, or null if entry does not exist
        /// </returns>
        public static FtpEntry GetEntry(this FtpClient ftpClient, FtpPath path)
        {
            return ftpClient.Process(handle => ProcessGetEntry(handle, path));
        }

        private static FtpEntry ProcessGetEntry(FtpSessionHandle sessionHandle, FtpPath path)
        {
            CheckProtection(sessionHandle, FtpProtection.ControlChannel);
            var reply = sessionHandle.SendCommand("STAT", sessionHandle.Session.FtpClient.FtpPlatform.EscapePath(path.ToString()));
            if (reply.Code != 213 || reply.Lines.Length <= 2)
                return null;
            // now get the type: the first entry is "." for folders or file itself for files/links
            var entry = EnumerateEntries(sessionHandle.Session.FtpClient, path, reply.Lines.Skip(1), ignoreSpecialEntries: false).First();
            // actually, it's always good here
            return new FtpEntry(path, entry.Size, entry.Type, entry.Date, entry.Target);
        }

        /// <summary>
        /// Sends a MLST command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static string Mlst(this FtpClient ftpClient, FtpPath path)
        {
            var reply = ftpClient.Process(session =>
            {
                CheckProtection(session, FtpProtection.ControlChannel);
                return ftpClient.Expect(ftpClient.SendCommand(session, "MLST", ftpClient.FtpPlatform.EscapePath(path.ToString())), 250);
            });
            return reply.Lines[1];
        }

        /// <summary>
        /// Sends a MLST command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static FtpEntry MlstEntry(this FtpClient ftpClient, FtpPath path)
        {
            return ParseMlsx(Mlst(ftpClient, path), path);
        }

        /// <summary>
        /// Sends LIST command.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IList<string> Mlsd(this FtpClient ftpClient, FtpPath path)
        {
            return ftpClient.Process(handle => ProcessMlsd(handle, path));
        }

        /// <summary>
        /// Sends MLSD command, parses result.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static IEnumerable<FtpEntry> MlsdEntries(this FtpClient ftpClient, FtpPath path)
        {
            return Mlsd(ftpClient, path).Select(m => ParseMlsx(m, path));
        }


        /// <summary>
        /// Processes the list.
        /// </summary>
        /// <param name="sessionHandle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static IList<string> ProcessMlsd(FtpSessionHandle sessionHandle, FtpPath path)
        {
            // Open data channel
            using (var dataStream = OpenDataStream(sessionHandle, FtpTransferMode.Binary))
            {
                // then command is sent
                var reply = sessionHandle.Session.FtpClient.Expect(sessionHandle.SendCommand("MLSD", sessionHandle.Session.FtpClient.FtpPlatform.EscapePath(path.ToString())), 125, 150, 425);
                if (!reply.Code.IsSuccess)
                {
                    dataStream.Abort();
                    FtpClient.ThrowException(reply);
                }
                using (var streamReader = new StreamReader(dataStream.Validated(), sessionHandle.Session.Encoding))
                {
                    var list = new List<string>();
                    for (;;)
                    {
                        var line = streamReader.ReadLine();
                        if (line == null)
                            break;
                        list.Add(line);
                    }
                    return list;
                }
            }
        }

        /// <summary>
        /// Parses a MLSx line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="parent">The parent.</param>
        /// <returns></returns>
        internal /* test */ static FtpEntry ParseMlsx(string line, FtpPath parent)
        {
            var mlsxLine = ReadMlsxLine(line);
            return new FtpEntry(parent, mlsxLine.Item1,
                size: GetFact(mlsxLine.Item2, "Size", f => long.Parse(f), () => (long?)null),
                type: GetFact(mlsxLine.Item2, "Type", GetTypeFact, () => FtpEntryType.File),
                date: GetFact(mlsxLine.Item2, "Modify", GetDateFact, () => DateTime.MinValue),
                target: null);
        }

        /// <summary>
        /// Gets the date fact.
        /// </summary>
        /// <param name="factValue">The fact value.</param>
        /// <returns></returns>
        private static DateTime GetDateFact(string factValue)
        {
            DateTime date;
            if (DateTime.TryParseExact(factValue, new[] { "yyyyMMddhhmmss", "yyyyMMddhhmmss.fff" }, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out date))
                return date;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Gets the type fact.
        /// </summary>
        /// <param name="factValue">The fact value.</param>
        /// <returns></returns>
        private static FtpEntryType GetTypeFact(string factValue)
        {
            if (string.Equals(factValue, "dir", StringComparison.CurrentCultureIgnoreCase)
            || string.Equals(factValue, "cdir", StringComparison.CurrentCultureIgnoreCase)
            || string.Equals(factValue, "pdir", StringComparison.CurrentCultureIgnoreCase))
                return FtpEntryType.Directory;
            if (string.Equals(factValue, "os.unix=symlink", StringComparison.CurrentCultureIgnoreCase))
                return FtpEntryType.Link;
            return FtpEntryType.File;
        }

        /// <summary>
        /// Gets the fact (abstract method for lazy people).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="facts">The facts.</param>
        /// <param name="factName">Name of the fact.</param>
        /// <param name="parseFactValue">The parse fact value.</param>
        /// <param name="getDefault">The get default.</param>
        /// <returns></returns>
        private static TValue GetFact<TValue>(IDictionary<string, string> facts, string factName, Func<string, TValue> parseFactValue,
            Func<TValue> getDefault)
        {
            string factValue;
            if (!facts.TryGetValue(factName, out factValue))
                return getDefault();
            try
            {
                return parseFactValue(factValue);
            }
            catch
            {
            }
            return getDefault();
        }

        private static Tuple<string, IDictionary<string, string>> ReadMlsxLine(string line)
        {
            line = line.TrimStart();

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0)
                return null;

            var allFacts = line.Substring(0, spaceIndex);
            var fileName = line.Substring(spaceIndex + 1);
            var facts = allFacts.Split(';');
            var factsByKey = (from fact in facts
                              let kv = fact.Split(new[] { '=' }, 2)
                              where kv.Length == 2
                              select new { Key = kv[0], Value = kv[1] })
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.InvariantCultureIgnoreCase);
            return Tuple.Create<string, IDictionary<string, string>>(fileName, factsByKey);
        }
    }
}
