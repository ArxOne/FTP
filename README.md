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

They are implemented in the form of extension methods to `FtpClient`
*  `IList<string> List(this FtpClient ftpClient, FtpPath path)` lists a directory content and returns raw lines.
*  `IEnumerable<FtpEntry> ListEntries(this FtpClient ftpClient, FtpPath path)` lists a directory content and returns parsed entries.
*  `IEnumerable<string> Stat(this FtpClient ftpClient, FtpPath path)` lists a directory content using the STAT command and returns raw lines.
*  `IEnumerable<FtpEntry> StatEntries(this FtpClient ftpClient, FtpPath path)` lists a directory using the STAT command and returns parsed entries.
*  `Stream Retr(this FtpClient ftpClient, FtpPath path, FtpTransferMode mode = FtpTransferMode.Binary)` opens a file for downloading
*  `Stream Stor(this FtpClient ftpClient, FtpPath path)` creates a file for uploading
*  `bool Rmd(this FtpClient ftpClient, FtpPath path)` removes the directory
*  `bool Dele(this FtpClient ftpClient, FtpPath path)` removes the file
*  `bool Delete(this FtpClient ftpClient, FtpPath path)` removes the entry, whenever it is a file or directory
*  `void RnfrTo(this FtpClient ftpClient, FtpPath from, FtpPath to)` renames/moves a file/directory
*  `FtpEntry GetEntry(this FtpClient ftpClient, FtpPath path)` gets informations about a file or directory
*  `IList<string> Mlst(this FtpClient ftpClient, FtpPath path)` gets a file status (raw form).
*  `FtpEntry MlstEntry(this FtpClient ftpClient, FtpPath path)` gets a file status 
*  `IList<string> Mlsd(this FtpClient ftpClient, FtpPath path)` lists a directory content and returns raw lines.
*  `IEnumerable<FtpEntry> MlsdEntries(this FtpClient ftpClient, FtpPath path)` lists a directory content and returns parsed Note that all commands use an `FtpPath` instance, which has an automatic implicit constructor from `string`, so you can just use strings as parameters, the implicit conversion will do the trick.

#### Core commands

For the hard-core developpers, here are some useful low-level commands.
First of all, it is necessary to understand that the `FtpClient` instance can handle multiple connections at the same time (thus, it uses one separate connection to send one command). Usually it uses only one session, and reuses it command after command. You don't manipulate sessions directly but they are used by the client).

* `bool Features.HasFeature(string feature)` indicates if a feature is supported by the server (the method is a method from the `Features` member of `FtpClient`)
* `FtpSession Session()` uses or create a session (this allows mutiple connections at same time with only one `FtpClient`)
* `FtpReply SendCommand(FtpSessionHandle session, string command, params string[] parameters)` sends a command and returns a raw FTP reply
*  `FtpReply Expect(FtpReply reply, params int[] codes)` expects the given reply to have one of the given codes.

For example:
```csharp
// main connection to target
using (var ftpClient = new FtpClient(new Uri("ftp://server"), new NetworkCredential("anonymous","me@me.com")))
{
    // using or creating a session
    using(var ftpSession = ftpClient.Session())
    {
        // sending a custom command
        var ftpReply = ftpSession.SendCommand("STUFF", "here", "now");
        // checkin the result. The Expect method returns the FtpReply, so SendCommand() and Expect() can be nested.
        FtpSession.Expect(ftpReply, 200, 250);
    }
}
```

