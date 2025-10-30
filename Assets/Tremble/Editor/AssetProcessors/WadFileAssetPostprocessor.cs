//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	public class WadFileAssetPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			foreach (string requestFileName in importedAssets)
			{
				// We imported something, but not a WadFileAsset
				if (AssetDatabase.GetMainAssetTypeAtPath(requestFileName) != typeof(WadFileAsset))
					continue;

				bool success = false;

				try
				{
					WadAssetImporterEditor.Extract(requestFileName);
					success = true;
				}
				finally
				{
					if (!success)
					{
						EditorUtility.DisplayDialog("Import Failed!", "Failed to import WAD file :(, try again!", "Okay");
					}
					AssetDatabase.DeleteAsset(requestFileName);
				}
			}
		}
	}
}