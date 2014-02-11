using System;
using System.IO;
using IronFoundry.Warden.Containers;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace IronFoundry.Warden.Tasks
{
    public class TarCommand : TaskCommand
    {
        private const string CurrentDirectory = ".";
        private static readonly char[] trimChars = new[] { '/' };
        private readonly string command;
        private readonly string directory;
        private readonly string tarFile;

        public TarCommand(Container container, string[] arguments)
            : base(container, arguments)
        {
            if (arguments.Length != 3)
            {
                throw new ArgumentException("tar command must have three arguments: operation (x or c), directory and file name.");
            }

            this.command = arguments[0];
            if (this.command.IsNullOrWhiteSpace() || (!(this.command == "x" || this.command == "c")))
            {
                throw new ArgumentException("tar command: first argument must be x (extract) or c (create).");
            }

            this.directory = container.ConvertToPathWithin(arguments[1]);
            if (!Directory.Exists(this.directory))
            {
                throw new ArgumentException(String.Format("tar command: second argument must be directory that exists ('{0}')", this.directory));
            }

            if (arguments[2].IsNullOrWhiteSpace())
            {
                throw new ArgumentException("tar command: third argument must be a file name.");
            }
            else
            {
                this.tarFile = container.ConvertToPathWithin(arguments[2]);
                if (this.command == "x" && !File.Exists(this.tarFile))
                {
                    throw new ArgumentException("tar command: third argument must be a file name that exists.");
                }
            }
        }

        public override TaskCommandResult Execute()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(directory);
                switch (command)
                {
                    case "c" :
                        CreateTarArchive();
                        break;
                    case "x" :
                        ExtractTarArchive();
                        break;
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
            return new TaskCommandResult(0, String.Format("tar: '{0}' -> '{1}'", directory, tarFile), null); 
        }

        private void CreateTarArchive()
        {
            if (File.Exists(tarFile))
            {
                File.Delete(tarFile);
            }

            using (var fs = File.Open(tarFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var gzipStream = new GZipOutputStream(fs))
                {
                    using (var tarArchive = TarArchive.CreateOutputTarArchive(gzipStream))
                    {
                        tarArchive.RootPath = CurrentDirectory;
                        var tarEntry = TarEntry.CreateEntryFromFile(CurrentDirectory);
                        tarArchive.WriteEntry(tarEntry, true);
                    }
                }
            }
        }

        private void ExtractTarArchive()
        {
            using (var fs = File.Open(tarFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var gzipStream = new GZipInputStream(fs))
                {
                    using (var tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
                    {
                        tarArchive.ExtractContents(CurrentDirectory);
                    }
                }
            }
        }
    }
}
