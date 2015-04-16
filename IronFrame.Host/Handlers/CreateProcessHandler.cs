using System.Threading.Tasks;
using IronFrame.Messages;
using IronFrame.Utilities;

namespace IronFrame.Host.Handlers
{
    internal class CreateProcessHandler
    {
        readonly IProcessRunner processRunner;
        readonly IProcessTracker processTracker;

        public CreateProcessHandler(IProcessRunner processRunner, IProcessTracker processTracker)
        {
            this.processRunner = processRunner;
            this.processTracker = processTracker;
        }

        public Task<CreateProcessResult> ExecuteAsync(CreateProcessParams p)
        {
            var runSpec = new ProcessRunSpec
            {
                ExecutablePath = p.executablePath,
                Arguments = p.arguments,
                Environment = p.environment,
                WorkingDirectory = p.workingDirectory,
                OutputCallback = (data) => 
                {
                    processTracker.HandleProcessData(p.key, ProcessDataType.STDOUT, data);
                },
                ErrorCallback = (data) => 
                {
                    processTracker.HandleProcessData(p.key, ProcessDataType.STDERR, data);
                },
            };

            var process = processRunner.Run(runSpec);

            processTracker.TrackProcess(p.key, process);

            var result = new CreateProcessResult
            {
                id = process.Id,
            };

            return Task.FromResult(result);
        }
    }
}
