namespace IronFoundry.Warden.Tasks
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Configuration;
    using Containers;
    using Utilities;

    /// <summary>
    /// Opens existing file and replaces the @ROOT@ tokens with paths in the container.
    /// </summary>
    public class ReplaceTokensCommand : PathCommand
    {
        private static readonly WardenConfig config = new WardenConfig();

        private readonly Func<string, string> tokenReplacer;

        public ReplaceTokensCommand(Container container, string[] arguments)
            : base(container, arguments)
        {
            tokenReplacer = (line) => line.Replace("@ROOT@", container.Directory.FullName).ToWinPathString();
        }

        protected override void ProcessPath(string path, StringBuilder output)
        {
            if (File.Exists(path))
            {
                using (var tempFile = new TempFile(container.Directory.FullName))
                {
                    var lines = File.ReadLines(path, Encoding.ASCII);
                    File.WriteAllLines(tempFile.FullName, lines.Select(tokenReplacer), Encoding.ASCII);
                    File.Copy(tempFile.FullName, path, true);
                }
                output.AppendFormat("replaced tokens in file '{0}'", path).AppendLine();
            }
            else
            {
                throw new WardenException("file does not exist: '{0}'", path);
            }
        }
    }
}
