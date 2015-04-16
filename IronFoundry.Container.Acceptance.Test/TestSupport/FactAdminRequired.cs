using System;
using System.Security.Principal;
using Xunit;
using Xunit.Sdk;

// TODO: Keep one copy of this file - perhaps in a shared library for tests?

/// <summary>
/// Tests for current identity running as Admin and skips test if they
/// are not currently running as admin.  Honors existing Skip messages if 
/// they are present.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Xunit.Sdk.FactDiscoverer", "xunit.execution")]
public class FactAdminRequired : FactAttribute
{
    public override string Skip
    {
        get
        {
            if (!string.IsNullOrEmpty(base.Skip) || IsAdmin())
            {
                return base.Skip;
            }

            return "Test required to run as admin to run.";
        }
        set
        {
            base.Skip = value;
        }
    }

    protected virtual bool IsAdmin()
    {
        bool isAdmin = false;

        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator)
                // The system account, while an admin, cannot start processes as another user.
                //
                // From MSDN:
                // You cannot call CreateProcessWithLogonW from a process that is running under
                // the "LocalSystem" account, because the function uses the logon SID in the
                // caller token, and the token for the "LocalSystem" account does not contain
                // this SID
                //
                // Thus, if running as System, skip.
                && !identity.IsSystem;
        }
        catch
        {
            isAdmin = false;
        }

        return isAdmin;
    }
}
