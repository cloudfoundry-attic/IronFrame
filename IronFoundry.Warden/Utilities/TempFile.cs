using System;
using System.IO;

namespace IronFoundry.Warden.Utilities
{
    public class TempFile : IDisposable
    {
        private readonly string basePath;
        private readonly FileInfo tempFileInfo;

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

            this.tempFileInfo = GetTempFileInfo(this.basePath, extension);
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

        private static FileInfo GetTempFileInfo(string basePath, string extension)
        {
            string fileName;
            int attempt = 0;
            bool exit = false;
            do
            {
                fileName = Path.GetRandomFileName();
                fileName = Path.ChangeExtension(fileName, extension);
                fileName = Path.Combine(basePath, fileName);

                try
                {
                    using (new FileStream(fileName, FileMode.CreateNew)) {}
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
    }
}
