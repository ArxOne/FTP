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

    [TestClass]
    public class MlsxTest
    {
        [TestMethod]
        [TestCategory("Mlsx")]
        public void SimpleFileTest()
        {
            var e = FtpClient.ParseMlsx(" Type=file;Size=1024990;Perm=r; /tmp/cap60.pl198.tar.gz", "/zap");
            Assert.IsNotNull(e);
            Assert.AreEqual("cap60.pl198.tar.gz", e.Path.GetFileName());
            Assert.AreEqual(1024990, e.Size);
            Assert.AreEqual(FtpEntryType.File, e.Type);
        }

        [TestMethod]
        [TestCategory("Mlsx")]
        public void SimpleDirTest()
        {
            var e = FtpClient.ParseMlsx(" Type=dir;Modify=19981107085215;Perm=el; /tmp", "/zap");
            Assert.IsNotNull(e);
            Assert.AreEqual("tmp", e.Path.GetFileName());
            Assert.AreEqual(null, e.Size);
            Assert.AreEqual(FtpEntryType.Directory, e.Type);
        }

        [TestMethod]
        [TestCategory("Mlsx")]
        public void DateTest()
        {
            var e = FtpClient.ParseMlsx(" Type=file;Modify=19990929003355.237; file1", "/zap");
            Assert.IsNotNull(e);
            Assert.AreEqual("file1", e.Path.GetFileName());
            Assert.AreEqual(null, e.Size);
            Assert.AreEqual(FtpEntryType.File, e.Type);
            Assert.AreEqual(1999, e.Date.Year);
            Assert.AreEqual(9, e.Date.Month);
            Assert.AreEqual(29, e.Date.Day);
            Assert.AreEqual(0, e.Date.Hour);
            Assert.AreEqual(33, e.Date.Minute);
            Assert.AreEqual(55, e.Date.Second);
            Assert.AreEqual(237, e.Date.Millisecond);
        }
    }
}
