//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Import from Other Games/Texture FGDs from other games")]
	public class ImportFgdPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text("When importing maps from other games, Tremble might encounter entities which it does not recognise.");

			Text("For example, if you import a map from Quake®, it will contain (among others) an",
				"'info_player_start' entity to define where the player should spawn.");

			Text("If you import the map and use Map Repair, you can either:");

			Number("Map an existing MonoBehaviour from your game to this unknown entity, or");
			Number("Have Tremble generate a new MonoBehaviour, by making assumptions based on what's in the map.");

			Text("However, this can be problematic. If the map doesn't contain data for a given field,",
				"Tremble will have no knowledge of it and won't generate complete C# script for it.");

			Text("Tremble will never generate anything invalid, but you may be missing functionality.");

			Text("This is where importing an FGD file can help!");

			Foldout("What's an FGD file?", () =>
			{
				Text("A Forge Game Data file. It contains a list of all entities in a game, and what",
					"fields they support.");

				Text("It can also contain helpful tooltips and hints as to how each field is used.");
			});

			H1("Importing an FGD file");
			Text("To import an FGD file, simply drag it into your project.");
			Text("Tremble will extract the FGD classes into a folder of the same name.");
		}
	}
}