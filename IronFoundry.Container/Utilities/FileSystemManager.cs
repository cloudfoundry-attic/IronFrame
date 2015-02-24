using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using IronFoundry.Container.Win32;

namespace IronFoundry.Container.Utilities
{
    internal class PlatformFileSystem
    {
        public virtual void Copy(string source, string destination, bool overwrite)
        {
            File.Copy(source, destination, overwrite);
        }

        public virtual void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
        {
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourceDirectory, destinationDirectory, overwrite);
        }

        public virtual void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public virtual void CreateDirectory(string path, DirectorySecurity security)
        {
            Directory.CreateDirectory(path, security);
        }

        public virtual void DeleteDirectory(string path)
        {
            Directory.Delete(path, true);
        }

        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        public virtual bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public virtual FileAttributes GetAttributes(string file)
        {
            return File.GetAttributes(file);
        }

        public virtual string GetFileName(string file)
        {
            return Path.GetFileName(file);
        }

        string GetFilePathFromTarRecordName(string name)
        {
            var path = name.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(path))
                path = path.Substring(Path.GetPathRoot(path).Length);

            return path;
        }

        string GetTarRecordNameFromFilePath(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/').TrimEnd('/');
        }

        bool IsPathRelative(string rootPath, string path)
        {
            if (!rootPath.EndsWith("\\"))
                rootPath += "\\";

            return path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase);
        }

        public virtual Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return new FileStream(path, fileMode, fileAccess, fileShare);
        }

        public virtual Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public virtual Stream OpenWrite(string path)
        {
            return File.OpenWrite(path);
        }

        public FileSystemRights ComputeEffectiveAccessRights(string directory, IdentityReference identity)
        {
            FileSystemEffectiveAccessComputer effectiveAccess = new FileSystemEffectiveAccessComputer();
            var rights = effectiveAccess.ComputeAccess(directory, identity);
            return rights;
        }

        public DirectorySecurity GetDirectoryAccessSecurity(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            DirectorySecurity security = dirInfo.GetAccessControl();
            return security;
        }

        public void SetDirectoryAccessSecurity(string path, DirectorySecurity security)
        {
            var dirInfo = new DirectoryInfo(path);
            dirInfo.SetAccessControl(security);
        }
    }

    internal class FileSystemManager
    {
        private readonly PlatformFileSystem fileSystem;

        public FileSystemManager() : this(new PlatformFileSystem())
        {
        }

        public FileSystemManager(PlatformFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public virtual void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            if (fileSystem.Exists(sourceFilePath) &&
                fileSystem.GetAttributes(sourceFilePath).HasFlag(FileAttributes.Directory))
            {
                throw new InvalidOperationException("The source path must not refer to a directory.");
            }

            if (fileSystem.Exists(destinationFilePath) &&
                fileSystem.GetAttributes(destinationFilePath).HasFlag(FileAttributes.Directory))
            {
                throw new InvalidOperationException("The destination path must not refer to a directory.");
            }

            var destinationFileDirectoryPath = Path.GetDirectoryName(destinationFilePath);
            fileSystem.CreateDirectory(destinationFileDirectoryPath);
            fileSystem.Copy(sourceFilePath, destinationFilePath, true);
        }

        public virtual void Copy(string source, string destination)
        {
            var sourceIsDirectory = fileSystem.GetAttributes(source).HasFlag(FileAttributes.Directory);
            var destinationIsDirectory = fileSystem.GetAttributes(destination).HasFlag(FileAttributes.Directory);

            if (sourceIsDirectory && destinationIsDirectory)
            {
                fileSystem.CopyDirectory(source, destination, true);
            }
            else if (!sourceIsDirectory && destinationIsDirectory)
            {
                var destinationFilePath = Path.Combine(destination, fileSystem.GetFileName(source));
                fileSystem.Copy(source, destinationFilePath, true);
            }
            else if (sourceIsDirectory && !destinationIsDirectory)
            {
                throw new InvalidOperationException(string.Format("Unable to copy directory {0} to file {1}.", source, destination));
            }
            else
            {
                fileSystem.Copy(source, destination, true);
            }
        }

        public virtual void DeleteDirectory(string path)
        {
            fileSystem.DeleteDirectory(path);
        }

        public virtual bool FileExists(string path)
        {
            return fileSystem.Exists(path);
        }

        /// <summary>
        /// Returns true if the path refers to an existing directory.
        /// </summary>
        public virtual bool DirectoryExists(string path)
        {
            return fileSystem.DirectoryExists(path);
        }

        /// <summary>
        /// Get the access that the specified user has to the specified directory.
        /// </summary>
        public virtual FileAccess GetEffectiveDirectoryAccess(string directory, IdentityReference identity)
        {
            FileAccess access = new FileAccess();

            FileSystemRights effectiveRights = fileSystem.ComputeEffectiveAccessRights(directory, identity);

            if (effectiveRights.HasFlag(FileSystemRights.ReadAndExecute))
            {
                access |= FileAccess.Read;
            }

            if (effectiveRights.HasFlag(FileSystemRights.Write | FileSystemRights.Delete))
            {
                access |= FileAccess.Write;
            }

            return access;
        }

        private IEnumerable<FileSystemAccessRule> GetAccessControlRules(FileAccess access, string username)
        {
            if ((int)access == 0)
            {
                // If no flags are set just return
                return new FileSystemAccessRule[0];
            }

            FileSystemAccessRule accessRule;
            const InheritanceFlags inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            List<FileSystemAccessRule> rules = new List<FileSystemAccessRule>();

            if (access.HasFlag(FileAccess.Read))
            {
                accessRule = new FileSystemAccessRule(username, FileSystemRights.ReadAndExecute, inheritanceFlags, PropagationFlags.None, AccessControlType.Allow);
                rules.Add(accessRule);
            }

            if (access.HasFlag(FileAccess.Write))
            {
                accessRule = new FileSystemAccessRule(username, FileSystemRights.Write, inheritanceFlags, PropagationFlags.None, AccessControlType.Allow);
                rules.Add(accessRule);

                accessRule = new FileSystemAccessRule(username, FileSystemRights.Delete, inheritanceFlags, PropagationFlags.None, AccessControlType.Allow);
                rules.Add(accessRule);
            }

            return rules;
        }

        /// <summary>
        /// Create a directory with the specified user access
        /// </summary>
        public virtual void CreateDirectory(string path, IEnumerable<UserAccess> userAccess)
        {
            IEnumerable<FileSystemAccessRule> rules = userAccess.SelectMany(ua => GetAccessControlRules(ua.Access, ua.UserName));

            DirectorySecurity security = new DirectorySecurity();
            foreach (FileSystemAccessRule rule in rules)
            {
                security.AddAccessRule(rule);
            }

            fileSystem.CreateDirectory(path, security);
        }

        public virtual void AddDirectoryAccess(string path, FileAccess access, string user)
        {
            DirectorySecurity security = fileSystem.GetDirectoryAccessSecurity(path);

            foreach (FileSystemAccessRule rule in GetAccessControlRules(access, user))
            {
                security.AddAccessRule(rule);
            }

            fileSystem.SetDirectoryAccessSecurity(path, security);
        }

        public virtual Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return fileSystem.Open(path, fileMode, fileAccess, fileShare);
        }
    }

    public class UserAccess
    {
        public FileAccess Access { get; set; }
        public string UserName { get; set; }
    }
}
