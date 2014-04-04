using System.Net;

namespace IronFoundry.Warden.Containers
{
    public interface IContainerUser
    {
        string UserName { get; }
        NetworkCredential GetCredential();
        void Delete();
    }
}