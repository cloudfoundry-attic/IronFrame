using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using NLog;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Containers
{
    public class Container
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private readonly ContainerHandle handle;
        private readonly ContainerUser user;
        private readonly ContainerDirectory directory;
        private readonly ProcessManager processManager;

        private ContainerPort port;
        private ContainerState state;

        public static Container Restore(string handle, ContainerState containerState)
        {
            return new Container(handle, containerState);
        }

        /// <summary>
        /// Used for restore.
        /// </summary>
        private Container(string handle, ContainerState containerState)
        {
            if (handle.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("handle");
            }
            this.handle = new ContainerHandle(handle);

            if (containerState == null)
            {
                throw new ArgumentNullException("containerState");
            }
            this.state = containerState;

            this.user = new ContainerUser(handle);
            this.directory = new ContainerDirectory(this.handle, this.user);

            this.processManager = new ProcessManager(this.user);

            if (this.state == ContainerState.Active)
            {
                this.RestoreProcesses();
            }
        }

        public Container()
        {
            this.handle = new ContainerHandle();
            this.user = new ContainerUser(handle, shouldCreate: true);
            this.directory = new ContainerDirectory(this.handle, this.user, true);
            this.state = ContainerState.Born;

            this.processManager = new ProcessManager(this.user);
        }

        public NetworkCredential GetCredential()
        {
            return user.GetCredential();
        }

        public ContainerHandle Handle
        {
            get { return handle; }
        }

        public ContainerUser User
        {
            get { return user; }
        }

        public ContainerState State
        {
            get { return state; }
        }

        public ContainerDirectory Directory
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

        public IEnumerable<string> ConvertToPathsWithin(string[] arguments)
        {
            foreach (string arg in arguments)
            {
                string rv = null;

                if (arg.Contains("@ROOT@"))
                {
                    rv = arg.Replace("@ROOT@", this.Directory).ToWinPathString();
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
                return pathTmp.Replace("@ROOT@", this.Directory).ToWinPathString();
            }
            else
            {
                return pathTmp;
            }
        }

        public TempFile TempFileInContainer(string extension)
        {
            return new TempFile(this.Directory, extension);
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
                processManager.StopProcesses(); // NB: do this first to unlock files.

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

        public void AddProcess(Process process, ResourceLimits rlimits)
        {
            processManager.AddProcess(process);
        }

        private void RestoreProcesses()
        {
            processManager.RestoreProcesses();
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
