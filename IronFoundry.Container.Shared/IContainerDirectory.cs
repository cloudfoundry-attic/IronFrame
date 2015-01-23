using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    public interface IContainerDirectory
    {
        string MapUserPath(string containerPath);
    }

    public class ContainerDirectory : IContainerDirectory
    {
        const string UserRelativePath = "user";

        readonly string containerPath;
        readonly string containerUserPath;

        public ContainerDirectory(string containerPath)
        {
            this.containerPath = containerPath;

            this.containerUserPath = CanonicalizePath(Path.Combine(containerPath, UserRelativePath), ensureTrailingSlash: true);
        }

        public static ContainerDirectory Create(FileSystemManager fileSystem, string containerBasePath, string containerHandle, IContainerUser containerUser)
        {
            // TODO: Sanitize the container handle for use in the filesystem
            var containerPath = Path.Combine(containerBasePath, containerHandle);
            var containerUserPath = Path.Combine(containerPath, UserRelativePath);
            var defaultAccess = GetDefaultDirectoryAccess();
            var userAccess = defaultAccess.ToList();
            userAccess.Add(new UserAccess { Access = FileAccess.ReadWrite, UserName = containerUser.UserName });

            fileSystem.CreateDirectory(containerPath, defaultAccess);
            fileSystem.CreateDirectory(containerUserPath, userAccess);

            return new ContainerDirectory(containerPath);
        }

        public string MapUserPath(string path)
        {
            path = path.TrimStart('/');
            var isRootPath = String.IsNullOrWhiteSpace(path);

            var mappedPath = CanonicalizePath(Path.Combine(containerUserPath, path), ensureTrailingSlash: isRootPath);

            if (!mappedPath.StartsWith(containerUserPath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The path is not a valid user path.", "path");

            return mappedPath;
        }

        static string CanonicalizePath(string path, bool ensureTrailingSlash = false)
        {
            path = Path.GetFullPath(path);

            if (ensureTrailingSlash && !path.EndsWith("\\"))
                path += "\\";

            return path;
        }

        static IEnumerable<UserAccess> GetDefaultDirectoryAccess()
        {
            return new[]
            {
                new UserAccess { UserName = GetBuiltInAdminGroupName(), Access = FileAccess.ReadWrite },
                new UserAccess { UserName = GetCurrentUserName(), Access = FileAccess.ReadWrite },
            };
        }

        static string GetCurrentUserName()
        {
            return WindowsIdentity.GetCurrent().Name;
        }

        static string GetBuiltInAdminGroupName()
        {
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        }
    }
}