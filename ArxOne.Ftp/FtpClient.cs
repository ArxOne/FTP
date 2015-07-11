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
    using Exceptions;
    using IO;
    using Platform;

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

        /// <summary>
        /// Gets a value indicating whether this instance is unix.
        /// Important: using this propery may send a command to server, so use outside command sequences
        /// </summary>
        /// <value><c>true</c> if this instance is unix; otherwise, <c>false</c>.</value>
        [Obsolete("Use ServerType instead")]
        public bool IsUnix { get { return ServerType == FtpServerType.Unix; } }

        private FtpServerType? _serverType;
        /// <summary>
        /// Gets the type of the server.
        /// </summary>
        /// <value>
        /// The type of the server.
        /// </value>
        public FtpServerType ServerType
        {
            get
            {
                if (!_serverType.HasValue)
                {
                    if (System.StartsWith("unix", StringComparison.InvariantCultureIgnoreCase))
                        _serverType = FtpServerType.Unix;
                    else if (System.StartsWith("windows", StringComparison.InvariantCultureIgnoreCase))
                        _serverType = FtpServerType.Windows;
                    else
                        _serverType = FtpServerType.Unknown;
                }
                return _serverType.Value;
            }
        }

        private FtpPlatform _ftpPlatform;

        /// <summary>
        /// Gets the FTP platform.
        /// </summary>
        /// <value>
        /// The FTP platform.
        /// </value>
        private FtpPlatform FtpPlatform
        {
            get
            {
                if (_ftpPlatform == null)
                    _ftpPlatform = GetFtpPlatform(ServerType);
                return _ftpPlatform;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientCore" /> class.
        /// </summary>
        /// <param name="protocol">The protocol.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="ftpPlatform">The FTP platform or null if it is to be determined automatically.</param>
        public FtpClient(FtpProtocol protocol, string host, int port, NetworkCredential credential, FtpClientParameters parameters = null, FtpPlatform ftpPlatform = null)
            : base(protocol, host, port, credential, parameters)
        {
            _ftpPlatform = ftpPlatform;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpClientCore" /> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="ftpPlatform">The FTP platform or null if it is to be determined automatically.</param>
        public FtpClient(Uri uri, NetworkCredential credential, FtpClientParameters parameters = null, FtpPlatform ftpPlatform = null)
            : base(uri, credential, parameters)
        {
            _ftpPlatform = ftpPlatform;
        }

        /// <summary>
        /// Gets the FTP platform.
        /// </summary>
        /// <param name="serverType">Type of the server.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">serverType;null</exception>
        private static FtpPlatform GetFtpPlatform(FtpServerType serverType)
        {
            switch (serverType)
            {
                case FtpServerType.Unknown:
                    return new FtpPlatform();
                case FtpServerType.Unix:
                    return new UnixFtpPlatform();
                case FtpServerType.Windows:
                    return new WindowsFtpPlatform();
                default:
                    throw new ArgumentOutOfRangeException("serverType", serverType, null);
            }
        }

        /// <summary>
        /// Checks the protection.
        /// </summary>
        /// <param name="sessionHandle">The session handle.</param>
        /// <param name="requiredChannelProtection">The required channel protection.</param>
        protected void CheckProtection(FtpSessionHandle sessionHandle, FtpProtection requiredChannelProtection)
        {
            // for FTP, don't even bother
            if (sessionHandle.Session.Protocol == FtpProtocol.Ftp)
                return;
            var prot = ChannelProtection.HasFlag(requiredChannelProtection) ? "P" : "C";
            sessionHandle.Session.State["PROT"] = prot;
        }

        /// <summary>
        /// Opens a data stream.
        /// </summary>
        /// <param name="handle">The sequence.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        internal Stream OpenDataStream(FtpSessionHandle handle, FtpTransferMode mode)
        {
            CheckProtection(handle, FtpProtection.DataChannel);
            return handle.Session.OpenDataStream(Passive, ConnectTimeout, ReadWriteTimeout, mode);
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
        public IList<string> List(FtpPath path)
        {
            return Process(handle => ProcessList(handle, path));
        }

        /// <summary>
        /// Processes the list.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private IList<string> ProcessList(FtpSessionHandle handle, FtpPath path)
        {
            // Open data channel
            using (var dataStream = OpenDataStream(handle, FtpTransferMode.Binary))
            {
                // then command is sent
                var reply = Expect(SendCommand(handle, "LIST", EscapePath(path.ToString())), 125, 150, 425);
                if (!reply.Code.IsSuccess)
                {
                    Abort(dataStream);
                    if (reply.Code.Class == FtpReplyCodeClass.Connections)
                        throw new IOException();
                }
                using (var streamReader = new StreamReader(dataStream, ((IFtpStream)dataStream).ProtocolEncoding))
                {
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
        }

        /// <summary>
        /// Escapes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private string EscapePath(string path)
        {
            return FtpPlatform.EscapePath(path);
        }

        /// <summary>
        /// Lists the entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IEnumerable<FtpEntry> ListEntries(FtpPath path)
        {
            return EnumerateEntries(path, List(path));
        }

        /// <summary>
        /// Lists the entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IEnumerable<FtpEntry> StatEntries(FtpPath path)
        {
            return EnumerateEntries(path, Stat(path));
        }

        /// <summary>
        /// Sends a STAT command.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public IEnumerable<string> Stat(FtpPath path)
        {
            var reply = Process(session =>
                  {
                      CheckProtection(session, FtpProtection.ControlChannel);
                      return Expect(SendCommand(session, "STAT", EscapePath(path.ToString())), 213);
                  });
            return reply.Lines.Skip(1).Take(reply.Lines.Length - 2);
        }

        /// <summary>
        /// Enumerates the entries.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="lines">The lines.</param>
        /// <param name="ignoreSpecialEntries">if set to <c>true</c> [ignore special entries].</param>
        /// <returns></returns>
        /// <exception cref="FtpException">Unhandled server type</exception>
        /// <exception cref="FtpProtocolException">Impossible to parse line:  + line;new FtpReplyCode(553)</exception>
        /// <exception cref="FtpReplyCode">553</exception>
        private IEnumerable<FtpEntry> EnumerateEntries(FtpPath parent, IEnumerable<string> lines, bool ignoreSpecialEntries = true)
        {
            foreach (var line in lines)
            {
                var ftpEntry = FtpPlatform.Parse(line, parent);
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
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public Stream Retr(FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            return Process(handle => ProcessRetr(handle, path, mode));
        }

        /// <summary>
        /// Processes the retr.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private Stream ProcessRetr(FtpSessionHandle handle, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            var stream = OpenDataStream(handle, mode);
            var reply = Expect(SendCommand(handle, "RETR", path.ToString()), 125, 150, 425, 550);
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
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public Stream Stor(FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            return Process(handle => ProcessStor(handle, path, mode));
        }

        /// <summary>
        /// Processes the stor.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        private Stream ProcessStor(FtpSessionHandle handle, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)
        {
            var stream = OpenDataStream(handle, mode);
            var reply = Expect(SendCommand(handle, "STOR", path.ToString()), 125, 150, 425, 550);
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
        public bool Rmd(FtpPath path)
        {
            var reply = Process(handle => Expect(SendCommand(handle, "RMD", path.ToString()), 250, 550));
            return reply.Code.IsSuccess;
        }

        /// <summary>
        /// Sends a DELE command (DELEte file).
        /// </summary>
        /// <param name="path">The path.</param>
        public bool Dele(FtpPath path)
        {
            var reply = Process(handle => Expect(SendSingleCommand("DELE", path.ToString()), 250, 550));
            return reply.Code.IsSuccess;
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">The is directory.</param>
        /// <returns></returns>
        private bool Delete(FtpPath path, bool? isDirectory)
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
        public bool Delete(FtpPath path, bool isDirectory)
        {
            return Delete(path, (bool?)isDirectory);
        }

        /// <summary>
        /// Deletes the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool Delete(FtpPath path)
        {
            return Delete(path, null);
        }

        /// <summary>
        /// Sends a MKD command (MaKe Directory).
        /// </summary>
        /// <param name="path">The path.</param>
        public void Mkd(FtpPath path)
        {
            Process(handle => Expect(SendCommand(handle, "MKD", path.ToString()), 257));
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
        /// Gets a <see cref="FtpEntry"/> about given path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The entry, or null if entry does not exist</returns>
        public FtpEntry GetEntry(FtpPath path)
        {
            return Process(handle => ProcessGetEntry(handle, path));
        }

        private FtpEntry ProcessGetEntry(FtpSessionHandle handle, FtpPath path)
        {
            CheckProtection(handle, FtpProtection.ControlChannel);
            var reply = handle.Session.SendCommand("STAT", EscapePath(path.ToString()));
            if (reply.Code != 213 || reply.Lines.Length <= 2)
                return null;
            // now get the type: the first entry is "." for folders or file itself for files/links
            var entry = EnumerateEntries(path, reply.Lines.Skip(1), ignoreSpecialEntries: false).First();
            // actually, it's always good here
            return new FtpEntry(path, entry.Size, entry.Type, entry.Date, entry.Target);
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
