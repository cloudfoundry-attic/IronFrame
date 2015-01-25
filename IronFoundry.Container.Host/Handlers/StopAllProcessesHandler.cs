using System.Linq;
using System.Threading.Tasks;
using IronFoundry.Container.Messages;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Container.Host.Handlers
{
    public class StopAllProcessesHandler
    {
        readonly IProcessTracker processTracker;

        public StopAllProcessesHandler(IProcessTracker processTracker)
        {
            this.processTracker = processTracker;
        }

        public async Task ExecuteAsync(StopAllProcessesParams p)
        {
            var processes = processTracker.GetAllChildProcesses();

            var tasks = processes
                .Select(process => Task.Run(() => StopProcess(process, p.timeout)))
                .ToList();

            await Task.WhenAll(tasks);
        }

        static void StopProcess(IProcess process, int timeout)
        {
            process.RequestExit();
            if (!process.WaitForExit(timeout))
                process.Kill();
        }
    }
}
