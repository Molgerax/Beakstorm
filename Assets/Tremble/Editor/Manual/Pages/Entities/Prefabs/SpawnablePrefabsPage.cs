//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Entities/Prefabs/Spawnable Prefabs")]
	public class SpawnablePrefabEntitiesPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Spawnable Prefabs are a quick way to add a single script to ANY prefab in editor, to",
				"allow your prefab to be instantiated from TrenchBroom without any code changes.",
				"Note that this method does not allow you to set variables on those prefabs!"
			);

			Image("T_Manual_SpawnablePrefab");

			Text(
				$"Add a '{nameof(TrembleSpawnablePrefab)}' component to your prefab. This gives you the following",
				"options:"
			);

			PropertyDescription("Category",
				"You can supply the 'Category' of prefab here, too - the menu category to",
				"show the entity under, for example 'player'. If omitted, we use the prefix defined in",
				"the settings (default: 'p')"
			);

			PropertyDescription("Only Variants",
				"Only Variants: Tremble will ignore the base prefab and only export Prefab Variants of",
				"the prefab. See the above explanation about 'onlyVariants' for more information."
			);

			ActionBar_SingleAction("Build Prefab Entity", () => GoToPage(typeof(PrefabBuilderPage)));
		}
	}
}