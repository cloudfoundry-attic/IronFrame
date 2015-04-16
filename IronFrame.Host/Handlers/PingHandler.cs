using System.Threading.Tasks;

namespace IronFrame.Host.Handlers
{
    internal class PingHandler
    {
        public Task ExecuteAsync()
        {
            return Task.FromResult(0);
        }
    }
}
