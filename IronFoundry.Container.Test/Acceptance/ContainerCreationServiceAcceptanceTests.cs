using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using IronFoundry.Container.Utilities;
using Xunit;

namespace IronFoundry.Container.Acceptance
{
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

    public class ContainerCreationServiceAcceptanceTests : IDisposable, IClassFixture<AcceptanceFixture>
    {
        public ContainerCreationServiceAcceptanceTests(AcceptanceFixture fixture)
        {
            Fixture = fixture;

            ContainerBasePath = Fixture.TempDirectory;

            Service = new ContainerService(ContainerBasePath, Fixture.SecurityGroupName);
        }

        private AcceptanceFixture Fixture { get; set; }
        private NetworkCredential UserCredential { get; set; }
        private string ContainerBasePath { get; set; }
        private IContainerService Service { get; set; }

        public virtual void Dispose()
        {
            Service.Dispose();

            if (UserCredential != null)
            {
                // Delete the test user
                var principalManager =
                    new LocalPrincipalManager(new DesktopPermissionManager());
                principalManager.DeleteUser(UserCredential.UserName);
            }
        }

        public class Create : ContainerCreationServiceAcceptanceTests
        {
            public Create(AcceptanceFixture fixture)
                : base(fixture)
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
                        Service.DestroyContainer(container.Handle);
                }
            }
        }

        public class WithContainer : ContainerCreationServiceAcceptanceTests
        {
            private const string RunBatFileContent = @"
@echo off
cmd.exe /C %*
                    ";

            public WithContainer(AcceptanceFixture fixture)
                : base(fixture)
            {
                var spec = new ContainerSpec
                {
                    Handle = Guid.NewGuid().ToString("N"),
                };

                Container = Service.CreateContainer(spec);

                WriteUserFileToContainer("run.bat", RunBatFileContent);
            }

            private IContainer Container { get; set; }

            private void WriteUserFileToContainer(string path, string contents)
            {
                var mappedPath = Container.Directory.MapUserPath(path);

                var directoryName = Path.GetDirectoryName(mappedPath);
                Directory.CreateDirectory(directoryName);
                File.WriteAllText(mappedPath, contents);
            }

            public override void Dispose()
            {
                Service.DestroyContainer(Container.Handle);
                base.Dispose();
            }

            [FactAdminRequired]
            public void CanRunAProcess()
            {
                var spec = new ProcessSpec
                {
                    ExecutablePath = "run.bat",
                    Arguments = new[] {"exit 0"}
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
                    Arguments = new[] {"exit 100"}
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
                    Arguments = new[] {"echo This is STDOUT"}
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
                    Arguments = new[] {"echo This is STDERR >&2"}
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
                    Arguments = new[] {"set"},
                    Environment = new Dictionary<string, string>
                    {
                        {"FOO", "1"},
                        {"BAR", "two"}
                    }
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