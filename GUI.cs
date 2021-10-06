﻿using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace SceneExporter
{

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class SceneExporterGUI : MonoBehaviour
	{

		static string strDistanceMax, strDirectory;
		static bool isStrDistanceMaxOk, isStrDirectoryOk;

		static bool wasGUIVisible = Core.IsGUIVisible;
		static Rect windowPosition = new Rect(UnityEngine.Random.Range(0.23f, 0.27f) * Screen.width, UnityEngine.Random.Range(0.13f, 0.17f) * Screen.height, 0, 0);

		void OnGUI()
		{
			if (Core.IsGUIVisible)
			{
				GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
				windowStyle.padding = new RectOffset(15, 15, 20, 11);
				windowPosition = GUILayout.Window(("SceneExporterGUI").GetHashCode(), windowPosition, OnWindow, "SceneExporter", windowStyle);
			}
		}

		void OnWindow(int windowId)
		{

			GUILayoutOption[] textFieldLayoutOptions = { GUILayout.Width(200f), GUILayout.Height(20f) };
			GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
			GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.alignment = TextAnchor.MiddleLeft;
			textFieldStyle.padding = boxStyle.padding = new RectOffset(4, 4, 3, 3);
			GUIStyle buttonExportStyle = new GUIStyle(GUI.skin.button);
			buttonExportStyle.margin = new RectOffset(74, 4, 4, 9);
			GUIStyle buttonApplyStyle = new GUIStyle(GUI.skin.button);
			buttonApplyStyle.margin = new RectOffset(92, 4, 13, 4);

			GUILayout.BeginVertical(GUILayout.Width(300f));

			GUILayout.Space(10f);
			if (GUILayout.Button("Export", buttonExportStyle, GUILayout.Width(160f), GUILayout.Height(30f)))
			{
				SceneExporter.Export();
			}

			GUILayout.BeginHorizontal();
			GUILayout.Label("Export textures");
			if (Core.IsExportingTextures != GUILayout.Toggle(Core.IsExportingTextures, Core.IsExportingTextures ? " Enabled" : " Disabled", textFieldLayoutOptions))
			{
				Core.IsExportingTextures = !Core.IsExportingTextures;
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
				ApplySettings();
			}

			GUILayout.EndVertical();

			GUI.DragWindow();

		}

		public static void ApplySettings()
		{

			bool isOk = true;

			float distance;
			isStrDistanceMaxOk = float.TryParse(strDistanceMax.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out distance);
			if (isStrDistanceMaxOk)
			{
				Core.DistanceMax = distance;
				strDistanceMax = Core.DistanceMax.ToString();
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

		static void ColorizeFieldIsWrong(GUIStyle style, bool isOk)
		{
			style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = (isOk) ? Color.white : Color.red;
		}

		public static void ToggleByButton()
		{
			Core.IsGUIVisible = !Core.IsGUIVisible;
		}
		void OnShowUI()
		{
			if (wasGUIVisible) ToggleByButton();
			wasGUIVisible = false;
		}
		void OnHideUI()
		{
			wasGUIVisible = Core.IsGUIVisible;
			if (wasGUIVisible) ToggleByButton();
		}

		public static void Initialize()
		{
			strDistanceMax = Core.DistanceMax.ToString();
			strDirectory = Settings.ScenesDirectoryToOutput();
			isStrDistanceMaxOk = isStrDirectoryOk = true;
		}

		void Awake()
		{
			DontDestroyOnLoad(gameObject);
			GameEvents.onShowUI.Add(OnShowUI);
			GameEvents.onHideUI.Add(OnHideUI);
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
				Button = ApplicationLauncher.Instance.AddModApplication(SceneExporterGUI.ToggleByButton, SceneExporterGUI.ToggleByButton, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, buttonTexture);
				IsButtonAdded = true;
			}

		}

	}

}
