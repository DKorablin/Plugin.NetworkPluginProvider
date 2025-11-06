using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>Update information</summary>
	internal class UpdateInfo
	{
		/// <summary>Path to download update information</summary>
		public String UpdatePath { get; }

		/// <summary>Path to download new plugin versions</summary>
		public String DownloadPath { get; }

		/// <summary>Array of plugin information</summary>
		public PluginInfo[] Plugins { get; }

		/// <summary>Creating update information</summary>
		/// <param name="updatePath">Path to search for the new version</param>
		/// <param name="plugins">Array of plugin information</param>
		public UpdateInfo(String updatePath, PluginInfo[] plugins)
		{
			this.Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

			if(updatePath.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
			{//The path ends with the XML file to update
				this.UpdatePath = updatePath;
				this.DownloadPath = updatePath.Replace(Path.GetFileName(updatePath), String.Empty);
			} else if(updatePath.EndsWith("/") || updatePath.EndsWith("\\"))
			{//The path ends with the folder in which to look for the update
				this.UpdatePath = updatePath + Constant.XmlFileName;
				this.DownloadPath = updatePath;
			} else
			{//The path ends with a folder without a terminator
				if(File.Exists(updatePath))
					updatePath += '\\';
				else if(Uri.IsWellFormedUriString(updatePath, UriKind.Absolute))
					updatePath += '/';
				else
					throw new NotSupportedException();

				this.UpdatePath = updatePath + Constant.XmlFileName;
				this.DownloadPath = updatePath;
			}
		}

		/// <summary>Load an array of plugin information from an XML file</summary>
		/// <param name="xmlFilePath">Path to the XML file to load data</param>
		/// <returns>Array of plugin information from an XML file</returns>
		internal static UpdateInfo LoadPlugins(String xmlFilePath)
		{
			Boolean isFile = File.Exists(xmlFilePath);
			Boolean isUrl = Uri.IsWellFormedUriString(xmlFilePath, UriKind.Absolute);
			if(!isFile && !isUrl)
				return null;

			XDocument doc = XDocument.Load(xmlFilePath);

			String updatePath = String.Empty;

			var pathAttr = doc.Root.Attribute("Path")
				?? throw new InvalidOperationException("Plugins.Path is the required field");
			updatePath = pathAttr.Value;

			List<PluginInfo> plugins = new List<PluginInfo>();
			foreach(XElement element in doc.Root.Elements("Plugin"))
			{
				PluginInfo item = new PluginInfo();
				if(element.Attribute("Name") == null)
					throw new InvalidOperationException("Name field is missing");
				if(element.Attribute("Version") == null)
					throw new InvalidOperationException("Version field is missing");

				item.Name = element.Attribute("Name").Value;
				item.Version = new Version(element.Attribute("Version").Value);

				item.Path = element.Attribute("Path") == null
					? item.Name + ".dll"
					: element.Attribute("Path").Value;

				if(element.Attribute("Description") != null)
					item.Description = element.Attribute("Description").Value;

				List<ReferenceInfo> references = new List<ReferenceInfo>();
				foreach(XElement refElement in element.Elements("Assembly"))
				{//Loading assemblies used by the current plugin
					ReferenceInfo refItem = new ReferenceInfo();
					if(refElement.Attribute("Name") == null)
						throw new InvalidOperationException("Name field is missing");
					if(refElement.Attribute("Version") == null)
						throw new InvalidOperationException("Version field is missing");

					refItem.Name = refElement.Attribute("Name").Value;
					refItem.Version = new Version(refElement.Attribute("Version").Value);

					if(refElement.Attribute("Path") == null)
						refItem.Path = refItem.Name + ".dll";
					else
						refItem.Path = refElement.Attribute("Path").Value;

					if(refElement.Attribute("Description") != null)
						refItem.Description = refElement.Attribute("Description").Value;

					references.Add(refItem);
				}
				item.References = references.ToArray();//The list of links
				plugins.Add(item);
			}
			return new UpdateInfo(updatePath, plugins.ToArray());
		}

		/// <summary>Save plugin information in an XML file</summary>
		/// <param name="localPath">Path to save update information</param>
		/// <param name="updatePath">Path to perform the update</param>
		/// <param name="plugins">Array of plugin information</param>
		public static void SavePlugins(String localPath, String updatePath, PluginInfo[] plugins)
		{
			if(String.IsNullOrEmpty(updatePath))
				throw new ArgumentNullException(nameof(updatePath), "Path is the required field");
			if(plugins.Length == 0)
				return;

			XDocument doc = new XDocument();
			XElement root = new XElement("Plugins");
			root.Add(new XAttribute("Path", updatePath));
			doc.Add(root);

			foreach(PluginInfo plugin in plugins)
			{
				XElement element = new XElement("Plugin");
				if(String.IsNullOrEmpty(plugin.Name))
					throw new InvalidOperationException("Plugin Name is the required field");
				else
					element.Add(new XAttribute("Name", plugin.Name));

				if(!String.IsNullOrEmpty(plugin.Path) && plugin.Path != plugin.Name + ".dll")
					element.Add(new XAttribute("Path", plugin.Path));

				if(plugin.Version == null)
					throw new InvalidOperationException("Plugin Version is the required field");
				else
					element.Add(new XAttribute("Version", plugin.Version.ToString()));

				element.Add(new XAttribute("Description", plugin.Description));

				foreach(ReferenceInfo refAsm in plugin.References)
				{//Adding assemblies used by the current plugin
					XElement refElement = new XElement("Assembly");

					if(String.IsNullOrEmpty(refAsm.Name))
						throw new InvalidOperationException("Assembly Name is the required field");
					else
						refElement.Add(new XAttribute("Name", refAsm.Name));

					if(!String.IsNullOrEmpty(refAsm.Path) && refAsm.Path != refAsm.Name + ".dll")
						refElement.Add(new XAttribute("Path", refAsm.Path));

					if(refAsm.Version == null)
						throw new InvalidOperationException("Assembly Version is the required field");
					else
						refElement.Add(new XAttribute("Version", refAsm.Version.ToString()));

					refElement.Add(new XAttribute("Description", refAsm.Description));

					element.Add(refElement);
				}
				doc.Root.Add(element);
			}
			doc.Save(localPath);
		}
	}
}