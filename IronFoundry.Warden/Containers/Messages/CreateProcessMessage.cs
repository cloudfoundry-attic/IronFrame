using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using Newtonsoft.Json;
using IronFoundry.Container.Messaging;

namespace IronFoundry.Warden.Containers.Messages
{
    [Obsolete]
    public class CreateProcessRequest : JsonRpcRequest<CreateProcessStartInfo>
    {
        public static string MethodName = "Container.CreateProcess";

        public CreateProcessRequest()
            : base(MethodName)
        {
        }

        public CreateProcessRequest(CreateProcessStartInfo startInfo)
            : base(MethodName)
        {
            @params = startInfo;
        }
    }

    [Obsolete]
    public class CreateProcessResult
    {
        public int Id { get; set; }
        public bool HasExited { get; set; }
        public int ExitCode { get; set; }
    }

    [Obsolete]
    public class CreateProcessResponse : JsonRpcResponse<CreateProcessResult>
    {
        public CreateProcessResponse(string id, CreateProcessResult result)
            : base(id, result)
        {
        }
    }

    [Obsolete]
    public class GetProcessExitInfoParams
    {
        public int Id { get; set; }
    }

    [Obsolete]
    public class GetProcessExitInfoResult
    {
        public int ExitCode { get; set; }
        public bool HasExited { get; set; }
        public string StandardError { get; set; }
        public string StandardOutputTail { get; set; }
    }

    [Obsolete]
    public class GetProcessExitInfoRequest : JsonRpcRequest<GetProcessExitInfoParams>
    {
        public static string MethodName = "Container.GetProcessExitInfo";
        public GetProcessExitInfoRequest()
            : base(MethodName)
        {
        }

        public GetProcessExitInfoRequest(GetProcessExitInfoParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    [Obsolete]
    public class GetProcessExitInfoResponse : JsonRpcResponse<GetProcessExitInfoResult>
    {
       
        public GetProcessExitInfoResponse(string id, GetProcessExitInfoResult result)
            : base(id, result)
        {
        }
    }
}
