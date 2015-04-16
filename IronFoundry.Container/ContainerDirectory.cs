﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container
{
    internal class ContainerDirectory : IContainerDirectory
    {
        const string BinRelativePath = "bin";
        const string UserRelativePath = "user";
        const string PrivateRelativePath = "private";

        readonly FileSystemManager fileSystem;
        readonly string containerPath;
        readonly string containerBinPath;
        readonly string containerUserPath;

        internal ContainerDirectory(FileSystemManager fileSystem, string containerPath)
        {
            this.fileSystem = fileSystem;
            this.containerPath = containerPath;

            this.containerBinPath = CanonicalizePath(Path.Combine(containerPath, BinRelativePath), ensureTrailingSlash: true);
            this.containerUserPath = CanonicalizePath(Path.Combine(containerPath, UserRelativePath), ensureTrailingSlash: true);
        }

        public string RootPath
        {
            get { return containerPath; }
        }

        public string UserPath
        {
            get { return containerUserPath; }
        }

        public static IContainerDirectory Create(FileSystemManager fileSystem, string containerBasePath, string containerHandle, IContainerUser containerUser)
        {
            // TODO: Sanitize the container handle for use in the filesystem
            var containerPath = Path.Combine(containerBasePath, containerHandle);
            var containerPrivatePath = Path.Combine(containerPath, PrivateRelativePath);
            var containerUserPath = Path.Combine(containerPath, UserRelativePath);
            var containerBinPath = Path.Combine(containerPath, BinRelativePath);

            fileSystem.CreateDirectory(containerPath, GetContainerUserAccess(containerUser.UserName, FileAccess.Read));
            fileSystem.CreateDirectory(containerPrivatePath, GetContainerDefaultAccess());
            fileSystem.CreateDirectory(containerBinPath, GetContainerUserAccess(containerUser.UserName, FileAccess.Read));
            fileSystem.CreateDirectory(containerUserPath, GetContainerUserAccess(containerUser.UserName, FileAccess.ReadWrite));

            return new ContainerDirectory(fileSystem, containerPath);
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
            var basePath = CanonicalizePath(Path.Combine(containerPath, pathPrefix));

            path = path.TrimStart('/', '\\');
            var isRootPath = String.IsNullOrWhiteSpace(path);

            var mappedPath = CanonicalizePath(Path.Combine(basePath, path), ensureTrailingSlash: isRootPath);

            if (!mappedPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The path is not a valid container path.", "path");

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

        public static ContainerDirectory Restore(FileSystemManager fileSystem, string containerPath)
        {
            return new ContainerDirectory(fileSystem, containerPath);
        }
    }
}
