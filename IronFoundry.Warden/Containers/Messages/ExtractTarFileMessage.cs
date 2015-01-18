using IronFoundry.Container.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ExtractTarFileInfo
    {
        [JsonProperty("tarFilePath")]
        public string TarFilePath { get; set; }

        [JsonProperty("destinationDirectoryPath")]
        public string DestinationDirectoryPath { get; set; }

        [JsonProperty("decompress")]
        public bool Decompress { get; set; }
    }

    public class ExtractTarFileRequest : JsonRpcRequest<ExtractTarFileInfo>
    {
        public const string MethodName = "Container.ExtractTarFile";

        public ExtractTarFileRequest(ExtractTarFileInfo @params) : base(MethodName, @params)
        {
        }
    }

    public class ExtractTarFileResponse : JsonRpcResponse
    {
        public ExtractTarFileResponse(JToken id) : base(id)
        {
        }
    }
}
