namespace IronFoundry.Warden.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Protocol;
    using Utilities;

    public interface IStreamingHandler
    {
        Task<StreamResponse> HandleAsync(MessageWriter messageWriter, CancellationToken cancellationToken);
    }
}
