using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("03d92867-75d3-47ce-baca-cd88c5e0d6b3")]
[assembly: ComVisible(false)]
[assembly: System.CLSCompliant(true)]

#if NETSTANDARD || NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://dkorablin.ru/project/Default.aspx?File=108")]

#else
[assembly: AssemblyTitle("Plugin.NetworkPluginProvider")]
[assembly: AssemblyProduct("Provides access to plugins througth the network")]
[assembly: AssemblyCompany("Danila Korablin")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2011-2024")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

#endif