using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Tasks
{
    class MkDirCommand : PathCommand
    {
        override protected void ProcessPath(string path, StringBuilder output)
        {
            if (CommandArgs.Arguments == null || CommandArgs.Arguments.Length == 0)
            {
                throw new ArgumentException("mkdir command requires at least one argument.");
            }

            string pathInContainer = Container.ConvertToUserPathWithin(path);

            log.Trace("MkDir: {0}", pathInContainer);
            Directory.CreateDirectory(pathInContainer);
            output.AppendFormat("mkdir: created directory '{0}'", pathInContainer).AppendLine();
        }
    }
}
