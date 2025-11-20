//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Basics/Live Update", "Using Live Update")]
	public class LiveUpdatePage : ManualPageBase
	{
		private static readonly string[] REFRESH_MODES = { "Disabled", "Enabled", "Enabled Outside Playmode" };

		protected override void OnGUI()
		{
			Experimental();

			int autoRefreshMode = EditorPrefs.GetInt("kAutoRefreshMode");
			if (autoRefreshMode != 1)
			{
				Callout($"Your project uses Auto Refresh Mode: {REFRESH_MODES[autoRefreshMode]}. This",
					$"will mean that Live Update will not function. Change it to {REFRESH_MODES[1]} to",
					"use Live Update!");
				ActionBar_SingleAction("Enable Auto Refresh during playmode", () => EditorPrefs.SetInt("kAutoRefreshMode", 1));
			}

			Text(
				"Tremble supports Live Editing - the act of editing a map while in Unity Editor's playmode.",
				"All you need to do is save changes to your map file while the game is running in editor playmode.",
				"The map is imported as normal, and then Tremble will try to 'reinstance' the map in memory."
			);

			Foldout("Wait, what's reinstancing?", () =>
			{
				Text("What this means is, Tremble will:");

				Bullet("Destroy all worldspawn geometry, and copy the new geometry from the new map");
				Bullet("For entities:");
				Indent(() =>
				{
					Bullet(
						"If an entity is not present in the running game, spawn it (note: this may mean, for example,",
						"that if you kill an enemy and then save the map, the enemy may get respawned)."
					);
					Bullet(
						"If an entity is present in the running game, but not in the map, it is removed",
						"(this does not work in all cases!)"
					);
					Bullet(
						"If any entity properties have changed, and are simple properties",
						"(floats, ints, Vector3s), these are updated."
					);
				});
			});

			H1("Handling Live Update logic");
			{
				H2("General Usage");

				Text(
					"If you need to re-run some logic in an entity after it is LiveUpdated, you can implement",
					$"the {nameof(IOnLiveUpdate)} interface, and override the OnLiveUpdate() method to perform any custom",
					"logic."
				);

				Text(
					"For example, finding the list of entities of a certain type (as these may have",
					"changed as a result of the map changing)."
				);

				Image("T_Manual_LiveUpdate1");

				H2("Simpler Usage");

				Text(
					"If this logic is identical to what would usually happen in your Start or Awake methods,",
					$"you can instead implement the {nameof(IOnLiveUpdate_Simple)} interface."
				);

				Text(
					"This will signal to Tremble that after a LiveUpdate occurs, it should call OnDestroy,",
					"followed by Awake and Start (in that order) on your entity. That way, as long as your",
					"Awake/Start methods set your entity up, and OnDestroy removes any state, it will 'just work'!"
				);

				Image("T_Manual_LiveUpdate2");
			}

			H2("Warnings!");
			{
				Text(
					"If LiveUpdate goes wrong or breaks, your game may be left in a broken state.",
					"To recover from this, you'll need to stop playmode, and start it again."
				);

				Callout(
					"Known bug: Sometimes, after LiveUpdate has failed, the map is left in a broken state",
					"outside of playmode too. This is a rare bug that I've only seen a handful of times.",
					"Of course, if this occurs, simply re-import your map again after exiting playmode to fix it."
				);
			}
		}
	}
}