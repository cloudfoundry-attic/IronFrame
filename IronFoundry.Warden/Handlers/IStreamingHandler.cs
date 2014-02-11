using System.Threading;
using System.Threading.Tasks;
using IronFoundry.Warden.Protocol;
using IronFoundry.Warden.Utilities;

namespace IronFoundry.Warden.Handlers
{
    public interface IStreamingHandler
    {
        Task<StreamResponse> HandleAsync(MessageWriter messageWriter, CancellationToken cancellationToken);
    }
}
