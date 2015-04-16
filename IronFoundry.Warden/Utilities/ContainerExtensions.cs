using System;
using System.Text.RegularExpressions;
using IronFoundry.Container;

namespace IronFoundry.Warden.Utilities
{
    public static class ContainerExtensions
    {
        private const string RootMarker = "@ROOT@";
        private static readonly Regex backslashCleanup = new Regex(@"\\+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string ConvertToUserPathWithin(this IContainer container, string path)
        {
            if (path == null)
                return null;

            string mappedPath;
            if (container.TryConvertToUserRelativePath(path, out mappedPath))
            {
                mappedPath = ToWinPathString(container.Directory.MapUserPath(mappedPath));
            }

            return mappedPath;
        }

        public static bool TryConvertToUserRelativePath(this IContainer container, string path, out string convertedPath)
        {
            bool madeConversion = false;
            convertedPath = path;

            if (path == null)
                return madeConversion;

            if (path.Trim().StartsWith(RootMarker))
            {
                convertedPath = path.Trim().Substring(RootMarker.Length);
                madeConversion = true;
            }

            return madeConversion;
        }

        public static string ReplaceRootTokensWithUserPath(this IContainer container, string line)
        {
            if (line == null)
                return null;

            string convertedLine = line.Contains(RootMarker)
                ? line.Replace(RootMarker, container.Directory.UserPath)
                : line;

            return convertedLine;
        }

        public static TempFile TempFileInContainer(this IContainer container, string extension)
        {
            return new TempFile(container.Directory.UserPath, extension);
        }
        public static TempFile FileInContainer(this IContainer container, string relativePath)
        {
            string pathInContainer = container.Directory.MapUserPath(relativePath);
            return new TempFile(pathInContainer, deleteIfExists: true);
        }

        static string ToWinPathString(string pathString)
        {
            return backslashCleanup.Replace(pathString.Replace('/', '\\'), @"\");
        }
    }
}
