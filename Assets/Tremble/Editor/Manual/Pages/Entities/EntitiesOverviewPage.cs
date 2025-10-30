//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Entities/Overview")]
	public class EntitiesOverviewPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Entities are used to place objects (for example prefabs, trigger volumes, etc.)",
				"into your map to make them more interactive."
			);

			Text("Tremble supports 3 kinds of Entities:");

			Space();

			PropertyDescription("Prefab Entities",
				"These are 1:1 representations of your Unity prefabs, which can be dropped into a map.",
				$"You can either use a '{nameof(TrembleSpawnablePrefab)}' component in your prefab to allow it to",
				"be simply spawned from your map, or you can mark your scripts up with",
				$"'[{FormatAttributeName(typeof(PrefabEntityAttribute))}]' to synchronise properties and more!");
			ActionBar(() =>
			{
				Action("More about Spawnable Prefabs", () => GoToPage(typeof(SpawnablePrefabEntitiesPage)));
				Action("More about Prefab Entities", () => GoToPage(typeof(PrefabEntitiesPage)));
			});

			Space();

			PropertyDescription("Brush Entities",
				"These are brushes (shapes/meshes) in the map, which you want to apply a MonoBehaviour script to.",
				"These can be Solids - for example, a moving platform whose shape you design in TrenchBroom,",
				"Triggers - for example an area which triggers an event, and more!");
			ActionBar_SingleAction("More about Brush Entities", () => GoToPage(typeof(BrushEntitiesPage)));

			Space();

			PropertyDescription("Point Entities",
				"These are points in space which contain data - for example a target location for spawning enemies,",
				"or a goal location for a player to go to.");
			ActionBar_SingleAction("More about Point Entities", () => GoToPage(typeof(PointEntitiesPage)));
		}
	}
}