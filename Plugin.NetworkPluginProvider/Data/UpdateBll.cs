using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using AlphaOmega.Web;
using SAL.Flatbed;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>Манипуляция с информацией о плагинах</summary>
	internal class UpdateBll
	{
		private readonly PluginLoader _loader;
		private UpdateInfo _local;
		private DateTime? _lastModified;

		/// <summary>Появилась новая версия</summary>
		public Boolean UpdateAvailable
		{
			get => this._local != null && (!this.CheckLocalPlugins() || this.IsNewVersion());
		}

		/// <summary>Локальная папка в которую сохраняются плагины после загрузки из интернетов</summary>
		//private String LocalPath { get { return this._loader.LocalPath; } }

		/// <summary>Создание экземпляра класса с указанием пути до XML файла с информацией</summary>
		/// <param name="xmlFilePath">Путь к XML файлу</param>
		public UpdateBll(Plugin plugin, String localPath)
			: this(new PluginLoader(plugin, localPath))
		{
		}

		public UpdateBll(PluginLoader loader)
		{
			this._loader = loader ?? throw new ArgumentNullException(nameof(loader));
			this._local = UpdateInfo.LoadPlugins(Path.Combine(this._loader.CurrentPath, Constant.XmlFileName));
		}

		/// <summary>Обновить список плагинов</summary>
		public void UpdatePlugins()
		{
			if(this._local == null)
				return;

			UpdateInfo source = UpdateInfo.LoadPlugins(this._local.UpdatePath);
			foreach(PluginInfo localInfo in this._local.Plugins)
			{//Удаление устаревших плагинов
				Boolean found = false;
				foreach(PluginInfo sourceInfo in source.Plugins)
					if(localInfo.Name == sourceInfo.Name)
					{
						found = true;
						break;
					}
				if(!found)
				{//Удаление плагина который больше не поддерживается
					try
					{//Файл может быть залочен. Нужен механизм удаения фалов после перезапуска
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
			{//Поиск обновлений или новых плагинов
				Boolean found = false;
				foreach(PluginInfo localInfo in this._local.Plugins)
					if(localInfo.Name == sourceInfo.Name && this._loader.Exists(localInfo.Path))
					{//Новая версия старого плагина
						found = true;
						if(localInfo.Version < sourceInfo.Version)
							this.DownloadPlugin(source.DownloadPath, sourceInfo);
						break;
					}
				if(!found)//Загрузка нового плагина
					this.DownloadPlugin(source.DownloadPath, sourceInfo);
			}

			this.SavePlugins(source);//Копирование XML файла в локальное хранилище
		}

		/// <summary>Проверка на наличие всех модулей</summary>
		/// <returns>Все модули в наличии</returns>
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

		/// <summary>Проверить наличие нового файла с описаниями плагинов</summary>
		/// <returns>Существует более новая версия</returns>
		private Boolean IsNewVersion()
		{
			String source = this._local.UpdatePath;//Путь для обновления
			String local = Path.Combine(this._loader.CurrentPath, Constant.XmlFileName);//Путь к локальному файлу

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
					throw new NotSupportedException(String.Format("Source: {0} Local: {1}", source, local));
			} else if(!String.IsNullOrEmpty(source))
				return true;
			else
				return false;
		}

		/// <summary>Загрузить плагин в локальный массив пагинов</summary>
		/// <param name="downloadPath">Путь по которому обновить плагин</param>
		/// <param name="info">Описание плагина для загрузки</param>
		private void DownloadPlugin(String downloadPath, PluginInfo info)
		{
			if(Uri.IsWellFormedUriString(downloadPath, UriKind.Absolute))
			{
				try
				{
					using(BinaryWebRequest request = new BinaryWebRequest(downloadPath + info.Path, this._loader.Plugin.Settings.UseDefaultCredentials))
					{//Копирование плагина
						Byte[] plugin = request.GetResponse();
						this._loader.SaveFile(info.Path, plugin);
					}

					foreach(ReferenceInfo refAsm in info.References)
					{//Копирование сборок, на которые ссылается плагин
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

				foreach(ReferenceInfo refAsm in info.References)//Копирование сборок на которые ссылается плагин
					this._loader.CopyFile(downloadPath, refAsm.Path);
			}
		}

		public void SavePlugins(UpdateInfo info)
			=> this.SavePlugins(info.UpdatePath, info.Plugins);

		/// <summary>Сохранить информацию о плагинах в XML файле</summary>
		/// <param name="updatePath">Путь по которому произвести обновление</param>
		/// <param name="plugins">Массив информации о плагинах</param>
		public void SavePlugins(String updatePath, PluginInfo[] plugins)
			=> UpdateInfo.SavePlugins(Path.Combine(this._loader.CurrentPath, Constant.XmlFileName), updatePath, plugins);

		public void LoadPlugins()
		{
			foreach(String file in this._loader.GetFiles(delegate (String fileName) { return Path.GetExtension(fileName).Equals(".dll", StringComparison.InvariantCultureIgnoreCase); }))
				try
				{
					Assembly asm = Assembly.LoadFile(file);
					this._loader.Plugin.Host.Plugins.LoadPlugin(asm, file, ConnectMode.Startup);
				} catch(BadImageFormatException)
				{//Ошибка загрузки плагина. Можно почитать заголовок загружаемого файла, но мне влом
					continue;
				} catch(Exception exc)
				{
					exc.Data.Add("Library", file);
					this._loader.Plugin.Trace.TraceData(TraceEventType.Error, 1, exc);
				}
		}
	}
}