//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	public class FgdFileAssetPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			foreach (string requestFileName in importedAssets)
			{
				// We imported something, but not a MapFileAsset
				if (AssetDatabase.GetMainAssetTypeAtPath(requestFileName) != typeof(FgdFileAsset))
					continue;

				bool success = false;

				try
				{
					FgdParser.Import(requestFileName);
					success = true;
				}
				finally
				{
					if (!success)
					{
						EditorUtility.DisplayDialog("Import Failed!", "Failed to import FGD file :(, try again!", "Okay");
					}
					AssetDatabase.DeleteAsset(requestFileName);
				}
			}
		}
	}
}