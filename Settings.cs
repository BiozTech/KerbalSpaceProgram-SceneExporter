using System;
using System.IO;
using UnityEngine;

namespace SceneExporter
{

	public static class Settings
	{

		public static bool IsEnabled = true;
		public static bool IsExportingTextures = false;
		public static float DistanceMax = 100f;

		public static KeyCode KeyRecord = KeyCode.F6;
		public static string ScenesDirectoryDefault = KSPUtil.ApplicationRootPath;
		public static string ScenesDirectory = ScenesDirectoryDefault;

		static string pluginDataDir = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/SceneExporter/PluginData");
		static string settingsFileName = Path.Combine(pluginDataDir, "settings.cfg");
		static string configTagMain = "SceneExporterSettings";
		static string configTagKey = "Key";
		static string configTagScenesDirectory = "ScenesDirectory";

		public static void Load()
		{
			if (!File.Exists(settingsFileName))
			{
				Save();
				Log("Have no settings file, huh? Here is one for you (づ｡◕‿‿◕｡)づ");
				return;
			}
			ConfigNode configNode = ConfigNode.Load(settingsFileName);
			if (!configNode.HasNode(configTagMain))
			{
				BackupAndSave();
				Log("General Failure reading settings file");
				return;
			}
			configNode = configNode.GetNode(configTagMain); // ;)
			try
			{

				bool isOk = true;
				bool isOkCurrent;

				isOkCurrent = configNode.HasValue(configTagKey);
				if (isOkCurrent)
				{
					KeyCode key;
					isOkCurrent = Enum.TryParse(configNode.GetValue(configTagKey), out key);
					if (isOkCurrent) KeyRecord = key;
				}
				isOk &= isOkCurrent;

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
					Log("General Failure reading settings file");
				}

			}
			catch (Exception stupid)
			{
				BackupAndSave();
				Log("Not to worry, we are still flying half a ship.");
				Log(stupid.Message);
			}
			SceneExporterGUI.Initialize();
		}

		public static void Save()
		{
			ConfigNode configNode = new ConfigNode(configTagMain);
			configNode.AddValue(configTagKey, KeyRecord, "Key to start recording (Alt+Key to open menu)");
			configNode.AddValue(configTagScenesDirectory, ScenesDirectoryToOutput(), "Folder the shots will be saved to");
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

		public static string ScenesDirectoryToOutput()
		{
			return (ComparePaths(ScenesDirectory, ScenesDirectoryDefault) != 0) ? ScenesDirectory : string.Empty;
		}
		public static int ComparePaths(string path1, string path2)
		{
			return String.Compare(Path.GetFullPath(path1).TrimEnd('\\'), Path.GetFullPath(path2).TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase);
		}

		public static void Log(object message)
		{
			Debug.Log("[SceneExporter] » " + message);
		}

	}

}
