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

        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        public virtual void ExtractTarArchive(Stream tarStream, string destinationDirectoryPath)
        {
            using (var tar = TarArchive.CreateInputTarArchive(tarStream))
            {
                tar.ExtractContents(destinationDirectoryPath);
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

        public virtual Stream OpenRead(string path)
        {
            return File.OpenRead(path);
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

        public virtual void ExtractTarFile(string tarFilePath, string destinationPath, bool decompress)
        {
            if (fileSystem.Exists(destinationPath) &&
                !fileSystem.GetAttributes(destinationPath).HasFlag(FileAttributes.Directory))
            {
                throw new InvalidOperationException("The destination path must not refer to a file.");
            }

            using (var fileStream = fileSystem.OpenRead(tarFilePath))
            {
                using (var tarStream = GetTarStream(fileStream, decompress))
                {
                    fileSystem.CreateDirectory(destinationPath);
                    fileSystem.ExtractTarArchive(tarStream, destinationPath);
                }
            }
        }

        Stream GetTarStream(Stream stream, bool decompress)
        {
            if (decompress)
                return new GZipInputStream(stream);
            else
                return stream;
        }
    }
}
