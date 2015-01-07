namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class CommandRunner : ICommandRunner
    {
        Dictionary<string, Func<IRemoteCommandArgs, TaskCommand>> commandGenerator = new Dictionary<string, Func<IRemoteCommandArgs, TaskCommand>>();

        public Task<TaskCommandResult> RunCommandAsync(string command, IRemoteCommandArgs rcArgs)
        {
            if (!commandGenerator.ContainsKey(command))
                throw new InvalidOperationException("Could not find command generator for key " + command);

            var generator = commandGenerator[command];
            var taskCommand = generator(rcArgs);
            
            return Task.FromResult(taskCommand.Execute());
        }

        public void RegisterCommand(string taskName, Func<IRemoteCommandArgs, TaskCommand> command)
        {
            commandGenerator.Add(taskName, command);
        }
    }
}
