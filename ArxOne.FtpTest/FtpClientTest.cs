#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.FtpTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using Ftp;
    using Ftp.Platform;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///This is a test class for FtpClientTest and is intended
    ///to contain all FtpClientTest Unit Tests
    ///</summary>
    [TestClass]
    public partial class FtpClientTest
    {
        // Don't know why, but with pure-ftp, protection on data channel fails
        public const FtpProtection FtpESProtection = FtpProtection.ControlChannel;

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void FtpListTest()
        {
            ListTest(true);
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void FtpActiveListTest()
        {
            ListTest(false);
        }

        //[TestMethod]
        //[TestCategory("RequireHost")]
        //[TestCategory("Ftpes")]
        //public void FtpesListWithProtectionTest()
        //{
        //    ListTest(true, protocol: "ftpes", protection: FtpProtection.DataChannel | FtpProtection.CommandChannel);
        //}

        [TestMethod]
        [TestCategory("Ftpes")]
        public void FtpesListWithoutProtectionTest()
        {
            ListTest(true, protocol: "ftpes", protection: FtpProtection.ControlChannel);
        }

        private static void ListTest(bool passive, string hostType = null, FtpProtection? protection = null, string protocol = "ftp")
        {
            var ftpTestHost = TestHost.Get(protocol, hostType);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection }))
            {
                var list = ftpClient.ListEntries("/");
                if (ftpClient.ServerType == FtpServerType.Unix)
                    Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void FtpStatTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var list = ftpClient.StatEntries("/");
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void FtpStatNoDotTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var list = ftpClient.StatEntries("/");
                Assert.IsFalse(list.Any(e => e.Name == "." || e.Name == ".."));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void FtpesStatTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential))
            {
                var list = ftpClient.StatEntries("/").ToArray();
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        [TestMethod]
        [TestCategory("Ftpes")]
        public void ReadFileTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential, new FtpClientParameters { ChannelProtection = FtpESProtection }))
            {
                using (var s = ftpClient.Retr("/var/log/installer/status"))
                using (var t = new StreamReader(s, Encoding.UTF8))
                {
                    var m = t.ReadToEnd();
                    Assert.IsTrue(m.Length > 0);
                }
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void CreateFileTest()
        {
            CreateFileTest(true);
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void ActiveCreateFileTest()
        {
            CreateFileTest(false);
        }

        public void CreateFileTest(bool passive, string hostType = null, string protocol = "ftp")
        {
            var ftpesTestHost = TestHost.Get(protocol, hostType);
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential, new FtpClientParameters { Passive = passive }))
            {
                var directory = ftpClient.ServerType == FtpServerType.Windows ? "/" : "/tmp/";
                var path = directory + "file." + Guid.NewGuid();
                using (var s = ftpClient.Stor(path))
                {
                    s.WriteByte(65);
                }
                using (var r = ftpClient.Retr(path))
                {
                    Assert.IsNotNull(r);
                    Assert.AreEqual(65, r.ReadByte());
                    Assert.AreEqual(-1, r.ReadByte());
                }
                ftpClient.Dele(path);
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void DeleteFileTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential, new FtpClientParameters { ChannelProtection = FtpESProtection }))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                using (var s = ftpClient.Stor(path))
                {
                    s.WriteByte(65);
                }
                Assert.IsTrue(ftpClient.Dele(path));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void DeleteFolderTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                ftpClient.Mkd(path);
                Assert.IsTrue(ftpClient.Rmd(path));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void DeleteTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential, new FtpClientParameters { ChannelProtection = FtpESProtection }))
            {
                var path = "/tmp/file." + Guid.NewGuid();
                ftpClient.Mkd(path);
                Assert.IsTrue(ftpClient.Delete(path));
                using (var s = ftpClient.Stor(path))
                {
                    s.WriteByte(65);
                }
                Assert.IsTrue(ftpClient.Delete(path));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void CreateSpecialNameFolderTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential))
            {
                var path = "/tmp/file." + Guid.NewGuid() + "(D)";
                ftpClient.Mkd(path);
                Assert.IsTrue(ftpClient.Rmd(path));
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void CreateNonExistingSubFolderTest()
        {
            var ftpesTestHost = TestHost.Get("ftpes");
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential))
            {
                var parent = "/tmp/" + Guid.NewGuid().ToString("N");
                var child = parent + "/" + Guid.NewGuid().ToString("N");
                var reply = ftpClient.SendSingleCommand("MKD", child);
                Assert.AreEqual(550, reply.Code.Code);
                ftpClient.SendSingleCommand("RMD", parent);
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftpes")]
        public void CreateFolderTwiceTest()
        {
            var testHost = TestHost.Get("ftpes");
            var client = new FtpClient(testHost.Uri, testHost.Credential);
            using (var ftpClient = client)
            {
                var path = "/tmp/" + Guid.NewGuid().ToString("N");
                var reply = ftpClient.SendSingleCommand("MKD", path);
                var reply2 = ftpClient.SendSingleCommand("MKD", path);
                Assert.AreEqual(550, reply2.Code.Code);
                ftpClient.Rmd(path);
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void FileExistsTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                const string directory = "/lib/";
                var list = ftpClient.StatEntries(directory).ToList();
                var oneFile = list.First(e => e.Type == FtpEntryType.File);
                var oneDirectory = list.First(e => e.Type == FtpEntryType.Directory);
                var oneLink = list.First(e => e.Type == FtpEntryType.Link);

                var oneFileEntry = ftpClient.GetEntry(directory + oneFile.Name);
                Assert.IsNotNull(oneFileEntry);
                Assert.AreEqual(FtpEntryType.File, oneFileEntry.Type);
                var oneDirectoryEntry = ftpClient.GetEntry(directory + oneDirectory.Name);
                Assert.IsNotNull(oneDirectoryEntry);
                Assert.AreEqual(FtpEntryType.Directory, oneDirectoryEntry.Type);
                var oneLinkEntry = ftpClient.GetEntry(directory + oneLink.Name);
                Assert.IsNotNull(oneLinkEntry);
                Assert.AreEqual(FtpEntryType.Link, oneLinkEntry.Type);
            }
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftps")]
        public void FtpsListTest()
        {
            ListTest(true, protocol: "ftps");
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftps")]
        public void FtpsCreateFileTest()
        {
            CreateFileTest(true, protocol: "ftps");
        }
        
        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void MlstTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var m = ftpClient.Mlst("/");
            }
        }
        
        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void MlstEntryTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var e = ftpClient.MlstEntry("/");
            }
        }
        
        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void MlsdTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var list = ftpClient.Mlsd("/").ToList();
            }
        }
        
        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void MlsdEntriesTest()
        {
            var ftpTestHost = TestHost.Get("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var list = ftpClient.MlsdEntries("/").ToList();
            }
        }
    }
}
