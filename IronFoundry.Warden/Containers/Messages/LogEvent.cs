using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using logmessage;

namespace IronFoundry.Warden.Containers.Messages
{
    public class LogEvent
    {
        public static string EventTopicName = "LogEvent";

        public LogEvent()
        {
            EventTopic = LogEvent.EventTopicName;
        }

        public string EventTopic { get; set; }
        public string LogData { get; set; }
        public LogMessageType MessageType { get; set; }
    }
}
