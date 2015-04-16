using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Warden.Utilities;
using NLog;

namespace IronFoundry.Warden.Tasks
{
    class ReplaceTokensCommand : PathCommand
    {
        override protected void ProcessPath(string path, StringBuilder output)
        {
            if (File.Exists(path))
            {
                using (var tempFile = new TempFile(this.Container.Directory.UserPath))
                {
                    var lines = File.ReadLines(path, Encoding.ASCII).Select(this.Container.ReplaceRootTokensWithUserPath).ToArray();
                    File.WriteAllLines(tempFile.FullName, lines, Encoding.ASCII);
                    File.Copy(tempFile.FullName, path, true);
                }
                output.AppendFormat("replaced tokens in file '{0}'", path).AppendLine();
            }
            else
            {
                throw new WardenException("file does not exist: '{0}'", path);
            }
        }
    }
}
