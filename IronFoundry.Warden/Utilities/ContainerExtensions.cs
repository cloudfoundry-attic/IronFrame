using IronFoundry.Warden.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Utilities
{
    public static class ContainerExtensions
    {
        public static IEnumerable<string> ConvertToPathsWithin(this IContainerClient container, string[] arguments)
        {
            foreach (string arg in arguments)
            {
                string rv = null;

                if (arg.Contains("@ROOT@"))
                {
                    rv = arg.Replace("@ROOT@", container.ContainerDirectoryPath).ToWinPathString();
                }
                else
                {
                    rv = arg;
                }

                yield return rv;
            }
        }

        public static string ConvertToPathWithin(this IContainerClient container, string path)
        {
            string pathTmp = path.Trim();
            if (pathTmp.StartsWith("@ROOT@"))
            {
                return pathTmp.Replace("@ROOT@", container.ContainerDirectoryPath).ToWinPathString();
            }
            else
            {
                return pathTmp;
            }
        }

        public static TempFile TempFileInContainer(this IContainerClient container, string extension)
        {
            return new TempFile(container.ContainerDirectoryPath, extension);
        }

        public static IEnumerable<string> ConvertToPathsWithin(this IContainer container, string[] arguments)
        {
            foreach (string arg in arguments)
            {
                string rv = null;

                if (arg.Contains("@ROOT@"))
                {
                    rv = arg.Replace("@ROOT@", container.ContainerDirectoryPath).ToWinPathString();
                }
                else
                {
                    rv = arg;
                }

                yield return rv;
            }
        }

        public static string ConvertToPathWithin(this IContainer container, string path)
        {
            string pathTmp = path.Trim();
            if (pathTmp.StartsWith("@ROOT@"))
            {
                return pathTmp.Replace("@ROOT@", container.ContainerDirectoryPath).ToWinPathString();
            }
            else
            {
                return pathTmp;
            }
        }

        public static TempFile TempFileInContainer(this IContainer container, string extension)
        {
            return new TempFile(container.ContainerDirectoryPath, extension);
        }
    }
}
