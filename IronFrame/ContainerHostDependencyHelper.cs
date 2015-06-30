using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IronFrame
{
    internal class ContainerHostDependencyHelper
    {
        const string ContainerHostAssemblyName = "IronFrame.Host";
        private string _GuardExePath;
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

        public virtual string GuardExe
        {
            get { return "Guard.exe"; }
        }

        public virtual string GuardExePath
        {
            get
            {
                if (_GuardExePath == null)
                {
                    _GuardExePath = Path.Combine(Path.GetDirectoryName(containerHostAssembly.Location), "Guard.exe");
                    if (!File.Exists(_GuardExePath))
                    {
                        _GuardExePath = Path.Combine(Directory.GetCurrentDirectory(), "Guard.exe");
                    }
                }
                return _GuardExePath;
            }
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
