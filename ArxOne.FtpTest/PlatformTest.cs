#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.FtpTest
{
    using System;
    using System.Linq;
    using System.Security.Authentication;
    using Ftp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    internal static class PlatformTest
    {
        public static void SpaceNameTest(string platform, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null, bool useStatInsteadOfList = false)
        {
            if (string.Equals(platform, "Cerberus", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("Cerberus does not return correct LIST");
            NameTest(platform, "A and B", "C and D", protocol, protection, sslProtocols, useStatInsteadOfList);
        }

        public static void BracketNameTest(string platform, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null, bool useStatInsteadOfList = false)
        {
            if (string.Equals(platform, "Cerberus", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("Cerberus does not return correct LIST");
            NameTest(platform, "X[]Y", "Z{}[]T", protocol, protection, sslProtocols, useStatInsteadOfList);
        }

        public static void ParenthesisNameTest(string platform, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null, bool useStatInsteadOfList = false)
        {
            if (string.Equals(platform, "Cerberus", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("Cerberus does not return correct LIST");
            NameTest(platform, "i()j", "k()l", protocol, protection, sslProtocols, useStatInsteadOfList);
        }

        private static void NameTest(string platform, string folderName, string childName, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null, bool useStatInsteadOfList = false)
        {
            if (useStatInsteadOfList && string.Equals(platform, "FileZilla", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("FileZilla does not support STAT command (and yes, it totally blows)");
            var testHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(testHost.Uri, testHost.Credential, new FtpClientParameters { ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                var folder = (ftpClient.ServerType == FtpServerType.Windows ? "/" : "/tmp/") + folderName;
                var file = folder + "/" + childName;
                try
                {
                    ftpClient.Mkd(folder);
                    using (var s = ftpClient.Stor(file))
                        s.WriteByte(123);

                    if (useStatInsteadOfList)
                    {
                        var c2 = ftpClient.StatEntries(folder).SingleOrDefault();
                        Assert.IsNotNull(c2);
                        Assert.AreEqual(childName, c2.Name);
                    }
                    else
                    {
                        var c = ftpClient.ListEntries(folder).SingleOrDefault();
                        Assert.IsNotNull(c);
                        Assert.AreEqual(childName, c.Name);
                    }

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

        public static void CreateReadTwiceTest(string platform, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            var testHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(testHost.Uri, testHost.Credential, new FtpClientParameters { ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                var folder = ftpClient.ServerType == FtpServerType.Windows ? "/" : "/tmp/";
                var file = folder + "/" + Guid.NewGuid().ToString("N");
                try
                {
                    using (var s = ftpClient.Stor(file))
                        s.WriteByte(56);

                    using (var r = ftpClient.Retr(file))
                    {
                        Assert.AreEqual(56, r.ReadByte());
                        Assert.AreEqual(-1, r.ReadByte());
                    }
                    using (var r2 = ftpClient.Retr(file))
                    {
                        Assert.AreEqual(56, r2.ReadByte());
                        Assert.AreEqual(-1, r2.ReadByte());
                    }
                }
                finally
                {
                    ftpClient.Dele(file);
                }
            }
        }

        public static void ListTest(string platform, bool passive, string protocol = "ftp", string directory = "/", bool directoryExists = true, FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            if (!directoryExists && string.Equals(platform, "PureFTPd", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("PureFTPd always gives a valid response, even if the directory does not exist");
            if (string.Equals(platform, "Cerberus", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("Cerberus does not return correct LIST");

            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                //if (string.Equals(platform, "FileZilla", StringComparison.InvariantCultureIgnoreCase)
                //    && ftpClient.Protocol != FtpProtocol.Ftp)
                //    Assert.Inconclusive("FileZilla causes me problems that I don't understand here (help welcome)");

                var list = ftpClient.ListEntries(directory);
                // a small requirement: have a /tmp folderS
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        public static void StatTest(string platform, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            if (string.Equals(platform, "FileZilla", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("FileZilla does not support STAT command (and yes, it just sucks)");
            if (string.Equals(platform, "Cerberus", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("Cerberus thinks STAT is for itself");
            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                var list = ftpClient.StatEntries("/");
                // a small requirement: have a /tmp folderS
                Assert.IsTrue(list.Any(e => e.Name == "tmp"));
            }
        }

        public static void StatNoDotTest(string platform, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            if (string.Equals(platform, "FileZilla", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("FileZilla does not support STAT command (and yes, it just sucks)");
            if (string.Equals(platform, "Cerberus", StringComparison.InvariantCultureIgnoreCase))
                Assert.Inconclusive("Cerberus thinks STAT is for itself");
            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                var list = ftpClient.StatEntries("/");
                Assert.IsFalse(list.Any(e => e.Name == "." || e.Name == ".."));
            }
        }

        public static void CreateFileTest(string platform, bool passive, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            var ftpesTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpesTestHost.Uri, ftpesTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection, SslProtocols = sslProtocols }))
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

        public static void MlstTest(string platform, bool passive = true, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                ExpectFeature(ftpClient, "MLST");
                var m = ftpClient.Mlst("/");
            }
        }

        public static void MlstEntryTest(string platform, bool passive = true, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                ExpectFeature(ftpClient, "MLST");
                var e = ftpClient.MlstEntry("/");
            }
        }

        public static void MlsdTest(string platform, bool passive = true, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                ExpectFeature(ftpClient, "MLSD");
                var list = ftpClient.Mlsd("/").ToList();
            }
        }

        public static void MlsdEntriesTest(string platform, bool passive = true, string protocol = "ftp", FtpProtection? protection = null, SslProtocols? sslProtocols = null)
        {
            var ftpTestHost = TestHost.Get(protocol, platform);
            using (var ftpClient = new FtpClient(ftpTestHost.Uri, ftpTestHost.Credential, new FtpClientParameters { Passive = passive, ChannelProtection = protection, SslProtocols = sslProtocols }))
            {
                ExpectFeature(ftpClient, "MLSD");
                var list = ftpClient.MlsdEntries("/").ToList();
            }
        }

        private static void ExpectFeature(FtpClient ftpClient, string feature)
        {
            if (!ftpClient.ServerFeatures.HasFeature(feature))
                Assert.Inconclusive("This server does not support {0} feature", feature);
        }
    }
}
