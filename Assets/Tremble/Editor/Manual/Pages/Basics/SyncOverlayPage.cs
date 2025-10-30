//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Basics/Sync Toolbar")]
	public class SyncOverlayPage : ManualPageBase
	{
		private bool m_ShowAlreadyHere;

		protected override void OnGUI()
		{
			Text(
				"By default, Tremble adds a toolbar to your scene view to quickly sync settings to",
				"TrenchBroom, or to (re)import your map."
			);

			Text(
				"You can reopen it with \"Tools\" > \"Tremble\" > \"Show Sync Toolbar\" (Unity 2022+)",
				"if you lose it, or by right-clicking on your scene tab and clicking \"Overlay Menu\"",
				"(or hitting [`] on your keyboard) and selecting \"Tremble Sync\".");

#if UNITY_2022_1_OR_NEWER
			ActionBar_SingleAction("Open the Sync Toolbar for me", () => SceneView.AddOverlayToActiveView(new TrembleSyncOverlay()));
#endif

			H1("Toolbar Buttons");
			Text("The sync toolbar looks like this:");
			ToolbarButtonDescription("T_TB_Import", "Re-import map(s) in current scene from TrenchBroom");
			ToolbarButtonDescription("T_TB_Sync", "Sync Unity prefabs and materials to TrenchBroom");
			ToolbarButtonDescription("T_TB_Config", "Open Tremble's Settings");
			ToolbarButtonDescription("T_TB_Manual", "Open the Tremble Manual (this!)");

			ActionBar(() =>
			{
				Action("Import", TrembleEditorAPI.ReimportCurrentMap);
				Action("Sync", TrembleEditorAPI.SyncToTrenchBroom);
				Action("Settings", TrembleEditorAPI.OpenSettings);
				Action("Manual", () => m_ShowAlreadyHere = true);
			});

			if (m_ShowAlreadyHere)
			{
				Text("You silly! You're already in the manual!");
			}
		}

		private void ToolbarButtonDescription(string textureName, string description)
		{
			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
			{
				Image(textureName, 30f, centred: false);
				GUILayout.BeginVertical();
				{
					GUILayout.Space(10f * ManualStyles.Scale);
					GUILayout.Label(description, ManualStyles.Styles.Text);
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();

			SmallSpace();
		}
	}
}