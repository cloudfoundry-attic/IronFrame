using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using Newtonsoft.Json;

namespace IronFoundry.Warden.Shared.Messaging
{
    public class CreateProcessRequest : JsonRpcRequest<CreateProcessStartInfo>
    {
        public CreateProcessRequest()
            : base("CreateProcess")
        {
        }

        public CreateProcessRequest(CreateProcessStartInfo startInfo)
            : base("CreateProcess")
        {
            @params = startInfo;
        }
    }

    public class CreateProcessResult
    {
        public int Id { get; set; }
        public bool HasExited { get; set; }
        public int ExitCode { get; set; }
    }

    public class CreateProcessResponse : JsonRpcResponse<CreateProcessResult>
    {
        public CreateProcessResponse()
            : base()
        {
        }

        public CreateProcessResponse(string id, CreateProcessResult result)
            : base(id, result)
        {
        }
    }

    public class GetProcessExitInfoParams
    {
        public int Id { get; set; }
    }

    public class GetProcessExitInfoResult
    {
        public int ExitCode { get; set; }
        public bool HasExited { get; set; }
        public string StandardError { get; set; }
        public string StandardOutputTail { get; set; }
    }

    public class GetProcessExitInfoRequest : JsonRpcRequest<GetProcessExitInfoParams>
    {
        public GetProcessExitInfoRequest()
            : base("GetProcessExitInfo")
        {
        }

        public GetProcessExitInfoRequest(GetProcessExitInfoParams @params)
            : base("GetProcessExitInfo")
        {
            this.@params = @params;
        }
    }

    public class GetProcessExitInfoResponse : JsonRpcResponse<GetProcessExitInfoResult>
    {
        public GetProcessExitInfoResponse()
            : base()
        {
        }

        public GetProcessExitInfoResponse(string id, GetProcessExitInfoResult result)
            : base(id, result)
        {
        }
    }
}
