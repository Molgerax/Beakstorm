// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public static class TrembleEditorAPI
	{
		/// <summary>
		/// Sync GameConfig, materials and entities to TrenchBroom
		/// </summary>
		public static void SyncToTrenchBroom() => _ = TrembleSync.PerformSync();
		
		/// <summary>
		/// Reimport the currently open map (if any)
		/// </summary>
		public static void ReimportCurrentMap() => TrembleMenu.PerformCurrentMapReimport();
		
		/// <summary>
		/// Reimport all maps in the project (slow!)
		/// </summary>
		public static void ReimportAllMaps(bool silent = false) => TrembleMenu.PerformAllMapsReimport(silent);

		/// <summary>
		/// Open the settings window
		/// </summary>
		public static void OpenSettings()
		{
			if (TrembleSyncSettings.Get().EmbedTrembleSettingsInProjectSettings)
			{
				SettingsService.OpenProjectSettings("Project/Tremble");
			}
			else
			{
				TrembleSyncSettingsWindow.OpenSettings();
			}
		}

		/// <summary>
		/// Open the manual window
		/// </summary>
		public static void OpenManual(Type page = null)
		{
			// Find page to start on
			if (page != null)
			{
				page.GetCustomAttributes(out ManualPageAttribute manualPage);
				if (manualPage == null)
				{
					Debug.LogError($"{page.Name} is not a valid manual page!");
					page = null;
				}
			}

			// Create list of sortings
			string[] sortOrderFirst =
			{
				// Main sections
				"Basics",
				"Entities",
				"Advanced",
				"User Generated Content",

				// Misc. important pages
				"Overview",
				"Setup",
				"UGC Setup",
				"Get Help & Support"
			};

			string[] sortOrderLast =
			{
				"Troubleshooting",
				"Debug"
			};

			ManualWindow.OpenManual($"Tremble Manual v{TrembleConsts.VERSION_STRING}", page ?? typeof(OverviewPage),
				sortOrderFirst, sortOrderLast);
		}

		/// <summary>
		/// Invalidate the materials and prefabs cache
		/// </summary>
		public static void InvalidateMaterialAndPrefabCache() => InvalidateMaterialAndPrefabCache(silent: false);
		public static void InvalidateMaterialAndPrefabCache(bool silent)
		{
			string baseq3Path = Path.Combine(Directory.GetCurrentDirectory(), "Library", "baseq3");

			if (Directory.Exists(baseq3Path))
			{
				Directory.Delete(baseq3Path, true);

				if (!silent)
				{
					Debug.Log("Note: Cache was invalidated. Next sync will be a little slower!");
				}
			}
		}
	}
}