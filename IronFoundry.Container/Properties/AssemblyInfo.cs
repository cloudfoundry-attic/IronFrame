using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("IronFoundry.Container")]

// For unit testing:
[assembly: InternalsVisibleTo("IronFoundry.Container.Test")]

// Necessary for NSubstitute
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

// The ContainerService still doesn't have a good way of cleaning up abandoned containers
// so the warden has a hack to do it.  The hack relies on access to some internal components (e.g. Container)
[assembly: InternalsVisibleTo("IronFoundry.Warden")]