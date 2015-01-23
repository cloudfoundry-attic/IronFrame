using System;
using System.ComponentModel;
using System.Net;
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
            var jobObjectName = Guid.NewGuid().ToString("N");
            try
            {
                jobObject = new JobObject(jobObjectName);

                client = Service.StartContainerHost(jobObject, null);
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

        // TODO: Figure out a reliable way to make the host fail to start...
        //[Fact]
        //public void WhenContainerHostFailsToStart_Throws()
        //{
        //    IContainerHostClient client = null;
        //    try
        //    {
        //        var ex = Record.Exception(() => client = Service.StartContainerHost("NonExistentJobName", null));
                
        //        Assert.NotNull(ex);
        //        Assert.Contains("Unable to open job object with name 'NonExistentJobName'", ex.Message);
        //    }
        //    finally
        //    {
        //        if (client != null)
        //            client.Shutdown();
        //    }
        //}

        [Fact]
        public void WhenCredentialsAreInvalid_Throws()
        {
            IContainerHostClient client = null;
            JobObject jobObject = null;
            var jobObjectName = Guid.NewGuid().ToString("N");
            try
            {
                jobObject = new JobObject(jobObjectName);
                var invalidCredentials = new NetworkCredential("InvalidUserName", "WrongPassword", Environment.MachineName);

                var ex = Record.Exception(() => client = Service.StartContainerHost(jobObject, invalidCredentials));

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
