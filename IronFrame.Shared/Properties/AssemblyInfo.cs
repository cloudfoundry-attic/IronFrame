using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("IronFrame.Shared")]

[assembly: InternalsVisibleTo("IronFrame")]
[assembly: InternalsVisibleTo("IronFrame.Host")]

// For unit testing:
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("IronFrame.Test")]
[assembly: InternalsVisibleTo("IronFrame.Acceptance.Test")]