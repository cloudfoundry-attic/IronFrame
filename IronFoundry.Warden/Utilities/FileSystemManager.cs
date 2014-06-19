using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace IronFoundry.Warden.Utilities
{
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
    }

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
    }
}
