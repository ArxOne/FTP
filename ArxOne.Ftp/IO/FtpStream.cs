#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp.IO
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using Exceptions;

    /// <summary>
    /// FTP stream
    /// </summary>
    internal class FtpStream : Stream, IFtpStream, IFtpSessionStream
    {
        /// <summary>
        /// Gets the protocol encoding.
        /// </summary>
        /// <value>The protocol encoding.</value>
        public Encoding ProtocolEncoding { get { return Session.Encoding; } }

        /// <summary>
        /// Gets or sets the holder.
        /// </summary>
        /// <value>The holder.</value>
        public FtpSession Session { get; protected set; }

        private bool _disposed;

        private Stream _innerStream;

        protected virtual Stream InnerStream { get { return _innerStream; } }

        private Socket _innerSocket;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpStream"/> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="session">The session.</param>
        public FtpStream(Socket socket, FtpSession session)
        {
            Session = session;
            Session.AddReference();
            SetSocket(socket);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpStream"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        protected FtpStream(FtpSession session)
        {
            Session = session;
            Session.AddReference();
        }

        /// <summary>
        /// Sets the socket.
        /// </summary>
        /// <param name="socket">The socket.</param>
        protected void SetSocket(Socket socket)
        {
            _innerStream = Session.CreateDataStream(socket);
            _innerSocket = socket;
            _innerSocket.SendBufferSize = 1492;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Net.Sockets.NetworkStream"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !_disposed)
            {
                _disposed = true;
                bool isConnected = true;
                try
                {
                    if (_innerSocket != null)
                    {
                        isConnected = _innerSocket.Connected;
                        if (isConnected)
                        {
                            Process(delegate
                            {
                                _innerSocket.Shutdown(SocketShutdown.Both);
                                _innerSocket.Close(300000);
                            });
                        }
                    }
                    Process(() => InnerStream.Dispose());
                }
                finally
                {
                    Release(true);
                }
                if (!isConnected)
                    throw new FtpTransportException("PASV stream already closed by server");
            }
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        /// <exception cref="T:System.IO.IOException">
        /// An I/O error occurs.
        /// </exception>
        public override void Flush()
        {
            InnerStream.Flush();
        }

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        public void Abort()
        {
            Release(false);
        }

        /// <summary>
        /// Releases the instance (deferences it from the session locks).
        /// </summary>
        /// <param name="expectEndReply">if set to <c>true</c> [expect end reply].</param>
        protected void Release(bool expectEndReply)
        {
            var session = Session;
            Session = null;
            if (session != null)
            {
                try
                {
                    if (expectEndReply)
                    {
                        Process(() => session.Expect(
                            226, // default ack
                            150 // if the stream was opened but nothing was sent, then we still shall exit gracefully
                            ));
                    }
                }
                // on long transfers, the command socket may be closed
                // however we need to signal it to client
                finally
                {
                    session.Release();
                }
            }
        }

        // below are wrapped methods where exceptions need to be reinterpreted
        #region Exception wrapper

        /// <summary>
        /// Processes the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        protected static void Process(Action action)
        {
            Process(delegate
            {
                action();
                return 0;
            });
        }

        /// <summary>
        /// Processes the specified func.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="func">The func.</param>
        /// <returns></returns>
        protected static TResult Process<TResult>(Func<TResult> func)
        {
            try
            {
                return func();
            }
            catch (SocketException se)
            {
                throw new FtpTransportException("Socket exception in PASV stream", se);
            }
            catch (IOException ioe)
            {
                throw new FtpTransportException("IO exception in PASV stream", ioe);
            }
        }

        #endregion

        #region Stream wrappers

        /// <summary>
        /// Reads data from the <see cref="T:System.Net.Sockets.NetworkStream"/>.
        /// </summary>
        /// <param name="buffer">An array of type <see cref="T:System.Byte"/> that is the location in memory to store data read from the <see cref="T:System.Net.Sockets.NetworkStream"/>.</param>
        /// <param name="offset">The location in <paramref name="buffer"/> to begin storing the data to.</param>
        /// <param name="size">The number of bytes to read from the <see cref="T:System.Net.Sockets.NetworkStream"/>.</param>
        /// <returns>
        /// The number of bytes read from the <see cref="T:System.Net.Sockets.NetworkStream"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="buffer"/> parameter is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="offset"/> parameter is less than 0.
        /// -or-
        /// The <paramref name="offset"/> parameter is greater than the length of <paramref name="buffer"/>.
        /// -or-
        /// The <paramref name="size"/> parameter is less than 0.
        /// -or-
        /// The <paramref name="size"/> parameter is greater than the length of <paramref name="buffer"/> minus the value of the <paramref name="offset"/> parameter.
        /// -or-
        /// An error occurred when accessing the socket. See the Remarks section for more information.
        /// </exception>
        /// <exception cref="T:System.IO.IOException">
        /// The underlying <see cref="T:System.Net.Sockets.Socket"/> is closed.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="T:System.Net.Sockets.NetworkStream"/> is closed.
        /// -or-
        /// There is a failure reading from the network.
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
        /// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// </PermissionSet>
        public override int Read(byte[] buffer, int offset, int size)
        {
            return Process(() => BaseRead(buffer, offset, size));
        }

        /// <summary>
        /// Call to base.Read.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        private int BaseRead(byte[] buffer, int offset, int size)
        {
            return InnerStream.Read(buffer, offset, size);
        }

        /// <summary>
        /// Writes data to the <see cref="T:System.Net.Sockets.NetworkStream"/>.
        /// </summary>
        /// <param name="buffer">An array of type <see cref="T:System.Byte"/> that contains the data to write to the <see cref="T:System.Net.Sockets.NetworkStream"/>.</param>
        /// <param name="offset">The location in <paramref name="buffer"/> from which to start writing data.</param>
        /// <param name="size">The number of bytes to write to the <see cref="T:System.Net.Sockets.NetworkStream"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="buffer"/> parameter is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="offset"/> parameter is less than 0.
        /// -or-
        /// The <paramref name="offset"/> parameter is greater than the length of <paramref name="buffer"/>.
        /// -or-
        /// The <paramref name="size"/> parameter is less than 0.
        /// -or-
        /// The <paramref name="size"/> parameter is greater than the length of <paramref name="buffer"/> minus the value of the <paramref name="offset"/> parameter.
        /// </exception>
        /// <exception cref="T:System.IO.IOException">
        /// There was a failure while writing to the network.
        /// -or-
        /// An error occurred when accessing the socket. See the Remarks section for more information.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="T:System.Net.Sockets.NetworkStream"/> is closed.
        /// -or-
        /// There was a failure reading from the network.
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
        /// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// </PermissionSet>
        public override void Write(byte[] buffer, int offset, int size)
        {
            Process(() => BaseWrite(buffer, offset, size));
        }

        /// <summary>
        /// Calls base.Write.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        private void BaseWrite(byte[] buffer, int offset, int size)
        {
            InnerStream.Write(buffer, offset, size);
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">
        /// An I/O error occurs.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The stream does not support seeking, such as if the stream is constructed from a pipe or console output.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// Methods were called after the stream was closed.
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="T:System.IO.IOException">
        /// An I/O error occurs.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// Methods were called after the stream was closed.
        /// </exception>
        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports reading; otherwise, false.
        /// </returns>
        public override bool CanRead
        {
            get { return InnerStream.CanRead; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports seeking; otherwise, false.
        /// </returns>
        public override bool CanSeek
        {
            get { return InnerStream.CanSeek; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports writing; otherwise, false.
        /// </returns>
        public override bool CanWrite
        {
            get { return InnerStream.CanWrite; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// A long value representing the length of the stream in bytes.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        /// A class derived from Stream does not support seeking.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// Methods were called after the stream was closed.
        /// </exception>
        public override long Length
        {
            get { return InnerStream.Length; }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The current position within the stream.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">
        /// An I/O error occurs.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The stream does not support seeking.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// Methods were called after the stream was closed.
        /// </exception>
        public override long Position
        {
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        #endregion
    }
}