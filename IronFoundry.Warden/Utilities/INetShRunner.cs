namespace IronFoundry.Warden.Utilities
{
    public interface INetShRunner
    {
        bool AddRule(ushort port, string userName);
        bool DeleteRule(ushort port);
    }
}