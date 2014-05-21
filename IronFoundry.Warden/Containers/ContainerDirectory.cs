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

        public ContainerDirectory(ContainerHandle handle, IContainerUser user, string containerBaseDirectory, bool shouldCreate = false)
        {
            if (handle == null)
                throw new ArgumentNullException("handle");

            this.user = user;

            this.containerDirectory = shouldCreate ?
                CreateContainerDirectory(containerBaseDirectory, handle, user) :
                FindContainerDirectory(containerBaseDirectory, handle);
        }

        public string FullName
        {
            get { return containerDirectory.FullName; }
        }

        void BindMount(BindMount bindMount)
        {
            var inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            // TODO: We need to determine the exact rights that are required for Read vs ReadWrite.
            // Apparently FileSystemRights.Read | FileSystemRights.Write weren't enough.
            var rights = FileSystemRights.FullControl;

            var accessRule = new FileSystemAccessRule(user.UserName, rights, inheritanceFlags, PropagationFlags.InheritOnly, AccessControlType.Allow);
            AddAccessRuleTo(accessRule, bindMount.SourcePath);
            AddAccessRuleTo(accessRule, bindMount.DestinationPath);
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

        //
        // TODO: Consolidate the various helper methods
        //

        private static DirectoryInfo CreateContainerDirectory(string containerBaseDirectory, ContainerHandle handle, IContainerUser user)
        {
            var dirInfo = GetContainerDirectoryInfo(containerBaseDirectory, handle);

            var inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var accessRule = new FileSystemAccessRule(user.UserName, FileSystemRights.FullControl, inheritanceFlags,
                PropagationFlags.None, AccessControlType.Allow);

            DirectoryInfo containerBaseInfo = dirInfo.Item1;
            DirectorySecurity security = containerBaseInfo.GetAccessControl();
            security.AddAccessRule(accessRule);

            string containerDirectory = dirInfo.Item2;
            return Directory.CreateDirectory(containerDirectory, security);
        }

        private static DirectoryInfo FindContainerDirectory(string containerBaseDirectory, ContainerHandle handle)
        {
            var dirInfo = GetContainerDirectoryInfo(containerBaseDirectory, handle);
            if (Directory.Exists(dirInfo.Item2))
            {
                return new DirectoryInfo(dirInfo.Item2);
            }
            else
            {
                throw new WardenException("Directory '{0}' does not exist!", dirInfo.Item2);
            }
        }

        private static Tuple<DirectoryInfo, string> GetContainerDirectoryInfo(string containerBaseDirectory, ContainerHandle handle)
        {
            string containerDirectory = Path.Combine(containerBaseDirectory, handle);

            return new Tuple<DirectoryInfo, string>(new DirectoryInfo(containerBaseDirectory), containerDirectory);
        }

        private void AddAccessRuleTo(FileSystemAccessRule accessRule, string path)
        {
            var pathInfo = new DirectoryInfo(path);
            if (pathInfo.Exists)
            {
                var pathSecurity = pathInfo.GetAccessControl();
                pathSecurity.AddAccessRule(accessRule);

                ReplaceAllChildPermissions(pathInfo, pathSecurity);
            }
            else
            {
                DirectoryInfo parentInfo = pathInfo.Parent;
                if (parentInfo.Exists)
                {
                    var pathSecurity = parentInfo.GetAccessControl();
                    pathSecurity.AddAccessRule(accessRule);

                    Directory.CreateDirectory(pathInfo.FullName, pathSecurity);

                    ReplaceAllChildPermissions(pathInfo, pathSecurity);
                }
            }
        }

        private static void ReplaceAllChildPermissions(DirectoryInfo dirInfo, DirectorySecurity security)
        {
            dirInfo.SetAccessControl(security);

            foreach (var fi in dirInfo.GetFiles())
            {
                var fileSecurity = fi.GetAccessControl();
                fileSecurity.SetAccessRuleProtection(false, false);
                fi.SetAccessControl(fileSecurity);
            }

            foreach (var di in dirInfo.GetDirectories())
            {
                ReplaceAllChildPermissions(di, security);
            }
        }
    }
}
