// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if HDRP_INSTALLED
using File = System.IO.File;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Camera/lighting setup for taking material screenshots. Do not use directly.
	/// </summary>
	public class MapTextureSnapshotter
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private GameObject m_SceneRoot;
		private Camera m_Camera;
		private MeshRenderer m_Renderer;

		private readonly Dictionary<Vector2Int, TexturePair> m_RenderTextures = new();
		private Texture2D m_CaptureTexture;
		private Light m_StudioLight;

		private AmbientMode m_OriginalAmbientMode;
		private Color m_OriginalAmbientColour;
		private float m_OriginalAmbientIntensity;

		private readonly List<GameObject> m_Lights = new();

		private TrembleSyncSettings m_Settings;

		private struct TexturePair
		{
			public Texture2D Texture;
			public Texture2D TextureSrgb;
			public RenderTexture RenderTexture;
			public RenderTexture RenderTextureSrgb;
		}

		public void Init(TrembleSyncSettings settings)
		{
			m_Settings = settings;

			SetupGameObjects(m_Settings.MaterialCaptureLightIntensity, m_Settings.MaterialCaptureLightAngle);

			List<Vector2Int> resolutions = new()
			{
				m_Settings.MaterialExportSize.ToVector2IntSize()
			};

			foreach (Material material in m_Settings.GetMaterialGroupsOrDefault().SelectMany(mg => mg.Materials))
			{
				MaterialRenderData renderData = m_Settings.GetMaterialRenderData(material);
				Vector2Int dataRes = new(renderData.ResolutionX.ToIntSize(), renderData.ResolutionY.ToIntSize());

				if (!resolutions.Contains(dataRes))
					resolutions.Add(dataRes);

				if (!material.TryGetMainTex(out Texture2D tex))
					continue;

				Vector2Int res = new(tex.width, tex.height);
				if (!resolutions.Contains(res))
					resolutions.Add(res);
			}

			for (int i = 0; i < 10; i++)
			{
				int power = 1 << i;
				Vector2Int r = Vector2Int.one * power; 
				if (!resolutions.Contains(r))
					resolutions.Add(r);
			}

			foreach (Vector2Int res in resolutions)
			{
				RenderTexture rt = new(res.x, res.y, 8, GraphicsFormat.R8G8B8A8_UNorm);
				rt.Create();
				RenderTexture rtSrgb = new(res.x, res.y, 8, GraphicsFormat.R8G8B8A8_SRGB);
				rtSrgb.Create();
				Texture2D output = new(res.x, res.y, rt.graphicsFormat, TextureCreationFlags.None);
				Texture2D outputSrgb = new(res.x, res.y, rtSrgb.graphicsFormat, TextureCreationFlags.None);
				m_RenderTextures.Add(res, new()
				{
					Texture = output,
					TextureSrgb = outputSrgb,
					RenderTexture = rtSrgb,
					RenderTextureSrgb = rtSrgb
				});
			}

			// Move far away!
			m_SceneRoot.transform.position = Vector3.up * 10000f;

			// Set up lighting
			m_OriginalAmbientMode = RenderSettings.ambientMode;
			m_OriginalAmbientColour = RenderSettings.ambientLight;
			m_OriginalAmbientIntensity = RenderSettings.ambientIntensity;

#if UNITY_2022_1_OR_NEWER
			foreach (Light foundLight in GameObject.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
#else
			foreach (Light foundLight in GameObject.FindObjectsOfType<Light>(false))
#endif
			{
				if (!foundLight.gameObject.activeSelf || foundLight == m_StudioLight)
					continue;

				m_Lights.Add(foundLight.gameObject);
				foundLight.gameObject.SetActive(false);
			}

			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = Color.white;
		}

		public void DeInit()
		{
			GameObject.DestroyImmediate(m_SceneRoot);

			if (m_RenderTextures.Values.Any(x => RenderTexture.active == x.RenderTexture))
			{
				RenderTexture.active = null;
			}

			// Release RT next frame - it seems if it's active above, it's not
			// cleared/unset until next frame causing an error (:
			foreach (TexturePair texPair in m_RenderTextures.Values)
			{
#if UNITY_EDITOR
				EditorApplication.delayCall += () =>
				{
					if (texPair.RenderTexture || texPair.RenderTextureSrgb)
					{
#endif
						texPair.RenderTexture.Release();
						texPair.RenderTextureSrgb.Release();
#if UNITY_EDITOR
					}
				};
#endif
			}

			foreach (GameObject foundLight in m_Lights)
			{
				foundLight.SetActive(true);
			}
			m_Lights.Clear();

			// Restore lighting
			RenderSettings.ambientMode = m_OriginalAmbientMode;
			RenderSettings.ambientLight = m_OriginalAmbientColour;
			RenderSettings.ambientIntensity = m_OriginalAmbientIntensity;
		}

		public void SnapshotMaterial(Material m, string outPath)
		{
			MaterialRenderData renderData = m_Settings.GetMaterialRenderData(m);

			Vector2Int res = renderData.GetResolutionOrDefault();

			Texture2D mainTex = null;
			if (renderData.ExportMode == MaterialImportMode.MainTex)
			{
				if (!m.TryGetMainTex(out mainTex))
				{
					renderData.ExportMode = MaterialImportMode.Legacy;
					Debug.LogWarning($"Couldn't find a Main Texture for material \"{m.name}\"," +
					                 "falling back to legacy render mode." +
					                 "Make sure the shader has a texture marked as [Main Texture] or" +
					                 "a texture named _MainTex, _BaseColor, or _Color.");
				}

				if (mainTex && !renderData.IsResolutionOverridden)
				{
					res = new(mainTex.width, mainTex.height);
				}
			}

			TexturePair tex = m_RenderTextures[res];

			switch (renderData.ExportMode)
			{
				case MaterialImportMode.Legacy:
					// Set up RT and material - we have to save it for HDRP
					bool createdTempMaterial = TryCreateUnlitCopy(m, out Material tempMaterial, renderData.ExportMode);

#if HDRP_INSTALLED && UNITY_EDITOR
					if (createdTempMaterial)
					{
						string tempName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(m)) + "_TrembleTemp.mat";
						string tempPath = Path.Combine("Assets", tempName);
						AssetDatabase.CreateAsset(tempMaterial, tempPath);
					}
#endif

					m_Renderer.material = tempMaterial ? tempMaterial : m;

					// Render once
					m_Camera.targetTexture = tex.RenderTexture;
					m_Camera.Render();

					if (RenderTexture.active != tex.RenderTexture)
						RenderTexture.active = tex.RenderTexture;

					// Copy texture from RT
					tex.Texture.ReadPixels(new(0, 0, res.x, res.y), 0, 0);
					File.WriteAllBytes(outPath, tex.Texture.EncodeToPNG());

					break;
				case MaterialImportMode.MainTex:
#if UNITY_EDITOR
					string path = AssetDatabase.GetAssetPath(mainTex);

					//TODO(jwf): colourise by main colour?

					// Is it a PNG, and we're not overriding resolution? Just copy it directly
					if (!renderData.IsResolutionOverridden && path.EndsWithInvariant(".png"))
					{
						File.Copy(Application.dataPath + path.Remove(0, 6), outPath, true);
						return;
					}
#endif

					Material blitMat = new Material(Shader.Find("Hidden/BlitWithMainColor"));
					blitMat.color = m.color;

					// Else we make a copy to resize
					m_Camera.targetTexture = null;
					GL.sRGBWrite = true;
					Graphics.Blit(mainTex, tex.RenderTextureSrgb, blitMat);

					Object.DestroyImmediate(blitMat);
					
					RenderTexture.active = tex.RenderTextureSrgb;

					// Copy texture from RT
					tex.TextureSrgb.ReadPixels(new(0, 0, res.x, res.y), 0, 0);
					File.WriteAllBytes(outPath, tex.TextureSrgb.EncodeToPNG());
					break;
			}

			// Delete temp material
#if HDRP_INSTALLED
			if (createdTempMaterial)
			{
			  AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tempMaterial));
			}
#endif

		}

		private static readonly Dictionary<string, string> s_ShaderRemaps = new()
		{
			{ "Standard", "Legacy Shaders/Diffuse" }, // or Particles/Standard Unlit?
			{ "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit" },
			{ "HDRP/Lit", "HDRP/Unlit" }
		};

		private bool TryCreateUnlitCopy(Material material, out Material newMaterial, MaterialImportMode mode)
		{

			// No lights in the scene. Assume we don't need to tweak materials as they're using ambient colour
			if (mode is MaterialImportMode.MainTex || (m_Lights.Count == 0 && mode is MaterialImportMode.Legacy))
			{
				newMaterial = material;
				return false;
			}

#if UNITY_EDITOR
			foreach ((string originalShader, string remapShader) in s_ShaderRemaps)
			{
				if (!material.shader.name.Equals(originalShader))
					continue;

				newMaterial = mode is MaterialImportMode.Legacy ? new(Shader.Find(remapShader)) : new(Shader.Find(originalShader));
				CopyMaterialProperties_Recursive(material, newMaterial);

				// Ensure colours/textures set properly
				newMaterial.color = material.color;
				newMaterial.mainTexture = material.mainTexture;

				// Render after post-processing (mostly for HDRP)
				newMaterial.renderQueue = 2501;

				return true;
			}
#endif

			newMaterial = material;
			return false;
		}

		[Conditional("UNITY_EDITOR")]
		private static void CopyMaterialProperties_Recursive(Material from, Material to)
		{
#if UNITY_EDITOR

#if UNITY_2022_1_OR_NEWER
			if (from.parent)
			{
				// If parent, copy that one first!
				CopyMaterialProperties_Recursive(from.parent, to);
			}
#endif

			// Copy shader keywords
			to.shaderKeywords = from.shaderKeywords;

			// Iterate over all properties of the material
			Shader shader = from.shader;
			int propertyCount = shader.GetPropertyCount();

			for (int i = 0; i < propertyCount; i++)
			{
				// Get the property type and name
				UnityEngine.Rendering.ShaderPropertyType propertyType = shader.GetPropertyType(i);
				string propertyName = shader.GetPropertyName(i);

				// Copy the property value based on its type
				switch (propertyType)
				{
					case UnityEngine.Rendering.ShaderPropertyType.Color:
						to.SetColor(propertyName, from.GetColor(propertyName));
						break;
					case UnityEngine.Rendering.ShaderPropertyType.Vector:
						to.SetVector(propertyName, from.GetVector(propertyName));
						break;
					case UnityEngine.Rendering.ShaderPropertyType.Float:
					case UnityEngine.Rendering.ShaderPropertyType.Range:
						to.SetFloat(propertyName, from.GetFloat(propertyName));
						break;
					case UnityEngine.Rendering.ShaderPropertyType.Texture:
						to.SetTexture(propertyName, from.GetTexture(propertyName));
						to.SetTextureOffset(propertyName, from.GetTextureOffset(propertyName));
						to.SetTextureScale(propertyName, from.GetTextureScale(propertyName));
						break;
				}
			}

			// Copy render queue and other settings
			to.renderQueue = from.renderQueue;
			to.enableInstancing = from.enableInstancing;
			to.doubleSidedGI = from.doubleSidedGI;
#endif
		}


		private void SetupGameObjects(float lightIntensity, float lightAngle)
		{
			m_SceneRoot = new("Scene Root");

			// Set up gameobjects
			GameObject snapCamera = new("Camera");
			snapCamera.transform.SetParent(m_SceneRoot.transform, false);
			snapCamera.transform.localPosition = new(0f, 0f, -0.1f);

			m_Camera = snapCamera.AddComponent<Camera>();
			m_Camera.orthographic = true;
			m_Camera.allowHDR = false;
			m_Camera.orthographicSize = 0.5f;
			m_Camera.nearClipPlane = 0.01f;
			m_Camera.farClipPlane = 0.5f;
			m_Camera.backgroundColor = Color.black;
			m_Camera.clearFlags = CameraClearFlags.Color;

			Vector3 lightDirection = Vector3.Lerp(Vector3.right, Vector3.forward, lightAngle);

			GameObject snapLight = new("Light");
			snapLight.transform.SetParent(m_SceneRoot.transform, false);
			snapLight.transform.localPosition = new(0f, 0f, -0.1f);
			snapLight.transform.rotation = Quaternion.LookRotation(lightDirection);
			m_StudioLight = snapLight.AddComponent<Light>();
			m_StudioLight.type = LightType.Directional;
			m_StudioLight.intensity = lightIntensity;
			m_StudioLight.shadows = LightShadows.None;
			m_StudioLight.color = Color.white;

			GameObject snapQuad = new("Quad");
			snapQuad.transform.SetParent(m_SceneRoot.transform, false);
			snapQuad.transform.localPosition = Vector3.zero;

			MeshFilter meshFilter = snapQuad.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = CreateQuadMesh();

			m_Renderer = snapQuad.AddComponent<MeshRenderer>();
		}

		private Mesh CreateQuadMesh()
		{
			Mesh quadMesh = new()
			{
				name = "Quad",
				vertices = new Vector3[]
				{
					new(-0.5f, -0.5f, 0f),
					new(-0.5f, +0.5f, 0f),
					new(+0.5f, +0.5f, 0f),
					new(+0.5f, -0.5f, 0f),
				},
				normals = new Vector3[]
				{
					new(0f, 0f, -1f),
					new(0f, 0f, -1f),
					new(0f, 0f, -1f),
					new(0f, 0f, -1f),
				},
				uv = new Vector2[]
				{
					new(0f, 0f),
					new(0f, 1f),
					new(1f, 1f),
					new(1f, 0f),
				},
			};

			List<int> indices = new() { 0, 1, 2, 0, 2, 3 };
			quadMesh.SetIndices(indices, MeshTopology.Triangles, 0);
			return quadMesh;
		}
	}
}