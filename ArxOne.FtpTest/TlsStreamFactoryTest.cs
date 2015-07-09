#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion
namespace ArxOne.FtpTest
{
    using Ftp;
    using Ftp.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///This is a test class for TlsStreamFactoryTest and is intended
    ///to contain all TlsStreamFactoryTest Unit Tests
    ///</summary>
    [TestClass]
    public class TlsStreamFactoryTest
    {
        [TestMethod]
        [TestCategory("Consistency")]
        public void GetTlsStreamTypeTest()
        {
            var type = TlsStreamFactory.TlsStreamType;
            Assert.IsNotNull(type);
        }
    }
}
