﻿using System;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    internal class StopProcessParams
    {
        public Guid key;
        public int timeout;
    }

    internal class StopProcessRequest : JsonRpcRequest<StopProcessParams>
    {
        public static string MethodName = "Container.StopProcess";

        public StopProcessRequest(StopProcessParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    internal class StopProcessResponse : JsonRpcResponse
    {
        public StopProcessResponse(JToken id)
            : base(id)
        {
        }
    }
}