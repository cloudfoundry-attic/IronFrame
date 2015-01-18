using System;
using Xunit;

namespace IronFoundry.Container
{
    public class ContainerHostServiceTests
    {
        ContainerHostService Service { get; set; }
        
        public ContainerHostServiceTests()
        {
            Service = new ContainerHostService();
        }

        [Fact]
        public void CanStartContainerHost()
        {
            IContainerHostClient client = null;

            try
            {
                client = Service.StartContainerHost("JobObjectName", null);
                Assert.True(client.Ping(TimeSpan.FromMilliseconds(1000)));
            }
            finally
            {
                client.Shutdown();
            }
        }
    }
}
