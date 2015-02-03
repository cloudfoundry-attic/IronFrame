using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Tasks
{
    class ExeCommand : ProcessCommand
    {
        private string executable;
        private string [] args;
        private string workingDir;

        protected override TaskCommandResult DoExecute()
        {
            var arguments = this.CommandArgs.Arguments;

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
                    this.args = arguments.Skip(1).ToArray();
                }

                this.workingDir = this.CommandArgs.WorkingDirectory;
            }

            return base.RunProcess(workingDir, executable, args);
        }
    }
}
