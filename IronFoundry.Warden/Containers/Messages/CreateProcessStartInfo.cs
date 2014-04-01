using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IronFoundry.Warden.Shared.Messaging;

namespace IronFoundry.Warden.Containers.Messages
{
    public class CreateProcessStartInfo
    {
        public CreateProcessStartInfo()
        {
            this.EnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public CreateProcessStartInfo(string fileName, string arguments = null) : this()
        {
            this.FileName = fileName;
            this.Arguments = arguments;
        }

        public string FileName { get; set; }
        public string Arguments { get; set; }
        public string UserName { get; set; }

        [JsonConverter(typeof(SecureStringJsonConverter))]
        public SecureString Password { get; set; }

        public string WorkingDirectory { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
    }
}
