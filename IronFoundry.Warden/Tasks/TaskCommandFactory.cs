using System;
using IronFoundry.Warden.Containers;
using IronFoundry.Warden.Protocol;

namespace IronFoundry.Warden.Tasks
{
    public class TaskCommandFactory
    {
        private readonly Container container;
        private readonly bool shouldImpersonate;
        private readonly ResourceLimits rlimits;

        public TaskCommandFactory(Container container, bool shouldImpersonate, ResourceLimits rlimits)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
            this.container = container;
            this.shouldImpersonate = shouldImpersonate;
            this.rlimits = rlimits;
        }

        public TaskCommand Create(string commandName, string[] arguments)
        {
            switch (commandName)
            {
                case "exe" :
                    return new ExeCommand(container, arguments, shouldImpersonate, rlimits);
                case "mkdir" :
                    return new MkdirCommand(container, arguments);
                case "iis" :
                    return new WebApplicationCommand(container, arguments, shouldImpersonate, rlimits);
                case "ps1" :
                    return new PowershellCommand(container, arguments, shouldImpersonate, rlimits);
                case "replace-tokens" :
                    return new ReplaceTokensCommand(container, arguments);
                case "tar" :
                    return new TarCommand(container, arguments);
                case "touch" :
                    return new TouchCommand(container, arguments);
                case "unzip" :
                    return new UnzipCommand(container, arguments);
                default :
                    throw new InvalidOperationException(String.Format("Unknown script command: '{0}'", commandName));
            }
        }
    }
}
