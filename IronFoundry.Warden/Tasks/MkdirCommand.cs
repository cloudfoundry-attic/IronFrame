namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using IronFoundry.Warden.Containers;

    public class MkdirCommand : PathCommand
    {
        public MkdirCommand(Container container, string[] arguments)
            : base(container, arguments)
        {
            if (base.arguments.IsNullOrEmpty())
            {
                throw new ArgumentException("mkdir command requires at least one argument.");
            }
        }

        protected override void ProcessPath(string path, StringBuilder output)
        {
            string pathInContainer = container.ConvertToPathWithin(path);
            Directory.CreateDirectory(pathInContainer);
            output.AppendFormat("mkdir: created directory '{0}'", pathInContainer).AppendLine();
        }
    }
}
