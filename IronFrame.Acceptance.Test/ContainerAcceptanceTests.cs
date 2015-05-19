using DiskQuotaTypeLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace IronFrame.Acceptance
{
    public class ContainerAcceptanceTests : IDisposable
    {
        string Container1Handle { get; set; }
        string Container2Handle { get; set; }
        string UserGroupName { get; set; }
        string ContainerBasePath { get; set; }
        string ReadOnlyBindMountPath { get; set; }
        string ReadWriteBindMountPath { get; set; }

        LocalUserGroupManager UserGroupManager { get; set; }
        IContainerService ContainerService { get; set; }
        IContainer Container1 { get; set; }
        IContainer Container2 { get; set; }

        public ContainerAcceptanceTests()
        {
            Container1Handle = GenerateRandomAlphaString();
            Container2Handle = GenerateRandomAlphaString();
            ContainerBasePath = CreateTempDirectory();
            ReadOnlyBindMountPath = CreateTempDirectory();
            ReadWriteBindMountPath = CreateTempDirectory();

            UserGroupName = "ContainerServiceTestsUserGroup_" + GenerateRandomAlphaString();
            UserGroupManager = new LocalUserGroupManager();
            UserGroupManager.CreateLocalGroup(UserGroupName);

            ContainerService = new ContainerService(ContainerBasePath, UserGroupName);
        }

        public void Dispose()
        {
            ContainerService.DestroyContainer(Container1Handle);
            ContainerService.DestroyContainer(Container2Handle);

            UserGroupManager.DeleteLocalGroup(UserGroupName);
        }

        public class DiskLimit : ContainerAcceptanceTests
        {
            [FactAdminRequired]
            public void Enforced()
            {
                Container1 = CreateContainer(Container1Handle);
                Container1.LimitDisk(10 * 1024);

                var pSpec = new ProcessSpec
                {
                    ExecutablePath = "cmd",
                    DisablePathMapping = true,
                    Privileged = false,
                    WorkingDirectory = Container1.Directory.UserPath,
                };
                var io1 = new StringProcessIO();

                var passed = 0;
                var failed = 0;
                for (int i = 0; i < 20; i++)
                {
                    pSpec.Arguments = new[] {"/C", "echo Hi Bob > bob" + i + ".txt"};
                    var proc = Container1.Run(pSpec, io1);
                    var exitCode = proc.WaitForExit();

                    if (exitCode == 0)
                    {
                        passed++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                Assert.Equal(13, passed);
                Assert.Equal(7, failed);
            }

            [FactAdminRequired]
            public void CanSetLargeQuota()
            {
                const ulong limit = 7UL*1024*1024*1024;
                Container1 = CreateContainer(Container1Handle);
                Container1.LimitDisk(limit);
                Assert.Equal(limit, Container1.CurrentDiskLimit());
            }

            [Fact]
            public void DeletingContainer_DeletesDiskQuota()
            {
                Container1 = CreateContainer(Container1Handle);
                Container1.LimitDisk(5000);

                Assert.Equal(5000UL, Container1.CurrentDiskLimit());

                ContainerService.DestroyContainer(Container1Handle);

                Assert.Equal(0UL, Container1.CurrentDiskLimit());
            }
        }

        public class Security : ContainerAcceptanceTests
        {
            [FactAdminRequired]
            public void UniqueUserPerContainer()
            {
                Container1 = CreateContainer(Container1Handle);
                Container2 = CreateContainer(Container2Handle);

                var pSpec = new ProcessSpec
                {
                    ExecutablePath = "whoami.exe",
                    DisablePathMapping = true,
                    Privileged = false
                };

                var io1 = new StringProcessIO();
                var io2 = new StringProcessIO();

                Container1.Run(pSpec, io1).WaitForExit();
                Container2.Run(pSpec, io2).WaitForExit();

                var user1 = io1.Output.ToString();
                var user2 = io2.Output.ToString();

                Assert.NotEmpty(user1);
                Assert.NotEmpty(user2);
                Assert.NotEqual(user1, user2);
            }

            [FactAdminRequired]
            public void ContainerUserInContainerGroup()
            {
                Container1 = CreateContainer(Container1Handle);

                var pSpec = new ProcessSpec
                {
                    ExecutablePath = "whoami.exe",
                    DisablePathMapping = true,
                    Arguments = new string[] { "/GROUPS" }
                };

                var io = new StringProcessIO();
                Container1.Run(pSpec, io).WaitForExit();
                var groupOutput = io.Output.ToString();

                Assert.Contains(UserGroupName, groupOutput);
            }

            //[FactAdminRequired(Skip = "Can't implement until we can copy files in.")]
            //public void DoNotShareSpaces()
            //{
            //    var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);

            //    Container1 = CreateContainer(containerService, Container1Handle);
            //    Container2 = CreateContainer(containerService, Container2Handle);

            //    Assert.Equal(Container1.Handle, Container1Handle);
            //    Assert.Equal(Container2.Handle, Container2Handle);

            //    // Copy a file into one container and attempt to copy out from other?
            //    throw new NotImplementedException();
            //}
        }

        public class Processes : ContainerAcceptanceTests
        {
            [FactAdminRequired]
            public void StartShortLivedTask()
            {
                Container1 = CreateContainer(Container1Handle);

                var pSpec = new ProcessSpec
                {
                    ExecutablePath = "cmd.exe",
                    DisablePathMapping = true,
                    Arguments = new string[] { "/C set" },
                    Environment = new Dictionary<string, string> 
                    { 
                        { "PROC_ENV", "VAL1" } 
                    },
                };

                // RUN THE SHORT LIVED PROCESS
                var io = new StringProcessIO();
                var process = Container1.Run(pSpec, io);

                int exitCode;
                bool exited = process.TryWaitForExit(2000, out exitCode);

                var output = io.Output.ToString().Trim();
                var error = io.Error.ToString().Trim();

                // VERIFY THE PROCESS RAN AND EXITED
                Assert.True(exited);
                Assert.Equal(exitCode, 0);

                // VERIFY THE ENVIRONMENT WAS SET
                Assert.Contains("CONTAINER_HANDLE=" + Container1.Handle, output);
                Assert.Contains("PROC_ENV=VAL1", output);
            }

            [FactAdminRequired]
            public void StartAndStopLongRunningProcess()
            {
                Container1 = CreateContainer(Container1Handle);
                var pSpec = new ProcessSpec
                {
                    ExecutablePath = "cmd.exe",
                    DisablePathMapping = true,
                    Arguments = new string[] { @"/C ""FOR /L %% IN () DO ping 127.0.0.1 -n 2""" },
                };

                // START THE LONG RUNNING PROCESS
                var io = new StringProcessIO();
                var process = Container1.Run(pSpec, io);

                int exitCode;
                bool exited = process.TryWaitForExit(500, out exitCode);

                // VERIFY IT HASNT EXITED YET
                Assert.False(exited);

                var actualProcess = Process.GetProcessById(process.Id);
                Assert.False(actualProcess.HasExited);

                // KILL THE PROCESS AND WAIT FOR EXIT
                process.Kill();
                exited = process.TryWaitForExit(2000, out exitCode);

                // VERIFY THE PROCESS WAS KILLED
                Assert.True(exited);
                Assert.True(actualProcess.HasExited);
                Assert.True(io.Output.ToString().Length > 0);
            }

            [FactAdminRequired]
            public void FindAndKillProcess()
            {
                Container1 = CreateContainer(Container1Handle);
                var pSpec = new ProcessSpec
                {
                    ExecutablePath = "cmd.exe",
                    DisablePathMapping = true,
                    Arguments = new string[] { @"/C ""FOR /L %% IN () DO ping 127.0.0.1 -n 2""" },
                };

                // START THE LONG RUNNING PROCESS
                var io = new StringProcessIO();
                var process = Container1.Run(pSpec, io);
                var foundProcessByPid = Container1.FindProcessById(process.Id);

                // KILL THE PROCESS AND WAIT FOR EXIT
                foundProcessByPid.Kill();
                int exitCode;
                var exited = process.TryWaitForExit(2000, out exitCode);

                // VERIFY THE PROCESS WAS KILLED
                Assert.True(exited);
            }

            [FactAdminRequired]
            public void FindMissingProcess()
            {
                Container1 = CreateContainer(Container1Handle);
                var foundProcessByPid = Container1.FindProcessById(-1);
                Assert.Null(foundProcessByPid);
            }
        }

        public class Properties : ContainerAcceptanceTests
        {
            ContainerSpec ContainerSpec { get; set; }

            public Properties()
            {
                ContainerSpec = new ContainerSpec
                {
                    Handle = Container1Handle,
                };
            }

            [FactAdminRequired]
            public void SetsPropertiesOnCreation()
            {
                ContainerSpec.Properties = new Dictionary<string, string>
                {
                    { "Foo", "The quick brown fox..." },
                    { "Bar", "...jumped over the lazy dog." },
                };

                Container1 = CreateContainer(ContainerSpec);

                var fooValue = Container1.GetProperty("Foo");
                var barValue = Container1.GetProperty("Bar");

                Assert.Equal("The quick brown fox...", fooValue);
                Assert.Equal("...jumped over the lazy dog.", barValue);
            }

            [FactAdminRequired]
            public void PersistsProperties()
            {
                Container1 = CreateContainer(ContainerSpec);

                Container1.SetProperty("Phrase", "The quick brown fox...");

                var value = Container1.GetProperty("Phrase");
                Assert.Equal("The quick brown fox...", value);

                Container1.RemoveProperty("Phrase");

                value = Container1.GetProperty("Phrase");
                Assert.Null(value);
            }

            [FactAdminRequired]
            public void ReturnsPropertiesInContainerInfo()
            {
                Container1 = CreateContainer(ContainerSpec);

                Container1.SetProperty("Foo", "The quick brown fox...");
                Container1.SetProperty("Bar", "...jumped over the lazy dog.");

                var info = Container1.GetInfo();

                Assert.Equal("The quick brown fox...", info.Properties["Foo"]);
                Assert.Equal("...jumped over the lazy dog.", info.Properties["Bar"]);
            }
        }

        public class ImpersonateContainerUser : ContainerAcceptanceTests
        {
            ContainerSpec ContainerSpec { get; set; }

            public ImpersonateContainerUser()
            {
                ContainerSpec = new ContainerSpec
                {
                    Handle = Container1Handle,
                };
            }

            [FactAdminRequired]
            public void RunsActionsInContextOfUser()
            {
                Container1 = CreateContainer(ContainerSpec);
                var path = Container1.Directory.MapUserPath("hi");

                Container1.ImpersonateContainerUser(() => File.WriteAllText(path, "foobar"));

                string user =
                    File.GetAccessControl(path)
                   .GetOwner(typeof(System.Security.Principal.NTAccount))
                   .ToString();

                Assert.EndsWith("c_" + Container1.Id, user);
            }
        }

        public IContainer CreateContainer(string handle)
        {
            var bindMounts = new[]
            {
                new BindMount
                {
                    Access = FileAccess.Read,
                    SourcePath = ReadOnlyBindMountPath,
                    DestinationPath = ReadOnlyBindMountPath
                },
                new BindMount
                {
                    Access = FileAccess.ReadWrite,
                    SourcePath = ReadWriteBindMountPath,
                    DestinationPath = ReadWriteBindMountPath
                }
            };

            var environment = new Dictionary<string, string>
            {
                {"CONTAINER_HANDLE", handle},
                {"CONTAINER_ENV1", "ENV1"}
            };

            var spec = new ContainerSpec
            {
                BindMounts = bindMounts,
                Environment = environment,
                Handle = handle
            };

            return CreateContainer(spec);
        }

        public IContainer CreateContainer(ContainerSpec spec)
        {
            var container = ContainerService.CreateContainer(spec);

            return container;
        }

        private static string CreateTempDirectory()
        {
            string containerBasePath = null;
            for (var attempt = 0; attempt < 10 && string.IsNullOrWhiteSpace(containerBasePath); attempt++)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                if (!Directory.Exists(tempPath))
                {
                    containerBasePath = tempPath;
                }
            }

            if (string.IsNullOrWhiteSpace(containerBasePath))
            {
                throw new Exception("Couldn't generate a temporary container directory");
            }

            Directory.CreateDirectory(containerBasePath);

            return containerBasePath;
        }

        private static string GenerateRandomAlphaString(int length = 8)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";

            var r = RandomFactory.Create();
            var handle = "";
            for (var count = 0; count < length; count++)
            {
                var chosenCharIndex = r.Next(0, alphabet.Length);
                handle += alphabet[chosenCharIndex];
            }

            return handle;
        }
    }

    internal class StringProcessIO : IProcessIO
    {
        public StringWriter Error = new StringWriter();
        public StringWriter Output = new StringWriter();

        public TextWriter StandardOutput
        {
            get { return Output; }
        }

        public TextWriter StandardError
        {
            get { return Error; }
        }

        public TextReader StandardInput { get; set; }
    }
}
