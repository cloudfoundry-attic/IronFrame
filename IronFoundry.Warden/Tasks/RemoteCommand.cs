using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container;
using IronFoundry.Warden.Containers;
using NLog;

namespace IronFoundry.Warden.Tasks
{
    public abstract class RemoteCommand
    {
        protected IContainer Container { get; set; }
        protected IRemoteCommandArgs CommandArgs { get; set; }
        protected IProcessIO IO { get; set; }

        public static RemoteCommand Create(IContainer container, IProcessIO io, string commandType, IRemoteCommandArgs args)
        {
            RemoteCommand command = null;

            switch (commandType.ToLowerInvariant())
            {
                case "mkdir":
                    command = new MkDirCommand();
                    break;
                case "touch":
                    command = new TouchCommand();
                    break;
                case "exe":
                    command = new ExeCommand();
                    break;
                case "ps1":
                    command = new PowerShellCommand();
                    break;
                case "unzip":
                    command = new UnzipCommand();
                    break;
                case "tar":
                    command = new TarCommand();
                    break;
                case "replace-tokens":
                    command = new ReplaceTokensCommand();
                    break;
                default:
                    throw new NotImplementedException(string.Format("Command type {0} is unknown.", commandType));
            }

            command.Container = container;
            command.IO = io;
            command.CommandArgs = args;

            return command;
        }

        public Task<TaskCommandResult> InvokeAsync()
        {
            return Task.Run((Func<TaskCommandResult>)this.Invoke);
        }

        protected abstract TaskCommandResult Invoke();
    }
}
