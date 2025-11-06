using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using AlphaOmega.Web;
using Plugin.FilePluginProvider;
using SAL.Flatbed;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>Manipulating plugin information</summary>
	internal class UpdateBll
	{
		private readonly PluginLoader _loader;

		private UpdateInfo _local;

		private DateTime? _lastModified;

		/// <summary>A new version has appeared</summary>
		public Boolean UpdateAvailable
		{
			get => this._local != null && (!this.CheckLocalPlugins() || this.IsNewVersion());
		}

		/// <summary>Creating a class instance with the path to the XML file containing the information</summary>
		/// <param name="plugin">The reference to the current plugin provider.</param>
		/// <param name="localPath">Path to the XML file</param>
		public UpdateBll(Plugin plugin, String localPath)
		: this(new PluginLoader(plugin, localPath))
		{
		}

		public UpdateBll(PluginLoader loader)
		{
			this._loader = loader ?? throw new ArgumentNullException(nameof(loader));
			this._local = UpdateInfo.LoadPlugins(Path.Combine(this._loader.CurrentPath, Constant.XmlFileName));
		}

		/// <summary>Update the plugin list</summary>
		public void UpdatePlugins()
		{
			if(this._local == null)
				return;

			UpdateInfo source = UpdateInfo.LoadPlugins(this._local.UpdatePath);
			foreach(PluginInfo localInfo in this._local.Plugins)
			{//Remove obsolete plugins
				Boolean found = false;
				foreach(PluginInfo sourceInfo in source.Plugins)
					if(localInfo.Name == sourceInfo.Name)
					{
						found = true;
						break;
					}
				if(!found)
				{//Remove a plugin that is no longer supported
					try
					{//The file may be locked. A mechanism for deleting files after a restart is needed
						this._loader.DeleteFile(localInfo.Path);
					} catch { }
					foreach(ReferenceInfo refAsm in localInfo.References)
						try
						{
							this._loader.DeleteFile(refAsm.Path);
						} catch { }
				}
			}

			foreach(PluginInfo sourceInfo in source.Plugins)
			{//Search for updates or new plugins
				Boolean found = false;
				foreach(PluginInfo localInfo in this._local.Plugins)
					if(localInfo.Name == sourceInfo.Name && this._loader.Exists(localInfo.Path))
					{//New version of an old plugin
						found = true;
						if(localInfo.Version < sourceInfo.Version)
							this.DownloadPlugin(source.DownloadPath, sourceInfo);
						break;
					}
				if(!found)//Download a new plugin
					this.DownloadPlugin(source.DownloadPath, sourceInfo);
			}

			this.SavePlugins(source);//Copy the XML file to local storage
		}

		/// <summary>Checking for the presence of all modules</summary> 
		/// <returns>All modules available</returns> 
		private Boolean CheckLocalPlugins()
		{
			foreach(PluginInfo local in this._local.Plugins)
			{
				if(!this._loader.Exists(local.Path))
					return false;
				foreach(ReferenceInfo child in local.References)
					if(!this._loader.Exists(child.Path))
						return false;
			}
			return true;
		}

		/// <summary>Check for a new file with plugin descriptions</summary>
		/// <returns>A newer version exists</returns>
		private Boolean IsNewVersion()
		{
			String source = this._local.UpdatePath;//Path to update
			String local = Path.Combine(this._loader.CurrentPath, Constant.XmlFileName);//Path to local file

			if(File.Exists(local))
			{
				if(Uri.IsWellFormedUriString(source, UriKind.Absolute))
				{
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(source));
					if(this._loader.Plugin.Settings.UseDefaultCredentials)
						request.Proxy = new WebProxy() { UseDefaultCredentials = true, };

					HttpWebResponse response = (HttpWebResponse)request.GetResponse();
					try
					{
						this._lastModified = response.LastModified;
						return this._lastModified > File.GetLastWriteTime(local);
					} finally
					{
						response.Close();
					}
				} else if(File.Exists(source))
					return File.GetLastWriteTime(source) > File.GetLastWriteTime(local);
				else
					throw new NotSupportedException($"Source: {source} Local: {local}");
			} else if(!String.IsNullOrEmpty(source))
				return true;
			else
				return false;
		}

		/// <summary>Download the plugin to the local plugin array</summary>
		/// <param name="downloadPath">Path to update the plugin</param>
		/// <param name="info">Description of the plugin to download</param>
		private void DownloadPlugin(String downloadPath, PluginInfo info)
		{
			if(Uri.IsWellFormedUriString(downloadPath, UriKind.Absolute))
			{
				try
				{
					using(BinaryWebRequest request = new BinaryWebRequest(downloadPath + info.Path, this._loader.Plugin.Settings.UseDefaultCredentials))
					{//Copy the plugin
						Byte[] plugin = request.GetResponse();
						this._loader.SaveFile(info.Path, plugin);
					}

					foreach(ReferenceInfo refAsm in info.References)
					{//Copying assemblies referenced by the plugin 
						using(BinaryWebRequest request = new BinaryWebRequest(downloadPath + refAsm.Path, this._loader.Plugin.Settings.UseDefaultCredentials))
						{
							Byte[] plugin = request.GetResponse();
							this._loader.SaveFile(refAsm.Path, plugin);
						}
					}
				} catch(WebException exc)
				{
					this._loader.Plugin.Trace.TraceData(TraceEventType.Error, 1, exc);
				}
			} else
			{
				this._loader.CopyFile(downloadPath, info.Path);

				foreach(ReferenceInfo refAsm in info.References)//Copying assemblies referenced by the plugin
					this._loader.CopyFile(downloadPath, refAsm.Path);
			}
		}

		public void SavePlugins(UpdateInfo info)
			=> this.SavePlugins(info.UpdatePath, info.Plugins);

		/// <summary>Save plugin information in an XML file</summary>
		/// <param name="updatePath">Path to update</param>
		/// <param name="plugins">Array of plugin information</param>
		public void SavePlugins(String updatePath, PluginInfo[] plugins)
		=> UpdateInfo.SavePlugins(Path.Combine(this._loader.CurrentPath, Constant.XmlFileName), updatePath, plugins);

		public void LoadPlugins()
		{
			foreach(String file in this._loader.GetFiles((String fileName) => { return FilePluginArgs.CheckFileExtension(fileName); }))
				try
				{
					Assembly asm = Assembly.LoadFile(file);
					this._loader.Plugin.Host.Plugins.LoadPlugin(asm, file, ConnectMode.Startup);
				} catch(BadImageFormatException)
				{//Error loading plugin. I could read the header of the file being loaded, but I'm too lazy.
					continue;
				} catch(Exception exc)
				{
					exc.Data.Add("Library", file);
					this._loader.Plugin.Trace.TraceData(TraceEventType.Error, 1, exc);
				}
		}
	}
}