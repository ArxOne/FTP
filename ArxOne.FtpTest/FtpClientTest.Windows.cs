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
        [TestCategory("Windows")]
        [TestCategory("RequireHost")]
        public void WindowsCreateFileTest()
        {
            CreateFileTest(true, "win");
        }

        [TestMethod]
        [TestCategory("Windows")]
        [TestCategory("RequireHost")]
        public void WindowsActiveCreateFileTest()
        {
            CreateFileTest(false, "win");
        }


        [TestMethod]
        [TestCategory("RequireHost")]
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
        [TestCategory("RequireHost")]
        [TestCategory("Windows")]
        public void WindowsSpaceNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "win"), "A and B", "C and D");
        }


        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Windows")]
        public void WindowsBracketsNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "win"), "X[]Y", "Z{}[]T");
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Windows")]
        public void WindowsParenthesisNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "win"), "i()j", "k()l");
        }

        [TestMethod]
        [TestCategory("Windows")]
        [TestCategory("RequireHost")]
        public void WindowsFtpListTest()
        {
            ListTest(true, "win");
        }

        [TestMethod]
        [TestCategory("Windows")]
        [TestCategory("RequireHost")]
        public void WindowsFtpActiveListTest()
        {
            ListTest(false, "win");
        }
    }
}
