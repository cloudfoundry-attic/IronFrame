using DiskQuotaTypeLibrary;
using NSubstitute;
using Xunit;

namespace IronFrame.Utilities
{
    public class ContainerDiskQuotaTests
    {
        private DIDiskQuotaUser quotaUser;
        private DiskQuotaControl diskQuotaControl;
        const string sid = "sid";

        public ContainerDiskQuotaTests()
        {
            diskQuotaControl = Substitute.For<DiskQuotaControl>();
            quotaUser = Substitute.For<DIDiskQuotaUser>();
            diskQuotaControl.FindUser(sid).Returns(quotaUser);
        }

        [Fact]
        public void CanOffsetDirectory()
        {
            var baseQuota = 9000ul;
            var offset = 1024ul;
            quotaUser.QuotaUsed.Returns(offset);

            var containerDiskQuota = new ContainerDiskQuota(diskQuotaControl, sid);

            containerDiskQuota.SetQuotaLimit(baseQuota);
            quotaUser.Received().QuotaLimit = baseQuota + offset;
        }

        [Fact]
        public void ReturnsZeroWhenNoQuotaIsSet()
        {
            quotaUser.QuotaUsed.Returns(1024ul);

            var containerDiskQuota = new ContainerDiskQuota(diskQuotaControl, sid);

            Assert.Equal(0ul, containerDiskQuota.CurrentLimit());
        }

        [Fact]
        public void RemovesOffsetFromCurrentLimit()
        {
            var baseQuota = 9000ul;
            var offset = 1024ul;
            quotaUser.QuotaUsed.Returns(offset);
            quotaUser.QuotaLimit.Returns(baseQuota + offset);

            var containerDiskQuota = new ContainerDiskQuota(diskQuotaControl, sid);

            Assert.Equal(baseQuota, containerDiskQuota.CurrentLimit());
        }

        [Fact]
        public void WhenQuotaUsedFails_ReturnZero()
        {
            quotaUser.QuotaUsed.Throws(new System.Runtime.InteropServices.COMException());
            var containerDiskQuota = new ContainerDiskQuota(diskQuotaControl, sid);

            Assert.Equal(0ul, containerDiskQuota.Usage());
        }
    }
}