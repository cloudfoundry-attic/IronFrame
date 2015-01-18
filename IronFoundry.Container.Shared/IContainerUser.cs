using System.Net;

namespace IronFoundry.Warden.Containers
{
    // BR: Move to IronFoundry.Container
    // BR: Investigate if we would need this in the host??  Can't delete a user in the host...
    public interface IContainerUser
    {
        string UserName { get; }
        NetworkCredential GetCredential();
        void Delete();
    }
}