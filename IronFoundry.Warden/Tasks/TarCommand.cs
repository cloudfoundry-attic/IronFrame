using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Tasks
{
    class TarCommand : RemoteCommand
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        private const string CurrentDirectory = ".";
        private static readonly char[] trimChars = new[] { '/' };
        private string command;
        private string directory;
        private string tarFile;

        private void Initialize()
        {
            var arguments = this.CommandArgs.Arguments;

            if (arguments.Length != 3)
            {
                throw new ArgumentException("tar command must have three arguments: operation (x or c), directory and file name.");
            }

            this.command = arguments[0];
            if (String.IsNullOrWhiteSpace(this.command) || (!(this.command == "x" || this.command == "c")))
            {
                throw new ArgumentException("tar command: first argument must be x (extract) or c (create).");
            }

            this.directory = this.Container.ConvertToUserPathWithin(arguments[1]);
            if (!Directory.Exists(this.directory))
            {
                throw new ArgumentException(String.Format("tar command: second argument must be directory that exists ('{0}')", this.directory));
            }

            if (String.IsNullOrWhiteSpace(arguments[2]))
            {
                throw new ArgumentException("tar command: third argument must be a file name.");
            }
            else
            {
                this.tarFile = this.Container.ConvertToUserPathWithin(arguments[2]);
                if (this.command == "x" && !File.Exists(this.tarFile))
                {
                    throw new ArgumentException("tar command: third argument must be a file name that exists.");
                }
            }
        }

        protected override TaskCommandResult Invoke()
        {
            Initialize();

            switch (command)
            {
                case "c":
                    CreateTarArchive();
                    break;
                case "x":
                    ExtractTarArchive();
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unknown tar command '{0}'", command));
            }

            return new TaskCommandResult(0, String.Format("tar: '{0}' -> '{1}'", directory, tarFile), null);
        }

        private void CreateTarArchive()
        {
            log.Trace("TAR (Create): '{0}' -> '{1}'", directory, tarFile);

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
                        tarArchive.RootPath = directory.Replace('\\', '/');
                        if (tarArchive.RootPath.EndsWith("/"))
                            tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

                        var tarEntry = TarEntry.CreateEntryFromFile(directory);
                        tarArchive.WriteEntry(tarEntry, true);

                        log.Debug("TAR DIRECTORY {0}", directory);
                        log.Debug("TAR ROOT {0}", tarArchive.RootPath);
                    }
                }
            }
        }

        private void ExtractTarArchive()
        {
            log.Trace("TAR (Extract): '{0}' -> '{1}'", tarFile, directory);

            using (var fs = File.Open(tarFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var gzipStream = new GZipInputStream(fs))
                {
                    using (var tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
                    {
                        tarArchive.ExtractContents(directory);
                    }
                }
            }
        }
    }
}
