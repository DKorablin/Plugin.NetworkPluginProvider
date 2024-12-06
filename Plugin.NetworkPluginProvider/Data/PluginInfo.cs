using System;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>The plugin information</summary>
	internal class PluginInfo
	{
		/// <summary>The plugin name</summary>
		public String Name { get; set; }

		/// <summary>The relative path to plugin</summary>
		public String Path { get; set; }

		/// <summary>The plugin description</summary>
		public String Description { get; set; }

		/// <summary>The plugin version</summary>
		public Version Version { get; set; }

		/// <summary>The list of referenced assemblies</summary>
		public ReferenceInfo[] References { get; set; } = new ReferenceInfo[] { };
	}

	/// <summary>The referenced assembly information</summary>
	internal struct ReferenceInfo
	{
		/// <summary>The referenced assembly name</summary>
		public String Name { get; set; }

		/// <summary>The relative path to referenced assembly</summary>
		public String Path { get; set; }

		/// <summary>The referenced assembly description</summary>
		public String Description { get; set; }

		/// <summary>The referenced assembly version</summary>
		public Version Version { get; set; }
	}
}