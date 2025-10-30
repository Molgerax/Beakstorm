//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Troubleshooting/Too dark or too bright materials")]
	public class TroubleshootingMaterialBrightnessPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Image("T_Manual_Trouble_MaterialBrightness");

			Text("Some users experience issues with materials exporting too bright or too dark when",
				"viewed in TrenchBroom. This can be due to the way Tremble captures Unity Materials to",
				"texture PNGs - which uses specific lighting, but can sometimes take on the environment",
				"from your scene.");

			Text("Please note that this does NOT affect how your materials will appear in Unity/your game!");

			Text("As a workaround, you can adjust how Tremble lights your materials in Tremble's Settings,",
				"under Advanced/Materials.");

			Callout("When changing these settings or force-resyncing below, note that you should",
				"Refresh Texture Collections (F5) and Refresh Entity Definitions (F6) in TrenchBroom",
				"to update to the latest data.");

			Image("T_Manual_Trouble_MaterialBrightnessSettings");

			ActionBar_SingleAction("Open in Settings", ()
				=> SettingsService.OpenProjectSettings("Project/Tremble/4 Advanced/3 Materials"));

			Text("As a last resort, you can also try force-syncing your project to TrenchBroom. You can do this",
				"by clearing the cache and then re-syncing, from Tools > Tremble > Sync Project to TrenchBroom (Clean, Slow)",
				"- or by pressing the button below.");

			ActionBar_SingleAction("Force-resync for me", ()=>
			{
				TrembleEditorAPI.InvalidateMaterialAndPrefabCache(silent: true);
				TrembleEditorAPI.SyncToTrenchBroom();

				if (TrenchBroomUtil.IsTrenchBroomRunning)
				{
					EditorUtility.DisplayDialog(
						title: "TrenchBroom is running",
						message: "You may need to refresh Textures and Entities in TrenchBroom (press F5, then F6 in TrenchBroom)!",

						"Oh, okay!");
				}
			});
		}
	}
}