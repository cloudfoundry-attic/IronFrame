using System;

namespace IronFoundry.Container.Messages
{
    public enum ProcessDataType
    {
        STDOUT = 1,
        STDERR = 2,
    }

    public class ProcessDataEvent
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
