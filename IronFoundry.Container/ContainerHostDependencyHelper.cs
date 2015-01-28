using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IronFoundry.Container
{
    public class ContainerHostDependencyHelper
    {
        const string ContainerHostAssemblyName = "IronFoundry.Container.Host";

        readonly Assembly containerHostAssembly;

        public ContainerHostDependencyHelper()
        {
            this.containerHostAssembly = GetContainerHostAssembly();
        }

        public virtual string ContainerHostExe
        {
            get { return ContainerHostAssemblyName + ".exe"; }
        }

        public virtual string ContainerHostExePath
        {
            get { return containerHostAssembly.Location; }
        }

        static Assembly GetContainerHostAssembly()
        {
            return Assembly.ReflectionOnlyLoad(ContainerHostAssemblyName);
        }

        public virtual IReadOnlyList<string> GetContainerHostDependencies()
        {
            return EnumerateLocalReferences(containerHostAssembly).ToList();
        }

        IEnumerable<string> EnumerateLocalReferences(Assembly assembly)
        {
            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                var referencedAssembly = Assembly.ReflectionOnlyLoad(referencedAssemblyName.FullName);

                if (!assembly.GlobalAssemblyCache)
                {
                    yield return referencedAssembly.Location;

                    foreach (var nestedReferenceFilePath in EnumerateLocalReferences(referencedAssembly))
                        yield return nestedReferenceFilePath;
                }
            }
        }
    }
}
