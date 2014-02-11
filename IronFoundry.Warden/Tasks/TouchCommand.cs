using System;
using System.IO;
using System.Text;
using IronFoundry.Warden.Configuration;
using IronFoundry.Warden.Containers;

namespace IronFoundry.Warden.Tasks
{
    public class TouchCommand : PathCommand
    {
        private static readonly WardenConfig config = new WardenConfig();

        public TouchCommand(Container container, string[] arguments)
            : base(container, arguments)
        {
        }

        protected override void ProcessPath(string path, StringBuilder output)
        {
            string pathInContainer = container.ConvertToPathWithin(path);
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
