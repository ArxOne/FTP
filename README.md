# Arx One FTP

A simple FTP/FTPS/FTPES client.
The client handles multiple connections and is thread safe.
It also has high-level commands but supports direct command sending.

## Suggestions, requests, love letters...

We are open to suggestions, don't hesitate to [submit one](https://github.com/ArxOne/FTP/issues).

## How to get it

As a [NuGet package](https://www.nuget.org/packages/ArxOne.Ftp).

## How to use it

### Instantiation

```csharp
using (var ftpClient = new FtpClient(ftpUri, ftpCredentials))
{
    // ... Enjoy!
}
```

### Commands

#### High-level commands

*  `IList<string> List(string path)` lists a directory content and returns raw lines.
*  `IEnumerable<FtpEntry> ListEntries(string path)` lists a directoy content and returns parsed entries.
*  `IEnumerable<string> Stat(string path)` lists a directory content using the STAT command and returns raw lines.
*  `IEnumerable<FtpEntry> StatEntries(string path)` lists a directory using the STAT command and returns parsed entries.
*  `Stream Retr(string path, FtpTransferMode mode = FtpTransferMode.Binary)` opens a file for downloading
*  `Stream Stor(string path)` creates a file for uploading
*  `bool Rmd(string path)` removes the directory
*  `bool Dele(string path)` removes the file
*  `bool Delete(string path)` removes the entry, whenever it is a file or directory
*  `void RnfrTo(string from, string to)` renames/moves a file/directory

#### Core commands

For the hard-core developpers, here are some useful low-level commands.
First of all, it is necessary to understand that the `FtpClient` instance can handle multiple sessions at the same time (thus, is uses one separate session to send one command). Usually it uses only one session, and reuses it. You don't manipulate sessions directly but they are used by the client).

* `bool HasFeature(string feature)` indicates if a feature is supported by the server
* `FtpSessionHandle Session()` uses or create a session (this allows mutiple sessions at same time with only one `FtpClient`)
* `FtpReply SendCommand(FtpSessionHandle session, string command, params string[] parameters)` sends a command and returns a raw FTP reply
*  `FtpReply Expect(FtpReply reply, params int[] codes)` expects the given reply to have one of the given codes.

For example:
```csharp
// main connection to target
using (var ftpClient = new FtpClient(new Uri("ftp://server"), new NetworkCredentials("anonymous","me@me.com"))
{
    // using or creating a session
    using(var ftpSession = ftpClient.Session())
    {
        // sending a custom command
        var ftpReply = ftpClient.SendCommand(session, "STUFF", "here", "now");
        // checkin the result. The Expect method returns the FtpReply, so SendCommand() and Expect() can be nested.
        ftpClient.Expect(ftpReply, 200, 250);
    }
}
```

