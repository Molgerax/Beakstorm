//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ScriptedImporter(1, "wad", -1000)]
	public class WadFileAssetImporter : ScriptedImporter
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Entry Point
		// -----------------------------------------------------------------------------------------------------------------------------
		public override void OnImportAsset(AssetImportContext ctx)
		{
			string assetName = Path.GetFileNameWithoutExtension(ctx.assetPath);

			WadFileAsset wadAsset = ScriptableObject.CreateInstance<WadFileAsset>();
			wadAsset.Modify(so =>
			{
				SerializedProperty materials = so.FindBackedProperty(nameof(wadAsset.MaterialNames));

				WadParser parser = new(ctx.assetPath);
				WadFile wadFile = parser.Parse();

				foreach (WadEntry entry in wadFile.Entries)
				{
					materials.AppendArrayElement().stringValue = entry.Name;
				}
			});
			wadAsset.name = assetName;

			ctx.AddObjectToAsset(assetName, wadAsset);
			ctx.SetMainObject(wadAsset);
		}
	}
}