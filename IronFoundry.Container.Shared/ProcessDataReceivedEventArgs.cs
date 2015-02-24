using System;

namespace IronFoundry.Container.Utilities
{
    public sealed class ProcessDataReceivedEventArgs : EventArgs
    {
        public ProcessDataReceivedEventArgs(string data)
        {
            this.Data = data;
        }

        public string Data { get; private set; }
    }
}
