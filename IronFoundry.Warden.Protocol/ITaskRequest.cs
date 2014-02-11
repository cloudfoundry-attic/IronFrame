namespace IronFoundry.Warden.Protocol
{
    public interface ITaskRequest : IContainerRequest
    {
        bool Privileged { get; }
        ResourceLimits Rlimits { get; }
        string Script { get; }
    }
}
