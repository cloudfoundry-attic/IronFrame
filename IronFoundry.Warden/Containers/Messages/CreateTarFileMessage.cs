using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class CreateTarFileInfo
    {
        [JsonProperty("tarFilePath")]
        public string TarFilePath { get; set; }

        [JsonProperty("sourceDirectoryPath")]
        public string SourceDirectoryPath { get; set; }

        [JsonProperty("compress")]
        public bool Compress { get; set; }
    }

    public class CreateTarFileRequest : JsonRpcRequest<CreateTarFileInfo>
    {
        public const string MethodName = "Container.CreateTarFile";

        public CreateTarFileRequest(CreateTarFileInfo @params)
            : base(MethodName, @params)
        {
        }
    }

    public class CreateTarFileResponse : JsonRpcResponse
    {
        public CreateTarFileResponse(JToken id)
            : base(id)
        {
        }
    }
}
