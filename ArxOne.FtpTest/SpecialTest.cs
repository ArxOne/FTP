#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.FtpTest
{
    using System;
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
                for (int i = 0; i < 100; i++)
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

#if DEBUG
        [TestProperty("Protocol", "FTP")]
        [TestMethod]
        public void SpecificBrownEduTest()
        {
            //using (var ftpClient = new FtpClient(new Uri("ftp://speedtest.tele2.net"), null, new FtpClientParameters()
            using (var ftpClient = new FtpClient(new Uri("ftp://ftp.cs.brown.edu"), null, new FtpClientParameters()
            {
                Passive = false,
            }))
            {
                var entries = ftpClient.MlsdEntries(""); //.Mlsd(new FtpPath("/"));
            }
        }
#endif
    }
}
