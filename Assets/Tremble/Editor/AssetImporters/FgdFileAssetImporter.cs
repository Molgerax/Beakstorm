//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;

namespace TinyGoose.Tremble.Editor
{
	[ScriptedImporter(1, "fgd", -1000)]
	public class FgdFileAssetImporter : ScriptedImporter
	{
		[SerializeField] private bool m_Test;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Entry Point
		// -----------------------------------------------------------------------------------------------------------------------------
		public override void OnImportAsset(AssetImportContext ctx)
		{
			string assetName = Path.GetFileNameWithoutExtension(ctx.assetPath);

			FgdFileAsset wadAsset = ScriptableObject.CreateInstance<FgdFileAsset>();
			wadAsset.name = assetName;

			ctx.AddObjectToAsset(assetName, wadAsset);
			ctx.SetMainObject(wadAsset);
		}
	}
}