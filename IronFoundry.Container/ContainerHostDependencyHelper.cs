using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IronFoundry.Container
{
    public class ContainerHostDependencyHelper
    {
        readonly string basePath;

        public ContainerHostDependencyHelper()
            : this(GetAssemblyBinPath())
        {
        }

        public ContainerHostDependencyHelper(string basePath)
        {
            this.basePath = basePath;
        }

        public virtual string ContainerHostExe
        {
            get { return "IronFoundry.Container.Host.exe"; }
        }

        public virtual string ContainerHostExePath
        {
            get { return Path.Combine(basePath, ContainerHostExe); }
        }

        static string GetAssemblyBinPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public virtual IReadOnlyList<string> GetContainerHostDependencies()
        {
            var hostAssembly = Assembly.ReflectionOnlyLoadFrom(ContainerHostExePath);
            return EnumerateLocalReferences(hostAssembly).ToList();
        }

        IEnumerable<string> EnumerateLocalReferences(Assembly assembly)
        {
            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                var fileName = assemblyName.Name + ".dll";
                var filePath = Path.Combine(basePath, fileName);
                if (File.Exists(filePath))
                {
                    var referencedAssembly = Assembly.ReflectionOnlyLoadFrom(filePath);
                    yield return filePath;

                    foreach (var nestedReferenceFilePath in EnumerateLocalReferences(referencedAssembly))
                        yield return nestedReferenceFilePath;
                }
            }
        }
    }
}
