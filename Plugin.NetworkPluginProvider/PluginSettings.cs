using System;
using System.ComponentModel;

namespace Plugin.NetworkPluginProvider
{
	public class PluginSettings
	{
		private Plugin Plugin { get; }

		internal PluginSettings(Plugin plugin)
			=> this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

		[Description("Пересоздать XML файл для обновления. Создаваемый файл не перезаписывается, а создаётся как копия")]
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
		[Description("Использовать авторизация по умолчанию")]
		[DisplayName("Default Credentials")]
		[DefaultValue(false)]
		public Boolean UseDefaultCredentials { get; set; }
	}
}