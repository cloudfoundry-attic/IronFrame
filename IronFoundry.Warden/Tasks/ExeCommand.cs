using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Containers;
    using Protocol;

    public class ExeCommand : ProcessCommand
    {
        private readonly string executable;
        private readonly string args;
        private readonly string workingDir;

        public ExeCommand(IContainer container, IRemoteCommandArgs rcArgs, ResourceLimits rlimits)
            : base(container, rcArgs.Arguments, rcArgs.Privileged, rcArgs.Environment, rlimits)
        {
            if (arguments.IsNullOrEmpty())
            {
                throw new ArgumentNullException("arguments");
            }
            else
            {
                this.executable = arguments[0];
                if (this.executable.IsNullOrWhiteSpace())
                {
                    throw new ArgumentNullException("First argument must be executable name.");
                }
                if (arguments.Length > 1)
                {
                    this.args = String.Join(" ", arguments.Skip(1));
                }

                workingDir = rcArgs.WorkingDirectory;
            }
        }

        protected override TaskCommandResult DoExecute()
        {
            return base.RunProcess(workingDir, executable, args);
        }
    }
}
