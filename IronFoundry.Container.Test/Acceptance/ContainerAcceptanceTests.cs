using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using IronFoundry.Container.Utilities;
using Xunit;

namespace IronFoundry.Container.Acceptance
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

        //[FactAdminRequired]
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

        [FactAdminRequired]
        public void UniqueUserPerContainer()
        {
            Container1 = CreateContainer(ContainerService, Container1Handle);
            Container2 = CreateContainer(ContainerService, Container2Handle);

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
            Container1 = CreateContainer(ContainerService, Container1Handle);

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

        [FactAdminRequired]
        public void StartShortLivedTask()
        {
            Container1 = CreateContainer(ContainerService, Container1Handle);

            var pSpec = new ProcessSpec
            {
                ExecutablePath = "cmd.exe",
                DisablePathMapping = true,
                Arguments = new string[] { "/C set" },
                Environment = new Dictionary<string, string> 
                { 
                    { "CONTAINER_HANDLE", Container1.Handle },
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

        //[FactAdminRequired]
        //public void StartAndStopLongRunningProcess()
        //{
        //    var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);
        //    Container1 = CreateContainer(containerService, Container1Handle);
        //    var pSpec = new ProcessSpec
        //    {
        //        ExecutablePath = "cmd.exe",
        //        DisablePathMapping = true,
        //        Arguments = new string[] { @"/C ""FOR /L %% IN () DO ping 127.0.0.1 -n 2""" },
        //    };
        //
        //    // START THE LONG RUNNING PROCESS
        //    var io = new StringProcessIO();
        //    var process = Container1.Run(pSpec, io);

        //    int exitCode;
        //    bool exited = process.TryWaitForExit(500, out exitCode);

        //    // VERIFY IT HASNT EXITED YET
        //    Assert.False(exited);

        //    var actualProcess = Process.GetProcessById(process.Id);

        //    // KILL THE PROCESS AND WAIT FOR EXIT
        //    process.Kill();
        //    exited = process.TryWaitForExit(2000, out exitCode);

        //    // VERIFY THE PROCESS WAS KILLED
        //    Assert.True(exited);
        //    Assert.True(actualProcess.HasExited);
        //    Assert.True(io.Output.ToString().Length > 0);
        //}

        public IContainer CreateContainer(IContainerService containerService, string handle)
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

            var container = containerService.CreateContainer(spec);

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
