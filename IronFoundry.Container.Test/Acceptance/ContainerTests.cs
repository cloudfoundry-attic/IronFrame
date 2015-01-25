using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IronFoundry.Container.TestSupport;
using IronFoundry.Container.Utilities;
using IronFoundry.Warden.Test;
using NSubstitute;
using Xunit;

namespace IronFoundry.Container.Acceptance
{
    public class ContainerTests : IDisposable
    {
        string Container1Handle { get; set; }
        string Container2Handle { get; set; }
        string UserGroupName { get; set; }
        string ContainerBasePath { get; set; }
        string ReadOnlyBindMountPath { get; set; }
        string ReadWriteBindMountPath { get; set; }

        LocalUserGroupManager UserGroupManager { get; set; }
        IContainer Container1 { get; set; }
        IContainer Container2 { get; set; }

        public ContainerTests()
        {
            Container1Handle = GenerateRandomAlphaString();
            Container2Handle = GenerateRandomAlphaString();
            ContainerBasePath = CreateTempDirectory();
            ReadOnlyBindMountPath = CreateTempDirectory();
            ReadWriteBindMountPath = CreateTempDirectory();

            UserGroupName = "ContainerServiceTestsUserGroup_" + GenerateRandomAlphaString();
            UserGroupManager = new LocalUserGroupManager();
            UserGroupManager.CreateLocalGroup(UserGroupName);
        }

        public void Dispose()
        {
            if (Container1 != null)
            {
                Container1.Destroy();
            }

            if (Container2 != null)
            {
                Container2.Destroy();
            }

            UserGroupManager.DeleteLocalGroup(UserGroupName);
        }

        [FactAdminRequired]
        public void DoNotShareSpaces()
        {
            var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);

            Container1 = CreateContainer(containerService, Container1Handle);
            Container2 = CreateContainer(containerService, Container2Handle);

            Assert.Equal(Container1.Handle, Container1Handle);
            Assert.Equal(Container2.Handle, Container2Handle);

            // Copy a file into one container and attempt to copy out from other?
            throw new NotImplementedException();
        }

        [FactAdminRequired]
        public void UniqueUserPerContainer()
        {
            var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);

            Container1 = CreateContainer(containerService, Container1Handle);
            Container2 = CreateContainer(containerService, Container2Handle);

            var pSpec = new ProcessSpec
            {
                ExecutablePath = "whoami.exe",
            };

            var io1 = new StringProcessIO();
            var io2 = new StringProcessIO();

            Container1.Run(pSpec, io1).WaitForExit();
            Container2.Run(pSpec, io2).WaitForExit();

            string user1 = io1.Output.ToString();
            string user2 = io2.Output.ToString();

            Assert.NotEmpty(user1);
            Assert.NotEmpty(user2);
            Assert.NotEqual(user1, user2);
        }

        [FactAdminRequired]
        public void ContainerUserInContainerGroup()
        {
            var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);
            Container1 = CreateContainer(containerService, Container1Handle);

            var pSpec = new ProcessSpec
            {
                ExecutablePath = "whoami.exe",
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
            var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);
            Container1 = CreateContainer(containerService, Container1Handle);

            var pSpec = new ProcessSpec
            {
                ExecutablePath = "cmd.exe",
                Arguments = new string[] {"/C echo %CONTAINER_HANDLE% && echo %PROC_ENV% 2>&1"},
                Environment = new Dictionary<string, string> {{"PROC_ENV", "VAL1"}},
            };

            // RUN THE SHORT LIVED PROCESS
            var io = new StringProcessIO();
            var process = Container1.Run(pSpec, io);

            int exitCode;
            bool exited = process.TryWaitForExit(2000, out exitCode);

            var output = io.Output.ToString();
            var error = io.Error.ToString();

            // VERIFY THE PROCESS RAN AND EXITED
            Assert.True(exited);
            Assert.Equal(exitCode, 0);
            
            // VERIFY THE ENVIRONMENT WAS SET
            Assert.Equal(output, Container1.Handle);
            Assert.Equal(error, "VAL1");
        }

        [FactAdminRequired]
        public void StartAndStopLongRunningProcess()
        {
            var containerService = new ContainerCreationService(ContainerBasePath, UserGroupName);
            Container1 = CreateContainer(containerService, Container1Handle);

            var pSpec = new ProcessSpec
            {
                ExecutablePath = "cmd.exe",
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

            // KILL THE PROCESS AND WAIT FOR EXIT
            process.Kill();
            exited = process.TryWaitForExit(2000, out exitCode);

            // VERIFY THE PROCESS WAS KILLED
            Assert.True(exited);
            Assert.True(actualProcess.HasExited);
            Assert.True(io.Output.ToString().Length > 0);
        }


        public IContainer CreateContainer(IContainerCreationService containerService, string handle)
        {
            var bindMounts = new BindMount[]
            {
                new BindMount { Access = FileAccess.Read, SourcePath = ReadOnlyBindMountPath, DestinationPath = ReadOnlyBindMountPath },
                new BindMount { Access = FileAccess.ReadWrite, SourcePath = ReadWriteBindMountPath, DestinationPath = ReadWriteBindMountPath },
            };

            var environment = new Dictionary<string, string>
            {
                { "CONTAINER_HANDLE", handle },
                { "CONTAINER_ENV1", "ENV1" },
            };

            ContainerSpec spec = new ContainerSpec
            {
                BindMounts = bindMounts,
                Environment = environment,
                Handle = handle
            };

            var container = containerService.CreateContainer(spec);

            return container;
        }

        static string CreateTempDirectory()
        {
            string containerBasePath = null;
            for(int attempt = 0; attempt < 10 && string.IsNullOrWhiteSpace(containerBasePath); attempt++)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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

        static string GenerateRandomAlphaString(int length = 8)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";

            Random r = RandomFactory.Create();
            string handle = "";
            for (int count = 0; count < length; count++)
            {
                int chosenCharIndex = r.Next(0, alphabet.Length);
                handle += alphabet[chosenCharIndex];
            }

            return handle;
        }
    }

    internal class StringProcessIO : IProcessIO
    {
        public StringWriter Output = new StringWriter();
        public StringWriter Error = new StringWriter();

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
