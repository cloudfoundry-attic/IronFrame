using IronFoundry.Warden.Containers;
using Xunit;

namespace IronFoundry.Warden.Test
{
    public class ContainerHandleTests
    {
        [Theory]
        [InlineData(1369422213166305, "16tfdq90d71")]
        [InlineData(1369422215826708, "16tfdqbhj8k")]
        [InlineData(1369422411336831, "16tfe06033v")]
        public void GivenInputData_GeneratesExpectedID(long input, string expectedID)
        {
            var handle = new ContainerHandle(input);
            Assert.Equal<string>(expectedID, handle);
        }
    }
}
