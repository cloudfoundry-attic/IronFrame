using System;
namespace IronFoundry.Warden.Utilities
{
    public interface IUserManager
    {
        System.Net.NetworkCredential CreateUser(string userName);
        void DeleteUser(string userName);
    }
}
