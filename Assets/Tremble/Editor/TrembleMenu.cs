//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyGoose.Tremble.Editor
{
	public static class TrembleMenu
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Main options
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Open Settings...", priority = +20)]
		public static void OpenSettings() => TrembleEditorAPI.OpenSettings();

#if UNITY_2022_1_OR_NEWER
		[MenuItem("Tools/Tremble/Show Sync Toolbar")]
		private static void ShowToolbar()
		{
			SceneView.AddOverlayToActiveView(new TrembleSyncOverlay());
		}
#endif

		[MenuItem("Tools/Tremble/Open Manual...", priority = +20)]
		private static void ShowManual() => TrembleEditorAPI.OpenManual();

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Import
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Re-Import Current Map", priority = -10)]
		private static void DoMenu_PerformCurrentMapReimport() => PerformCurrentMapReimport();


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Sync
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Sync Project to TrenchBroom", priority = -12)]
		private static void DoMenu_PerformQuickSync() => TrembleEditorAPI.SyncToTrenchBroom();

		[MenuItem("Tools/Tremble/Sync Project to TrenchBroom (Clean, Slow)", priority = -11)]
		private static void DoMenu_PerformFullSync()
		{
			TrembleEditorAPI.InvalidateMaterialAndPrefabCache(silent: true);
			TrembleEditorAPI.SyncToTrenchBroom();
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Advanced
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Advanced/Re-Import ALL Maps", priority = 9)]
		private static void DoMenu_PerformAllMapsReimport() => PerformAllMapsReimport();

		[MenuItem("Tools/Tremble/Advanced/Delete all autosaves", priority = 9)]
		private static void DoMenu_DeleteAutosaves() => DeleteAutosaves();

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Debug
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Advanced/Debug/Open TB Game folder", priority = 9)]
		private static void DoMenu_OpenGameFolder() => EditorUtility.OpenWithDefaultApp(TrenchBroomUtil.GetGameFolder());

		[MenuItem("Tools/Tremble/Advanced/Debug/Open Project baseq3 folder", priority = 9)]
		private static void DoMenu_OpenBaseQ3() => EditorUtility.OpenWithDefaultApp(TrembleConsts.BASEQ3_PATH);

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Helpers
		// -----------------------------------------------------------------------------------------------------------------------------
		// Support for syncing after compile
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void MaybePerformSyncAfterCompile()
		{
			// Because this is called when the Library folder is recreated (but before
			// AssetDatabase is truly ready!), we need to be careful accessing the Sync
			// settings. This fixes the bug where your material groups get lost, which
			// was because a new Sync settings asset was being created!
			TrembleSyncSettings tss = TrembleSyncSettings.Get(createIfNotExists: false);
			if (!tss || !tss.AutoSyncOnCompile)
				return;

			EditorApplication.delayCall += TrembleEditorAPI.SyncToTrenchBroom;
		}

		internal static void PerformCurrentMapReimport()
		{
			for (int sceneIdx = 0; sceneIdx < EditorSceneManager.sceneCount; sceneIdx++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(sceneIdx);

				foreach (GameObject go in scene.GetRootGameObjects())
				{
					PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(go);

					if (prefabType == PrefabAssetType.Model)
					{
						// Model intact, reimport
						string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
						if (path.IsNullOrEmpty())
							continue;

						if (!AssetDatabaseUtil.IsTrembleMapFile(path))
							continue;

						AssetDatabase.ImportAsset(path);
					}
					else if (prefabType == PrefabAssetType.MissingAsset)
					{
						// Broken (failed import before) - try to find one
						string path = TrembleAssetLoader.FindAssetPath<UnityEngine.Object>(go.name);

						if (path == null || !AssetDatabaseUtil.IsTrembleMapFile(path))
							continue;

						AssetDatabase.ImportAsset(path);
					}
				}
			}
		}

		
		internal static void PerformAllMapsReimport(bool silent = false)
		{
			List<string> mapsToReimport = AssetDatabaseUtil.GetAllTrembleMapPaths();

			if (!Application.isBatchMode && !silent && !EditorUtility.DisplayDialog("Map Reimport", $"Are you sure you want to reimport {mapsToReimport.Count} maps?", "Sure!", "No"))
				return;

			int task = Progress.Start("Tremble Import", options: Progress.Options.Synchronous | Progress.Options.Managed);

			try
			{
				for (int i = 0; i < mapsToReimport.Count; i++)
				{
					string filename = Path.GetFileName(mapsToReimport[i]);

					Progress.Report(task, i + 1, mapsToReimport.Count, $"Importing {filename}... ({i + 1}/{mapsToReimport.Count})");
					AssetDatabase.ImportAsset(mapsToReimport[i], ImportAssetOptions.ForceUpdate);
				}
			}
			finally
			{
				Progress.Finish(task);
			}
		}

		
		internal static void DeleteAutosaves()
		{
			List<string> maps = new();

			foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
			{
				if (!AssetDatabaseUtil.IsTrembleMapFile(assetPath))
					continue;

				if (!assetPath.Contains("autosave/"))
					continue;

				maps.Add(assetPath);
			}

			IEnumerable<string> autosaveFolders = maps
				.Select(map => Directory.GetParent(map)?.FullName)
				.Select(absDir => Path.GetRelativePath(Directory.GetCurrentDirectory(), absDir))
				.Distinct();

			foreach (string autosaveFolder in autosaveFolders)
			{
				AssetDatabase.DeleteAsset(autosaveFolder);
			}
		}
	}
}