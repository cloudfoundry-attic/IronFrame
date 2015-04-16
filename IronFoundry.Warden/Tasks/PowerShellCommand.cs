using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Tasks
{
    class PowerShellCommand : ProcessCommand
    {
        private readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private const string powershellArgFmt = "-NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File \"{0}\"";

        protected override TaskCommandResult DoExecute()
        {
            using (var ps1File = this.Container.TempFileInContainer(".ps1"))
            {
                string[] script = this.CommandArgs.Arguments.Select(this.Container.ReplaceRootTokensWithUserPath).ToArray();

                log.Trace("Invoke Powershell ({0}): {1}", ps1File.FullName, string.Join(@"\n", script));
                File.WriteAllLines(ps1File.FullName, script, Encoding.ASCII);
                string psArgs = String.Format(powershellArgFmt, ps1File.FullName);
                return base.RunProcess(ps1File.DirectoryName, "powershell.exe", new []{ psArgs });
            }
        }
    }
}
