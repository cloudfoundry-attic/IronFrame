using System.Runtime.InteropServices;
using Xunit;

namespace IronFoundry.Container.Win32
{
    public class SafeAllocationTests
    {
        [Fact]
        public void CanAllocateEnums()
        {
            using (SafeAllocation.Create<ACCESS_MASK>())
            {
                // NO NEED TO DO ANYTHING
            }
        }

        [Fact]
        public void CanGetBackEnumValue()
        {
            using (var allocation = SafeAllocation.Create<ACCESS_MASK>())
            {
                Marshal.WriteInt32(allocation.DangerousGetHandle(), (int) ACCESS_MASK.DELETE);
                var actual = (ACCESS_MASK) allocation.Read<uint>(0);

                Assert.Equal(ACCESS_MASK.DELETE, actual);
            }
        }
    }
}
