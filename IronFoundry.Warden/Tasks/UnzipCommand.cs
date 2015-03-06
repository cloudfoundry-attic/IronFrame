using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using IronFoundry.Warden.Properties;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Tasks
{
    class UnzipCommand : RemoteCommand
    {
        private FileInfo zipFile;
        private DirectoryInfo destDir;

        private void Initialize()
        {
            var arguments = this.CommandArgs.Arguments;

            if (arguments == null || arguments.Length != 2)
            {
                throw new ArgumentException("unzip: must have exactly two arguments.");
            }

            if (String.IsNullOrWhiteSpace(arguments[0]))
            {
                throw new ArgumentNullException(Resources.UnzipCommand_MissingZipFileErrorMessage);
            }

            this.zipFile = new FileInfo(arguments[0]);
            if (!this.zipFile.Exists)
            {
                throw new ArgumentException(Resources.UnzipCommand_MissingZipFileErrorMessage);
            }

            if (String.IsNullOrWhiteSpace(arguments[1]))
            {
                throw new ArgumentNullException(Resources.UnzipCommand_MissingDestDirErrorMessage);
            }
            this.destDir = new DirectoryInfo(this.Container.ConvertToUserPathWithin(arguments[1]));
        }

        protected override TaskCommandResult Invoke()
        {
            Initialize();

            if (destDir.Exists)
            {
                destDir.Delete(true);
            }
            else
            {
                destDir.Create();
            }

            var fastZip = new FastZip();
            fastZip.ExtractZip(zipFile.FullName, destDir.FullName, null);

            return new TaskCommandResult(
                0,
                String.Format("Extracted '{0}' to '{1}'", zipFile.FullName, destDir.FullName),
                null);
        }
    }
}
