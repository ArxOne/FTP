#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.FtpTest
{
    using Ftp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///This is a test class for FtpReplyTest and is intended
    ///to contain all FtpReplyTest Unit Tests
    ///</summary>
    [TestClass]
    public class FtpReplyTest
    {
        [TestMethod]
        [TestCategory("FtpClient")]
        public void ReadMultilineTest()
        {
            var ftpReply = new FtpReply();
            Assert.IsTrue(ftpReply.ParseLine("220---------- Welcome to Pure-FTPd [privsep] [TLS] ----------"));
            Assert.IsTrue(ftpReply.ParseLine("220-You are user number 1 of 1000 allowed."));
            Assert.IsTrue(ftpReply.ParseLine("220-Local time is now 01:29. Server port: 21."));
            Assert.IsTrue(ftpReply.ParseLine("220-This is a private system - No anonymous login"));
            Assert.IsTrue(ftpReply.ParseLine("220-IPv6 connections are also welcome on this server."));
            Assert.IsFalse(ftpReply.ParseLine("220 You will be disconnected after 15 minutes of inactivity."));
        }
    }
}
