//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

//#define USE_ADDRESSABLES_IN_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble
{
	public static class TrembleAssetLoader
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Adaptor
		// -----------------------------------------------------------------------------------------------------------------------------
		private static IAssetLoaderAdaptor s_LoaderAdaptor;

		private static IAssetLoaderAdaptor LoaderAdaptor
		{
			get
			{
				if (s_LoaderAdaptor == null)
				{
#if UNITY_EDITOR && !USE_ADDRESSABLES_IN_EDITOR
					s_LoaderAdaptor = new EditorLoaderAdaptor();
#elif UNITY_SERVER
					s_LoaderAdaptor = new NullLoaderAdaptor();
#elif ADDRESSABLES_INSTALLED
					s_LoaderAdaptor = new AddressablesLoaderAdaptor();
#else
					s_LoaderAdaptor = new NullLoaderAdaptor();
#endif
				}

				return s_LoaderAdaptor;
			}
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Generic
		// -----------------------------------------------------------------------------------------------------------------------------
		public static string FindAssetPath<TAsset>(string name = "") where TAsset : Object
		{
			string[] paths = FindAssetPaths<TAsset>(name);

			switch (paths.Length)
			{
				// Nothing matched - return null :(
				case 0:
					return null;

				// Multiple matches - check if we have an EXACT match
				case > 1:
				{
					foreach (string path in paths)
					{
						string filename = Path.GetFileNameWithoutExtension(path);
						if (!filename.EqualsInvariant(name))
							continue;

						return path;
					}

					break;
				}
			}

			// Only a single match, or couldn't disambiguate - return first
			return paths[0];
		}

		public static string[] FindAssetPaths<TAsset>(string name = "") where TAsset : Object => FindAssetPaths(typeof(TAsset), name);

		public static string[] FindAssetPaths(Type type, string name = "") => LoaderAdaptor.FindAssetPaths(type, name);

		public static string GetPath(Object obj) => LoaderAdaptor.GetPath(obj);

		public static GameObject LoadPrefabByName(string name) => LoadAssetByName<GameObject>(name);
		public static GameObject LoadPrefabByPath(string path) => LoadAssetByPath<GameObject>(path);

		public static TAsset LoadAssetByName<TAsset>(string name) where TAsset : Object
		{
			string path = FindAssetPath<TAsset>(name);
			return LoadAssetByPath<TAsset>(path);
		}

		public static TAsset LoadAssetByPath<TAsset>(string path) where TAsset : Object
			=> LoaderAdaptor.LoadAssetByPath<TAsset>(path);

		public static async Task PreloadAssetsByPaths(IList<string> paths)
			=> await LoaderAdaptor.PreloadAssetsByPaths(paths);

		public static bool SupportsPreloading => LoaderAdaptor.SupportsPreloading;

		public static void ReleaseAll() => LoaderAdaptor.ReleaseAll();

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Queries
		// -----------------------------------------------------------------------------------------------------------------------------
		private static int s_NumAssetMetadatas;
		private static readonly Dictionary<string, int> s_MapNameToMetadataIndex = new();
		private static readonly Dictionary<string, int> s_PathToMetadataIndex = new();

		public static bool TryGetAssetDataFromMapName(string mapName, out AssetMetadata foundMetadata)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			CreateLookups(syncSettings);

			bool found = s_MapNameToMetadataIndex.TryGetValue(mapName, out int index);
			foundMetadata = found ? syncSettings.AssetMetadatas[index] : default;
			return found;
		}

		public static bool TryGetAssetDataFromPath(string path, out AssetMetadata foundMetadata)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			CreateLookups(syncSettings);

			bool found = s_PathToMetadataIndex.TryGetValue(path, out int index);
			foundMetadata = found ? syncSettings.AssetMetadatas[index] : default;
			return found;
		}

		private static void CreateLookups(TrembleSyncSettings syncSettings)
		{
			if (syncSettings.AssetMetadatas == null)
				return;

			if (s_NumAssetMetadatas == syncSettings.AssetMetadatas.Length)
				return;

			s_NumAssetMetadatas = syncSettings.AssetMetadatas.Length;
			s_MapNameToMetadataIndex.Clear();
			s_PathToMetadataIndex.Clear();

			for (int i = 0; i < syncSettings.AssetMetadatas.Length; i++)
			{
				s_MapNameToMetadataIndex[syncSettings.AssetMetadatas[i].MapName] = i;
				s_PathToMetadataIndex[syncSettings.AssetMetadatas[i].Path] = i;
			}
		}

		public static AssetMetadata[] FindAssetDataMany(Func<AssetMetadata, bool> filter)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			return syncSettings.AssetMetadatas != null
				? syncSettings.AssetMetadatas.Where(filter).ToArray()
				: Array.Empty<AssetMetadata>();
		}
		public static bool TryFindAssetData(Func<AssetMetadata, bool> filter, out AssetMetadata found)
		{
			AssetMetadata[] results = FindAssetDataMany(filter);
			found = results.Length > 0 ? results[0] : default;
			return results.Length > 0;
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Specifics
		// -----------------------------------------------------------------------------------------------------------------------------
		public static Material LoadClipMaterial() => LoaderAdaptor.LoadClipMaterial();
		public static TrembleSyncSettings LoadSyncSettings() => LoaderAdaptor.LoadSyncSettings();
	}
}