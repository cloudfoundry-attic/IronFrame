namespace IronFoundry.Warden.Jobs
{
    public interface IJobResult
    {
        int ExitCode { get; }
        string Stdout { get; }
        string Stderr { get; }
    }
}
