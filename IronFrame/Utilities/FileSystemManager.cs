using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Compilation;
using Microsoft.Win32.SafeHandles;

namespace IronFrame.Utilities
{
    enum SymbolicLink
    {
        File = 0,
        Directory = 1
    }

    internal class PlatformFileSystem
    {
        private const UInt32 GENERIC_READ = 0x80000000;
        private const UInt32 FILE_SHARE_READ = 0x01;
        private const UInt32 OPEN_EXISTING = 0x3;
        private const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const UInt32 FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
        private const UInt32 FSCTL_GET_REPARSE_POINT = 0x900a8;
        private const UInt32 IO_REPARSE_TAG_SYMLINK = 0xA000000C;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct REPARSE_DATA_BUFFER
        {
            public UInt32 ReparseTag;
            public UInt16 ReparseData;
            public UInt16 Reserverd;
            public UInt16 SubstituteNameOffset;
            public UInt16 SubstituteNameLength;
            public UInt16 PrintNameOffset;
            public UInt16 PrintNameLength;
            public UInt32 Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16000)] public string PathBuffer;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateSymbolicLink(
            string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode, IntPtr lpSecurityAttributes,
            UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, UInt32 dwIoControlCode, IntPtr lpInBuffer,
            UInt32 nInBufferSize, out REPARSE_DATA_BUFFER rdb, UInt32 nOutBufferSize, out IntPtr lpBytesReturned,
            IntPtr lpOverlapped);

        public static string SymlinkTargetFromHandle(SafeFileHandle handle)
        {
            var rdb = new REPARSE_DATA_BUFFER();
            var x = 10;
            var outSize = new IntPtr(x);

            if (
                !DeviceIoControl(handle, FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, out rdb, (UInt32) Marshal.SizeOf(rdb),
                    out outSize, IntPtr.Zero))
            {
                throw new Win32Exception();
            }

            if (rdb.ReparseTag != IO_REPARSE_TAG_SYMLINK)
            {
                throw new ApplicationException("not a symlink");
            }

            var arrayOffset = rdb.PrintNameOffset/2;
            return rdb.PathBuffer.Substring(arrayOffset, arrayOffset + (rdb.PrintNameLength/2));
        }

        public static SafeFileHandle OpenSymlinkDirectory(string dir)
        {
            if (String.IsNullOrEmpty(dir))
            {
                throw new ArgumentNullException("dir");
            }

            var handle = CreateFile(
                dir,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero
            );

            if (handle.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return handle;
        }

        public virtual void SymlinkDirectory(string symlinkFile, string target)
        {
            if (!CreateSymbolicLink(symlinkFile, target, SymbolicLink.Directory))
            {
                throw new Win32Exception();
            }
        }

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

        public virtual IEnumerable<string> EnumerateDirectories(string path)
        {
            return Directory.EnumerateDirectories(path);
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

    internal interface IFileSystemManager
    {
        void CopyFile(string sourceFilePath, string destinationFilePath);
        void Copy(string source, string destination);
        void Symlink(string symlinkFile, string target);
        void DeleteDirectory(string path);
        bool FileExists(string path);
        bool DirIsSymlink(string dir);
        string GetSymlinkTarget(string source);

        /// <summary>
        /// Returns true if the path refers to an existing directory.
        /// </summary>
        bool DirectoryExists(string path);

        IEnumerable<string> EnumerateDirectories(string path);

        /// <summary>
        /// Get the access that the specified user has to the specified directory.
        /// </summary>
        FileAccess GetEffectiveDirectoryAccess(string directory, IdentityReference identity);

        /// <summary>
        /// Create a directory with the specified user access
        /// </summary>
        void CreateDirectory(string path, IEnumerable<UserAccess> userAccess);

        void AddDirectoryAccess(string path, FileAccess access, string user);
        void RemoveDirectoryAccess(string path, string user);
        Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);
    }

    internal class FileSystemManager : IFileSystemManager
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
                throw new InvalidOperationException(string.Format("Unable to copy directory {0} to file {1}.", source,
                    destination));
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

        public virtual IEnumerable<string> EnumerateDirectories(string path)
        {
            return fileSystem.EnumerateDirectories(path);
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
            if ((int) access == 0)
            {
                // If no flags are set just return
                return new FileSystemAccessRule[0];
            }

            FileSystemAccessRule accessRule;
            const InheritanceFlags inheritanceFlags = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            List<FileSystemAccessRule> rules = new List<FileSystemAccessRule>();

            if (access.HasFlag(FileAccess.Read))
            {
                accessRule = new FileSystemAccessRule(username, FileSystemRights.ReadAndExecute, inheritanceFlags,
                    PropagationFlags.None, AccessControlType.Allow);
                rules.Add(accessRule);
            }

            if (access.HasFlag(FileAccess.Write))
            {
                accessRule = new FileSystemAccessRule(username, FileSystemRights.Write, inheritanceFlags,
                    PropagationFlags.None, AccessControlType.Allow);
                rules.Add(accessRule);

                accessRule = new FileSystemAccessRule(username, FileSystemRights.Delete, inheritanceFlags,
                    PropagationFlags.None, AccessControlType.Allow);
                rules.Add(accessRule);
            }

            return rules;
        }

        /// <summary>
        /// Create a directory with the specified user access
        /// </summary>
        public virtual void CreateDirectory(string path, IEnumerable<UserAccess> userAccess)
        {
            IEnumerable<FileSystemAccessRule> rules =
                userAccess.SelectMany(ua => GetAccessControlRules(ua.Access, ua.UserName));

            DirectorySecurity security = new DirectorySecurity();
            foreach (FileSystemAccessRule rule in rules)
            {
                security.AddAccessRule(rule);
            }

            fileSystem.CreateDirectory(path, security);
        }

        public virtual void AddDirectoryAccess(string path, FileAccess access, string user)
        {
            using (var dirMutex = new System.Threading.Mutex(false, path.Replace('\\', '_')))
            {
                dirMutex.WaitOne();
                try
                {
                    DirectorySecurity security = fileSystem.GetDirectoryAccessSecurity(path);

                    foreach (FileSystemAccessRule rule in GetAccessControlRules(access, user))
                    {
                        security.AddAccessRule(rule);
                    }

                    fileSystem.SetDirectoryAccessSecurity(path, security);
                }
                finally
                {
                    dirMutex.ReleaseMutex();
                }
            }
        }

        public virtual void RemoveDirectoryAccess(string path, string user)
        {
            if (DirectoryExists(path) || FileExists(path))
            {
                using (var dirMutex = new System.Threading.Mutex(false, path.Replace('\\', '_')))
                {
                    dirMutex.WaitOne();
                    try
                    {
                        DirectorySecurity security = fileSystem.GetDirectoryAccessSecurity(path);

                        // RemoveAccessRuleAll ignores everything in the ACL but the username
                        var userACL = new FileSystemAccessRule(user, FileSystemRights.ListDirectory,
                            AccessControlType.Allow);
                        security.RemoveAccessRuleAll(userACL);

                        fileSystem.SetDirectoryAccessSecurity(path, security);
                    }
                    finally
                    {
                        dirMutex.ReleaseMutex();
                    }
                }
            }
        }


        public virtual Stream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return fileSystem.Open(path, fileMode, fileAccess, fileShare);
        }

        public void Symlink(string symlinkFile, string target)
        {
            fileSystem.SymlinkDirectory(symlinkFile, target);
        }

        public virtual bool DirIsSymlink(string directory)
        {
            return (DirectoryExists(directory) || FileExists(directory))
                   && (fileSystem.GetAttributes(directory) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

        public virtual string GetSymlinkTarget(string source)
        {
            var strDest = "";

            using (var handle = PlatformFileSystem.OpenSymlinkDirectory(source))
            {
                strDest = PlatformFileSystem.SymlinkTargetFromHandle(handle);
            }

            return strDest;
        }
    }

    internal class UserAccess
    {
        public FileAccess Access { get; set; }
        public string UserName { get; set; }
    }
}