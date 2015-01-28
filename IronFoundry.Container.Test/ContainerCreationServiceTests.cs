using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronFoundry.Warden.Containers;
using IronFoundry.Container.Utilities;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container
{
    public class ContainerCreationServiceTests
    {
        string ContainerBasePath { get; set; }
        string ContainerUserGroup { get; set; }
        FileSystemManager FileSystem { get; set; }
        ContainerHandleHelper HandleHelper { get; set; }
        IProcessRunner ProcessRunner { get; set; }
        IContainerHostService ContainerHostService { get; set; }
        IContainerHostClient ContainerHostClient { get; set; }
        IUserManager UserManager { get; set; }
        ILocalTcpPortManager TcpPortManager { get; set; }
        ContainerCreationService Service { get; set; }

        public ContainerCreationServiceTests()
        {
            ContainerBasePath = @"C:\Containers";
            ContainerUserGroup = "ContainerUsers";

            FileSystem = Substitute.For<FileSystemManager>();
            HandleHelper = Substitute.For<ContainerHandleHelper>();
            ProcessRunner = Substitute.For<IProcessRunner>();
            TcpPortManager = Substitute.For<ILocalTcpPortManager>();
            UserManager = Substitute.For<IUserManager>();

            ContainerHostClient = Substitute.For<IContainerHostClient>();

            ContainerHostService = Substitute.For<IContainerHostService>();
            ContainerHostService.StartContainerHost(null, null, null, null)
                .ReturnsForAnyArgs(ContainerHostClient);

            UserManager.CreateUser(null).ReturnsForAnyArgs(new NetworkCredential("username", "password"));

            Service = new ContainerCreationService(HandleHelper, UserManager, FileSystem, TcpPortManager, ProcessRunner, ContainerHostService, ContainerBasePath);
        }

        public class CreateContainer : ContainerCreationServiceTests
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
                HandleHelper.GenerateHandle().Returns(Guid.NewGuid().ToString("N"));
                var spec = new ContainerSpec
                {
                    Handle = handle,
                };

                var container = Service.CreateContainer(spec);

                Assert.NotNull(container.Handle);
                Assert.NotEmpty(container.Handle);
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
                HandleHelper.GenerateId("handle").Returns("DEADBEEF");
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
                HandleHelper.GenerateId("handle").Returns("DEADBEEF");
                var spec = new ContainerSpec
                {
                    Handle = "handle",
                };

                Service.CreateContainer(spec);

                var expectedPath = Path.Combine(ContainerBasePath, "DEADBEEF");
                FileSystem.Received(1).CreateDirectory(expectedPath, Arg.Any<IEnumerable<UserAccess>>());
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

            public class AcceptanceFixture : IDisposable
            {
                private LocalUserGroupManager userGroupManager;

                public string TempDirectory { get; set; }
                public string SecurityGroupName { get; set; }

                public AcceptanceFixture()
                {
                    userGroupManager = new LocalUserGroupManager();

                    var uniqueId = Guid.NewGuid().ToString("N");

                    SecurityGroupName = "ContainerUsers_" + uniqueId;
                    
                    userGroupManager.CreateLocalGroup(SecurityGroupName);

                    TempDirectory = Path.Combine(Path.GetTempPath(), "Containers_" + uniqueId);
                    Directory.CreateDirectory(TempDirectory);
                }

                public virtual void Dispose()
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
            }

            public class Acceptance : ContainerCreationServiceTests, IDisposable, IClassFixture<AcceptanceFixture>
            {
                AcceptanceFixture Fixture { get; set; }

                public Acceptance(AcceptanceFixture fixture)
                {
                    Fixture = fixture;

                    ContainerBasePath = Fixture.TempDirectory;

                    FileSystem = new FileSystemManager();
                    HandleHelper = new ContainerHandleHelper();
                    ProcessRunner = new ProcessRunner();
                    
                    ContainerHostService = new ContainerHostService(FileSystem, ProcessRunner, new ContainerHostDependencyHelper());

                    UserManager = new LocalPrincipalManager(new DesktopPermissionManager(), Fixture.SecurityGroupName);

                    Service = new ContainerCreationService(HandleHelper, UserManager, FileSystem, TcpPortManager, ProcessRunner, ContainerHostService, ContainerBasePath);
                }

                public virtual void Dispose()
                {
                    Service.Dispose();
                }

                public class Create : Acceptance
                {
                    public Create(AcceptanceFixture fixture) : base(fixture)
                    {
                    }

                    [FactAdminRequired]
                    public void CanCreateContainer()
                    {
                        var spec = new ContainerSpec
                        {
                            Handle = Guid.NewGuid().ToString("N"),
                        };

                        IContainer container = null;
                        try
                        {
                            container = Service.CreateContainer(spec);

                            Assert.NotNull(container);
                        }
                        finally
                        {
                            if (container != null)
                                container.Destroy();
                        }
                    }
                }

                public class WithContainer : Acceptance
                {
                    const string RunBatFileContent = @"
@echo off
cmd.exe /C %*
                    ";

                    IContainer Container { get; set; }
                    
                    public WithContainer(AcceptanceFixture fixture) : base(fixture)
                    {
                        var spec = new ContainerSpec
                        {
                            Handle = Guid.NewGuid().ToString("N"),
                        };

                        Container = Service.CreateContainer(spec);

                        WriteUserFileToContainer("run.bat", RunBatFileContent);
                    }

                    void WriteUserFileToContainer(string path, string contents)
                    {
                        var containerImpl = (IronFoundry.Container.Container)Container;
                        var mappedPath = containerImpl.Directory.MapUserPath(path);

                        var directoryName = Path.GetDirectoryName(mappedPath);
                        Directory.CreateDirectory(directoryName);
                        File.WriteAllText(mappedPath, contents);
                    }

                    public override void Dispose()
                    {
                        Container.Destroy();
                        base.Dispose();
                    }

                    [FactAdminRequired]
                    public void CanRunAProcess()
                    {
                        var spec = new ProcessSpec
                        {
                            ExecutablePath = "run.bat",
                            Arguments = new [] { "exit 0" },
                        };
                        var io = new TestProcessIO();

                        var process = Container.Run(spec, io);
                        var exitCode = process.WaitForExit();

                        Assert.Equal(0, exitCode);
                    }

                    [FactAdminRequired]
                    public void CanGetExitCode()
                    {
                        var spec = new ProcessSpec
                        {
                            ExecutablePath = "run.bat",
                            Arguments = new[] { "exit 100" },
                        };
                        var io = new TestProcessIO();

                        var process = Container.Run(spec, io);
                        var exitCode = process.WaitForExit();

                        Assert.Equal(100, exitCode);
                    }

                    [FactAdminRequired]
                    public void CanGetProcessOutput()
                    {
                        var spec = new ProcessSpec
                        {
                            ExecutablePath = "run.bat",
                            Arguments = new[] { "echo This is STDOUT" },
                        };
                        var io = new TestProcessIO();

                        var process = Container.Run(spec, io);
                        process.WaitForExit();

                        Assert.Contains("This is STDOUT", io.Output.ToString());
                    }

                    [FactAdminRequired]
                    public void CanGetProcessErrors()
                    {
                        var spec = new ProcessSpec
                        {
                            ExecutablePath = "run.bat",
                            Arguments = new[] { "echo This is STDERR >&2" },
                        };
                        var io = new TestProcessIO();

                        var process = Container.Run(spec, io);
                        process.WaitForExit();

                        Assert.Contains("This is STDERR", io.Error.ToString());
                    }

                    [FactAdminRequired]
                    public void CanSetEnvironmentVariables()
                    {
                        var spec = new ProcessSpec
                        {
                            ExecutablePath = "run.bat",
                            Arguments = new[] { "set" },
                            Environment = new Dictionary<string,string>
                            {
                                { "FOO", "1" },
                                { "BAR", "two" },
                            },
                        };
                        var io = new TestProcessIO();

                        var process = Container.Run(spec, io);
                        process.WaitForExit();

                        var stdout = io.Output.ToString();
                        Assert.Contains("FOO=1", stdout);
                        Assert.Contains("BAR=two", stdout);
                    }
                }
            }
        }
    }
}
