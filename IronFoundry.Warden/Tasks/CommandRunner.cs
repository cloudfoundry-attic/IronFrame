using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Tasks
{
    public class CommandRunner : ICommandRunner
    {
        Dictionary<string, Func<bool, string[], TaskCommand>> commandGenerator = new Dictionary<string, Func<bool, string[], TaskCommand>>();

        public Task<TaskCommandResult> RunCommandAsync(bool shouldImpersonate, string command, params string[] arguments)
        {
            if (!commandGenerator.ContainsKey(command))
                throw new InvalidOperationException("Could not find command generator for key " + command);

            var generator = commandGenerator[command];
            var taskCommand = generator(shouldImpersonate, arguments);
            
            return Task.FromResult(taskCommand.Execute());
        }

        public void RegisterCommand(string taskName, Func<bool, string[], TaskCommand> command)
        {
            commandGenerator.Add(taskName, command);
        }
    }
}
