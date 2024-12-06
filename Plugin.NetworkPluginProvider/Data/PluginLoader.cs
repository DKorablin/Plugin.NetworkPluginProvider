using System;
using System.IO;

namespace Plugin.NetworkPluginProvider.Data
{
	/// <summary>Класс манипуляций с загруженными плагинами</summary>
	internal class PluginLoader : PluginLoaderBase
	{
		internal PluginLoader(Plugin plugin, String localPath)
			: base(plugin,localPath)
		{
		}

		/// <summary>Удалить файл</summary>
		/// <param name="fileName">Наименование файла для удаления</param>
		internal void DeleteFile(String fileName)
		{
			fileName = Path.GetFileName(fileName);
			base.PerformIOAction(fileName, delegate(String path) { File.Delete(path); });
		}

		/// <summary>Сохранить файл</summary>
		/// <param name="fileName">Наименование файла для сохранения</param>
		/// <param name="bytes">Содержимое файла</param>
		internal void SaveFile(String fileName, Byte[] bytes)
		{
			fileName = Path.GetFileName(fileName);
			base.PerformIOAction(fileName, delegate(String path) { File.WriteAllBytes(path, bytes); });
		}

		/// <summary>Скопировать файл из одного места в другое место</summary>
		/// <param name="downloadPath">Исходный путь</param>
		/// <param name="fileName">Целевой путь</param>
		internal void CopyFile(String downloadPath, String fileName)
		{
			fileName = Path.GetFileName(fileName);
			base.PerformIOAction(fileName, delegate(String path) { File.Copy(Path.Combine(downloadPath, fileName), path, true); });
		}
	}
}