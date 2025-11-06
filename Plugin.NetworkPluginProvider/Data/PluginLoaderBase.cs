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

		/// <summary>Plugin loader</summary>
		internal Plugin Plugin { get; }

		/// <summary>Get the name of the application for which the autostart function is defined</summary>
		protected String ApplicationName
		{
			get
			{
				String application;
				if(Assembly.GetEntryAssembly() != null)
					application = Assembly.GetEntryAssembly().GetName().Name;
				else
					application = Process.GetCurrentProcess().ProcessName;
				//Because this is a plugin loader. The Kernel definitely won't be here.
				/*IList<IPluginBase> kernels = this.Plugin.Host.Plugins.FindPluginType<IPluginKernel>();
				foreach(var kernel in kernels)
				application += "|" + kernel.ID.ToString();*/

				return application;
			}
		}

		/// <summary>A temporary folder exists. This means there was an error that caused the folder to change.</summary>
		private Boolean IsTempPathExists { get => Directory.Exists(this.GetTempPath()); }

		/// <summary>The folder where plugins are saved.</summary>
		private String LocalPath { get; }

		/// <summary>Temporary folder where plugins are saved if saving to the main plugins folder is not possible</summary>
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

		/// <summary>Use a temporary path for files</summary>
		internal Boolean IsTempPathUsed { get; private set; } = false;

		/// <summary>Current path for saving/loading plugins</summary>
		internal String CurrentPath { get => this.IsTempPathUsed ? this.TempPath : this.LocalPath; }

		internal PluginLoaderBase(Plugin plugin, String localPath)
		{
			this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
			this.LocalPath = localPath;

			if(this.IsTempPathExists && !PluginLoaderBase.HasPermissionOnDir(this.LocalPath))
				this.IsTempPathUsed = true;
		}

		/// <summary>Get a temporary path for saving plugins</summary>
		/// <returns>Reference to the temporary folder</returns>
		private String GetTempPath()
		=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), this.ApplicationName);

		internal Boolean Exists(String fileName)
		{
			fileName = Path.GetFileName(fileName);
			return File.Exists(Path.Combine(this.CurrentPath, fileName));
		}

		/// <summary>Checking directory permissions</summary>
		/// <param name="path">Directory to check permissions for</param>
		/// <returns>The directory has write and delete permissions</returns>
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
			{//File not found.
				this.Plugin.Trace.TraceData(TraceEventType.Error, 1, exc);
			} catch(UnauthorizedAccessException)
			{//Folder access denied
				if(!this.IsTempPathUsed)
				{
					this.IsTempPathUsed = true;
					this.PerformIOAction(fileName, deleg);
				} else
					throw;
			} catch(IOException)
			{//File in use by another application
				if(File.Exists(filePath))
				{//Does the file exist?
					String oldPath = filePath.Remove(filePath.Length - 1) + "_";
					if(File.Exists(oldPath))
						File.Delete(oldPath);

					File.Move(filePath, oldPath);//Will only be loaded the next time the program is loaded.
					this.PerformIOAction(fileName, deleg);
				} else
					throw;
			}
		}
	}
}