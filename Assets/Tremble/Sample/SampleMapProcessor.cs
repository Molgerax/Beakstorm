using System.Collections.Generic;
using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// Map processors allow you to manage map import from a top-down perspective.
	// You can find which GameObjects have been created for various entities, and write custom
	// code to query information from the map.
	
	// You do NOT need to provide a map processor - this is just for more advanced use-cases. 
	
	public class SampleMapProcessor : MapProcessorBase
	{
		public override void OnProcessingStarted(GameObject root, MapBsp mapBsp)
		{
			// Called right as the map BSP has been parsed, but before any entities have been loaded.

			// For our sample, let's create our game GUI here and store it into the map
			GameObject sceneControllerPrefab = TrembleAssetLoader.LoadAssetByName<GameObject>("P_SceneController");
			InstantiatePrefab(sceneControllerPrefab);
		}

		public override void ProcessWorldSpawnProperties(MapBsp mapBsp, BspEntity entity, GameObject rootGameObject)
		{
			// Called once per map, when parsing the worldspawn properties

			// You can manually query "entity" for properties, like this: (uncomment to try!)
			// if (entity.GetString("entry_message", out string entryMessage))
			// {
			// 	Debug.Log($"Entry message for this map is: {entryMessage}");
			// }
		}
		
		public override void ProcessPrefabEntity(MapBsp mapBsp, BspEntity entity, GameObject prefab)
		{
			// Called once per prefab entity in the world

			// You can query "entity" for properties, like this: (uncomment to try!)
			// Debug.Log($"Saw prefab entity '{entity.GetClassname()}'");
		}

		public override void ProcessBrushEntity(MapBsp mapBsp, BspEntity entity, GameObject brush)
		{
			// Called once per brush entity in the world

			// You can query "entity" for properties, like this: (uncomment to try!)
			// Debug.Log($"Saw brush entity '{entity.GetClassname()}'");
		}

		public override void ProcessPointEntity(MapBsp mapBsp, BspEntity entity, GameObject point)
		{
			// Called once per point entity in the world

			// You can query "entity" for properties, like this: (uncomment to try!)
			// Debug.Log($"Saw raw point entity '{entity.GetClassname()}'");
		}

		public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
		{
			// Called after all entities have been spawned into the world.
			// You could write code here to hook up things which require a view of the entire map.
			// e.g. a spawn system could find all spawn points in the world here and store them somewhere.

			// For our sample, let's just log them.
			List<BspEntity> sheepSpawners = mapBsp.FindEntitiesOfClass("sample_sheep_spawn");
			Debug.Log($"SAMPLE: Map processed. {sheepSpawners.Count} sheep spawners in the map:");

			foreach (BspEntity sheepSpawnerEntity in sheepSpawners)
			{
				if (!TrembleMapImportSettings.Current.TryGetComponentForEntity(sheepSpawnerEntity, out SheepSpawner sheepSpawner))
					continue;

				Debug.Log($"SAMPLE:  a Sheep Spawner spawning every {sheepSpawner.SecondsBetweenSpawns.Min} to {sheepSpawner.SecondsBetweenSpawns.Max} seconds");
			}
		}
	}
}