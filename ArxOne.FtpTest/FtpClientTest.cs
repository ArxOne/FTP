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

        [DebuggerDisplay("{HostType}-->{Uri}")]
        internal class TestHost
        {
            public string HostType { get; set; }
            public Uri Uri { get; set; }
            public NetworkCredential Credential { get; set; }
        }

        // Credentials.txt is a simple text file with URI (including credentials) formed as follows:
        // - simple uri (such as 'ftp://user:pass@host:21')
        // - specific host type (such as 'win-->ftp://user:pass@host:21').
        // Please also note that first match is returned, so if only a protocol is asked, then any host type may be returned

        private static IEnumerable<TestHost> EnumerateCredentials()
        {
            const string credentialsTxt = "credentials.txt";
            if (!File.Exists(credentialsTxt))
                Assert.Inconclusive("File '{0}' not found", credentialsTxt);
            using (var streamReader = File.OpenText(credentialsTxt))
            {
                for (; ; )
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        yield break;

                    var typeAndUri = line.Split(new[] { "-->" }, 2, StringSplitOptions.RemoveEmptyEntries);
                    string hostType;
                    string uriAndCredentials;

                    if (typeAndUri.Length == 1)
                    {
                        hostType = null;
                        uriAndCredentials = line;
                    }
                    else
                    {
                        hostType = typeAndUri[0];
                        uriAndCredentials = typeAndUri[1];
                    }

                    Uri uri;
                    try
                    {
                        uri = new Uri(uriAndCredentials);
                    }
                    catch (UriFormatException)
                    {
                        continue;
                    }
                    var literalLoginAndPassword = HttpUtility.UrlDecode(uri.UserInfo.Replace("_at_", "@"));
                    var loginAndPassword = literalLoginAndPassword.Split(new[] { ':' }, 2);
                    var networkCredential = loginAndPassword.Length == 2
                        ? new NetworkCredential(loginAndPassword[0], loginAndPassword[1])
                        : CredentialCache.DefaultNetworkCredentials;
                    yield return new TestHost { HostType = hostType, Uri = uri, Credential = networkCredential };
                }
            }
        }

        internal static TestHost GetTestHost(string protocol, string hostType = null)
        {
            var t = EnumerateCredentials().FirstOrDefault(c => c.Uri.Scheme == protocol && (hostType == null || c.HostType == hostType));
            if (t == null)
            {
                if (hostType == null)
                    Assert.Inconclusive("Found no configuration for protocol '{0}'", protocol);
                else
                    Assert.Inconclusive("Found no configuration for protocol '{0}' and host type '{1}'", protocol, hostType);
            }
            return t;
        }

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
            var ftpTestHost = GetTestHost(protocol, hostType);
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
            var ftpTestHost = GetTestHost("ftp");
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
            var ftpTestHost = GetTestHost("ftp");
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var ftpesTestHost = GetTestHost(protocol, hostType);
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var ftpesTestHost = GetTestHost("ftpes");
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
            var testHost = GetTestHost("ftpes");
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
            var ftpTestHost = GetTestHost("ftp");
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
        [TestCategory("Ftp")]
        public void SpaceNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp"), "A and B", "C and D");
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void BracketsNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp"), "X[]Y", "Z{}[]T");
        }

        [TestMethod]
        [TestCategory("RequireHost")]
        [TestCategory("Ftp")]
        public void ParenthesisNameTest()
        {
            FolderAndChildTest(GetTestHost("ftp"), "i()j", "k()l");
        }

        private void FolderAndChildTest(TestHost testHost, string folderName, string childName)
        {
            using (var ftpClient = new FtpClient(testHost.Uri, testHost.Credential))
            {
                var folder = (ftpClient.ServerType == FtpServerType.Windows ? "/" : "/tmp/") + folderName;
                var file = folder + "/" + childName;
                try
                {
                    ftpClient.Mkd(folder);
                    using (var s = ftpClient.Stor(file))
                        s.WriteByte(123);

                    var c = ftpClient.ListEntries(folder).SingleOrDefault();
                    Assert.IsNotNull(c);
                    Assert.AreEqual(childName, c.Name);
                    var c2 = ftpClient.StatEntries(folder).SingleOrDefault();
                    Assert.IsNotNull(c2);
                    Assert.AreEqual(childName, c2.Name);

                    using (var r = ftpClient.Retr(file))
                    {
                        Assert.AreEqual(123, r.ReadByte());
                        Assert.AreEqual(-1, r.ReadByte());
                    }
                }
                finally
                {
                    ftpClient.Dele(file);
                    ftpClient.Rmd(folder);
                }
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
            var ftpTestHost = GetTestHost("ftp");
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
            var ftpTestHost = GetTestHost("ftp");
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
            var ftpTestHost = GetTestHost("ftp");
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
            var ftpTestHost = GetTestHost("ftp");
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential))
            {
                var list = ftpClient.MlsdEntries("/").ToList();
            }
        }
    }
}
