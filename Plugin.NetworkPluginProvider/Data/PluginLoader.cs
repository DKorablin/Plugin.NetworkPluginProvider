using System;
using System.IO;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>Class for handling loaded plugins</summary>
	internal class PluginLoader : PluginLoaderBase
	{
		internal PluginLoader(Plugin plugin, String localPath)
		: base(plugin, localPath)
		{
		}

		/// <summary>Delete file</summary>
		/// <param name="fileName">Name of the file to delete</param>
		internal void DeleteFile(String fileName)
		{
			fileName = Path.GetFileName(fileName);
			base.PerformIOAction(fileName, delegate (String path) { File.Delete(path); });
		}

		/// <summary>Save file</summary>
		/// <param name="fileName">Name of file to save</param>
		/// <param name="bytes">File contents</param>
		internal void SaveFile(String fileName, Byte[] bytes)
		{
			fileName = Path.GetFileName(fileName);
			base.PerformIOAction(fileName, delegate (String path) { File.WriteAllBytes(path, bytes); });
		}

		/// <summary>Copy a file from one location to another</summary>
		/// <param name="downloadPath">Source path</param>
		/// <param name="fileName">Destination path</param>
		internal void CopyFile(String downloadPath, String fileName)
		{
			fileName = Path.GetFileName(fileName);
			base.PerformIOAction(fileName, delegate (String path) { File.Copy(Path.Combine(downloadPath, fileName), path, true); });
		}
	}
}