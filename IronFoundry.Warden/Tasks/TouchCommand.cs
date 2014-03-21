namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.IO;
    using System.Text;
    using IronFoundry.Warden.Configuration;
    using IronFoundry.Warden.Containers;
    using Warden.Utilities;

    public class TouchCommand : PathCommand
    {
        private static readonly WardenConfig config = new WardenConfig();

        public TouchCommand(IContainer container, string[] arguments)
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
