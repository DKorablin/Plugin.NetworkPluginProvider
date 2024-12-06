using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>Информация о обновлени</summary>
	internal class UpdateInfo
	{
		/// <summary>Путь по которому загружать информацию для обновления</summary>
		public String UpdatePath { get; }

		/// <summary>Путь по которому загружать новые версии плагинов</summary>
		public String DownloadPath { get; }

		/// <summary>Массив информации о плагинах</summary>
		public PluginInfo[] Plugins { get; }

		/// <summary>Создание информации о обновлении</summary>
		/// <param name="updatePath">Путь по которому осуществлять поиск новой верси</param>
		/// <param name="plugins">Массив информации о плагинах</param>
		public UpdateInfo(String updatePath, PluginInfo[] plugins)
		{
			this.Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

			if(updatePath.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
			{//Путь заканчивается XML файлом для обновления
				this.UpdatePath = updatePath;
				this.DownloadPath = updatePath.Replace(Path.GetFileName(updatePath), String.Empty);
			} else if(updatePath.EndsWith("/") || updatePath.EndsWith("\\"))
			{//Путь заканчивается папкой в которой искать обновление
				this.UpdatePath = updatePath + Constant.XmlFileName;
				this.DownloadPath = updatePath;
			} else
			{//Путь заканчивается папкой без терминатора
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

		/// <summary>Загрузить массив информации о плагинах из XML файла</summary>
		/// <param name="xmlFilePath">Путь к XML файлу для загрузки данных</param>
		/// <returns>Массив информации о плагинах из XML файла</returns>
		internal static UpdateInfo LoadPlugins(String xmlFilePath)
		{
			Boolean isFile = File.Exists(xmlFilePath);
			Boolean isUrl = Uri.IsWellFormedUriString(xmlFilePath, UriKind.Absolute);
			if(!isFile && !isUrl)
				return null;

			XDocument doc = XDocument.Load(xmlFilePath);

			String updatePath = String.Empty;

			var pathAttr = doc.Root.Attribute("Path")
				?? throw new ArgumentNullException("Plugins.Path is the required field");
			updatePath = pathAttr.Value;

			List<PluginInfo> plugins = new List<PluginInfo>();
			foreach(XElement element in doc.Root.Elements("Plugin"))
			{
				PluginInfo item = new PluginInfo();
				if(element.Attribute("Name") == null)
					throw new ArgumentNullException("Name field is missing");
				if(element.Attribute("Version") == null)
					throw new ArgumentNullException("Version field is missing");

				item.Name = element.Attribute("Name").Value;
				item.Version = new Version(element.Attribute("Version").Value);

				item.Path = element.Attribute("Path") == null
					? item.Name + ".dll"
					: element.Attribute("Path").Value;

				if(element.Attribute("Description") != null)
					item.Description = element.Attribute("Description").Value;

				List<ReferenceInfo> references = new List<ReferenceInfo>();
				foreach(XElement refElement in element.Elements("Assembly"))
				{//Загрузка сборок, которые используются текущим плагином
					ReferenceInfo refItem = new ReferenceInfo();
					if(refElement.Attribute("Name") == null)
						throw new ArgumentNullException("Name field is missing");
					if(refElement.Attribute("Version") == null)
						throw new ArgumentNullException("Version field is missing");

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
				item.References = references.ToArray();//Список ссылок
				plugins.Add(item);
			}
			return new UpdateInfo(updatePath, plugins.ToArray());
		}

		/// <summary>Сохранить информацию о плагинах в XML файле</summary>
		/// <param name="localPath">Путь по которому необходимо сохранить информацию для обновления</param>
		/// <param name="updatePath">Путь по которому произвести обновление</param>
		/// <param name="plugins">Массив информации о плагинах</param>
		public static void SavePlugins(String localPath, String updatePath, PluginInfo[] plugins)
		{
			if(String.IsNullOrEmpty(updatePath))
				throw new ArgumentNullException("Plugins.Path", "Path is the required field");
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
					throw new ArgumentNullException("plugin.Name", "Plugin Name is the required field");
				else
					element.Add(new XAttribute("Name", plugin.Name));

				if(!String.IsNullOrEmpty(plugin.Path) && plugin.Path != plugin.Name + ".dll")
					element.Add(new XAttribute("Path", plugin.Path));

				if(plugin.Version == null)
					throw new ArgumentNullException("plugin.Version", "Plugin Version is the required field");
				else
					element.Add(new XAttribute("Version", plugin.Version.ToString()));

				element.Add(new XAttribute("Description", plugin.Description));

				foreach(ReferenceInfo refAsm in plugin.References)
				{//Добавление сборок, которые используются текущим плагином
					XElement refElement = new XElement("Assembly");

					if(String.IsNullOrEmpty(refAsm.Name))
						throw new ArgumentNullException("assembly.Name", "Assembly Name is the required field");
					else
						refElement.Add(new XAttribute("Name", refAsm.Name));

					if(!String.IsNullOrEmpty(refAsm.Path) && refAsm.Path != refAsm.Name + ".dll")
						refElement.Add(new XAttribute("Path", refAsm.Path));

					if(refAsm.Version == null)
						throw new ArgumentNullException("assembly.Version", "Assembly Version is the required field");
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