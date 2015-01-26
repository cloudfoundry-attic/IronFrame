using System;
using System.ComponentModel;
using System.Net;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Containers;
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
            JobObject jobObject = null;
            var containerId = Guid.NewGuid().ToString("N");
            var jobObjectName = containerId;
            try
            {
                jobObject = new JobObject(containerId);

                client = Service.StartContainerHost(containerId, jobObject, null);
                Assert.True(client.Ping(TimeSpan.FromSeconds(5)));
            }
            finally
            {
                if (client != null)
                    client.Shutdown();

                if (jobObject != null)
                    jobObject.Dispose();
            }
        }

        [Fact]
        public void WhenContainerHostFailsToStart_Throws()
        {
            IContainerHostClient client = null;
            JobObject jobObject = null;
            var containerId = Guid.NewGuid().ToString("N");
            try
            {
                jobObject = new JobObject(containerId);
                var ex = Record.Exception(() => client = Service.StartContainerHost("", jobObject, null));
                
                Assert.NotNull(ex);
                Assert.Contains("Must specify container-id as the first argument.", ex.Message);
            }
            finally
            {
                if (client != null)
                    client.Shutdown();

                if (jobObject != null)
                    jobObject.Dispose();
            }
        }

        [Fact]
        public void WhenCredentialsAreInvalid_Throws()
        {
            IContainerHostClient client = null;
            JobObject jobObject = null;
            var containerId = Guid.NewGuid().ToString("N");
            try
            {
                jobObject = new JobObject(containerId);
                var invalidCredentials = new NetworkCredential("InvalidUserName", "WrongPassword", Environment.MachineName);

                var ex = Record.Exception(() => client = Service.StartContainerHost(containerId, jobObject, invalidCredentials));

                Assert.IsAssignableFrom<Win32Exception>(ex);
            }
            finally
            {
                if (client != null)
                    client.Shutdown();

                if (jobObject != null)
                    jobObject.Dispose();
            }
        }
    }
}
