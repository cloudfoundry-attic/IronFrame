using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class StopParameters
    {
        public StopParameters()
        {
        }

        public StopParameters(bool kill)
        {
            Kill = kill;
        }

        public bool Kill { get; set; }
    }

    public class StopRequest : JsonRpcRequest<StopParameters>
    {
        public const string MethodName = "Container.Stop";

        // Required for serialization
        public StopRequest()
            : base(MethodName)
        {
        }

        public StopRequest(StopParameters parameters) : base(MethodName, parameters)
        {
        }

        public StopRequest(bool kill) : base(MethodName, new StopParameters(kill))
        {
        }
    }

    public class StopResponse : JsonRpcResponse
    {
        public StopResponse(JToken id) : base(id)
        {
        }
    }
}
