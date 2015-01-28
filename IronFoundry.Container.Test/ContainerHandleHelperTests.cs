using System;
using Xunit;

namespace IronFoundry.Container
{
    public class ContainerHandleHelperTests
    {
        ContainerHandleHelper Helper { get; set; }

        public ContainerHandleHelperTests()
        {
            Helper = new ContainerHandleHelper();
        }

        public class GenerateHandle : ContainerHandleHelperTests
        {
            [Theory]
            [InlineData(1, "idq1ypm7dyb")]
            [InlineData(2, "1of9dl2qia1")]
            public void GeneratesIdFromRandomGenerator(int input, string expectedId)
            {
                var handle = Helper.GenerateHandle(new Random(input));
                Assert.Equal<string>(expectedId, handle);
            }

            [Fact]
            public void GeneratesUniqueIds()
            {
                var handle1 = Helper.GenerateHandle();
                var handle2 = Helper.GenerateHandle();

                Assert.Equal(11, handle1.ToString().Length);
                Assert.Equal(11, handle2.ToString().Length);
                Assert.NotEqual(handle1, handle2);
            }
        }

        public class GenerateId : ContainerHandleHelperTests
        {
            [Fact]
            public void GeneratesHashOfHandleWithKnownLength()
            {
                var id = Helper.GenerateId(Guid.NewGuid().ToString("N"));

                Assert.Equal(18, id.Length);
            }

            [Fact]
            public void GeneratesUniqueHashesForDifferentHandles()
            {
                var id1 = Helper.GenerateId(Guid.NewGuid().ToString("N"));
                var id2 = Helper.GenerateId(Guid.NewGuid().ToString("N"));

                Assert.NotEqual(id1, id2);
            }
        }
    }
}
