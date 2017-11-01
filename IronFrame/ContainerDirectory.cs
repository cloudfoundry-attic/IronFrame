using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using IronFrame.Utilities;

namespace IronFrame
{
    internal class ContainerDirectory : IContainerDirectory
    {
        const string BinRelativePath = "bin";
        const string UserRelativePath = "user";
        const string PrivateRelativePath = "private";

        readonly IFileSystemManager fileSystem;
        readonly string containerPath;
        readonly string containerBinPath;
        readonly string containerUserPath;

        internal ContainerDirectory(IFileSystemManager fileSystem, string containerPath)
        {
            this.fileSystem = fileSystem;
            this.containerPath = containerPath;

            this.containerBinPath = CanonicalizePath(Path.Combine(containerPath, BinRelativePath), ensureTrailingSlash: true);
            this.containerUserPath = CanonicalizePath(Path.Combine(containerPath, UserRelativePath), ensureTrailingSlash: true);
        }

        public void CreateSubdirectories(IContainerUser containerUser)
        {
            var containerPrivatePath = Path.Combine(containerPath, PrivateRelativePath);
            var containerUserPath = Path.Combine(containerPath, UserRelativePath);
            var containerBinPath = Path.Combine(containerPath, BinRelativePath);

            fileSystem.CreateDirectory(containerPath, GetContainerUserAccess(containerUser.UserName, FileAccess.Read));
            fileSystem.CreateDirectory(containerPrivatePath, GetContainerDefaultAccess());
            fileSystem.CreateDirectory(containerBinPath, GetContainerUserAccess(containerUser.UserName, FileAccess.Read));
            fileSystem.CreateDirectory(containerUserPath, GetContainerUserAccess(containerUser.UserName, FileAccess.ReadWrite));
        }

        public string RootPath
        {
            get { return containerPath; }
        }

        public string UserPath
        {
            get { return containerUserPath; }
        }

        public string Volume
        {
            get { return Path.GetPathRoot(containerPath); }
        }

        public void Destroy()
        {
            try
            {
                fileSystem.DeleteDirectory(containerPath);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        public string MapBinPath(string path)
        {
            return MapContainerPath(BinRelativePath, path);
        }

        private string MapContainerPath(string pathPrefix, string path)
        {
            if (path.Trim() != string.Empty)
            {
                var rootPath = Path.GetPathRoot(path);

                if (rootPath.Length > 0 && char.IsLetter(rootPath[0]))
                {
                    return path;
                }
            }

            var basePath = CanonicalizePath(Path.Combine(containerPath, pathPrefix));

            path = path.TrimStart('/', '\\');
            var isRootPath = String.IsNullOrWhiteSpace(path);

            var mappedPath = CanonicalizePath(Path.Combine(basePath, path), ensureTrailingSlash: isRootPath);

            if (!mappedPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The path is not a valid container path.", path);

            return mappedPath;
        }

        public string MapPrivatePath(string path)
        {
            return MapContainerPath(PrivateRelativePath, path);
        }

        public string MapUserPath(string path)
        {
            return MapContainerPath(UserRelativePath, path);
        }

        static string CanonicalizePath(string path, bool ensureTrailingSlash = false)
        {
            path = Path.GetFullPath(path);

            if (ensureTrailingSlash && !path.EndsWith("\\"))
                path += "\\";

            return path;
        }

        static IEnumerable<UserAccess> GetContainerDefaultAccess()
        {
            return new[]
            {
                new UserAccess { UserName = GetBuiltInAdminGroupName(), Access = FileAccess.ReadWrite },
                new UserAccess { UserName = GetCurrentUserName(), Access = FileAccess.ReadWrite },
            };
        }

        static IEnumerable<UserAccess> GetContainerUserAccess(string username, FileAccess access)
        {
            var result = GetContainerDefaultAccess().ToList();
            result.Add(new UserAccess { UserName = username, Access = access });
            return result;
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

        public static ContainerDirectory Restore(IFileSystemManager fileSystem, string containerPath)
        {
            return new ContainerDirectory(fileSystem, containerPath);
        }

        public void CreateBindMounts(BindMount[] bindMounts, IContainerUser containerUser)
        {
            foreach (var bindMount in bindMounts)
            {
                var mappedDestinationPath = MapUserPath(bindMount.DestinationPath);
                var parentDir = Directory.GetParent(mappedDestinationPath).FullName;
                
                if (CanonicalizePath(parentDir, ensureTrailingSlash: true) != MapUserPath(""))
                {
                   fileSystem.CreateDirectory(parentDir, GetContainerUserAccess(containerUser.UserName, FileAccess.ReadWrite));
                }
                    
                var cleanedSourcePath = bindMount.SourcePath.Replace("/", "\\");
                fileSystem.Symlink(mappedDestinationPath, cleanedSourcePath);
                fileSystem.AddDirectoryAccess(mappedDestinationPath, FileAccess.Read, containerUser.UserName);
           
                fileSystem.AddDirectoryAccess(cleanedSourcePath, FileAccess.Read, containerUser.UserName);

                while (fileSystem.DirIsSymlink(cleanedSourcePath))
                {
                    var symlinkDest = fileSystem.GetSymlinkTarget(cleanedSourcePath);
                    fileSystem.AddDirectoryAccess(symlinkDest, FileAccess.Read, containerUser.UserName);
                    cleanedSourcePath = symlinkDest;
                }
            }
        }

        public void DeleteBindMounts(BindMount[] bindMounts, IContainerUser containerUser)
        {
            foreach (var bindMount in bindMounts)
            {
                var cleanedSourcePath = bindMount.SourcePath.Replace("/", "\\");
                fileSystem.RemoveDirectoryAccess(cleanedSourcePath, containerUser.UserName);

                while (fileSystem.DirIsSymlink(cleanedSourcePath))
                {
                    var symlinkDest = fileSystem.GetSymlinkTarget(cleanedSourcePath);
                    fileSystem.RemoveDirectoryAccess(symlinkDest, containerUser.UserName);
                    cleanedSourcePath = symlinkDest;
                }
            }
        }
    }
}
