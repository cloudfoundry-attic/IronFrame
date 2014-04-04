namespace IronFoundry.Warden.Containers
{
    using System;
    using System.IO;
    using System.Security.AccessControl;
    using IronFoundry.Warden.Configuration;

    public interface IContainerDirectory
    {
        string FullName { get; }

        void Delete();
    }

    public class ContainerDirectory : IContainerDirectory
    {
        private readonly DirectoryInfo containerDirectory;
        private readonly IWardenConfig wardenConfig;

        public ContainerDirectory(ContainerHandle handle, IContainerUser user, bool shouldCreate = false) : this(handle, user, shouldCreate, new WardenConfig()) 
        {
        }

        public ContainerDirectory(ContainerHandle handle, IContainerUser user, bool shouldCreate, IWardenConfig wardenConfig)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            this.wardenConfig = wardenConfig;

            if (shouldCreate)
            {
                this.containerDirectory = CreateContainerDirectory(wardenConfig, handle, user);
            }
            else
            {
                this.containerDirectory = FindContainerDirectory(wardenConfig, handle);
            }
        }

        public void Delete()
        {
            if (wardenConfig.DeleteContainerDirectories)
            {
                containerDirectory.Delete(true);
            }
        }

        public static implicit operator string(ContainerDirectory containerDirectory)
        {
            return containerDirectory.ToString();
        }

        public override string ToString()
        {
            return FullName;
        }

        public string FullName
        {
            get { return containerDirectory.FullName; }
        }

        private static DirectoryInfo CreateContainerDirectory(IWardenConfig config, ContainerHandle handle, IContainerUser user)
        {
            var dirInfo = GetContainerDirectoryInfo(config, handle);

            var inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            var accessRule = new FileSystemAccessRule(user.UserName, FileSystemRights.FullControl, inheritanceFlags,
                PropagationFlags.None, AccessControlType.Allow);

            DirectoryInfo containerBaseInfo = dirInfo.Item1;
            DirectorySecurity security = containerBaseInfo.GetAccessControl();
            security.AddAccessRule(accessRule);

            string containerDirectory = dirInfo.Item2;
            return Directory.CreateDirectory(containerDirectory, security);
        }

        private static DirectoryInfo FindContainerDirectory(IWardenConfig config, ContainerHandle handle)
        {
            var dirInfo = GetContainerDirectoryInfo(config, handle);
            if (Directory.Exists(dirInfo.Item2))
            {
                return new DirectoryInfo(dirInfo.Item2);
            }
            else
            {
                throw new WardenException("Directory '{0}' does not exist!", dirInfo.Item2);
            }
        }

        private static Tuple<DirectoryInfo, string> GetContainerDirectoryInfo(IWardenConfig config, ContainerHandle handle)
        {
            string containerBasePath = config.ContainerBasePath;
            string containerDirectory = Path.Combine(containerBasePath, handle);

            return new Tuple<DirectoryInfo, string>(new DirectoryInfo(containerBasePath), containerDirectory);
        }
    }
}
