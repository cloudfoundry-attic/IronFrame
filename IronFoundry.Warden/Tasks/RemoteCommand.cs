using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Tasks
{
    public interface IRemoteCommandArgs
    {
        bool Privileged { get; }
        string[] Arguments { get; }
        IDictionary<string, string> Environment { get; }
    }

    public class RemoteCommand : IRemoteCommandArgs
    {
        public RemoteCommand(bool privileged, string command, string[] arguments = null, IDictionary<string,string> environment = null)
        {
            Command = command;
            Arguments = arguments ?? new string[0];
            Privileged = privileged;
            Environment = environment ?? new Dictionary<string, string>(0);
        }

        public string Command { get; private set; }
        public bool Privileged { get; private set; }
        public string[] Arguments { get; private set; }
        public IDictionary<string, string> Environment { get; private set; }
    }
}
