//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Basics/Settings", "Tremble's Settings")]
	public class SettingsPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Tremble's default settings are designed to be suitable for most users, and most games.",
				"If you're a bit more advanced, or just want to tweak various things - click ''Tools'' > ",
				"''Tremble'' > ''Open Settings...'' in the Unity menu."
			);

			Text(
				"Each tab shows settings for a different aspect of Tremble.",
				"Help text is provided for each option."
			);

			ActionBar_SingleAction("Open Settings for me", TrembleEditorAPI.OpenSettings);
		}
	}
}