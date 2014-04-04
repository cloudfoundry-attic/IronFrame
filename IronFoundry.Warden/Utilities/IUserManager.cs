using System.Net;

namespace IronFoundry.Warden.Utilities
{
    public interface IUserManager
    {
        NetworkCredential CreateUser(string userName);
        void DeleteUser(string userName);
    }
}