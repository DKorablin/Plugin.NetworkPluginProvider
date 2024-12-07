using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Plugin.FilePluginProvider;
using Plugin.NetworkPluginProvider.Data;
using SAL.Flatbed;

namespace Plugin.NetworkPluginProvider
{
	public class Plugin : IPluginProvider, IPluginSettings<PluginSettings>
	{
		private TraceSource _trace;
		private PluginSettings _settings;
		private FilePluginArgs _args;

		internal TraceSource Trace { get => this._trace ?? (this._trace = Plugin.CreateTraceSource<Plugin>()); }

		internal IHost Host { get; }

		/// <summary>Родительский провайдер плагинов</summary>
		IPluginProvider IPluginProvider.ParentProvider { get; set; }

		Object IPluginSettings.Settings { get => this.Settings; }

		public PluginSettings Settings
		{
			get
			{
				if(this._settings == null)
				{
					this._settings = new PluginSettings(this);
					this.Host.Plugins.Settings(this).LoadAssemblyParameters(this._settings);
				}
				return this._settings;
			}
		}

		public Plugin(IHost host)
			=> this.Host = host ?? throw new ArgumentNullException(nameof(host));

		/// <summary>Пересоздать XML файл для обновления.</summary>
		/// <remarks>Создаваемый файл не перезаписывается, а создаётся как копия</remarks>
		public void RebuildXmlFile()
		{
			UpdateInfo info = UpdateInfo.LoadPlugins(Path.Combine(this._args.PluginPath[0], Constant.XmlFileName));

			PluginInfo[] plugins = new PluginInfo[this.Host.Plugins.Count];
			Int32 index = 0;
			foreach(IPluginDescription plugin in this.Host.Plugins)
				plugins[index++] = new PluginInfo() { Name = plugin.Name, Version = plugin.Version, Description = plugin.Description, };

			String fileName = Plugin.GetUniqueFileName(this._args.PluginPath[0], Constant.XmlFileName, 0);

			String downloadPath = info == null ? String.Empty : info.UpdatePath;
			UpdateInfo.SavePlugins(Path.Combine(this._args.PluginPath[0], fileName), downloadPath, plugins);
		}

		Boolean IPlugin.OnConnection(ConnectMode mode)
		{
			this._args = new FilePluginArgs();
			return true;
		}

		Boolean IPlugin.OnDisconnection(DisconnectMode mode)
		{
			switch(mode)
			{
			case DisconnectMode.FlatbedClosed:
			case DisconnectMode.HostShutdown:
				return true;
			default:
				this.Trace.TraceEvent(TraceEventType.Error, 10, "Settings provider plugin can't be unloaded at the runtime");
				return false;
			}
		}

		void IPluginProvider.LoadPlugins()
		{
			//AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			/*if(String.IsNullOrEmpty(pluginPath))
				pluginPath = AppDomain.CurrentDomain.BaseDirectory;*/

			this.FindPluginI();
		}

		Assembly IPluginProvider.ResolveAssembly(String assemblyName)
		{
			if(String.IsNullOrEmpty(assemblyName))
				throw new ArgumentNullException(nameof(assemblyName));

			AssemblyName targetName = new AssemblyName(assemblyName);
			foreach(String pluginPath in this._args.PluginPath)
				if(Directory.Exists(pluginPath))
					foreach(String file in new PluginLoader(this, pluginPath).GetFiles((String fileName) => { return FilePluginArgs.CheckFileExtension(fileName); }))
						try
						{
							AssemblyName name = AssemblyName.GetAssemblyName(file);
							if(name.FullName == targetName.FullName)
								return Assembly.LoadFile(file);
							//return assembly;//TODO: Reference DLL из оперативной памяти не цепляются!
						} catch(Exception)//Пропускаем все ошибки. Мы Resolve'им библиотеку, а не разбираемся с плагинами
						{
							continue;
						}

			this.Trace.TraceEvent(TraceEventType.Warning, 5, "The provider {2} is unable to locate the assembly {0} in the path {1}", assemblyName, String.Join(",", this._args.PluginPath), this.GetType());
			IPluginProvider parentProvider = ((IPluginProvider)this).ParentProvider;
			return parentProvider == null
				? null
				: parentProvider.ResolveAssembly(assemblyName);
		}

		private void FindPluginI()
		{
			foreach(String pluginPath in this._args.PluginPath)
				if(Directory.Exists(pluginPath))
				{
					UpdateBll bll = new UpdateBll(this, pluginPath);
					if(bll.UpdateAvailable)
						bll.UpdatePlugins();

					bll.LoadPlugins();
					/*if(this.Host.Plugins.Count == 0)//TODO: Проблема возникает при Assembly.LoadFile(...) если такая сборка уже была загружена
						foreach(String file in Directory.GetFiles(pluginPath))
							if(FilePluginArgs.CheckExtension(file))
								try
								{
									Assembly asm = Assembly.LoadFile(file);
									this.Host.Plugins.LoadPlugin(asm, file, ConnectMode.Startup);
								} catch(BadImageFormatException)//Ошибка загрузки плагина. Можно почитать заголовок загружаемого файла, но мне влом
								{
									continue;
								} catch(Exception exc)
								{
									exc.Data.Add("Library", file);
									this.Host.LogException(this, exc);
								}*/
				}
		}

		private static TraceSource CreateTraceSource<T>(String name = null) where T : IPlugin
		{
			TraceSource result = new TraceSource(typeof(T).Assembly.GetName().Name + name);
			result.Switch.Level = SourceLevels.All;
			result.Listeners.Remove("Default");
			result.Listeners.AddRange(System.Diagnostics.Trace.Listeners);
			return result;
		}

		/// <summary>Получить уникальное имя файла не повторяющееся на файловой системе.</summary>
		/// <param name="path">Путь к папке в которой осуществлять поиск файла.</param>
		/// <param name="fileName">Наименование файла.</param>
		/// <param name="index">Индекс файла, который будет подставляться к названию файла.</param>
		/// <returns>Уникальное наименование файла в папке <see cref="T:path"/>.</returns>
		private static String GetUniqueFileName(String path, String fileName, Int32 index)
		{
			String file = index > 0
				? $"{Path.GetFileNameWithoutExtension(fileName)}({index}){Path.GetExtension(fileName)}"
				: fileName;

			return File.Exists(Path.Combine(path, file))
				? Plugin.GetUniqueFileName(path, fileName, ++index)
				: file;
		}
	}
}