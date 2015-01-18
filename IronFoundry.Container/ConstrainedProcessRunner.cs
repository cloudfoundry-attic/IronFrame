using System;
using IronFoundry.Container.Messages;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container
{
    public class ConstrainedProcessRunner : IProcessRunner
    {
        readonly IContainerHostClient hostClient;

        public ConstrainedProcessRunner(IContainerHostClient hostClient)
        {
            this.hostClient = hostClient;
        }

        public IProcess Run(ProcessRunSpec runSpec)
        {
            Guid processKey = Guid.NewGuid();
            
            CreateProcessParams @params = new CreateProcessParams
            {
                key = processKey,
                executablePath = runSpec.ExecutablePath,
                arguments = runSpec.Arguments,
                environment = runSpec.Environment,
                workingDirectory = runSpec.WorkingDirectory
            };

            var result = hostClient.CreateProcess(@params);
            var process = new ConstrainedProcess(hostClient, processKey, result.id, runSpec.OutputCallback, runSpec.ErrorCallback);

            return process;
        }

        public void Dispose()
        {
            hostClient.Shutdown();
        }
    }
}
