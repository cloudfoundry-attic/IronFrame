using System;
using System.Collections.Generic;
using System.Net;

namespace IronFrame.Utilities
{
    internal interface IProcessRunner : IDisposable
    {
        IProcess Run(ProcessRunSpec runSpec);
        void StopAll(bool kill);
        IProcess FindProcessById(int id);
    }

    internal class ProcessRunSpec
    {
        static readonly string[] EmptyArguments = new string[0];

        public ProcessRunSpec()
        {
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Arguments = EmptyArguments;
        }

        public string ExecutablePath { get; set; }
        public string[] Arguments { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public string WorkingDirectory { get; set; }

        public NetworkCredential Credentials { get; set; }

        public bool BufferedInputOutput { get; set; }

        public Action<string> OutputCallback { get; set; }
        public Action<string> ErrorCallback { get; set; }
    }
}
