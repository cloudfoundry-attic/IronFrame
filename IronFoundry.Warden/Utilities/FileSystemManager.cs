using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IronFoundry.Warden.Utilities
{
    public class PlatformFileSystem
    {
        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        public virtual string GetFileName(string file)
        {
            return Path.GetFileName(file);
        }

        public virtual FileAttributes GetAttributes(string file)
        {
            return File.GetAttributes(file);
        }

        public virtual void Copy(string source, string destination, bool overwrite)
        {
            File.Copy(source, destination, overwrite);
        }

        public virtual void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
        {
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourceDirectory, destinationDirectory, overwrite);
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
    }
}
