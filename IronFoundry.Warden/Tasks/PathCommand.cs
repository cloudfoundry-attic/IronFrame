using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Tasks
{
    public abstract class PathCommand : RemoteCommand
    {
        protected readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        override protected TaskCommandResult Invoke()
        {
            TaskCommandResult finalResult = null;
            var output = new StringBuilder();

            foreach (string path in this.CommandArgs.Arguments)
            {
                try
                {
                    ProcessPath(path, output);
                }
                catch (Exception ex)
                {
                    log.Error("ProcessPath Exception: {0}", ex.ToString());
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
