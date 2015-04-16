using System.Collections.Generic;

namespace IronFoundry.Container
{
    public sealed class ProcessSpec
    {
        public string ExecutablePath { get; set; }
        public string[] Arguments { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public string WorkingDirectory { get; set; }
        public bool Privileged { get; set; }
        public bool DisablePathMapping { get; set; }
    }
}
