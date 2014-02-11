namespace IronFoundry.Warden.Jobs
{
    using System.Threading.Tasks;

    public interface IJobListener
    {
        Task ListenStatusAsync(IJobStatus jobStatus);
    }
}
