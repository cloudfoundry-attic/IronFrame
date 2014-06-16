using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class CopyFileInfo
    {
        [JsonProperty("sourceFilePath")]
        public string SourceFilePath;

        [JsonProperty("destinationFilePath")]
        public string DestinationFilePath;
    }

    public class CopyFileInRequest : JsonRpcRequest<CopyFileInfo>
    {
        public const string MethodName = "Container.CopyFileIn";

        public CopyFileInRequest(CopyFileInfo @params)
            : base(MethodName, @params)
        {
        }
    }

    public class CopyFileOutRequest : JsonRpcRequest<CopyFileInfo>
    {
        public const string MethodName = "Container.CopyFileOut";

        public CopyFileOutRequest(CopyFileInfo @params)
            : base(MethodName, @params)
        {
        }
    }

    public class CopyFileResult
    {
    }

    public class CopyFileResponse : JsonRpcResponse<CopyFileResult>
    {
        public CopyFileResponse(JToken id) : base(id, new CopyFileResult())
        {
        }
    }
}
