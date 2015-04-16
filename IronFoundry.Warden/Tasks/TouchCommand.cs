using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Tasks
{
    class TouchCommand : PathCommand
    {
        override protected void ProcessPath(string path, StringBuilder output)
        {
            string pathInContainer = this.Container.ConvertToUserPathWithin(path);

            log.Trace("Touch: {0}", pathInContainer);
            var fi = new FileInfo(pathInContainer);
            if (fi.Exists)
            {
                fi.CreationTime = fi.LastWriteTime = fi.LastAccessTime = DateTime.Now;
            }
            else
            {
                File.WriteAllText(fi.FullName, String.Empty);
            }
            output.AppendFormat("touch file '{0}'", fi.FullName).AppendLine();
        }
    }
}
