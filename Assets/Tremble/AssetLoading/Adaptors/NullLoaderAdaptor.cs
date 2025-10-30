//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble
{
	public class NullLoaderAdaptor : IAssetLoaderAdaptor
	{
		public bool SupportsPreloading => false;

		public string[] FindAssetPaths(Type type, string name = "")
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return Array.Empty<string>();
		}

		public TAsset LoadAssetByPath<TAsset>(string path) where TAsset : Object
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return null;
		}

		public Task PreloadAssetsByPaths(IList<string> paths)
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return Task.CompletedTask;
		}

		public string GetPath(Object obj)
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return null;
		}

		public TrembleSyncSettings GetSyncSettings()
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return null;
		}

		public TrembleSyncSettings LoadSyncSettings()
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return null;
		}

		public Material LoadClipMaterial()
		{
			Debug.LogError("No valid loader adaptor - ensure you have Addressables installed or are in editor!");
			return null;
		}
		public void ReleaseAll() { }
	}
}