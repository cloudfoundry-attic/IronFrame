using System;
using System.IO;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Warden.Containers;
using Xunit;

namespace IronFoundry.Warden.Test.Acceptance
{
    public class ContainerManagerAcceptanceTests : IDisposable
    {
        private LocalUserGroupManager userGroupManager;

        public string SecurityGroupName { get; set; }
        public string TempDirectory { get; set; }
        ContainerManager ContainerManager { get; set; }

        public ContainerManagerAcceptanceTests()
        {
            ContainerManager = new ContainerManager();

            var uniqueId = Guid.NewGuid().ToString("N");

            SecurityGroupName = "ContainerUsers_" + uniqueId;

            userGroupManager = new LocalUserGroupManager();
            userGroupManager.CreateLocalGroup(SecurityGroupName);

            TempDirectory = Path.Combine(Path.GetTempPath(), "Containers_" + uniqueId);
            Directory.CreateDirectory(TempDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(TempDirectory, true);
            }
            finally
            {
                userGroupManager.DeleteLocalGroup(SecurityGroupName);
            }
        }

        public class RestoreContainers : ContainerManagerAcceptanceTests
        {
            public string Handle { get; set; }
            string ContainerPath { get; set; }

            public RestoreContainers()
            {
                Handle = new ContainerHandle().ToString();
                ContainerPath = Path.Combine(TempDirectory, Handle);

                Directory.CreateDirectory(ContainerPath);
            }

            [FactAdminRequired]
            public void CanRestoreCreatedContainer()
            {
                ContainerManager.RestoreContainers(TempDirectory, SecurityGroupName);

                var client = ContainerManager.GetContainer(Handle);

                Assert.NotNull(client);
            }

            [FactAdminRequired]
            public async Task RestoredCanBeDestroyed()
            {
                ContainerManager.RestoreContainers(TempDirectory, SecurityGroupName);

                var client = ContainerManager.GetContainer(Handle);

                await client.Destroy();

                Assert.False(Directory.Exists(ContainerPath));
            }
        }
    }
}
