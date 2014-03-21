namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading;
    using IronFoundry.Warden.Shared.Messaging;
    using NLog;
    using Protocol;
    using Utilities;
    using System.Security.Principal;

    public interface IContainer
    {
        string ContainerDirectoryPath { get; }
        string ContainerUserName { get; }
        ContainerHandle Handle { get; }
        ContainerState State { get; }

        IProcess CreateProcess(CreateProcessStartInfo si, bool impersonate = false);
        void Destroy();
        WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false);
        ProcessStats GetProcessStatistics();
        void Initialize();
        int ReservePort(int requestedPort);
        void Stop();
    }

    public class Container : IContainer
    {
        private const string TEMP_PATH = "tmp";

        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private readonly ContainerHandle handle;
        private readonly IContainerUser user;
        private readonly IContainerDirectory directory;
        private readonly ProcessManager processManager;

        private int? assignedPort;
        private ContainerState state;

        public static IContainer Restore(string handle, ContainerState containerState)
        {
            if (handle.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("handle");
            }

            var containerHandle = new ContainerHandle(handle);

            if (containerState == null)
            {
                throw new ArgumentNullException("containerState");
            }

            var user = new ContainerUser(handle);
            var directory = new ContainerDirectory(containerHandle, user);

            var container = new Container(containerHandle, user, directory, new ProcessManager(containerHandle));
            container.State = containerState;

            if (container.state == ContainerState.Active)
            {
                container.RestoreProcesses();
            }

            return container;
        }

        public Container(ContainerHandle handle, IContainerUser user, IContainerDirectory directory, ProcessManager manager)
        {
            this.handle = handle;
            this.user = user;
            this.directory = directory;
            this.state = ContainerState.Born;
            this.processManager = manager;
        }

        public Container()
        {
            this.handle = new ContainerHandle();
            this.user = new ContainerUser(handle, shouldCreate: true);
            this.directory = new ContainerDirectory(this.handle, this.user, true);
            this.state = ContainerState.Born;

            this.processManager = new ProcessManager(handle);
        }

        private NetworkCredential GetCredential()
        {
            return user.GetCredential();
        }

        public string ContainerDirectoryPath
        {
            get { return directory.FullName; }
        }

        public string ContainerUserName
        {
            get { return user.UserName; }
        }

        public ContainerHandle Handle
        {
            get { return handle; }
        }

        private IContainerUser User
        {
            get { return user; }
        }

        public ContainerState State
        {
            get { return state; }
            private set { state = value; }
        }

        public IContainerDirectory Directory
        {
            get { return directory; }
        }

        public bool HasProcesses
        {
            get
            {
                rwlock.EnterReadLock();
                try
                {
                    return processManager.HasProcesses;
                }
                finally
                {
                    rwlock.ExitReadLock();
                }
            }
        }

        public void Initialize()
        {
            ChangeState(ContainerState.Active);
        }

        public void Stop()
        {
            processManager.StopProcesses();
            ChangeState(ContainerState.Stopped);
        }

        public WindowsImpersonationContext GetExecutionContext(bool shouldImpersonate = false)
        {
            return Impersonator.GetContext(this.GetCredential(), shouldImpersonate);
        }

        public int ReservePort(int port)
        {
            if (!assignedPort.HasValue)
            {
                var localTcpPortManager = new LocalTcpPortManager((ushort)port, this.ContainerUserName);
                assignedPort = localTcpPortManager.ReserveLocalPort();
            }

            return assignedPort.Value;
        }

        public ProcessStats GetProcessStatistics()
        {
            return processManager.GetProcessStats();
        }


        public static void CleanUp(string handle)
        {
            ContainerUser.CleanUp(handle);
            ContainerDirectory.CleanUp(handle);
        }

        public void Destroy()
        {
            rwlock.EnterWriteLock();
            try
            {
                processManager.StopProcesses();
                processManager.Dispose();

                if (assignedPort.HasValue)
                {
                    var portManager = new LocalTcpPortManager((ushort)assignedPort.Value, this.ContainerUserName);
                    portManager.ReleaseLocalPort();
                }

                directory.Delete();

                user.Delete();

                this.state = ContainerState.Destroyed;
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        private void RestoreProcesses()
        {
            processManager.RestoreProcesses();
        }

        public virtual IProcess CreateProcess(CreateProcessStartInfo startInfo, bool shouldImpersonate = false)
        {

            if (shouldImpersonate)
            {
                startInfo.UserName = this.User.UserName;
                startInfo.Password = this.User.GetCredential().SecurePassword;

                startInfo.EnvironmentVariables.Clear();
                CopyEnvVariableTo(startInfo.EnvironmentVariables, new[] {
                    "Path", 
                    "SystemRoot", 
                    "SystemDrive",
                    "windir",
                    "PSModulePath",
                    "ProgramData",
                    "PATHEXT",
                });

                var tmpDir = Path.Combine(this.directory.FullName, TEMP_PATH);

                startInfo.EnvironmentVariables["APPDATA"] = tmpDir;
                startInfo.EnvironmentVariables["LOCALAPPDATA"] = tmpDir;
                startInfo.EnvironmentVariables["USERPROFILE"] = tmpDir;
                startInfo.EnvironmentVariables["TMP"] = tmpDir;
                startInfo.EnvironmentVariables["TEMP"] = tmpDir;
            }

            return processManager.CreateProcess(startInfo);
        }

        private void CopyEnvVariableTo(Dictionary<string, string> target, string[] sourceKeys)
        {
            var environment = Environment.GetEnvironmentVariables();

            foreach (var key in sourceKeys)
            {
                if (environment.Contains(key))
                {
                    target[key.ToString()] = environment[key].ToString();
                }
            }
        }

        private void ChangeState(ContainerState containerState)
        {
            rwlock.EnterWriteLock();
            try
            {
                this.state = containerState;
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }
    }
}
