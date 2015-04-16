using System.Security.Principal;
using System.Threading.Tasks;

public static class TaskExtensions
{
    /// <summary>
    /// Used to silence warning CS4014 caused when an async call is not awaited or assigned to a variable.
    /// For example see MessagingClientTest.ThrowsWhenReceivingDuplicateRequest
    /// 
    /// This seems cleaner than ignoring it with pragma.
    /// </summary>
    public static void Forget(this Task task)
    { }
}

public static class WindowsIdentityExtensionMethods
{
    public static string GetUserName(this WindowsIdentity identity)
    {
        int splitIndex = identity.Name.IndexOf("\\");
        string username = (splitIndex < 0) ? string.Empty : identity.Name.Substring(splitIndex + 1);
        return username;
    }
}
