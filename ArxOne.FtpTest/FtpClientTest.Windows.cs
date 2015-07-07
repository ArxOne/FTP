#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.FtpTest
{
    using System.Linq;
    using Ftp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    partial class FtpClientTest
    {
        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Windows")]
        [TestCategory("Credentials")]
        public void WindowsCreateFileTest()
        {
            CreateFileTest(true, "win");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Windows")]
        [TestCategory("Credentials")]
        public void WindowsActiveCreateFileTest()
        {
            CreateFileTest(false, "win");
        }


        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        [TestCategory("Windows")]
        public void WindowsServerTest()
        {
            var ftpTestHost = GetTestHost("ftp", "win");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var i = ftpClient.ServerType;
                var s = ftpClient.StatEntries("/").ToArray();
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        [TestCategory("Windows")]
        public void WindowsSpaceNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "win"), "A and B", "C and D");
        }


        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        [TestCategory("Windows")]
        public void WindowsBracketsNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "win"), "X[]Y", "Z{}[]T");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Credentials")]
        [TestCategory("Windows")]
        public void WindowsParenthesisNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "win"), "i()j", "k()l");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Windows")]
        [TestCategory("Credentials")]
        public void WindowsFtpListTest()
        {
            FtpListTest(true, "win");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("Windows")]
        [TestCategory("Credentials")]
        public void WindowsFtpActiveListTest()
        {
            FtpListTest(false, "win");
        }
    }
}
