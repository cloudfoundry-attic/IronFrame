using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("IronFoundry.Container")]

// Temporary, until we have a supported solution for restoring containers
[assembly: InternalsVisibleTo("IronFoundry.Warden")]

// For unit testing:
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("IronFoundry.Container.Test")]
