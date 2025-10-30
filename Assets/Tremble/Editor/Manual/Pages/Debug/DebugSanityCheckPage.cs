//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Debug/Sanity Check")]
	public class DebugSanityCheckPage : ManualPageBase
	{
		private PrefabNameLookup m_PrefabNameLookup;
		private MapTypeLookup m_MapTypeLookup;

		private readonly Dictionary<Type, MonoScript> m_TypeToMonoScript = new();

		private int m_SelectedTabIdx = 0;
		private readonly string[] m_TabNames = new[] { "Basics", "Entities" };

		protected override void OnInit()
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			m_PrefabNameLookup = new(syncSettings);
			m_MapTypeLookup = new(syncSettings);
		}

		protected override void OnGUI()
		{
			Tabs(ref m_SelectedTabIdx, m_TabNames);

			switch (m_SelectedTabIdx)
			{
				case 0:
					OnBasicsGUI();
					break;
				case 1:
					OnEntitiesGUI();
					break;
			}
		}

		private void OnBasicsGUI()
		{
			Text("This page will verify that Tremble is installed and working correctly!");

			// TB?
			bool trenchBroomInstalled = !TrenchBroomUtil.TrenchBroomPath.IsNullOrEmpty();
			CheckListItem("TrenchBroom installed and detected?", trenchBroomInstalled);

			if (!trenchBroomInstalled)
			{
				Callout("If TrenchBroom is installed, but not detected here, open it",
					"and Tremble should detect its location!");
			}

			// Installed game into TB?
			bool gameFolderExists = Directory.Exists(TrenchBroomUtil.GetGameFolder());
			CheckListItem("TrenchBroom game folder exists?", gameFolderExists, "Sync to create it!", TrembleEditorAPI.SyncToTrenchBroom);

			bool gameInstalledInTBPrefs = TrenchBroomUtil.DoesEnginePathExistInTrenchBroomPrefs();
			CheckListItem("TrenchBroom knows game location?", gameInstalledInTBPrefs, "Sync to create it!", TrembleEditorAPI.SyncToTrenchBroom);

			// BaseQ3 folder
			bool baseQ3FolderExists = Directory.Exists(TrembleConsts.BASEQ3_PATH);
			CheckListItem("BaseQ3 folder exists in Library?", baseQ3FolderExists, "Sync to create it!", TrembleEditorAPI.SyncToTrenchBroom);

			// DLL loaded?
			bool dllIsLoaded = Q3Map2Dll.IsLoaded;
			CheckListItem("Q3Map2 DLL loaded successfully?", dllIsLoaded);
		}

		private void OnEntitiesGUI()
		{
			Foldout("Prefab Entities", () =>
			{
				Text("The following are the prefabs that Tremble knows about:");

				EditorGUI.BeginDisabledGroup(true);
				{
					foreach (string prefabPath in m_PrefabNameLookup.AllPrefabPaths)
					{
						GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

						m_PrefabNameLookup.TryGetMapNameFromPrefabPath(prefabPath, out string mapName);

						GUILayout.BeginHorizontal();
						{
							EditorGUILayout.PrefixLabel(mapName);
							EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
						}
						GUILayout.EndHorizontal();
					}
				}
				EditorGUI.EndDisabledGroup();
			});

			Foldout("Point Entities", () =>
			{
				Text("The following are the [PointEntity] MonoBehaviours that Tremble knows about:");

				bool wasAnyPresent = false;

				foreach (Type type in m_MapTypeLookup.AllTypes)
				{
					if (!m_MapTypeLookup.TryGetMapNameFromClass(type, out string mapName))
					{
						Text($"Invalid: '{type.Name}' - no map name known!");
						continue;
					}

					if (!m_MapTypeLookup.TryGetEntityTypeFromMapName(mapName, out EntityType entityType) || entityType != EntityType.Point)
						continue;

					DisabledTypeField(type, mapName);
					wasAnyPresent = true;
				}

				if (!wasAnyPresent)
				{
					Text("There are none (which might be fine)!");
				}
			});

			Foldout("Brush Entities", () =>
			{
				Text("The following are the [BrushEntity] MonoBehaviours that Tremble knows about:");
				bool wasAnyPresent = false;

				foreach (Type type in m_MapTypeLookup.AllTypes)
				{
					if (!m_MapTypeLookup.TryGetMapNameFromClass(type, out string mapName))
					{
						Text($"Invalid: '{type.Name}' - no map name known!");
						continue;
					}

					if (!m_MapTypeLookup.TryGetEntityTypeFromMapName(mapName, out EntityType entityType) || entityType != EntityType.Brush)
						continue;

					DisabledTypeField(type, mapName);
					wasAnyPresent = true;
				}

				if (!wasAnyPresent)
				{
					Text("There are none (which might be fine)!");
				}
			});
		}

		private void DisabledTypeField(Type type, string label)
		{
			EditorGUI.BeginDisabledGroup(true);
			{
				if (!m_TypeToMonoScript.TryGetValue(type, out MonoScript monoScript))
				{
					GameObject go = new("Tester");
					MonoBehaviour mb = (MonoBehaviour)go.AddComponent(type);
					monoScript = MonoScript.FromMonoBehaviour(mb);
					m_TypeToMonoScript[type] = monoScript;

					GameObject.DestroyImmediate(go);
				}

				GUILayout.BeginHorizontal();
				{
					EditorGUILayout.PrefixLabel(label);
					EditorGUILayout.ObjectField(monoScript, typeof(MonoScript), false);
				}
				GUILayout.EndHorizontal();
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}