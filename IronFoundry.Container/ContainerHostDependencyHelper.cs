﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IronFoundry.Container
{
    internal class ContainerHostDependencyHelper
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

        public string ContainerHostExeConfig
        {
            get { return ContainerHostExe + ".config"; }
        }

        public string ContainerHostExeConfigPath
        {
            get { return ContainerHostExePath + ".config"; }
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

                if (!referencedAssembly.GlobalAssemblyCache)
                {
                    yield return referencedAssembly.Location;

                    foreach (var nestedReferenceFilePath in EnumerateLocalReferences(referencedAssembly))
                        yield return nestedReferenceFilePath;
                }
            }
        }
    }
}
