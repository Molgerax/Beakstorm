//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Advanced/Runtime Querying")]
	public class RuntimeQueryingPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"At runtime, Tremble does not perform any logic and has zero overhead by design.",
				"Almost all Tremble functionality only runs in the Editor. However, you may need to query",
				"a loaded map to find certain objects at runtime."
			);

			Text(
				"Of course, since entities are just MonoBehaviours, you can use Unity's functionality",
				"(such as gameObject.GetComponentsInChildren<T>()) to find all objects of a given type."
			);

			Text(
				"However, if you want to find a specific instance of an entity in the map,",
				"you can do the following:"
			);

			Number(
				"Ensure the entity you want to find is marked with a unique",
				$"{TrembleSyncSettings.Get().IdentityPropertyName} property in TrenchBroom. You can also have multiple",
				$"objects with the same {TrembleSyncSettings.Get().IdentityPropertyName}, but when you query the map",
				"you will then get a list of entities back."
			);
			Indent(() =>
			{
				Bullet(
					"For example, in the worldspawn entity you could have a start_spawn field",
					"which names a spawn point to start from. We'll set it to 'my_cool_spawnpoint'.",
					$"Then, you give your spawn point entity in the world a {TrembleSyncSettings.Get().IdentityPropertyName}"
					,"field, with the value 'my_cool_spawnpoint'."
				);
			});

			Number(
				$"Now, given a reference to the {nameof(MapDocument)} for your map (which is added to the root",
				$"GameObject), you can query for your {TrembleSyncSettings.Get().IdentityPropertyName} with the Q() method."
			);

			Code(
				"// Get the map document - assumes one map in the scene.",
				"// You might store a reference somewhere to this instead.",
				"",
				$"{nameof(MapDocument)} map = GameObject.FindAnyObjectByType<{nameof(MapDocument)}>();",
				"",
				$"// 1. Find a single entity with this {TrembleSyncSettings.Get().IdentityPropertyName}",
				"if (map.Q(\"my_cool_spawnpoint\", out SpawnPoint spawnPoint))",
				"{",
				"    spawnPoint.DoAThing();",
				"}",
				"",
				$"// 2. Find many entities with this {TrembleSyncSettings.Get().IdentityPropertyName}",
				"if (map.Q(\"my_cool_spawnpoint\", out SpawnPoint[] spawnPoints))",
				"{",
				"    foreach (SpawnPoint spawnPoint in spawnPoints)",
				"    {",
				"        spawnPoint.DoAThing();",
				"    }",
				"}"
			);
		}
	}
}