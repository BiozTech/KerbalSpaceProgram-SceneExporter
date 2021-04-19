using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace SceneExporter
{

	[KSPAddon(KSPAddon.Startup.Instantly, true)]

	public class SceneExporterGUI : MonoBehaviour
	{

		static string strKeyRecord, strDistanceMax, strDirectory;
		static bool isStrDistanceMaxOk, isStrDirectoryOk;

		static Rect windowPosition = new Rect(UnityEngine.Random.Range(0.23f, 0.27f) * Screen.width, UnityEngine.Random.Range(0.13f, 0.17f) * Screen.height, 0, 0);

		void OnGUI()
		{
			if (Settings.IsGUIVisible)
			{
				GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
				windowStyle.padding = new RectOffset(15, 15, 20, 11);
				windowPosition = GUILayout.Window(("SceneExporterGUI").GetHashCode(), windowPosition, OnWindow, "SceneExporter Settings", windowStyle);
			}
		}

		void OnWindow(int windowId)
		{

			GUILayoutOption[] textFieldLayoutOptions = { GUILayout.Width(200f), GUILayout.Height(20f) };
			GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
			GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.alignment = TextAnchor.MiddleLeft;
			textFieldStyle.padding = boxStyle.padding = new RectOffset(4, 4, 3, 3);
			GUIStyle buttonApplyStyle = new GUIStyle(GUI.skin.button);
			buttonApplyStyle.margin = new RectOffset(92, 4, 13, 4);

			GUILayout.BeginVertical(GUILayout.Width(300f));

			GUILayout.Space(6f);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Exporting");
			if (Settings.IsEnabled != GUILayout.Toggle(Settings.IsEnabled, Settings.IsEnabled ? " Enabled" : " Disabled", textFieldLayoutOptions))
			{
				Settings.IsEnabled = !Settings.IsEnabled;
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Export textures");
			if (Settings.IsExportingTextures != GUILayout.Toggle(Settings.IsExportingTextures, Settings.IsExportingTextures ? " Enabled" : " Disabled", textFieldLayoutOptions))
			{
				Settings.IsExportingTextures = !Settings.IsExportingTextures;
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Key");
			ColorizeFieldIsWrong(boxStyle, !Settings.IsReadingKeys);
			GUILayout.Box(strKeyRecord, boxStyle, GUILayout.Width(125f), GUILayout.Height(20f));
			GUILayout.Space(1f);
			if (GUILayout.Button("Set", GUILayout.Width(70f), GUILayout.Height(20f)))
			{
				Settings.IsReadingKeys = !Settings.IsReadingKeys;
			}
			if (Settings.IsReadingKeys)
			{
				if (Event.current.isKey && Event.current.type == EventType.KeyUp && Event.current.keyCode != KeyCode.LeftAlt && Event.current.keyCode != KeyCode.RightAlt && Event.current.keyCode != KeyCode.AltGr)
				{
					Settings.IsReadingKeys = false;
					Settings.KeyRecord = Event.current.keyCode;
					strKeyRecord = Settings.KeyRecord.ToString();
					Settings.Save();
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Radius");
			ColorizeFieldIsWrong(textFieldStyle, isStrDistanceMaxOk);
			strDistanceMax = GUILayout.TextField(strDistanceMax, textFieldStyle, textFieldLayoutOptions);
			GUILayout.EndHorizontal();

			GUILayout.Label("Exported scenes directory");
			ColorizeFieldIsWrong(textFieldStyle, isStrDirectoryOk);
			strDirectory = GUILayout.TextField(strDirectory, textFieldStyle);

			if (GUILayout.Button("Apply", buttonApplyStyle, GUILayout.Width(125f)))
			{

				bool isOk = true;

				float distance;
				isStrDistanceMaxOk = float.TryParse(strDistanceMax.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out distance);
				if (isStrDistanceMaxOk)
				{
					Settings.DistanceMax = distance;
					strDistanceMax = Settings.DistanceMax.ToString();
				}
				isOk &= isStrDistanceMaxOk;

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

			GUILayout.EndVertical();

			GUI.DragWindow();

		}

		public static void ToggleByKey()
		{
			if (SceneExporterToolbarButton.IsButtonAdded)
			{
				if (Settings.IsGUIVisible) SceneExporterToolbarButton.Button.SetFalse(); else SceneExporterToolbarButton.Button.SetTrue();
			} else {
				Settings.IsGUIVisible = !Settings.IsGUIVisible;
			}
			Settings.IsReadingKeys = false;
		}
		public static void ToggleByButton()
		{
			Settings.IsGUIVisible = !Settings.IsGUIVisible;
			Settings.IsReadingKeys = false;
		}

		public static void Initialize()
		{
			strKeyRecord = Settings.KeyRecord.ToString();
			strDistanceMax = Settings.DistanceMax.ToString();
			strDirectory = Settings.ScenesDirectoryToOutput();
			isStrDistanceMaxOk = isStrDirectoryOk = true;
		}

		static void ColorizeFieldIsWrong(GUIStyle style, bool isOk)
		{
			style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = (isOk) ? Color.white : Color.red;
		}

		void Awake()
		{
			DontDestroyOnLoad(gameObject);
			Initialize();
		}

	}

	[KSPAddon(KSPAddon.Startup.EveryScene, false)]

	public class SceneExporterToolbarButton : MonoBehaviour
	{

		public static ApplicationLauncherButton Button;
		public static bool IsButtonAdded = false;

		void Start()
		{

			if (!IsButtonAdded && ApplicationLauncher.Instance != null)
			{
				var resources = new System.Resources.ResourceManager("SceneExporter.Resources", typeof(SceneExporterToolbarButton).Assembly);
				Byte[] bytes = resources.GetObject("ToolbarIcon") as Byte[];
				Texture2D buttonTexture = new Texture2D(38, 38, TextureFormat.ARGB32, false);
				buttonTexture.LoadRawTextureData(bytes);
				buttonTexture.Apply();
				bool isAlreadyVisible = Settings.IsGUIVisible;
				Settings.IsGUIVisible = false;
				Button = ApplicationLauncher.Instance.AddModApplication(SceneExporterGUI.ToggleByButton, SceneExporterGUI.ToggleByButton, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, buttonTexture);
				if (isAlreadyVisible) Button.SetTrue();
				IsButtonAdded = true;
			}
		//	Texture2D pngToTexture = GameDatabase.Instance.GetTexture("CameraTools/Textures/Untitled", false);
		//	Byte[] textureToBytes = pngToTexture.GetRawTextureData();
		//	File.WriteAllBytes("ToolbarIcon.txt", textureToBytes);

		}

	}

}
