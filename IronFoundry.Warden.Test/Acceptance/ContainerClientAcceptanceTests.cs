using System;
using System.IO;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Containers;
using Xunit;
using IContainer = IronFoundry.Container.IContainer;

namespace IronFoundry.Warden.Test.Acceptance
{
    public class ContainerClientAcceptanceTests : IDisposable
    {
        private LocalUserGroupManager userGroupManager;

        public string SecurityGroupName { get; set; }
        public string TempDirectory { get; set; }
        ContainerService ContainerService { get; set; }

        public ContainerClientAcceptanceTests()
        {
            var uniqueId = Guid.NewGuid().ToString("N");

            SecurityGroupName = "ContainerUsers_" + uniqueId;

            userGroupManager = new LocalUserGroupManager();
            userGroupManager.CreateLocalGroup(SecurityGroupName);

            TempDirectory = Path.Combine(Path.GetTempPath(), "Containers_" + uniqueId);
            Directory.CreateDirectory(TempDirectory);

            ContainerService = new ContainerService(TempDirectory, SecurityGroupName);
        }

        public void Dispose()
        {
            try
            {
                try
                {
                    foreach (var container in ContainerService.GetContainers())
                    {
                        ContainerService.DestroyContainer(container.Handle);
                    }

                    ContainerService.Dispose();
                }
                finally
                {
                    Directory.Delete(TempDirectory, true);
                }
            }
            finally
            {
                userGroupManager.DeleteLocalGroup(SecurityGroupName);
            }
        }

        public class RestoreFromFileSystem : ContainerClientAcceptanceTests
        {
            public string Handle { get; set; }
            public IContainer Container { get; set; }
            string ContainerPath { get; set; }

            public RestoreFromFileSystem()
            {
                Handle = Guid.NewGuid().ToString("N");

                var containerSpec = new ContainerSpec
                {
                    Handle = Handle
                };

                Container = ContainerService.CreateContainer(containerSpec);
                ContainerPath = Path.Combine(TempDirectory, Container.Id);
            }

            [FactAdminRequired]
            public void CanRestoreCreatedContainer()
            {
                IContainerClient client = ContainerClient.RestoreFromFileSystem(ContainerPath);

                Assert.NotNull(client);
            }

            [FactAdminRequired]
            public async Task RestoredCanBeDestroyed()
            {
                Container.Stop(true);
                IContainerClient client = ContainerClient.RestoreFromFileSystem(ContainerPath);
                
                await client.Destroy();

                Assert.False(Directory.Exists(ContainerPath));
            }
        }
    }
}
