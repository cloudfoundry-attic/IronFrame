using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Containers;
    using Protocol;
    using Warden.Utilities;

    public class PowershellCommand : ProcessCommand
    {
        private readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private const string powershellArgFmt = "-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File \"{0}\"";

        public PowershellCommand(IContainer container, IRemoteCommandArgs rcArgs, ResourceLimits rlimits)
            : base(container, rcArgs.Arguments, rcArgs.Privileged, rcArgs.Environment, rlimits)
        {
            if (base.arguments.IsNullOrEmpty())
            {
                throw new ArgumentException("powershell: command must have at least one argument.");
            }
        }

        protected override TaskCommandResult DoExecute()
        {
            using (var ps1File = container.TempFileInContainer(".ps1"))
            {
                string[] script = container.ConvertToPathsWithin(arguments).ToArray();

                log.Debug("Invoke Powershell ({0}): {1}", ps1File, script);
                File.WriteAllLines(ps1File.FullName, script, Encoding.ASCII);
                string psArgs = String.Format(powershellArgFmt, ps1File.FullName);
                return base.RunProcess(ps1File.DirectoryName, "powershell.exe", psArgs);
            }
        }
    }
}
