﻿using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messaging
{
    /// <summary>
    /// Represents a JsonRpcResponse but must have either a result filed so derive from JsonRcpResponse<TResult>
    /// or from JsonRpcErrorResponse.
    /// </summary>
    internal abstract class JsonRpcResponse
    {
        private JsonRpcResponse()
        {
            this.jsonrpc = "2.0";
        }

        protected JsonRpcResponse(JToken id) : this()
        {
            this.id = id;
        }

        public string jsonrpc { get; set; }
        public JToken id { get; set; }
    }

    internal class JsonRpcResponse<TResult> : JsonRpcResponse
    {
        public JsonRpcResponse(JToken id, TResult result) : base(id)
        {
            this.result = result;
        }

        public TResult result { get; set; }
    }

    
    internal class JsonRpcErrorInfo 
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }

    internal class JsonRpcErrorResponse : JsonRpcResponse
    {
        public JsonRpcErrorResponse(JToken id) : base (id)
        {
            error = new JsonRpcErrorInfo();
        }

        public JsonRpcErrorInfo error { get; set; }
    }
}
