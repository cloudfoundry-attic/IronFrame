namespace IronFoundry.Warden.Jobs
{
    public enum JobDataSource
    {
        stdout,
        stderr
    }

    public interface IJobStatus
    {
        int? ExitStatus { get; }
        JobDataSource DataSource { get; }
        string Data { get; }
    }
}
