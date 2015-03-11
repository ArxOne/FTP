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
    ///This is a test class for FtpReplyCodeTest and is intended
    ///to contain all FtpReplyCodeTest Unit Tests
    ///</summary>
    [TestClass]
    public class FtpReplyCodeTest
    {
        [TestMethod]
        [TestCategory("FtpClient")]
        public void ClassTest()
        {
            var code = new FtpReplyCode(450);
            Assert.AreEqual(FtpReplyCodeClass.Filesystem, code.Class);
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        public void SeverityTest()
        {
            var code = new FtpReplyCode(450);
            Assert.AreEqual(FtpReplyCodeSeverity.TransientNegativeCompletion, code.Severity);
        }
    }
}
