using System.Threading.Tasks;

namespace IronFoundry.Warden.Jobs
{
    public interface IJobListener
    {
        Task ListenStatusAsync(IJobStatus jobStatus);
    }
}
