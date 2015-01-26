namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container
    public interface IFirewallManager
    {
        void OpenPort(int port, string name);
        void ClosePort(string name);
    }
}