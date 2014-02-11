namespace IronFoundry.Warden.Jobs
{
    public interface IJobManager
    {
        uint StartJobFor(IJobRunnable runnable);
        Job GetJob(uint jobId);
        void RemoveJob(uint jobId);
    }
}
