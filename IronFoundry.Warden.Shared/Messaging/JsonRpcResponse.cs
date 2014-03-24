using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Shared.Messaging
{
    /// <summary>
    /// Represents a JsonRpcResponse but must have either a result filed so derive from JsonRcpResponse<TResult>
    /// or from JsonRpcErrorResponse.
    /// </summary>
    public abstract class JsonRpcResponse
    {
        private JsonRpcResponse()
        {
            this.jsonrpc = "2.0";
        }

        protected JsonRpcResponse(string id) : this()
        {
            this.id = id;
        }

        public string jsonrpc { get; set; }
        public string id { get; set; }
    }

    public class JsonRpcResponse<TResult> : JsonRpcResponse
    {
        public JsonRpcResponse(string id, TResult result) : base(id)
        {
            this.result = result;
        }

        public TResult result { get; set; }
    }

    
    public class JsonRpcErrorInfo 
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }

    public class JsonRpcErrorResponse : JsonRpcResponse
    {
        public JsonRpcErrorResponse(string id) : base (id)
        {
            error = new JsonRpcErrorInfo();
        }

        public JsonRpcErrorInfo error { get; set; }
    }
}
