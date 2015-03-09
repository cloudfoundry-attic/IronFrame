using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

public static class WindowsIdentityExtensionMethods
{
    public static string GetUserName(this WindowsIdentity identity)
    {
        int splitIndex = identity.Name.IndexOf("\\");
        string username = (splitIndex < 0) ? string.Empty : identity.Name.Substring(splitIndex + 1);
        return username;
    }
}
