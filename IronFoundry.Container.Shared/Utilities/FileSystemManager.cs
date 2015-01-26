using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using IronFoundry.Container.Win32;

namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container
    public class PlatformFileSystem
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

        public virtual void CreateTarArchive(string sourceDirectoryPath, Stream tarStream)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryPath);

            var tarFileRecords = GetTarFileRecords(directoryInfo, "").ToList();

            using (var tarWriter = new TarOutputStream(tarStream))
            {
                foreach (var tarFileRecord in tarFileRecords)
                {
                    WriteFileToTar(tarWriter, tarFileRecord);
                }

                tarWriter.Finish();
            }
        }

        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        public virtual bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public virtual void ExtractTarArchive(Stream tarStream, string destinationDirectoryPath)
        {
            using (var tarReader = new TarInputStream(tarStream))
            {
                for (var tarEntry = tarReader.GetNextEntry(); tarEntry != null; tarEntry = tarReader.GetNextEntry())
                {
                    if (tarEntry.IsDirectory)
                        continue;

                    string relativeFilePath = GetFilePathFromTarRecordName(tarEntry.Name);
                    string filePath = Path.GetFullPath(Path.Combine(destinationDirectoryPath, relativeFilePath));

                    if (!IsPathRelative(destinationDirectoryPath, filePath))
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "The normalized path '{0}' is not relative to the destination path '{1}'.",
                                filePath,
                                destinationDirectoryPath));
                    }

                    string directoryPath = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directoryPath);

                    using (var fs = OpenWrite(filePath))
                    {
                        tarReader.CopyEntryContents(fs);
                    }

                    var modifiedTime = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc);
                    File.SetLastWriteTimeUtc(filePath, modifiedTime);
                }
            }
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

        IEnumerable<TarFileRecord> GetTarFileRecords(DirectoryInfo directoryInfo, string basePath)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                var relativeFilePath = Path.Combine(basePath, fileInfo.Name);
                
                yield return new TarFileRecord
                {
                    Name = GetTarRecordNameFromFilePath(relativeFilePath),
                    Size = fileInfo.Length,
                    FilePath = fileInfo.FullName,
                };
            }

            foreach (var childDirectoryInfo in directoryInfo.GetDirectories())
            {
                foreach (var childRecord in GetTarFileRecords(childDirectoryInfo, Path.Combine(basePath, childDirectoryInfo.Name)))
                    yield return childRecord;
            }
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

        public virtual Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public virtual Stream OpenWrite(string path)
        {
            return File.OpenWrite(path);
        }

        void WriteFileToTar(TarOutputStream tarWriter, TarFileRecord tarFileRecord)
        {
            var tarEntry = TarEntry.CreateTarEntry(tarFileRecord.Name);
            tarEntry.Size = tarFileRecord.Size;
            tarEntry.ModTime = File.GetLastWriteTimeUtc(tarFileRecord.FilePath);

            tarWriter.PutNextEntry(tarEntry);
            using (var fs = OpenRead(tarFileRecord.FilePath))
            {
                fs.CopyTo(tarWriter);
                tarWriter.CloseEntry();
            }
        }

        class TarFileRecord
        {
            public string Name { get; set; }
            public long Size { get; set; }
            public string FilePath { get; set; }
        }


        /// <summary>
        /// Returns true if the user has read access to the directory
        /// </summary>
        public bool HasDirectoryReadAccess(string directory, NetworkCredential credential)
        {
            // TODO - Change this to use the Windows API to determine the effective rights:
            // Either:
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa446637%28v=vs.85%29.aspx
            // OR
            // https://code.msdn.microsoft.com/windowsapps/Effective-access-rights-dd5b13a8#content

            bool hasReadAccess = false;

            using (Impersonator.GetContext(credential, true))
            {
                // Test for READ access
                var dirInfo = new DirectoryInfo(directory);
                Action readAcl = () => dirInfo.GetAccessControl(AccessControlSections.Access);
                if (!readAcl.ThrowsException<UnauthorizedAccessException>())
                {
                    hasReadAccess = true;
                }
            }

            return hasReadAccess;
        }

        /// <summary>
        /// Returns true if the specified user can write to the directory
        /// </summary>
        public bool HasDirectoryWriteAccess(string directory, NetworkCredential credential)
        {
            // TODO - Change this to use the Windows API to determine the effective rights:
            // Either:
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa446637%28v=vs.85%29.aspx
            // OR
            // https://code.msdn.microsoft.com/windowsapps/Effective-access-rights-dd5b13a8#content

            bool hasWriteAccess = false;

            using (Impersonator.GetContext(credential, true))
            {
                string testFileName = "IF_WARDEN_WRITE_TEST." + Path.GetRandomFileName();
                string testFilePath = Path.Combine(directory, testFileName);
                try
                {
                    FileInfo file = new FileInfo(testFilePath);
                    file.Create().Close();
                    file.Delete();

                    hasWriteAccess = true;
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return hasWriteAccess;
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

    // BR: Move this to IronFoundry.Container
    public class FileSystemManager
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

        public virtual void CreateTarFile(string sourcePath, string tarFilePath, bool compress)
        {
            if (fileSystem.Exists(tarFilePath) &&
                fileSystem.GetAttributes(tarFilePath).HasFlag(FileAttributes.Directory))
            {
                throw new InvalidOperationException("The tar file path must not refer to a directory.");
            }

            using (var fileStream = fileSystem.OpenWrite(tarFilePath))
            {
                using (var tarStream = GetTarOutputStream(fileStream, compress))
                {
                    var tarDirectoryPath = Path.GetDirectoryName(tarFilePath);
                    fileSystem.CreateDirectory(tarDirectoryPath);
                    fileSystem.CreateTarArchive(sourcePath, tarStream);
                }
            }
        }

        public virtual void ExtractTarFile(string tarFilePath, string destinationPath, bool decompress)
        {
            if (fileSystem.Exists(destinationPath) &&
                !fileSystem.GetAttributes(destinationPath).HasFlag(FileAttributes.Directory))
            {
                throw new InvalidOperationException("The destination path must not refer to a file.");
            }

            using (var fileStream = fileSystem.OpenRead(tarFilePath))
            {
                using (var tarStream = GetTarInputStream(fileStream, decompress))
                {
                    fileSystem.CreateDirectory(destinationPath);
                    fileSystem.ExtractTarArchive(tarStream, destinationPath);
                }
            }
        }

        Stream GetTarInputStream(Stream stream, bool decompress)
        {
            if (decompress)
                return new GZipInputStream(stream);
            else
                return stream;
        }

        Stream GetTarOutputStream(Stream stream, bool compress)
        {
            if (compress)
                return new GZipOutputStream(stream);
            else
                return stream;
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
        public virtual FileAccess GetEffectiveDirectoryAccess(string directory, NetworkCredential credential)
        {
            // TODO Modify this so it doesn't require the network credentials, only the username.

            FileAccess access = new FileAccess();

            if (fileSystem.HasDirectoryReadAccess(directory, credential))
            {
                access |= FileAccess.Read;
            }

            if (fileSystem.HasDirectoryWriteAccess(directory, credential))
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
    }

    public class UserAccess
    {
        public FileAccess Access { get; set; }
        public string UserName { get; set; }
    }
}
