using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid("03d92867-75d3-47ce-baca-cd88c5e0d6b3")]
[assembly: System.CLSCompliant(true)]

#if NETSTANDARD || NETCOREAPP
[assembly: AssemblyMetadata("ProjectUrl", "https://dkorablin.ru/project/Default.aspx?File=108")]

#else
[assembly: AssemblyDescription("Provides access to plugins througth the network")]
[assembly: AssemblyCopyright("Copyright © Danila Korablin 2011-2025")]

#endif