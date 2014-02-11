using Newtonsoft.Json;

namespace IronFoundry.Warden.Tasks
{
    public class TaskCommandDTO
    {
        [JsonProperty(PropertyName="cmd")]
        public string Command { get; set; }

        [JsonProperty(PropertyName="args")]
        public string[] Args { get; set; }
    }
}
