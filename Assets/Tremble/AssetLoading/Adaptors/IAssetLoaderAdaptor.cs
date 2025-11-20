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
	public interface IAssetLoaderAdaptor
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Finding & loading assets
		// -----------------------------------------------------------------------------------------------------------------------------
		string[] FindAssetPaths(Type type, string name = "");
		TAsset LoadAssetByPath<TAsset>(string path) where TAsset : Object;
		Task PreloadAssetsByPaths(IList<string> paths);
		bool SupportsPreloading { get; }

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Getting path for a loaded asset
		// -----------------------------------------------------------------------------------------------------------------------------
		string GetPath(Object obj);

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Misc
		// -----------------------------------------------------------------------------------------------------------------------------
		TrembleSyncSettings LoadSyncSettings();
		Material LoadClipMaterial();
		void ReleaseAll();
	}
}