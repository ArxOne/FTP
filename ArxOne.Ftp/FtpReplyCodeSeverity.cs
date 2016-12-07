#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.Ftp
{
    /// <summary>
    /// Reply code severy class
    /// </summary>
    public enum FtpReplyCodeSeverity
    {
        /// <summary>
        /// The requested action is being initiated; expect another
        /// reply before proceeding with a new command.  (The
        /// user-process sending another command before the
        /// completion reply would be in violation of protocol; but
        /// server-FTP processes should queue any commands that
        /// arrive while a preceding command is in progress.)  This
        /// type of reply can be used to indicate that the command
        /// was accepted and the user-process may now pay attention
        /// to the data connections, for implementations where
        /// simultaneous monitoring is difficult.  The server-FTP
        /// process may send at most, one 1yz reply per command.
        /// </summary>
        PositivePreliminary = 100,

        /// <summary>
        /// The requested action has been successfully completed.  A
        /// new request may be initiated.
        /// </summary>
        PositiveCompletion = 200,

        /// <summary>
        /// The command has been accepted, but the requested action
        /// is being held in abeyance, pending receipt of further
        /// information.  The user should send another command
        /// specifying this information.  This reply is used in
        /// command sequence groups.
        /// </summary>
        PositiveIntermediate = 300,

        /// <summary>
        /// The command was not accepted and the requested action did
        /// not take place, but the error condition is temporary and
        /// the action may be requested again.  The user should
        /// return to the beginning of the command sequence, if any.
        /// It is difficult to assign a meaning to "transient",
        /// particularly when two distinct sites (Server- and
        /// User-processes) have to agree on the interpretation.
        /// Each reply in the 4yz category might have a slightly
        /// different time value, but the intent is that the
        /// user-process is encouraged to try again.  A rule of thumb
        /// in determining if a reply fits into the 4yz or the 5yz
        /// (Permanent Negative) category is that replies are 4yz if
        /// the commands can be repeated without any change in
        /// command form or in properties of the User or Server
        /// (e.g., the command is spelled the same with the same
        /// arguments used; the user does not change his file access
        /// or user name; the server does not put up a new
        /// implementation.)
        /// /// </summary>
        TransientNegativeCompletion = 400,

        /// <summary>
        /// The command was not accepted and the requested action did
        /// not take place.  The User-process is discouraged from
        /// repeating the exact request (in the same sequence).  Even
        /// some "permanent" error conditions can be corrected, so
        /// the human user may want to direct his User-process to
        /// reinitiate the command sequence by direct action at some
        /// point in the future (e.g., after the spelling has been
        /// changed, or the user has altered his directory status.)
        /// </summary>
        PermanentNegativeCompletion = 500,
    }
}