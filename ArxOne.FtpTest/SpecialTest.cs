#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.FtpTest
{
    using Ftp;
    using Ftp.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SpecialTest
    {

        [TestCategory("Platform")]
        [TestProperty("Platform", "Xlight FTP")]
        [TestProperty("Protocol", "FTP")]
        [TestMethod]
        public void BroadcastTest()
        {
            var testHost = TestHost.Get(platform: "xlightftpd", protocol: "ftp");
            using (var ftpClient = new FtpClient(testHost.Uri, testHost.Credential))
            {
                for (int i = 0; i < 1000; i++)
                {
                    try
                    {
                        ftpClient.ListEntries("/");
                    }
                    catch (FtpProtocolException)
                    {
                    }
                }
            }
        }

    }
}
