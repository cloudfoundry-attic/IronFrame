using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Tasks
{
    public class RemoteCommand
    {
        public RemoteCommand(bool shouldImpersonate, string command, params string[] arguments)
        {
            Command = command;
            Arguments = arguments;
            ShouldImpersonate = shouldImpersonate;
        }

        public string Command { get; private set; }
        public bool ShouldImpersonate { get; private set; }
        public string[] Arguments { get; private set; }
    }
}
