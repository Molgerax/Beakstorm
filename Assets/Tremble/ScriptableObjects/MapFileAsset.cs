// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble
{
	/// <summary>
	/// A "Map Request" file. This gets around a shortcoming in Unity where
	/// you cannot create new assets that are not ".asset" files.
	///
	/// We create a blank "Map Request" asset in Unity, and then an Asset
	/// Postprocessor picks this new asset up, and creates a blank
	/// TrenchBroom map with the same name (and deletes the request).
	/// </summary>
	[CreateAssetMenu(menuName = "Tremble Map", fileName = "New Map")]
	public class MapFileAsset : ScriptableObject
	{
	}
}