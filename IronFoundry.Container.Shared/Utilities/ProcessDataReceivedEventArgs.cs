using System;

namespace IronFoundry.Container.Utilities
{
    // BR: Move this to IronFoundry.Container.Shared
    public class ProcessDataReceivedEventArgs : EventArgs
    {
        public ProcessDataReceivedEventArgs(string data)
        {
            this.Data = data;
        }

        public string Data { get; private set; }
    }
}
