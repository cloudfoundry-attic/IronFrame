using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Utilities
{
    public interface IFilePermissionManager
    {
        void SetPermissionForUser(string path, string user);
    }
}
