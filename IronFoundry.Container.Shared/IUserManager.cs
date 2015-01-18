using System.Net;

namespace IronFoundry.Warden.Utilities
{
    // BR: Move this to IronFoundry.Container
    public interface IUserManager
    {
        NetworkCredential CreateUser(string userName);
        void DeleteUser(string userName);
    }
}