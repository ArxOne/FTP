#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.FtpTest
{
    using Ftp;
    using Ftp.Platform;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    class ListParseTest
    {

        /// <summary>
        ///A test for ParseUnix
        ///</summary>
        [TestMethod]
        [TestCategory("Unix")]
        public void ParseUnix1Test()
        {
            var entry = FtpPlatform.ParseUnix("drwxr-xr-x    4 1001     1001         4096 Jan 21 14:41 nas-1", null);
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Directory, entry.Type);
            Assert.AreEqual("nas-1", entry.Name);
        }

        /// <summary>
        ///A test for ParseUnix
        ///</summary>
        [TestMethod]
        [TestCategory("Unix")]
        public void ParseUnix2Test()
        {
            var entry = FtpPlatform.ParseUnix("drwxr-xr-x    4 nas-1    nas-1        4096 Jan 21 15:41 nas-1", null);
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Directory, entry.Type);
            Assert.AreEqual("nas-1", entry.Name);
        }

        /// <summary>
        ///A test for ParseUnix
        ///</summary>
        [TestMethod]
        [TestCategory("Unix")]
        public void ParseUnix3Test()
        {
            var entry = FtpPlatform.ParseUnix("lrwxrwxrwx    1 0        0               4 Sep 03  2009 lib64 -> /lib", null);
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Link, entry.Type);
            Assert.AreEqual("lib64", entry.Name);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void ParseWindowsTest()
        {
            var entry = WindowsFtpPlatform.ParseLine("    03-07-15  03:52PM                22286 03265480-photo-logo.png", null);
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.File, entry.Type);
            Assert.AreEqual("03265480-photo-logo.png", entry.Name);
        }

        [TestMethod]
        [TestCategory("Windows")]
        public void ParseWindows2Test()
        {
            var entry = WindowsFtpPlatform.ParseLine("    04-04-15  12:12PM       <DIR>          New folder", null);
            Assert.IsNotNull(entry);
            Assert.AreEqual(FtpEntryType.Directory, entry.Type);
            Assert.AreEqual("New folder", entry.Name);
        }
    }
}
