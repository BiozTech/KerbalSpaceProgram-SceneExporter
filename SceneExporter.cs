using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SceneExporter
{

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class SceneExporter : MonoBehaviour
	{

		static string activeDirectory = Settings.ScenesDirectory;
		static string texturesDirectory = Path.Combine(activeDirectory, "textures");
		static StringBuilder sb = new StringBuilder();
		static StringBuilder sbMaterials = new StringBuilder();
		static HashSet<string> materialsCache = new HashSet<string>();
		static int vertexIndexOffset, gameObjectIndex;

		public static void Export()
		{
			SceneExporterGUI.ApplySettings();
			activeDirectory = Path.Combine(Settings.ScenesDirectory, DateTime.Now.ToString("yyMMdd-HHmmss"));
			if (!Directory.Exists(activeDirectory)) Directory.CreateDirectory(activeDirectory);
			if (Core.IsExportingTextures)
			{
				texturesDirectory = Path.Combine(activeDirectory, "textures");
				if (!Directory.Exists(texturesDirectory)) Directory.CreateDirectory(texturesDirectory);
			}
			Core.Log(String.Format("Starting exporting. Looking for gameobjects within {0} meters range.", Core.DistanceMax));
			DoExport();
			Core.Log(String.Format("{0} gameobjects exported.", gameObjectIndex));
		}

		static void DoExport()
		{

			sb.Clear();
			sbMaterials.Clear();
			materialsCache.Clear();
			vertexIndexOffset = gameObjectIndex = 0;

			sb.AppendLine("mtllib scene.mtl");

			foreach (GameObject go in FindObjectsOfType<GameObject>())
			{
				if (go.activeInHierarchy && go.scene.isLoaded && (go.transform.position - Camera.main.transform.position).magnitude < Core.DistanceMax)
				{
					foreach (MeshRenderer meshRenderer in go.GetComponents<MeshRenderer>())
					{
						try
						{
							DoExportMesh(meshRenderer);
							DoExportMaterials(meshRenderer);
						}
						catch (Exception stupid)
						{
							Core.LogException("mesh", go.name);
							Debug.LogException(stupid);
						}
					}
					foreach (SkinnedMeshRenderer skinnedMeshRenderer in go.GetComponents<SkinnedMeshRenderer>())
					{
						try
						{
							DoExportMesh(skinnedMeshRenderer);
							DoExportMaterials(skinnedMeshRenderer);
						}
						catch (Exception stupid)
						{
							Core.LogException("skinnedMesh", go.name);
							Debug.LogException(stupid);
						}
					}
				}
			}

			File.WriteAllText(Path.Combine(activeDirectory, "scene.obj"), sb.ToString());
			File.WriteAllText(Path.Combine(activeDirectory, "scene.mtl"), sbMaterials.ToString());

		}

		static void DoExportMesh(Renderer renderer)
		{

			GameObject go = renderer.gameObject;
			Vector3 actualScale;
			Vector2 uvOffset;
			Mesh mesh = new Mesh();
			SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
			if (skinnedMeshRenderer != null)
			{
				skinnedMeshRenderer.BakeMesh(mesh);
				actualScale = go.transform.localScale;
				uvOffset = new Vector2(-0.5f, -0.5f);
			} else {
				mesh = go.GetComponent<MeshFilter>().sharedMesh;
				actualScale = go.transform.lossyScale;
				uvOffset = Vector2.zero;
			}
			if (mesh.bounds.size.magnitude > Core.DistanceMax * 2f)
			{
				return;
			}
			Quaternion rotationToActiveVessel = (FlightGlobals.ActiveVessel != null) ? Quaternion.Inverse(FlightGlobals.ActiveVessel.transform.rotation) : Quaternion.identity;

			sb.AppendLine(String.Format("g {0}_{1}", NameWritableToFile(go.name), gameObjectIndex++));

			foreach (Vector3 vertex in mesh.vertices)
			{
				Vector3 v = rotationToActiveVessel * (go.transform.position + go.transform.rotation * Vector3.Scale(vertex, actualScale));
				sb.AppendLine(String.Format("v {0} {1} {2}", -v.x, v.y, v.z));
			}
			foreach (Vector3 normal in mesh.normals)
			{
				Vector3 v = go.transform.TransformDirection(normal);
				sb.AppendLine(String.Format("vn {0} {1} {2}", -v.x, v.y, v.z));
			}
			foreach (Vector2 v in mesh.uv)
			{
				sb.AppendLine(String.Format("vt {0} {1}", v.x + uvOffset.x, v.y + uvOffset.y));
			}
			for (int i = mesh.uv.Length; i < mesh.vertices.Length; i++)
			{
				sb.AppendLine("vt 0 0"); // for meshes without (?) uv map
			}

			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				if (i < renderer.sharedMaterials.Length)
				{
					sb.AppendLine(String.Format("usemtl {0}", NameWritableToFile(renderer.sharedMaterials[i].name)));
					sb.AppendLine(String.Format("usemap {0}", NameWritableToFile(renderer.sharedMaterials[i].name)));
				} else {
					sb.AppendLine(String.Format("usemtl {0}_{1}", NameWritableToFile(go.name), i));
					sb.AppendLine(String.Format("usemap {0}_{1}", NameWritableToFile(go.name), i));
				}
				int[] triangles = mesh.GetTriangles(i);
				for (int t = 0; t < triangles.Length; t += 3)
				{
					sb.AppendLine(String.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", triangles[t] + 1 + vertexIndexOffset, triangles[t+1] + 1 + vertexIndexOffset, triangles[t+2] + 1 + vertexIndexOffset));
				}
			}
			vertexIndexOffset += mesh.vertices.Length;

		}

		static void DoExportMaterials(Renderer renderer)
		{

			string uvOffset = (renderer is SkinnedMeshRenderer) ? " -o 0.5 0.5 0.5" : string.Empty;

			Material[] materials = renderer.sharedMaterials;
			foreach (Material material in materials)
			{

				string currentMaterialName = NameWritableToFile(material.name);
				sbMaterials.AppendLine("newmtl " + currentMaterialName);
				if (material.HasProperty("_Color"))
				{
					sbMaterials.AppendLine(String.Format("Kd {0} {1} {2}", material.color.r, material.color.g, material.color.b));
					sbMaterials.AppendLine(String.Format("Tr {0}", 1f - material.color.a));
				}
				if (material.HasProperty("_SpecColor"))
				{
					Color specColor = material.GetColor("_SpecColor");
					sbMaterials.AppendLine(String.Format("Ks {0} {1} {2}", specColor.r, specColor.g, specColor.b));
				}

				if (Core.IsExportingTextures)
				{
					if (material.HasProperty(TextureName.MainTex) && material.GetTexture(TextureName.MainTex) != null)
					{
						sbMaterials.AppendLine(String.Format("map_Kd{2} textures/{0}{1}.png", currentMaterialName, TextureName.MainTex, uvOffset));
					}
					if (material.HasProperty(TextureName.Emissive) && material.GetTexture(TextureName.Emissive) != null)
					{
						sbMaterials.AppendLine(String.Format("map_Ks{2} textures/{0}{1}.png", currentMaterialName, TextureName.Emissive, uvOffset));
					}
					if (material.HasProperty(TextureName.BumpMap) && material.GetTexture(TextureName.BumpMap) != null)
					{
						sbMaterials.AppendLine(String.Format("map_Bump{2} textures/{0}{1}.png", currentMaterialName, TextureName.BumpMap, uvOffset));
					}

					if (!materialsCache.Contains(material.name))
					{
						materialsCache.Add(material.name);
						string[] allTextureNames = material.GetTexturePropertyNames();
						foreach (string textureName in allTextureNames)
						{
							Texture texture = material.GetTexture(textureName);
							if (texture != null)
							{
								File.WriteAllBytes(Path.Combine(texturesDirectory, String.Format("{0}{1}.png", currentMaterialName, textureName)), TextureToTexture2D(texture).EncodeToPNG());
							}
						}
					}
				}

				sbMaterials.AppendLine("illum 2");

			}

		}

		static Texture2D TextureToTexture2D(Texture texture)
		{
			Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
			RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 32);
			Graphics.Blit(texture, renderTexture);
			RenderTexture.active = renderTexture;
			texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			texture2D.Apply();
			return texture2D;
		}

		static string NameWritableToFile(string name)
		{
			return Regex.Replace(name, @"[ /\\]", "_");
		}

		void Awake()
		{
			DontDestroyOnLoad(gameObject);
			Settings.Load();
		}

		static class TextureName
		{
			public static string MainTex
			{
				get { return "_MainTex"; }
			}
			public static string Emissive
			{
				get { return "_Emissive"; }
			}
			public static string BumpMap
			{
				get { return "_BumpMap"; }
			}
		}

	}

}
