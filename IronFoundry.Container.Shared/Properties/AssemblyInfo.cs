using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("IronFoundry.Container.Shared")]

[assembly: InternalsVisibleTo("IronFoundry.Container")]
[assembly: InternalsVisibleTo("IronFoundry.Container.Host")]

// For unit testing:
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("IronFoundry.Container.Test")]