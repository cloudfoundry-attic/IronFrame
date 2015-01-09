namespace IronFoundry.Warden.Tasks
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class TaskCommandDTO
    {
        [JsonProperty(PropertyName="cmd")]
        public string Command { get; set; }

        [JsonProperty(PropertyName="args")]
        public string[] Args { get; set; }

        [JsonProperty(PropertyName="env")]
        public Dictionary<string, string> Environment { get; set; } 

        [JsonProperty(PropertyName = "working_dir")]
        public string WorkingDirectory { get; set; } 
    }
}
