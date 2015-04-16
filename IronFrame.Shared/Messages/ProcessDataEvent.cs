using System;

namespace IronFrame.Messages
{
    internal enum ProcessDataType
    {
        STDOUT = 1,
        STDERR = 2,
    }

    internal class ProcessDataEvent
    {
        public ProcessDataEvent()
        {
        }

        public ProcessDataEvent(Guid key, ProcessDataType dataType, string data)
        {
            this.key = key;
            this.dataType = dataType;
            this.data = data;
        }

        public Guid key { get; set; }
        public ProcessDataType dataType { get; set; }
        public string data { get; set; }
    }
}
