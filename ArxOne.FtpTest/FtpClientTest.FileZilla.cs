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
        [TestCategory("FileZillaUnix")]
        [TestCategory("Credentials")]
        public void FileZillaUnixCreateFileTest()
        {
            CreateFileTest(true, "fzx");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Credentials")]
        public void FileZillaUnixActiveCreateFileTest()
        {
            CreateFileTest(false, "fzx");
        }
        
        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Windows")]
        public void FileZillaUnixServerTest()
        {
            var ftpTestHost = GetTestHost("ftp", "fzx");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var i = ftpClient.ServerType;
                var s = ftpClient.StatEntries("/").ToArray();
            }
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Windows")]
        public void FileZillaUnixSpaceNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "fzx"), "A and B", "C and D");
        }


        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Windows")]
        public void FileZillaUnixBracketsNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "fzx"), "X[]Y", "Z{}[]T");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Windows")]
        public void FileZillaUnixParenthesisNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp", "fzx"), "i()j", "k()l");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Credentials")]
        public void FileZillaUnixFtpListTest()
        {
            FtpListTest(true, "fzx");
        }

        [TestMethod]
        [TestCategory("FtpClient")]
        [TestCategory("FileZillaUnix")]
        [TestCategory("Credentials")]
        public void FileZillaUnixFtpActiveListTest()
        {
            FtpListTest(false, "fzx");
        }
    }
}
