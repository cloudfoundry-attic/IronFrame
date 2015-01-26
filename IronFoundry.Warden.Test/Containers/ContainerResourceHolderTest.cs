using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.PInvoke;
using IronFoundry.Warden.Utilities;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Warden.Test
{
    public class ContainerResourceHolderTest
    {
        public class ResourceHolderContext : IDisposable
        {
            protected readonly IWardenConfig wardenConfig;
            protected string tempDir;

            public ResourceHolderContext()
            {
                wardenConfig = Substitute.For<IWardenConfig>();
                tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                wardenConfig.ContainerBasePath.Returns(tempDir);
            }

            virtual public void Dispose()
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public class MockedResourceContext
        {
            protected ContainerHandle handle;
            protected IContainerUser user;
            protected IContainerDirectory directory;
            protected JobObject jobObject;
            protected ILocalTcpPortManager localTcpManager;

            public MockedResourceContext()
            {
                handle = Substitute.For<ContainerHandle>();
                user = Substitute.For<IContainerUser>();
                directory = Substitute.For<IContainerDirectory>();
                jobObject = Substitute.For<JobObject>();
                localTcpManager = Substitute.For<ILocalTcpPortManager>();
            }
        }

        public class GivenDestroyedHolder : MockedResourceContext
        {
            [Fact]
            public void TerminatesJobObjectProcesses()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);

                resourceHolder.Destroy();

                jobObject.ReceivedWithAnyArgs().TerminateProcessesAndWait();
            }

            [Fact]
            public void DisposesJobObject()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);

                resourceHolder.Destroy();

                jobObject.Received().Dispose();
            }

            [Fact]
            public void RequestsRemoveUser()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);

                resourceHolder.Destroy();

                user.Received().Delete();
            }

            [Fact]
            public void RequestsDeleteDirectory()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);

                resourceHolder.Destroy();

                directory.Received().Delete();
            }

            [Fact]
            public void RequestsReleasePortIfPortAssigned()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true) { AssignedPort = 8888 };

                resourceHolder.Destroy();

                localTcpManager.Received().ReleaseLocalPort(Arg.Any<int>(), Arg.Any<string>());
            }

            [Fact]
            public void DoesNotTryToReleasePortIfNoPortAssigned()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);

                resourceHolder.Destroy();

                localTcpManager.DidNotReceive().ReleaseLocalPort(Arg.Any<int>(), Arg.Any<string>());
            }

            [Fact]
            public void DeletesOtherResources_WhenDeleteDirectoryThrows()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);
                resourceHolder.AssignedPort = 8888;

                directory.When(x => x.Delete()).Do(x => { throw new IOException(); });

                resourceHolder.Destroy();

                user.Received(1, x => x.Delete());
                jobObject.Received(1, x => x.TerminateProcessesAndWait(Arg.Any<int>()));
                jobObject.Received(1, x => x.Dispose());
                localTcpManager.Received(1, x => x.ReleaseLocalPort(Arg.Any<int>(), Arg.Any<string>()));
            }

            [Fact]
            public void DeletesOtherResources_WhenDeleteUserThrows()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);
                resourceHolder.AssignedPort = 8888;

                user.When(x => x.Delete()).Do(x => { throw new System.DirectoryServices.DirectoryServicesCOMException(); });

                resourceHolder.Destroy();

                directory.Received(1, x => x.Delete());
                jobObject.Received(1, x => x.TerminateProcessesAndWait(Arg.Any<int>()));
                jobObject.Received(1, x => x.Dispose());
                localTcpManager.Received(1, x => x.ReleaseLocalPort(Arg.Any<int>(), Arg.Any<string>()));
            }

            [Fact]
            public void DeletesOtherResources_WhenReleaseLocalPortThrows()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, true);
                localTcpManager.When(x => x.ReleaseLocalPort(Arg.Any<int>(), Arg.Any<string>())).Do(x => { throw new Exception(); });

                resourceHolder.Destroy();

                directory.Received(1, x => x.Delete());
                jobObject.Received(1, x => x.TerminateProcessesAndWait(Arg.Any<int>()));
                jobObject.Received(1, x => x.Dispose());
                user.Received(1, x => x.Delete());
            }

            [Fact]
            public void WhenDoNotDeleteDirectorySpecified_DoesNotInvokeDirectoryDelete()
            {
                var resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager, false);

                resourceHolder.Destroy();

                directory.DidNotReceive(x => x.Delete());
            }
        }
    }
}
