using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace IronFoundry.Warden.Test
{
    /// <summary>
    /// Tests for current identity running as Admin and skips test if they
    /// are not currently running as admin.  Honors existing Skip messages if 
    /// they are present.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [TestCaseDiscoverer("Xunit.Sdk.FactDiscoverer", "xunit2")]
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
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                isAdmin = false;
            }

            return isAdmin;
        }
    }
}
