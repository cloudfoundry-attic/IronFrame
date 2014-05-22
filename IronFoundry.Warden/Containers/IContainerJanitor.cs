using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers
{
    public interface IContainerJanitor
    {
        Task DestroyContainerAsync(string handle, string containerBasePath, string tcpPort, bool deleteDirectories, int? containerPort);
    }
}
