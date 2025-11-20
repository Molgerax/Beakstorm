//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	internal static class AssetDatabaseUtil
	{
		internal static List<string> GetAllTrembleMapPaths()
		{
			List<string> allMaps = new();

			string[] assetPaths = AssetDatabase.GetAllAssetPaths();
			foreach (string assetPath in assetPaths)
			{
				if (!IsTrembleMapFile(assetPath))
					continue;

				if (assetPath.Contains("autosave/"))
					continue;

				allMaps.Add(assetPath);
			}

			return allMaps;
		}

		internal static bool IsTrembleMapFile(string assetPath)
			=> Path.GetExtension(assetPath).EqualsInvariant(".map");
	}
}