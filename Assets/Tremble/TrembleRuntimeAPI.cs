//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if ADDRESSABLES_INSTALLED
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
#endif

namespace TinyGoose.Tremble
{
	public static class TrembleRuntimeAPI
	{
		public const string TREMBLE_GAME_RESOURCE = "Tremble_Game";
		public const string TREMBLE_BASEQ3_RESOURCE = "Tremble_BaseQ3";

		private static bool s_Initialised = false;

		public static async Task Initialise()
		{
			if (s_Initialised)
				return;

#if ADDRESSABLES_INSTALLED && !UNITY_EDITOR
			if (Addressables.ResourceLocators == null || !Addressables.ResourceLocators.Any())
			{
				await Addressables.InitializeAsync().Task;
			}
			else
			{
				Debug.Log("Addressables is already initialised!");
			}
#endif

#if !UNITY_EDITOR || USE_TYPEUTIL_IN_EDITOR
			TypeDatabase.GenerateClassLookup();
#endif

			// Only do this for PC platforms - for consoles/mobile... we need something else
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
			// Unpack textures/models for TB
			Task unpack1 = PackedResourceUtil.UnpackResourceIntoFolder(TREMBLE_BASEQ3_RESOURCE, TrembleConsts.BASEQ3_PATH);

			// Unpack game definition for TB
			Task unpack2 = PackedResourceUtil.UnpackResourceIntoFolder(TREMBLE_GAME_RESOURCE, TrenchBroomUtil.GetGameFolder());

			await Task.WhenAll(unpack1, unpack2);

			// Write engine path into TB
			await TrenchBroomUtil.WriteEnginePathIntoTrenchBroomPrefs();
#endif

			s_Initialised = true;
		}

		public static void DestroyAllExistingMaps()
		{
#if UNITY_2022_1_OR_NEWER
			MapDocument[] maps = GameObject.FindObjectsByType<MapDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
			MapDocument[] maps = GameObject.FindObjectsOfType<MapDocument>(true);
#endif
			foreach (MapDocument map in maps)
			{
				GameObject.Destroy(map.gameObject);
			}
		}

		public static async Task<MapDocument> LoadMapAsync(string mapFile, MapProcessorClass[] processors = null, bool outputErrorsAndWarnings = false, float? maxMeshSurfaceArea = null, float? smoothingAngle = null)
		{
			if (!s_Initialised)
			{
				Debug.Log("TrembleRuntimeAPI was not initialised - doing that now. Call TrembleRuntimeAPI.Initialise() somewhere during game launch to avoid hitches!");
				await Initialise();
			}

			MapBsp mapBsp = await PreloadResourcesForMapAsync(mapFile);

			Q3Map2Result? overrideResult = null;

			TrembleMapImport importer = new(new()
			{
				ProcessorClasses = processors,

				SplitMesh = maxMeshSurfaceArea.HasValue,
				MaxMeshSurfaceArea = maxMeshSurfaceArea.GetValueOrDefault(0f),
				SmoothingAngle = smoothingAngle.GetValueOrDefault(359f),

				OnError = outputErrorsAndWarnings ? Debug.LogError : null,
				OnWarning = outputErrorsAndWarnings ? Debug.LogWarning : null,
				OnQ3Map2Result = (r, _) => overrideResult = r
			});

			MapDocument document = await importer.ImportMapAndCreatePrefab(mapFile, mapBsp);
			if (overrideResult.HasValue)
			{
				document.INTERNAL_SetResult(overrideResult.Value, overrideResult.ToString());
			}
			return document;
		}

		public static async Task<MapBsp> PreloadResourcesForMapAsync(string mapFile)
		{
			if (!TrembleAssetLoader.SupportsPreloading)
				return null;

			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			MapBsp mapBsp = MapCompiler.BuildBsp(syncSettings, mapFile, syncSettings.ExtraCommandLineArgs, out Q3Map2Result result);

			if (result == Q3Map2Result.Failed)
				return null;

			List<string> pathsToPreload = new(128);
			mapBsp.GatherResourceList(pathsToPreload);

			await TrembleAssetLoader.PreloadAssetsByPaths(pathsToPreload);

			return mapBsp;
		}
	}
}