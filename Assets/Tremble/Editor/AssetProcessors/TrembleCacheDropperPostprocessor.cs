//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	public class TrembleCacheDropperPostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (!TrembleSyncSettings.Get().AutomaticallyReimportWhenDependencyChanges)
			{
				MapHierarchyIcons.ResetModifiedTimes();
			}

			EditorLoaderAdaptor.DropCachedAssetPaths();
		}
	}
}