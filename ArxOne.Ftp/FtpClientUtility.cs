﻿#region Arx One FTP
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
        /// Opens a data stream.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static FtpStream OpenDataStream(this FtpSession session, FtpTransferMode mode)
        {
            var client = session.Connection.Client;
            return session.OpenDataStream(client.Passive, client.ConnectTimeout, client.ReadWriteTimeout, mode, null);
        }

        /// <summary>
        /// Opens a data stream.
        /// </summary>
        /// <param name="session">The session handle.</param>
        /// <param name="transferMode">The mode.</param>
        /// <param name="streamMode">The stream mode.</param>
        /// <returns></returns>
        public static FtpStream OpenDataStream(this FtpSession session, FtpTransferMode transferMode, FtpStreamMode streamMode)
        {
            var client = session.Connection.Client;
            return session.OpenDataStream(client.Passive, client.ConnectTimeout, client.ReadWriteTimeout, transferMode, streamMode);
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
        /// <param name="session">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static IList<string> ProcessList(FtpSession session, FtpPath path)
        {
            // Open data channel
            using (var dataStream = OpenDataStream(session, FtpTransferMode.Binary, FtpStreamMode.Read))
            {
                // then command is sent
                var reply = session.Expect(session.SendCommand("LIST", session.Connection.Client.GetPlatform(session).EscapePath(path.ToString())), 125, 150, 425);
                if (!reply.Code.IsSuccess)
                {
                    dataStream.Abort();
                    session.ThrowException(reply);
                }
                using (var streamReader = new StreamReader(dataStream.Validated(), session.Connection.Encoding))
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
                      session.CheckProtection(FtpProtection.ControlChannel);
                      return session.Expect(session.SendCommand("STAT", ftpClient.GetPlatform(session).EscapePath(path.ToString())), 213, 211);
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
            // ToArray() here in order to release the FtpSession
            foreach (var line in lines.ToArray())
            {
                var ftpEntry = ftpClient.Platform.Parse(line, parent);
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
        /// <param name="session">The handle.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private static Stream ProcessRetr(FtpSession session, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            var stream = OpenDataStream(session, mode, FtpStreamMode.Read);
            var reply = session.Expect(session.SendCommand("RETR", path.ToString()), 125, 150, 425, 550);
            if (!reply.Code.IsSuccess)
            {
                stream.Abort();
                session.ThrowException(reply);
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
        /// <param name="session">The handle.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private static Stream ProcessStor(FtpSession session, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            var stream = OpenDataStream(session, mode, FtpStreamMode.Write);
            var reply = session.Expect(session.SendCommand("STOR", path.ToString()), 125, 150, 425, 550);
            if (!reply.Code.IsSuccess)
            {
                stream.Abort();
                session.ThrowException(reply);
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
            var reply = ftpClient.Process(session => session.Expect(session.SendCommand("RMD", path.ToString()), 250, 550));
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
            var reply = ftpClient.Process(session => session.Expect(session.SendCommand("DELE", path.ToString()), 250, 550));
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
            ftpClient.Process(session => session.Expect(session.SendCommand("MKD", path.ToString()), 257));
        }

        /// <summary>
        /// Sends RNFR / RNTO pair.
        /// </summary>
        /// <param name="ftpClient">The FTP client.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public static void RnfrTo(this FtpClient ftpClient, string from, string to)
        {
            ftpClient.Process(delegate (FtpSession session)
                      {
                          session.Expect(session.SendCommand("RNFR", from), 350);
                          session.Expect(session.SendCommand("RNTO", to), 250);
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

        private static FtpEntry ProcessGetEntry(FtpSession session, FtpPath path)
        {
            session.CheckProtection(FtpProtection.ControlChannel);
            var reply = session.SendCommand("STAT", session.Connection.Client.GetPlatform(session).EscapePath(path.ToString()));
            if ((reply.Code != 213 && reply.Code != 211 && reply.Code != 212) || reply.Lines.Length <= 2)
                return null;
            // now get the type: the first entry is "." for folders or file itself for files/links
            var entry = EnumerateEntries(session.Connection.Client, path, reply.Lines.Skip(1), ignoreSpecialEntries: false).First();
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
                session.CheckProtection(FtpProtection.ControlChannel);
                return session.Expect(session.SendCommand("MLST", ftpClient.GetPlatform(session).EscapePath(path.ToString())), 250);
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
        /// <param name="session">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static IList<string> ProcessMlsd(FtpSession session, FtpPath path)
        {
            // Open data channel
            using (var dataStream = OpenDataStream(session, FtpTransferMode.Binary, FtpStreamMode.Read))
            {
                // then command is sent
                var reply = session.Expect(session.SendCommand("MLSD", session.Connection.Client.GetPlatform(session).EscapePath(path.ToString())), 125, 150, 425);
                if (!reply.Code.IsSuccess)
                {
                    dataStream.Abort();
                    session.ThrowException(reply);
                }
                using (var streamReader = new StreamReader(dataStream.Validated(), session.Connection.Encoding))
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
            if (DateTime.TryParseExact(factValue, new[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" }, CultureInfo.InvariantCulture,
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
