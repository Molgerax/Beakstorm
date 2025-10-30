using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// Make a point entity in the map editor, coloured green in the map
	
	[PointEntity("sheep_spawn", category: "sample", colour: "0.0 1.0 0.0", size: 1f)]
	public class SheepSpawner : MonoBehaviour, IOnImportFromMapEntity
	{
		// Uses a custom "IntInRange" type - see IntInRangeFieldConverter for how to (de-)serialise custom types in Tremble
		[SerializeField] private IntInRange m_SecondsBetweenSpawns = new(3, 5);

		// Our sheep prefab - we can set this in the map, but if this is null we look it up in
		// OnImportFromMapEntity. If we were a Prefab Entity, the value from the prefab would
		// be the default, so that might be a better option.
		[SerializeField] private GameObject m_SheepPrefab;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Public
		// -----------------------------------------------------------------------------------------------------------------------------
		public IntInRange SecondsBetweenSpawns => m_SecondsBetweenSpawns;
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private SampleWorldspawn m_Worldspawn;
		private float m_TimeToSpawn;


		public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
		{
			// If we set a prefab in the map, use that - don't override it!
			if (m_SheepPrefab)
				return;

			// Set the field - because this is serialised, this is saved at edit-time.
			m_SheepPrefab = TrembleAssetLoader.LoadAssetByName<GameObject>("P_Sheep");
		}
		
		private void Start()
		{
			// Find out worldspawn
			MapDocument map = gameObject.GetComponentInParent<MapDocument>();
			m_Worldspawn = map.GetComponentInChildren<SampleWorldspawn>();

			// Set spawn timer
			m_TimeToSpawn = m_SecondsBetweenSpawns.GetRandom();
		}

		private void Update()
		{
			// Tick down, spawn if needed
			m_TimeToSpawn -= Time.deltaTime;
			if (m_TimeToSpawn > 0f)
				return;

			// If we set a max number of sheep on the worldspawn, honour it!
			if (m_Worldspawn && m_Worldspawn.MaxSheep > 0)
			{
				int numSpawnSheep = FindObjectsOfType<Sheep>().Length;
				if (numSpawnSheep >= m_Worldspawn.MaxSheep)
				{
					Debug.Log($"Max sheep ({numSpawnSheep}) set in worldspawn - holding off for now...");
					m_TimeToSpawn = m_SecondsBetweenSpawns.GetRandom();

					return;
				}
			}

			Instantiate(m_SheepPrefab, transform);
			m_TimeToSpawn = m_SecondsBetweenSpawns.GetRandom();
		}
	}
}