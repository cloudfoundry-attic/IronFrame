using System.Threading.Tasks;

namespace IronFoundry.Container.Host.Handlers
{
    public class PingHandler
    {
        public Task ExecuteAsync()
        {
            return Task.FromResult(0);
        }
    }
}
