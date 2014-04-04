namespace IronFoundry.Warden.Utilities
{
    public interface IFirewallManager
    {
        void OpenPort(ushort port, string name);
        void ClosePort(string name);
    }
}