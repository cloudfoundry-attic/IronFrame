using IronFoundry.Warden.Containers;
using Xunit;

namespace IronFoundry.Warden.Test
{
    using System;

    public class ContainerHandleTests
    {
        [Theory]
        [InlineData(1, "idq1ypm7dyb")]
        [InlineData(2, "1of9dl2qia1")]
        public void GeneratesIdFromRandomGenerator(int input, string expectedId)
        {
            var handle = new ContainerHandle(new Random(input));
            Assert.Equal<string>(expectedId, handle);
        }

        [Fact]
        public void GeneratesUniqueIds()
        {
            var handle1 = new ContainerHandle();
            var handle2 = new ContainerHandle();

            Assert.Equal(11, handle1.ToString().Length);
            Assert.Equal(11, handle2.ToString().Length);
            Assert.NotEqual(handle1, handle2);
        }
    }
}
