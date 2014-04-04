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

        public class WhenCreatingHolder : ResourceHolderContext
        {
            private IResourceHolder containerResources;
            public WhenCreatingHolder()
            {
                containerResources = ContainerResourceHolder.Create(wardenConfig);
            }

            public override void Dispose()
            {
                var principal = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Machine), containerResources.User.UserName);
                if (principal != null)
                {
                    principal.Delete();
                }

                base.Dispose();
            }

            [FactAdminRequired]
            public void CreateProducesContainerResourcesReference()
            {
                
                Assert.NotNull(containerResources);
            }

            [FactAdminRequired]
            public void CreatesContainerHandle()
            {
                Assert.NotEmpty(containerResources.Handle.ToString());
            }

            [FactAdminRequired]
            public void CreatesUserBasedOnHandle()
            {
                Assert.Equal("warden_" + containerResources.Handle.ToString(), containerResources.User.UserName);
            }

            [FactAdminRequired]
            public void CreateDirectoryForContainer()
            {
                Assert.Equal(Path.Combine(tempDir, containerResources.Handle.ToString()), containerResources.Directory.FullName);
            }

            [FactAdminRequired]
            public void CreatesJobObjectBasedOnHandle()
            {
                using (var jobObjectHandle = new SafeJobObjectHandle(NativeMethods.OpenJobObject(NativeMethods.JobObjectAccessRights.AllAccess, false, containerResources.Handle.ToString())))
                {
                    Assert.False(jobObjectHandle.IsInvalid);
                }
            }
        }

        public class GivenDestroyedHolder
        {
            private ContainerHandle handle;
            private IContainerUser user;
            private IContainerDirectory directory;
            private JobObject jobObject;
            private ContainerResourceHolder resourceHolder;
            private ILocalTcpPortManager localTcpManager;

            public GivenDestroyedHolder()
            {
                handle = Substitute.For<ContainerHandle>();
                user = Substitute.For<IContainerUser>();
                directory = Substitute.For<IContainerDirectory>();
                jobObject = Substitute.For<JobObject>();
                localTcpManager = Substitute.For<ILocalTcpPortManager>();
                
                resourceHolder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager);
                resourceHolder.Destroy();
            }

            [Fact]
            public void TerminatesJobObjectProcesses()
            {
                jobObject.ReceivedWithAnyArgs().TerminateProcessesAndWait();
            }

            [Fact]
            public void DisposesJobObject()
            {
                jobObject.Received().Dispose();
            }

            [Fact]
            public void RequestsRemoveUser()
            {
                user.Received().Delete();
            }

            [Fact]
            public void RequestsDeleteDirectory()
            {
                directory.Received().Delete();
            }

            [Fact]
            public void RequestsReleasePortIfPortAssigned()
            {   
                var holder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager) {AssignedPort = 8888};

                holder.Destroy();

                localTcpManager.Received().ReleaseLocalPort(Arg.Any<ushort>(), Arg.Any<string>());
            }

            [Fact]
            public void DoesNotTryToReleasePortIfNoPortAssigned()
            {
                var holder = new ContainerResourceHolder(handle, user, directory, jobObject, localTcpManager);

                holder.Destroy();

                localTcpManager.DidNotReceive().ReleaseLocalPort(Arg.Any<ushort>(), Arg.Any<string>());
            }
        }
    }
}
