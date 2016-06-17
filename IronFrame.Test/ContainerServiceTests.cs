using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DiskQuotaTypeLibrary;
using IronFrame.Utilities;
using NSubstitute;
using NSubstitute.Core.Arguments;
using Xunit;

namespace IronFrame
{
    public class ContainerServiceTests
    {
        IContainerDirectory containerDirectory;
        string ContainerBasePath { get; set; }
        string ContainerUserGroup { get; set; }
        IFileSystemManager FileSystem { get; set; }
        ContainerHandleHelper HandleHelper { get; set; }
        IProcessRunner ProcessRunner { get; set; }
        IContainerHostService ContainerHostService { get; set; }
        IContainerHostClient ContainerHostClient { get; set; }
        IContainerPropertyService ContainerPropertiesService { get; set; }
        IUserManager UserManager { get; set; }
        ILocalTcpPortManager TcpPortManager { get; set; }
        ContainerService Service { get; set; }
        IDiskQuotaManager diskQuotaManager { get; set; }
        IContainerDiskQuota containerDiskQuota { get; set; }
        public string Id { get; set; }
        string sid { get; set; }

        class TestContainerFactory : IContainerFactory
        {
            private int destroyCount = 0;

            public IContainer CreateContainer(string id,
                string handle,
                IContainerUser user,
                IContainerDirectory directory,
                IContainerPropertyService propertyService,
                ILocalTcpPortManager tcpPortManager,
                JobObject jobObject,
                IContainerDiskQuota containerDiskQuota,
                IProcessRunner processRunner,
                IProcessRunner constrainedProcessRunner,
                ProcessHelper processHelper,
                Dictionary<string, string> defaultEnvironment,
                ContainerHostDependencyHelper dependencyHelper)
            {
                if (handle == "KnownBadHandle")
                {
                    var badContainer = Substitute.For<IContainer>();
                    badContainer.Handle.Returns("KnownBadHandle");
                    badContainer.When(x => x.Destroy()).Do(x => { if (destroyCount++ == 0) throw new Exception(); });

                    return badContainer;
                }
                else
                {
                    return new Container(
                        id,
                        handle,
                        user,
                        directory,
                        propertyService,
                        tcpPortManager,
                        jobObject,
                        containerDiskQuota,
                        processRunner,
                        constrainedProcessRunner,
                        processHelper,
                        defaultEnvironment,
                        dependencyHelper
                    );
                }
            }
        }

        public ContainerServiceTests()
        {
            ContainerBasePath = @"C:\Containers";
            ContainerUserGroup = "ContainerUsers";

            ContainerPropertiesService = Substitute.For<IContainerPropertyService>();

            FileSystem = Substitute.For<IFileSystemManager>();

            Id = "DEADBEEF";

            HandleHelper = Substitute.For<ContainerHandleHelper>();
            HandleHelper.GenerateId(null).ReturnsForAnyArgs(Id);

            ProcessRunner = Substitute.For<IProcessRunner>();
            TcpPortManager = Substitute.For<ILocalTcpPortManager>();
            UserManager = Substitute.For<IUserManager>();

            ContainerHostClient = Substitute.For<IContainerHostClient>();

            ContainerHostService = Substitute.For<IContainerHostService>();
            ContainerHostService.StartContainerHost(null, null, null, null)
                .ReturnsForAnyArgs(ContainerHostClient);

            UserManager.CreateUser(null).ReturnsForAnyArgs(new NetworkCredential("username", "password"));
            sid = "S-1234";
            UserManager.GetSID(null).ReturnsForAnyArgs(sid);

            diskQuotaManager = Substitute.For<IDiskQuotaManager>();
            containerDiskQuota = Substitute.For<IContainerDiskQuota>();
            diskQuotaManager.CreateDiskQuotaControl(null, "").ReturnsForAnyArgs(containerDiskQuota);

            var directoryFactory = Substitute.For<IContainerDirectoryFactory>();
            containerDirectory = Substitute.For<IContainerDirectory>();
            directoryFactory.Create(FileSystem, ContainerBasePath, Id).Returns(containerDirectory);

            var containerFactory = new TestContainerFactory();

            Service = new ContainerService(
                HandleHelper,
                UserManager,
                FileSystem,
                ContainerPropertiesService,
                TcpPortManager,
                ProcessRunner,
                ContainerHostService,
                diskQuotaManager,
                directoryFactory,
                containerFactory,
                ContainerBasePath
            );
        }

        public class CreateContainer : ContainerServiceTests
        {
            [Fact]
            public void WhenSpecIsNull_Throws()
            {
                var ex = Record.Exception(() => Service.CreateContainer(null));

                Assert.IsAssignableFrom<ArgumentException>(ex);
            }

            [Fact]
            public void UsesProvidedHandle()
            {
                var spec = new ContainerSpec
                {
                    Handle = "container-handle",
                };

                var container = Service.CreateContainer(spec);

                Assert.Equal("container-handle", container.Handle);
            }

            [InlineData(null)]
            [InlineData("")]
            [Theory]
            public void WhenHandleIsNotProvided_GeneratesHandle(string handle)
            {
                var expectedHandle = Guid.NewGuid().ToString("N");
                HandleHelper.GenerateHandle().Returns(expectedHandle);
                var spec = new ContainerSpec
                {
                    Handle = handle,
                };

                var container = Service.CreateContainer(spec);

                Assert.NotEqual(handle, container.Handle);
                Assert.Equal(expectedHandle, container.Handle);
            }

            [Fact]
            public void GeneratesIdFromHandle()
            {
                HandleHelper.GenerateId("handle").Returns("derived-id");
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                var container = Service.CreateContainer(spec);

                Assert.Equal("derived-id", container.Id);
            }


            [Fact]
            public void CreatesContainerSpecificUser()
            {
                UserManager.CreateUser("").ReturnsForAnyArgs(new NetworkCredential());
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                Service.CreateContainer(spec);

                UserManager.Received(1).CreateUser("c_DEADBEEF");
            }

            [Fact]
            public void CreatesContainerSpecificDirectory()
            {
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                Service.CreateContainer(spec);

                containerDirectory.Received().CreateSubdirectories(Arg.Any<IContainerUser>());
            }

            [Fact]
            public void CreatesBindMounts()
            {
                var bindMounts = new[]
                {
                    new BindMount()
                };
                var spec = new ContainerSpec
                {
                    BindMounts = bindMounts
                };
                Service.CreateContainer(spec);
                containerDirectory.Received().CreateBindMounts(bindMounts, Arg.Any<IContainerUser>());
            }

            [Fact]
            public void GenerateDiskQuotaControlUsingTheContainerDirectory()
            {
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                Service.CreateContainer(spec);
                diskQuotaManager.Received().CreateDiskQuotaControl(containerDirectory, sid);
            }


            [Fact]
            public void CreatesContainerSpecificHost()
            {
                var expectedCredentials = new NetworkCredential();
                UserManager.CreateUser("").ReturnsForAnyArgs(expectedCredentials);
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                Service.CreateContainer(spec);

                ContainerHostService.Received(1).StartContainerHost(Arg.Any<string>(), Arg.Any<IContainerDirectory>(), Arg.Any<JobObject>(), expectedCredentials);
            }

            [Fact]
            public void SetsProperties()
            {
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                    Properties = new Dictionary<string,string>
                    {
                        { "name1", "value1" },
                        { "name2", "value2" },
                    },
                };

                var container = Service.CreateContainer(spec);

                ContainerPropertiesService.Received(1).SetProperties(container, spec.Properties);
            }

            [Fact]
            public void CleansUpWhenItFails()
            {
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                ContainerHostService.StartContainerHost(null, null, null, null)
                    .ThrowsForAnyArgs(new Exception());

                try
                {
                    Service.CreateContainer(spec);
                }
                catch (Exception)
                {
                    // Expect this exception.
                }

                // Created and deleted the user
                UserManager.Received(1).CreateUser(Arg.Any<string>());
                UserManager.Received(1).DeleteUser(Arg.Any<string>());
                UserManager.Received(1).CreateProfile(Arg.Any<string>());
                UserManager.Received(1).DeleteProfile(Arg.Any<string>());

                containerDirectory.Received().Destroy();
            }
        }

        public class RestoreContainer : ContainerServiceTests
        {
            [Fact]
            public void RestoresContainerForEachDirectory()
            {
                var containerPaths = new string[]
                {
                    ContainerBasePath + "\\Container1",
                    ContainerBasePath + "\\Container2",
                };
                FileSystem.EnumerateDirectories(ContainerBasePath)
                    .Returns(containerPaths);

                Service.RestoreFromContainerBasePath();

                Assert.Collection(Service.GetContainers(),
                    x =>
                    {
                        Assert.Equal("Container1", x.Id);
                        Assert.Equal(ContainerBasePath + "\\Container1", x.Directory.RootPath);
                    },
                    x =>
                    {
                        Assert.Equal("Container2", x.Id);
                        Assert.Equal(ContainerBasePath + "\\Container2", x.Directory.RootPath);
                    });
            }
        }

        public class WithContainer : ContainerServiceTests
        {
            string Handle { get; set; }
            IContainer Container { get; set; }
            
            public WithContainer()
            {
                Handle = "KnownHandle";
                var spec = new ContainerSpec
                {
                    Handle = Handle,
                };

                Container = Service.CreateContainer(spec);
            }

            public class GetContainers : WithContainer
            {
                [Fact]
                public void CreateShouldAddToTheList()
                {
                    var containers = Service.GetContainers();
                    Assert.Collection(containers,
                        x => Assert.Same(Container, x)
                    );
                }
            }

            public class GetContainerByHandle : WithContainer
            {
                [Fact]
                public void CanGetContainerByHandle()
                {
                    var container = Service.GetContainerByHandle(Handle);

                    Assert.Same(Container, container);
                }

                [Fact]
                public void IsNotCaseSensitive()
                {
                    var container = Service.GetContainerByHandle("knOwnhAndlE");

                    Assert.Same(Container, container);
                }

                [Fact]
                public void WhenHandleDoesNotExist_ReturnsNull()
                {
                    var container = Service.GetContainerByHandle("UnknownHandle");

                    Assert.Null(container);
                }
            }

            public class DestroyContainer : WithContainer
            {
                [Fact]
                public void CanDestroyEnumeratedContainers()
                {
                    foreach (var container in Service.GetContainers())
                    {
                        Service.DestroyContainer(container.Handle);
                    }
                }

                [Fact]
                public void ContainerIsRemovedFromContainerList()
                {
                    Assert.NotNull(Service.GetContainerByHandle(Container.Handle));
                    Service.DestroyContainer(Container.Handle);
                    Assert.Null(Service.GetContainerByHandle(Container.Handle));
                }

                [Fact]
                public void ContainerIsRemovedFromContainerListOnException()
                {
                    var badSpec = new ContainerSpec
                    {
                        Handle = "KnownBadHandle",
                    };

                    var badContainer = Service.CreateContainer(badSpec);

                    Assert.NotNull(Service.GetContainerByHandle(badContainer.Handle));
                    try
                    {
                        Service.DestroyContainer(badContainer.Handle);
                    } catch (Exception) { }
                    Assert.Null(Service.GetContainerByHandle(badContainer.Handle));
                }

                [Fact]
                public void ContainerRemainsInContainerListForRemoveOnException()
                {
                    var badSpec = new ContainerSpec
                    {
                        Handle = "KnownBadHandle",
                    };

                    var badContainer = Service.CreateContainer(badSpec);

                    Assert.NotNull(Service.GetContainerByHandle(badContainer.Handle));
                    try {
                        Service.DestroyContainer(badContainer.Handle);
                    } catch (Exception) { }
                    Assert.NotNull(Service.GetContainerByHandleIncludingDestroyed(badContainer.Handle));
                }

                [Fact]
                public void ContainerRemainsInContainerListForRemoveOnExceptionUntilSuccess()
                {
                    var badSpec = new ContainerSpec
                    {
                        Handle = "KnownBadHandle",
                    };

                    var badContainer = Service.CreateContainer(badSpec);

                    Assert.NotNull(Service.GetContainerByHandle(badContainer.Handle));
                    try {
                        Service.DestroyContainer(badContainer.Handle);
                    } catch (Exception) { }
                    Assert.NotNull(Service.GetContainerByHandleIncludingDestroyed(badContainer.Handle));

                    Service.DestroyContainer(badContainer.Handle);
                    Assert.Null(Service.GetContainerByHandleIncludingDestroyed(badContainer.Handle));
                }
            }
        }

        public class EnvsFromList : ContainerServiceTests
        {
            [Fact]
            public void ShouldConvertEnvsFromEmptyList()
            {
                var list = new List<string>() {};

                var translation = ContainerService.EnvsFromList(list);
                Assert.Equal(translation.Count, 0);
            }

            [Fact]
            public void ShouldConvertEnvsFromList()
            {
                var list = new List<string>()
                {
                    "a=b",
                    "test=1234",
                    "my=varwith=init"
                };

                var translation = ContainerService.EnvsFromList(list);
                Assert.Equal(translation.Count, list.Count);
                Assert.Equal(translation["a"], "b");
                Assert.Equal(translation["test"], "1234");
                Assert.Equal(translation["my"], "varwith=init");
            }
        }

    }
}
