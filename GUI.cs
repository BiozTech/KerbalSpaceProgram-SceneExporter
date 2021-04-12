using System;
using System.IO;
using UnityEngine;

namespace SceneExporter
{

	[KSPAddon(KSPAddon.Startup.Instantly, true)]

	public class SceneExporterGUI : MonoBehaviour
	{

		static string strKeyRecord, strDirectory;
		static bool isStrKeyRecordOk, isStrDirectoryOk;

		static bool isGUIVisible = true;

		static Rect windowPosition = new Rect(0.25f * Screen.width, 0.15f * Screen.height, 0, 0);

		void OnGUI()
		{
			if (isGUIVisible)
			{
				GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
				windowStyle.padding = new RectOffset(11, 15, 20, 11);
				windowPosition = GUILayout.Window(("SceneExporterGUI").GetHashCode(), windowPosition, OnWindow, "SceneExporter Settings", windowStyle);
			}
		}

		void OnWindow(int windowId)
		{

			GUILayoutOption[] textFieldLayoutOptions = { GUILayout.Width(200f), GUILayout.Height(20f) };
			GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
			//GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
			//boxStyle.alignment = TextAnchor.MiddleLeft;
			//boxStyle.padding = new RectOffset(3, 3, 3, 3);
			GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
			toggleStyle.margin = new RectOffset(4, 4, 4, 8);
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.margin = new RectOffset(4, 4, 13, 4);

			GUILayout.BeginVertical(GUILayout.Width(300f));

			GUILayout.Space(6f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Recording");
			if (Settings.IsEnabled != GUILayout.Toggle(Settings.IsEnabled, Settings.IsEnabled ? " Enabled" : " Disabled", textFieldLayoutOptions))
			{
				Settings.IsEnabled = !Settings.IsEnabled;
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Exporting textures");
			if (Settings.IsExportingTextures != GUILayout.Toggle(Settings.IsExportingTextures, Settings.IsExportingTextures ? " Enabled" : " Disabled", textFieldLayoutOptions))
			{
				Settings.IsExportingTextures = !Settings.IsExportingTextures;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Key");
			ColorizeTextField(textFieldStyle, isStrKeyRecordOk);
			strKeyRecord = GUILayout.TextField(strKeyRecord, textFieldStyle, textFieldLayoutOptions);
			GUILayout.EndHorizontal();

			GUILayout.Label("Exported scenes directory");
			ColorizeTextField(textFieldStyle, isStrDirectoryOk);
			strDirectory = GUILayout.TextField(strDirectory, textFieldStyle);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Apply", buttonStyle, GUILayout.Width(125f)))
			{

				bool isOk = true;

				KeyCode key;
				isStrKeyRecordOk = Enum.TryParse(strKeyRecord, out key);
				if (isStrKeyRecordOk) Settings.KeyRecord = key;
				isOk &= isStrKeyRecordOk;

				if (strDirectory != string.Empty)
				{
					isStrDirectoryOk = Directory.Exists(strDirectory);
					if (isStrDirectoryOk) Settings.ScenesDirectory = strDirectory;
					isOk &= isStrDirectoryOk;
				} else {
					isStrDirectoryOk = true;
					Settings.ScenesDirectory = Settings.ScenesDirectoryDefault;
				}

				if (isOk) Settings.Save();

			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();

			GUI.DragWindow();

		}

		static void ColorizeTextField(GUIStyle style, bool isStrOk)
		{
			style.normal.textColor = style.focused.textColor = style.hover.textColor = (isStrOk) ? Color.white : Color.red;
		}

		public static void Toggle()
		{
			isGUIVisible = !isGUIVisible;
		}

		public static void Initialize()
		{
			strKeyRecord = Settings.KeyRecord.ToString();
			strDirectory = Settings.ScenesDirectoryToOutput();
			isStrKeyRecordOk = isStrDirectoryOk = true;
		}

		void Awake()
		{
			DontDestroyOnLoad(gameObject);
			Initialize();
		}

	}

}
