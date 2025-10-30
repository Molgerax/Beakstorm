//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[CustomEditor(typeof(WadFileAssetImporter))]
	public class WadAssetImporterEditor : UnityEditor.Editor
	{
		private Vector2 m_ScrollPos = Vector2.zero;
		private bool m_Running = false;

		public override void OnInspectorGUI()
		{
			if (target is not WadFileAssetImporter wadAssetImporter)
				return;

			WadFileAsset wadAsset = AssetDatabase.LoadAssetAtPath<WadFileAsset>(wadAssetImporter.assetPath);

			GUILayout.Label($"{wadAsset.MaterialNames.Length} material(s):");

			GUI.color = new Color(0.8f, 0.8f, 0.8f);
			{
				m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, GUILayout.Height(100f));
				{
					foreach (string material in wadAsset.MaterialNames)
					{
						GUILayout.BeginHorizontal();
						{
							GUIContent materialIcon = EditorGUIUtility.IconContent("Material Icon");
							GUILayout.Label(materialIcon, GUILayout.Width(16f), GUILayout.Height(16f));
							GUILayout.Label(material);
						}
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndScrollView();
			}
			GUI.color = Color.white;

			GUI.enabled = !m_Running;
			if (GUILayout.Button("Extract Wad Materials"))
			{
				m_Running = true;
				Extract(wadAssetImporter.assetPath);
				m_Running = false;
			}
		}

		public static void Extract(string wadFilePath)
		{
			WadParser parser = new(wadFilePath);
			WadFile wadFile = parser.Parse();

			// Create folder
			string folderPath = Path.ChangeExtension(wadFilePath, null);
			string texturesPath = Path.Combine(folderPath, "Textures");
			string materialsPath = Path.Combine(folderPath, "Materials");
			DirectoryUtil.EmptyAndCreateDirectory(texturesPath);
			DirectoryUtil.EmptyAndCreateDirectory(materialsPath);

			// Step 1: write textures
			try
			{
				AssetDatabase.StartAssetEditing();
				EditorUtility.DisplayProgressBar("Extracting Wad Textures", "One mo..", 0f);

				for (int entryIdx = 0; entryIdx < wadFile.Entries.Count; entryIdx++)
				{
					WadEntry entry = wadFile.Entries[entryIdx];

					float progress = (float)entryIdx / wadFile.Entries.Count;
					EditorUtility.DisplayProgressBar("Extracting Wad Textures", $"{entry.Name} ({entryIdx + 1} of {wadFile.Entries.Count}) - {progress * 100:F0}%", progress);

					string texturePath = Path.Combine(texturesPath, $"T_{entry.Name}.png");
					parser.WriteTextureToFile(entry, texturePath);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			EditorApplication.delayCall += () => CreateMaterials(materialsPath, wadFilePath, wadFile);
		}

		private static void CreateMaterials(string materialFolderPath, string wadFilePath, WadFile wadFile)
		{
			List<Material> createdMaterials = new(wadFile.Entries.Count);

			// Step 2: Create materials
			try
			{
				AssetDatabase.StartAssetEditing();
				EditorUtility.DisplayProgressBar("Creating Materials", "One mo..", 0f);

				string texturesPath = Path.Combine(Path.GetDirectoryName(materialFolderPath), "Textures");

				for (int entryIdx = 0; entryIdx < wadFile.Entries.Count; entryIdx++)
				{
					WadEntry entry = wadFile.Entries[entryIdx];

					float progress = (float)entryIdx / wadFile.Entries.Count;
					EditorUtility.DisplayProgressBar("Creating Materials", $"{entry.Name} ({entryIdx + 1} of {wadFile.Entries.Count}) - {progress * 100:F0}%", progress);

					string texturePath = Path.Combine(texturesPath, $"T_{entry.Name}.png");
					string materialPath = Path.Combine(materialFolderPath, $"{entry.Name}.mat");

					Material mat = TrembleMapImport.CreateEditorMaterial(texturePath, smoothness: 0.1f, metallic: 0f);
					AssetDatabase.CreateAsset(mat, materialPath);

					createdMaterials.Add(mat);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
			}

			// Add to material group, if project is using them
			if (TrembleSyncSettings.Get().MaterialGroups.Length > 0)
			{
				string wadName = Path.GetFileNameWithoutExtension(wadFilePath);
				MaterialGroupUtil.MoveMaterialsToGroup(createdMaterials.ToArray(), wadName, createIfNotExist: true);
			}

			TrembleEditorAPI.SyncToTrenchBroom();
		}
	}
}