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
            var ftpTestHost = TestHost.Get("ftp", "win");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var i = ftpClient.ServerType;
                var s = ftpClient.StatEntries("/").ToArray();
            }
        }
    }
}
