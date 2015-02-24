using System.Linq;
using System.Threading.Tasks;
using IronFoundry.Container.Messages;

namespace IronFoundry.Container.Host.Handlers
{
    internal sealed class StopAllProcessesHandler
    {
        readonly IProcessTracker processTracker;

        public StopAllProcessesHandler(IProcessTracker processTracker)
        {
            this.processTracker = processTracker;
        }

        public Task ExecuteAsync(StopAllProcessesParams p)
        {
            var processes = processTracker.GetAllChildProcesses();

            var tasks = processes
                .Select(process => StopProcessHandler.StopProcessAsync(process, p.timeout));

            return Task.WhenAll(tasks);
        }

    }
}
