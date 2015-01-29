using System.Linq;
using System.Threading.Tasks;
using IronFoundry.Container.Messages;
using IronFoundry.Container.Utilities;

namespace IronFoundry.Container.Host.Handlers
{
    public class StopAllProcessesHandler
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
