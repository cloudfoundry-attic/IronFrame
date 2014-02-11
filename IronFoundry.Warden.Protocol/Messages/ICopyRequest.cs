namespace IronFoundry.Warden.Protocol
{
    public interface ICopyRequest : IContainerRequest
    {
        string SrcPath { get; }
        string DstPath { get; }
    }
}
