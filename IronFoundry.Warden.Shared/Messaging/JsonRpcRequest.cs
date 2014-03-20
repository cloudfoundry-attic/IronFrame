using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string id { get; set; }
    }

    public class JsonRpcRequest<T> : JsonRpcRequest
    {
        public JsonRpcRequest(string method)
            : base(method)
        {
        }

        public T @params { get; set; }
    }
}
