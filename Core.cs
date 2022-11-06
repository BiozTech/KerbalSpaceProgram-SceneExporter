using System;
using System.IO;
using UnityEngine;

namespace SceneExporter
{

	public static class Core
	{

		public static bool IsGUIVisible = false, IsExportingTextures = false;
		public static float DistanceMax = 50f;

		public static void Log(object message)
		{
			Debug.Log("[SceneExporter] » " + message);
		}
		public static void LogException(string componentName, string gameObjectName)
		{
			Log(String.Format("It seems you've caught an exception on a {0} of {1}. Now things are beginning to make sense. More later.", componentName, gameObjectName));
		}

	}

	public static class Settings
	{

		public static string ScenesDirectoryDefault = KSPUtil.ApplicationRootPath, ScenesDirectory = ScenesDirectoryDefault;

		static string pluginDataDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/BiozTech/PluginData"), settingsFileName = Path.Combine(pluginDataDir, "SceneExporterSettings.cfg");
		static string configTagMain = "SceneExporterSettings", configTagScenesDirectory = "ScenesDirectory";

		public static void Load()
		{
			if (!File.Exists(settingsFileName))
			{
				Save();
				Core.Log("Have no settings file, huh? Here is one for you (づ｡◕‿‿◕｡)づ");
				return;
			}
			ConfigNode configNode = ConfigNode.Load(settingsFileName);
			if (!configNode.HasNode(configTagMain))
			{
				BackupAndSave();
				Core.Log("General Failure reading settings file");
				return;
			}
			configNode = configNode.GetNode(configTagMain);
			try
			{

				bool isOk = true, isOkCurrent;

				isOkCurrent = configNode.HasValue(configTagScenesDirectory);
				if (isOkCurrent)
				{
					string directory = configNode.GetValue(configTagScenesDirectory);
					if (directory != string.Empty)
					{
						isOkCurrent = Directory.Exists(directory);
						if (isOkCurrent) ScenesDirectory = directory;
					}
				}
				isOk &= isOkCurrent;

				if (!isOk)
				{
					BackupAndSave();
					Core.Log("General Failure reading settings file");
				}

			}
			catch (Exception stupid)
			{
				BackupAndSave();
				Core.Log("Not to worry, we are still flying half a ship.");
				Core.Log(stupid.Message);
			}
			SceneExporterGUI.Initialize();
		}

		public static void Save()
		{
			ConfigNode configNode = new ConfigNode(configTagMain);
			configNode.AddValue(configTagScenesDirectory, DirectoryToOutput(ScenesDirectory, ScenesDirectoryDefault), "Folder the scene will be saved to");
			if (!Directory.Exists(pluginDataDir)) Directory.CreateDirectory(pluginDataDir);
			File.WriteAllText(settingsFileName, configNode.ToString(), System.Text.Encoding.Unicode);
		}
		static void BackupAndSave()
		{
			string settingsFileBackupName = settingsFileName + ".bak";
			if (File.Exists(settingsFileBackupName)) File.Delete(settingsFileBackupName);
			File.Move(settingsFileName, settingsFileBackupName);
			Save();
		}

		public static string DirectoryToOutput(string directory, string directoryDefault)
		{
			return (ComparePaths(directory, directoryDefault) != 0) ? directory : string.Empty;
		}
		public static int ComparePaths(string path1, string path2)
		{
			return String.Compare(Path.GetFullPath(path1).TrimEnd('\\'), Path.GetFullPath(path2).TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase);
		}

	}

}
