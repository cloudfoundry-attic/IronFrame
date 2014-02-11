using System;
using System.Collections.Generic;
using System.Text;
using IronFoundry.Warden.Containers;

namespace IronFoundry.Warden.Tasks
{
    public abstract class PathCommand : TaskCommand
    {
        public PathCommand(Container container, string[] arguments)
            : base(container, arguments)
        {
            if (base.arguments.IsNullOrEmpty())
            {
                throw new ArgumentNullException("Command requires at least one argument.");
            }
        }

        public override TaskCommandResult Execute()
        {
            TaskCommandResult finalResult = null;
            var output = new StringBuilder();

            foreach (string path in arguments)
            {
                try
                {
                    ProcessPath(path, output);
                }
                catch (Exception ex)
                {
                    finalResult = new TaskCommandResult(1, null, ex.Message);
                    break;
                }
            }

            if (finalResult == null)
            {
                string stdout = output.ToString();
                finalResult = new TaskCommandResult(0, stdout, null);
            }

            return finalResult;
        }

        protected abstract void ProcessPath(string path, StringBuilder output);
    }
}
