namespace IronFoundry.Warden.Utilities
{
    public interface ILocalTcpPortManager
    {
        ushort ReserveLocalPort(ushort port, string userName);
        void ReleaseLocalPort(ushort? port, string userName);
    }
}