using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Containers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.AccessControl;
    using IronFoundry.Warden.Configuration;
    using IronFoundry.Warden.Containers.Messages;

    public interface IContainerDirectory
    {
        string FullName { get; }

        void BindMounts(IEnumerable<BindMount> mounts);
        void Delete();
    }

    public class ContainerDirectory : IContainerDirectory
    {
        private readonly IContainerUser user;
        private readonly DirectoryInfo containerDirectory;
        private readonly FileSystemManager fileSystem;
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public ContainerDirectory(ContainerHandle handle, IContainerUser user, FileSystemManager fileSystem, string containerBaseDirectory, bool shouldCreate = false)
        {
            if (handle == null)
                throw new ArgumentNullException("handle");

            this.user = user;
            this.fileSystem = fileSystem;

            string containerPath = Path.Combine(containerBaseDirectory, handle);
            this.containerDirectory = shouldCreate ?
                CreateContainerDirectory(containerPath) :
                FindContainerDirectory(containerPath);
        }

        public string FullName
        {
            get { return containerDirectory.FullName; }
        }

        private void BindMount(BindMount bindMount)
        {
            log.Debug("BindMount ({0}): Source: {1} Dest: {2}", bindMount.Access, bindMount.SourcePath,
                bindMount.DestinationPath);

            // We can ignore the bindMount.TargetPath because we don't mount anything.
            // Since we have no containers, we just read everything from the source.
            string containerPath = bindMount.SourcePath;
            
            if (fileSystem.DirectoryExists(containerPath))
            {
                FileAccess effectiveAccess = fileSystem.GetEffectiveDirectoryAccess(containerPath, user.GetCredential());

                // The AND provides us with the flags that are common between the requested access and the effective access.
                // The XOR then flips those bits to 0 and leaves as 1 those that aren't found in effective access.
                FileAccess accessNeeded = bindMount.Access ^ (bindMount.Access & effectiveAccess);

                fileSystem.AddDirectoryAccess(containerPath, accessNeeded, user.UserName);
            }
            else
            {
                var access = GetDefaultDirectoryAccess();
                fileSystem.CreateDirectory(containerPath, access);
            }
        }

        public void BindMounts(IEnumerable<BindMount> mounts)
        {
            foreach (var mount in mounts)
            {
                BindMount(mount);
            }
        }

        public void Delete()
        {
            containerDirectory.Delete(true);
        }

        public override string ToString()
        {
            return FullName;
        }

        public static implicit operator string(ContainerDirectory containerDirectory)
        {
            return containerDirectory.ToString();
        }

        private DirectoryInfo CreateContainerDirectory(string containerPath)
        {
            var defaultAccess = GetDefaultDirectoryAccess();
            fileSystem.CreateDirectory(containerPath, defaultAccess);
            fileSystem.AddDirectoryAccess(containerPath, FileAccess.ReadWrite, user.UserName);

            return new DirectoryInfo(containerPath);
        }

        private static DirectoryInfo FindContainerDirectory(string containerPath)
        {
            if (Directory.Exists(containerPath))
            {
                return new DirectoryInfo(containerPath);
            }
            else
            {
                throw new WardenException("Directory '{0}' does not exist!", containerPath);
            }
        }

        /// <summary>
        /// Return the default access to use for new directories.  
        /// </summary>
        private IEnumerable<UserAccess> GetDefaultDirectoryAccess()
        {
            return new[]
            {
                new UserAccess {UserName = GetBuiltInAdminGroupName(), Access = FileAccess.ReadWrite},
                new UserAccess {UserName = user.UserName, Access = FileAccess.ReadWrite},
            };
        }

        private string GetBuiltInAdminGroupName()
        {
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        }

    }
}
