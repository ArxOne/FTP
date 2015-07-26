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
    using System.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [DebuggerDisplay("{HostType}-->{Uri}")]
    public class TestHost
    {
        public string HostType { get; set; }
        public Uri Uri { get; set; }
        public NetworkCredential Credential { get; set; }

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
                for (;;)
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        yield break;

                    line = line.Trim();
                    if (line == "")
                        continue;

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

        public static TestHost Get(string protocol, string platform = null)
        {
            var t = EnumerateCredentials().FirstOrDefault(c => string.Equals(c.Uri.Scheme, protocol, StringComparison.InvariantCultureIgnoreCase)
            && (platform == null || string.Equals(c.HostType, platform, StringComparison.InvariantCultureIgnoreCase)));
            if (t == null)
            {
                if (platform == null)
                    Assert.Inconclusive("Found no configuration for protocol '{0}'", protocol);
                else
                    Assert.Inconclusive("Found no configuration for protocol '{0}' and host type '{1}'", protocol, platform);
            }
            return t;
        }
    }
}
