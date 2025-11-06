using System;
using System.ComponentModel;

namespace Plugin.NetworkPluginProvider
{
	public class PluginSettings
	{
		private Plugin Plugin { get; }

		internal PluginSettings(Plugin plugin)
			=> this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

		[Description("Recreate the XML file for the update. The file created is not overwritten, but created as a copy.")]
		public Boolean RebuildXmlFile
		{
			get => false;
			set
			{
				if(value)
					this.Plugin.RebuildXmlFile();
			}
		}

		[Category("Proxy")]
		[Description("Use default authorization")]
		[DisplayName("Default Credentials")]
		[DefaultValue(false)]
		public Boolean UseDefaultCredentials { get; set; }
	}
}