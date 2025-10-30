// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public static class TrembleConsts
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Consts
		// -----------------------------------------------------------------------------------------------------------------------------
		
		// Version
		private const int VERSION_MAJOR = 1;
		private const int VERSION_MINOR = 10;
		private const int VERSION_PATCH = 0;
		public static readonly string VERSION_STRING = $"{VERSION_MAJOR}.{VERSION_MINOR}.{VERSION_PATCH}";
		
		// Game name
		public static readonly string GAME_NAME = $"Unity: {Application.productName}";
		public static readonly string ASSET_STORE_GAME_NAME = "Unity: AssetStore_Tremble";
		public static bool INTERNAL_IsTrembleAssetStoreProject => GAME_NAME.EqualsInvariant(ASSET_STORE_GAME_NAME);

		// Baseq3 path
		public static readonly string BASEQ3_PATH = Path.Combine(Directory.GetCurrentDirectory(), "Library", "baseq3");
		public static readonly string BASEQ3_PATH_OLD = Path.Combine(Directory.GetCurrentDirectory(), "Library", "baseq3_old");

		public static readonly string BASE_MODELS_PATH = Path.Combine(BASEQ3_PATH, "models");
		public static readonly string BASE_MATERIALS_PATH = Path.Combine(BASEQ3_PATH, "textures");
		public static readonly string MODTIME_DB_PATH = new(Path.Combine(BASEQ3_PATH, "modtime.db"));

		public const string MAPFIX_ENTITY_NAME = "ent_mapfix";
		
#if UNITY_EDITOR
		private static string s_TrembleInstallFolder;
		public static string EDITOR_GetTrembleInstallFolder()
		{
			// Use cached version
			if (s_TrembleInstallFolder != null && Directory.Exists(s_TrembleInstallFolder))
				return s_TrembleInstallFolder;

			// Find
			string path = AssetDatabase.FindAssets("t:asmdef tinygoose.tremble.editor")
				.Select(AssetDatabase.GUIDToAssetPath)
				.FirstOrDefault();

			if (path == null)
				return null;

			string dir = Path.GetDirectoryName(path); // Editor folder
			dir = Path.GetDirectoryName(dir); // Root folder

			s_TrembleInstallFolder = dir;
			return s_TrembleInstallFolder;
		}
#endif
	}

	public static class FgdConsts
	{
		// Classes
		public const string CLASS_MAP_BASE = "Tremble";
		public const string CLASS_MAP_POINT_BASE = "PointEntity";
		public const string CLASS_MAP_PREFAB_BASE = "PrefabEntity";

		// Properties
		public const string WORLDSPAWN = "worldspawn";

		public const string PROPERTY_SPAWNFLAGS = "spawnflags";
		public const string PROPERTY_CLASSNAME = "classname";
		public const string PROPERTY_TARGET = "target";
		public const string PROPERTY_PARENT = "parent";

		public const string PROPERTY_ORIGIN = "origin";
		public const string PROPERTY_ANGLES = "angles";
		public const string PROPERTY_SCALE = "scale";

		public const string PREFAB_PREFIX = "prefab";
		public const string POINT_PREFIX = "ent";
		public const string BRUSH_PREFIX = "func";
	}
}