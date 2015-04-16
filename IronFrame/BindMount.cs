using System.IO;

namespace IronFrame
{
    public sealed class BindMount
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public FileAccess Access { get; set; }
    }
}
