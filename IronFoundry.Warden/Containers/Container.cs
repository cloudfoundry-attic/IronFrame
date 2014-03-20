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

    public class Container
    {
        private const string TEMP_PATH = "tmp";

        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private readonly ContainerHandle handle;
        private readonly IContainerUser user;
        private readonly IContainerDirectory directory;
        private readonly ProcessManager processManager;

        private ContainerPort port;
        private ContainerState state;

        public static Container Restore(string handle, ContainerState containerState)
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

        public NetworkCredential GetCredential()
        {
            return user.GetCredential();
        }

        public ContainerHandle Handle
        {
            get { return handle; }
        }

        public IContainerUser User
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

        public void AfterCreate()
        {
            ChangeState(ContainerState.Active);
        }

        public void Stop()
        {
            processManager.StopProcesses();
        }

        public void AfterStop()
        {
            ChangeState(ContainerState.Stopped);
        }

        public ContainerPort ReservePort(ushort suggestedPort)
        {
            rwlock.EnterUpgradeableReadLock();
            try
            {
                if (port == null)
                {
                    rwlock.EnterWriteLock();
                    try
                    {
                        port = new ContainerPort(suggestedPort, this.user);
                    }
                    finally
                    {
                        rwlock.ExitWriteLock();
                    }
                }
                else
                {
                    log.Trace("Container '{0}' already assigned port '{1}'", handle, port);
                }
            }
            finally
            {
                rwlock.ExitUpgradeableReadLock();
            }

            return port;
        }

        public ProcessStats GetProcessStatistics()
        {
            return processManager.GetProcessStats();
        }

        public IEnumerable<string> ConvertToPathsWithin(string[] arguments)
        {
            foreach (string arg in arguments)
            {
                string rv = null;

                if (arg.Contains("@ROOT@"))
                {
                    rv = arg.Replace("@ROOT@", this.Directory.FullName).ToWinPathString();
                }
                else
                {
                    rv = arg;
                }

                yield return rv;
            }
        }

        public string ConvertToPathWithin(string path)
        {
            string pathTmp = path.Trim();
            if (pathTmp.StartsWith("@ROOT@"))
            {
                return pathTmp.Replace("@ROOT@", this.Directory.FullName).ToWinPathString();
            }
            else
            {
                return pathTmp;
            }
        }

        public TempFile TempFileInContainer(string extension)
        {
            return new TempFile(this.Directory.FullName, extension);
        }

        public static void CleanUp(string handle)
        {
            ContainerUser.CleanUp(handle);
            ContainerDirectory.CleanUp(handle);
            ContainerPort.CleanUp(handle, 0); // TODO
        }

        public void Destroy()
        {
            rwlock.EnterWriteLock();
            try
            {
                processManager.StopProcesses();
                processManager.Dispose();

                if (port != null)
                {
                    port.Delete(user);
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

        public virtual IProcess CreateProcess(CreateProcessStartInfo startInfo)
        {
            if (startInfo.UserName != null)
            {
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
