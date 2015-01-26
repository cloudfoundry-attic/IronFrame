namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container
    public interface ILocalTcpPortManager
    {
        int ReserveLocalPort(int port, string userName);
        void ReleaseLocalPort(int? port, string userName);
    }
}