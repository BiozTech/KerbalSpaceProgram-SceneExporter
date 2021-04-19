using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

		void Update()
		{
			if (Input.GetKeyDown(Settings.KeyRecord))
			{
				if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr))
				{
					SceneExporterGUI.ToggleByKey();
				} else {
					if (Settings.IsEnabled)
					{
						activeDirectory = Path.Combine(Settings.ScenesDirectory, DateTime.Now.ToString("yyMMdd-HHmmss"));
						//if (!Directory.Exists(activeDirectory)) Directory.CreateDirectory(activeDirectory);
						if (Settings.IsExportingTextures)
						{
							texturesDirectory = Path.Combine(activeDirectory, "textures");
							if (!Directory.Exists(texturesDirectory)) Directory.CreateDirectory(texturesDirectory);
						}
						DoExport();
					}
				}
			}
		}

		void DoExport()
		{

			sb.Clear();
			sbMaterials.Clear();
			materialsCache.Clear();
			vertexIndexOffset = gameObjectIndex = 0;

			sb.AppendLine("mtllib scene.mtl");

			foreach (GameObject go in FindObjectsOfType<GameObject>())
			{
				if (go.activeInHierarchy && go.scene.isLoaded && go.transform.position.magnitude < Settings.DistanceMax)
				{
					MeshFilter meshFilter = go.GetComponent<MeshFilter>();
					MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
					if (meshFilter != null && meshRenderer != null)
					{
						DoExportMesh(go, meshFilter.sharedMesh, meshRenderer);
						DoExportMaterials(go, meshRenderer);
					}
					foreach (SkinnedMeshRenderer skinnedMeshRenderer in go.GetComponents<SkinnedMeshRenderer>())
					{
						Mesh meshBaked = new Mesh();
						skinnedMeshRenderer.BakeMesh(meshBaked);
						DoExportMesh(go, meshBaked, skinnedMeshRenderer);
						DoExportMaterials(go, skinnedMeshRenderer);
					}
				}
			}

			//File.WriteAllText(Path.Combine(activeDirectory, "scene.obj"), sb.ToString());
			//File.WriteAllText(Path.Combine(activeDirectory, "scene.mtl"), sbMaterials.ToString());
			File.WriteAllText("scene.obj", sb.ToString());
			File.WriteAllText("scene.mtl", sbMaterials.ToString());

		}

		void DoExportMesh(GameObject go, Mesh mesh, Renderer renderer)
		{

			sb.AppendLine(String.Format("g {0}_{1}", ReplaceSpaces(go.name), gameObjectIndex++));

			foreach (Vector3 vertex in mesh.vertices)
			{
				//Vector3 v = go.transform.TransformPoint(vertex);
				Vector3 v = go.transform.position + go.transform.rotation * Vector3.Scale(vertex, go.transform.lossyScale);
				sb.AppendLine(String.Format("v {0} {1} {2}", -v.x, v.y, v.z));
			}
			foreach (Vector3 normal in mesh.normals)
			{
				Vector3 v = go.transform.TransformDirection(normal);
				sb.AppendLine(String.Format("vn {0} {1} {2}", -v.x, v.y, v.z));
			}
			foreach (Vector2 v in mesh.uv)
			{
				sb.AppendLine(String.Format("vt {0} {1}", v.x, v.y));
			}
			for (int i = mesh.uv.Length; i < mesh.vertices.Length; i++)
			{
				sb.AppendLine("vt 0 0"); // for meshes without (?) uv map
			}

			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				if (i < renderer.sharedMaterials.Length)
				{
					sb.AppendLine(String.Format("usemtl {0}", ReplaceSpaces(renderer.sharedMaterials[i].name)));
					sb.AppendLine(String.Format("usemap {0}", ReplaceSpaces(renderer.sharedMaterials[i].name)));
				} else {
					sb.AppendLine(String.Format("usemtl {0}_{1}", ReplaceSpaces(go.name), i));
					sb.AppendLine(String.Format("usemap {0}_{1}", ReplaceSpaces(go.name), i));
				}
				int[] triangles = mesh.GetTriangles(i);
				for (int t = 0; t < triangles.Length; t += 3)
				{
					sb.AppendLine(String.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", triangles[t] + 1 + vertexIndexOffset, triangles[t+1] + 1 + vertexIndexOffset, triangles[t+2] + 1 + vertexIndexOffset));
				}
			}
			vertexIndexOffset += mesh.vertices.Length;

		}

		void DoExportMaterials(GameObject go, Renderer renderer)
		{

			Material[] materials = renderer.sharedMaterials;
			foreach (Material material in materials)
			{

				string currentMaterialName = ReplaceSpaces(material.name);
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

				if (Settings.IsExportingTextures)
				{
					if (material.HasProperty(TextureName.MainTex) && material.GetTexture(TextureName.MainTex) != null)
					{
						sbMaterials.AppendLine(String.Format("map_Kd textures/{0}{1}.png", currentMaterialName, TextureName.MainTex));
					}
					if (material.HasProperty(TextureName.Emissive) && material.GetTexture(TextureName.Emissive) != null) // SpecGlossMap?
					{
						sbMaterials.AppendLine(String.Format("map_Ks textures/{0}{1}.png", currentMaterialName, TextureName.Emissive));
					}
					if (material.HasProperty(TextureName.BumpMap) && material.GetTexture(TextureName.BumpMap) != null)
					{
						sbMaterials.AppendLine(String.Format("map_Bump textures/{0}{1}.png", currentMaterialName, TextureName.BumpMap));
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

		Texture2D TextureToTexture2D(Texture texture)
		{
			Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
			RenderTexture renderTexture = new RenderTexture(texture.width, texture.height, 32);
			Graphics.Blit(texture, renderTexture);
			RenderTexture.active = renderTexture;
			texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
			texture2D.Apply();
			return texture2D;
		}

		string ReplaceSpaces(string str)
		{
			return str.Replace(" ", "_");
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
