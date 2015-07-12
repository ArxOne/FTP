#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp
{
    using System.Diagnostics;

    /// <summary>
    /// Represents a FTP path
    /// </summary>
    [DebuggerDisplay("FTP path: {_path}")]
    public class FtpPath
    {
        /// <summary>
        /// The separator character
        /// </summary>
        public const char Separator = '/';

        // Currently, this is loosely parsed (ideally it would be cut in pieces)
        // but we don't need it

        /// <summary>
        /// The _path
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpPath"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public FtpPath(string path)
        {
            // only constraint: a rooted path
            if (path.StartsWith(Separator.ToString()))
                _path = path;
            else
                _path = Separator + path;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _path;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="FtpPath"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FtpPath(string path)
        {
            if (path == null)
                return null;
            return new FtpPath(path);
        }

        /// <summary>
        /// Adds a new part to path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static FtpPath operator +(FtpPath path, string fileName)
        {
            if (fileName.StartsWith(Separator.ToString()))
                return new FtpPath(fileName);
            // simple concatenation
            if (path._path.EndsWith(Separator.ToString()))
                return new FtpPath(path._path + fileName);
            return new FtpPath(path._path + Separator + fileName);
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            var path = _path.TrimEnd(Separator);
            var index = _path.LastIndexOf(Separator);
            if (index < 0)
                return path;
            return path.Substring(index + 1);
        }
    }
}
