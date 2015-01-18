using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Container.Helpers
{
    public class TempFile : IDisposable
    {
        private readonly string basePath;
        private readonly FileInfo tempFileInfo;

        /// <summary>
        /// Create a tempfile using the full path specified.
        /// </summary>
        /// <remarks>
        /// Use this overload if you know the name of the file you want to use but
        /// want to guarantee it gets deleted after you are done with it.
        /// </remarks>
        public TempFile(string path, bool deleteIfExists)
        {
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("path");
            }

            this.basePath = Path.GetDirectoryName(path);
            FileMode openMode = deleteIfExists ? FileMode.Create : FileMode.CreateNew;

            this.tempFileInfo = GetTempFileInfo(
                this.basePath,
                () => path,
                openMode);
        }

        public TempFile(string basePath, string extension = ".tmp")
        {
            if (basePath.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("basePath");
            }

            this.basePath = basePath;

            if (!Directory.Exists(this.basePath))
            {
                throw new ArgumentException("basePath directory must exist.");
            }

            this.tempFileInfo = GetTempFileInfo(
                this.basePath,
                () => GenerateRandomFileName(extension),
                FileMode.CreateNew
                );
        }

        public string FullName
        {
            get { return tempFileInfo.FullName; }
        }

        public FileInfo FileInfo
        {
            get { return tempFileInfo; }
        }

        public string DirectoryName
        {
            get { return basePath; }
        }

        public void Dispose()
        {
            tempFileInfo.Delete();
        }

        private static FileInfo GetTempFileInfo(string basePath, Func<string> filenameGenerator, FileMode openMode)
        {
            string fileName;
            int attempt = 0;
            bool exit = false;
            do
            {
                // If the subdirectories don't exist, create them.
                if (!basePath.IsNullOrWhiteSpace() && !Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                fileName = filenameGenerator();
                try
                {
                    using (new FileStream(fileName, openMode)) { }
                    exit = true;
                }
                catch (IOException ex)
                {
                    if (++attempt == 10)
                    {
                        throw new IOException(String.Format("No unique temporary file name in '{0}' is available.", basePath), ex);
                    }
                }

            } while (!exit);

            return new FileInfo(fileName);
        }

        public string GenerateRandomFileName(string extension)
        {
            string fileName = Path.GetRandomFileName();
            fileName = Path.ChangeExtension(fileName, extension);
            fileName = Path.Combine(basePath, fileName);

            return fileName;
        }

        public string ReadAllText()
        {
            return File.ReadAllText(FullName);
        }
    }
}
