using System.Threading.Tasks;

namespace IronFoundry.Container.Host.Handlers
{
    internal sealed class PingHandler
    {
        public Task ExecuteAsync()
        {
            return Task.FromResult(0);
        }
    }
}
