using System.IO;

namespace IronFoundry.Container
{
    public class BindMount
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public FileAccess Access { get; set; }
    }
}
