//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

#if ADDRESSABLES_INSTALLED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble
{
	public class AddressablesLoaderAdaptor : IAssetLoaderAdaptor
	{
		public bool SupportsPreloading => true;

		private readonly List<AsyncOperationHandle> m_OwnedHandles = new(1024);
		private readonly Dictionary<string, object> m_LoadedAssets = new(1024);

		public string[] FindAssetPaths(Type type, string name = "")
		{
			return TrembleAssetLoader.FindAssetDataMany(a => a.Path.ContainsInvariant(name))
				.Where(a => a.FullTypeName.EqualsInvariant(type.FullName))
				.Select(a => a.Path)
				.ToArray();
		}

		public TAsset LoadAssetByPath<TAsset>(string path) where TAsset : Object
		{
			if (path.IsNullOrEmpty())
				return null;

			if (!TrembleAssetLoader.TryGetAssetDataFromPath(path, out AssetMetadata assetData))
			{
				Debug.LogError($"Not loading invalid addressable: {path}");
				return null;
			}

			if (m_LoadedAssets.TryGetValue(assetData.AddressableName, out object existingAsset) &&
			    existingAsset is TAsset tAsset)
			{
				return tAsset;
			}

			AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(assetData.AddressableName);
			TAsset asset = handle.WaitForCompletion();
			if (!asset)
			{
				Addressables.Release(handle);
				return null;
			}

			m_OwnedHandles.Add(handle);
			m_LoadedAssets[path] = handle;
			return asset;
		}

		public async Task PreloadAssetsByPaths(IList<string> paths)
		{
			if (paths.Count == 0)
				return;

			List<string> addresses = new(paths.Count);

			// First, remove dupes, invalid and ones we already loaded
			for (int i = 0; i < paths.Count; i++)
			{
				string path = paths[i];

				if (path.IsNullOrEmpty() || !TrembleAssetLoader.TryGetAssetDataFromPath(path, out AssetMetadata assetData))
				{
					Debug.LogError($"Not loading invalid addressable: {path}");
					paths.RemoveAt(i--);
               continue;
				}

				if (m_LoadedAssets.ContainsKey(assetData.AddressableName))
				{
					paths.RemoveAt(i--);
					continue;
				}

				if (paths.IndexOf(path) != i)
				{
					paths.RemoveAt(i--);
					continue;
				}

				// This is valid - add to list of addresses
				addresses.Add(assetData.AddressableName);
			}

			if (addresses.Count == 0)
				return;

			AsyncOperationHandle<IList<Object>> resultsHandle = Addressables.LoadAssetsAsync<Object>(addresses, null, Addressables.MergeMode.Union);
			await resultsHandle.Task;

			for (int i = 0; i < paths.Count; i++)
			{
				string path = paths[i];
				if (TrembleAssetLoader.TryGetAssetDataFromPath(path, out AssetMetadata assetData))
				{
					m_LoadedAssets[assetData.AddressableName] = resultsHandle.Result[i];
				}
			}
			m_OwnedHandles.Add(resultsHandle);
		}

		public string GetPath(Object obj)
		{
			Debug.LogError("GetPath is not implemented in Addressables (yet!)");
			return null;
		}

		public TrembleSyncSettings LoadSyncSettings()
		{
			return Addressables
					.LoadAssetAsync<TrembleSyncSettings>(TrembleSyncSettings.ADDRESSABLE_KEY)
					.WaitForCompletion();
		}

		public Material LoadClipMaterial()
		{
			return Addressables.LoadAssetAsync<Material>("M_NullRender").WaitForCompletion();
		}

		public void ReleaseAll()
		{
			foreach (AsyncOperationHandle handle in m_OwnedHandles)
			{
				Addressables.Release(handle);
			}
			m_OwnedHandles.Clear();
		}

		private static bool IsAddressablePathValid(string path)
		{
			if (path == null)
				return false;

			AsyncOperationHandle<IList<IResourceLocation>> locationLoadOperation = Addressables.LoadResourceLocationsAsync(path);
			IList<IResourceLocation> results = locationLoadOperation.WaitForCompletion();
			bool isValidPath = results is { Count: > 0 };
			Addressables.Release(locationLoadOperation);

			return isValidPath;
		}
	}
}
#endif