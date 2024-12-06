using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Collections.Generic;

namespace Plugin.NetworkPluginProvider.Data
{
	internal class PluginLoaderBase
	{
		private String _tempPath;

		public delegate Boolean ComparerFunc(String filePath);

		/// <summary>Загрузчик плагинов</summary>
		internal Plugin Plugin { get; }

		/// <summary>Получить наименование приложения для которого прописывается функция автозапуска</summary>
		protected String ApplicationName
		{
			get
			{
				String application;
				if(Assembly.GetEntryAssembly() != null)
					application = Assembly.GetEntryAssembly().GetName().Name;
				else
					application = Process.GetCurrentProcess().ProcessName;
				//Т.к. это загрузчик плагинов. И Kernel'а тут точно не будет.
				/*IList<IPluginBase> kernels = this.Plugin.Host.Plugins.FindPluginType<IPluginKernel>();
				foreach(var kernel in kernels)
					application += "|" + kernel.ID.ToString();*/

				return application;
			}
		}

		/// <summary>Существует временная папка. Т.е. была ошибка из-за которой произошла смена папки</summary>
		private Boolean IsTempPathExists { get => Directory.Exists(this.GetTempPath()); }

		/// <summary>Папка куда сохраняются плагины</summary>
		private String LocalPath { get; }

		/// <summary>Временная папка, куда сохраняются плагины, если невозможно сохранить в основную папку с плагинами</summary>
		private String TempPath
		{
			get
			{
				if(this._tempPath == null)
				{
					this._tempPath = this.GetTempPath();
					if(!Directory.Exists(this._tempPath))
						Directory.CreateDirectory(this._tempPath);
				}
				return this._tempPath;
			}
		}

		/// <summary>Использовать временный путь к файлам</summary>
		internal Boolean IsTempPathUsed { get; private set; } = false;

		/// <summary>Текущий путь для сохранения/загрузки плагинов</summary>
		internal String CurrentPath { get => this.IsTempPathUsed ? this.TempPath : this.LocalPath; }

		internal PluginLoaderBase(Plugin plugin, String localPath)
		{
			this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
			this.LocalPath = localPath;

			if(this.IsTempPathExists && !PluginLoaderBase.HasPermissionOnDir(this.LocalPath))
				this.IsTempPathUsed = true;
		}

		/// <summary>Получить временный путь для сохранения плагинов</summary>
		/// <returns>Ссылка на временную папку</returns>
		private String GetTempPath()
			=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), this.ApplicationName);

		internal Boolean Exists(String fileName)
		{
			fileName = Path.GetFileName(fileName);
			return File.Exists(Path.Combine(this.CurrentPath, fileName));
		}

		/// <summary>Проверка разрешений на директорию</summary>
		/// <param name="path">Директроия на которую провериь разрешения</param>
		/// <returns>На директорию присутсвуют разрешения для записи и удаления</returns>
		private static Boolean HasPermissionOnDir(String path)
		{
			DirectorySecurity acl;
#if NETSTANDARD || NETCOREAPP
			acl = new DirectoryInfo(path).GetAccessControl();
#else
			acl = Directory.GetAccessControl(path);
#endif
			if(acl == null)
				return false;
			AuthorizationRuleCollection accessRules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
			if(accessRules == null)
				return false;

			Boolean writeAllow = false;
			Boolean writeDeny = false;

			Boolean deleteAllow = false;
			Boolean deleteDeny = false;

			foreach(FileSystemAccessRule rule in accessRules)
			{
				if((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write)
				{
					if(rule.AccessControlType == AccessControlType.Allow)
						writeAllow = true;
					else if(rule.AccessControlType == AccessControlType.Deny)
						writeDeny = true;
				}
				if((rule.FileSystemRights & FileSystemRights.Delete) == FileSystemRights.Delete)
				{
					if(rule.AccessControlType == AccessControlType.Allow)
						deleteAllow = true;
					else if(rule.AccessControlType == AccessControlType.Deny)
						deleteDeny = true;
				}
			}

			return (writeAllow && !writeDeny) && (deleteAllow && !deleteDeny);
		}

		internal IEnumerable<String> GetFiles(ComparerFunc deleg)
		{
			foreach(String file in Directory.GetFiles(this.CurrentPath))
				if(deleg(file))
					yield return file;
		}

		protected internal void PerformIOAction(String fileName, Action<String> deleg)
		{
			String filePath = Path.Combine(this.CurrentPath, fileName);
			try
			{
				deleg(filePath);
			} catch(FileNotFoundException exc)
			{//Файл не найден.
				this.Plugin.Trace.TraceData(TraceEventType.Error, 1, exc);
			} catch(UnauthorizedAccessException)
			{//Нет доступа на папку
				if(!this.IsTempPathUsed)
				{
					this.IsTempPathUsed = true;
					this.PerformIOAction(fileName, deleg);
				} else
					throw;
			} catch(IOException)
			{//Файл занят другим приложением
				if(File.Exists(filePath))
				{//А файл-то есть?
					String oldPath = filePath.Remove(filePath.Length - 1) + "_";
					if(File.Exists(oldPath))
						File.Delete(oldPath);

					File.Move(filePath, oldPath);//Будет загружен только при следующей загрузки программы
					this.PerformIOAction(fileName, deleg);
				} else
					throw;
			}
		}
	}
}