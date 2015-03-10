using System.Threading.Tasks;

namespace IronFoundry.Container.Host.Handlers
{
    internal class PingHandler
    {
        public Task ExecuteAsync()
        {
            return Task.FromResult(0);
        }
    }
}
