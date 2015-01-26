namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container
    public interface INetShRunner
    {
        bool AddRule(int port, string userName);
        bool DeleteRule(int port);
    }
}