using System;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Shared.Messaging
{
    public class JsonRpcRequest
    {
        public JsonRpcRequest(string method)
        {
            this.method = method;
            id = Guid.NewGuid().ToString();
        }

        public string jsonrpc { get { return "2.0"; } }
        public string method { get; private set; }
        public JToken id { get; set; }
    }

    public class JsonRpcRequest<T> : JsonRpcRequest
    {
        public JsonRpcRequest(string method)
            : base(method)
        {
        }

        public JsonRpcRequest(string method, T @params)
            : base(method)
        {
            this.@params = @params;
        }

        public T @params { get; set; }
    }
}
