//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public class PrefabNameLookup
	{
		public PrefabNameLookup(TrembleSyncSettings syncSettings)
		{
			m_SyncSettings = syncSettings;

#if UNITY_EDITOR
			GenerateLookup();
#endif
		}
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Setup
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly TrembleSyncSettings m_SyncSettings;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly TwoWayDictionary<string, string> m_PrefabPathToMapName = new();

		public bool TryGetPrefabPathFromMapName(string mapName, out string foundName)
		{
#if !UNITY_EDITOR
			if (TrembleAssetLoader.TryGetAssetDataFromMapName(mapName, out AssetMetadata data))
			{
				foundName = data.Path;
				return true;
			}
			
			foundName = mapName;
			return false;
#else
			Debug.Assert(m_PrefabPathToMapName != null, nameof(m_PrefabPathToMapName) + " != null");
			return m_PrefabPathToMapName.TryGetKey(mapName, out foundName);
#endif
		}
		public bool TryGetMapNameFromPrefabPath(string prefabPath, out string foundName)
		{
			Debug.Assert(m_PrefabPathToMapName != null, nameof(m_PrefabPathToMapName) + " != null");
			return m_PrefabPathToMapName.TryGetValue(prefabPath, out foundName);
		}

		public bool IsValidAsset(string mapName)
		{
			return TrembleAssetLoader.TryGetAssetDataFromMapName(mapName, out _);
		}

		public List<string> AllPrefabPaths => m_PrefabPathToMapName.Keys.ToList();

#if UNITY_EDITOR
		private void GenerateLookup()
		{
			foreach (string prefabEntityPath in TrembleAssetLoader.FindAssetPaths<GameObject>())
			{
				GameObject asset = TrembleAssetLoader.LoadPrefabByPath(prefabEntityPath);
				if (!asset)
					continue;

				// Work out prefix/name for this prefab
				string prefix = FgdConsts.PREFAB_PREFIX;

				string mapName = Path.GetFileNameWithoutExtension(prefabEntityPath);
				mapName = mapName.Sanitise(new() { '{', '}', ' ', '[', ']', '"', '\'', '.', ',' });

				if (m_SyncSettings.TypeNamingConvention != NamingConvention.PreserveExact && mapName.StartsWithInvariant("P_", caseSensitive: true))
				{
					mapName = mapName.Substring(2);
				}

				mapName = mapName.ToNamingConvention(m_SyncSettings.TypeNamingConvention);

				bool isExportedType = false;

				if (asset.TryGetComponent(out TrembleSpawnablePrefab spawnablePrefab))
				{
					if (!spawnablePrefab.Category.IsNullOrEmpty())
					{
						prefix = spawnablePrefab.Category;
					}

					isExportedType = !spawnablePrefab.OnlyVariants || EDITOR_IsVariant(asset);
				}
				else
				{
					foreach (MonoBehaviour component in asset.GetComponents<MonoBehaviour>())
					{
						if (!component)
						{
							Debug.LogWarning($"Missing component on prefab {asset.name}!", asset);
							continue;
						}
						
						if (!component.GetType().TryGetCustomAttribute(out PrefabEntityAttribute pea))
							continue;

						if (pea.Category != null)
						{
							prefix = pea.Category;
						}

						if (pea.TrenchBroomName != null)
						{
							mapName = pea.TrenchBroomName;
						}

						isExportedType = !pea.OnlyVariants || EDITOR_IsVariant(asset);

						break;
					}
				}

				if (!isExportedType)
					continue;

				if (!mapName.StartsWithInvariant(prefix, caseSensitive: true))
				{
					mapName = prefix + "_" + mapName;
				}

				try
				{
					m_PrefabPathToMapName.Add(prefabEntityPath, mapName);
				}
				catch (ArgumentException)
				{
					Debug.LogError($"Prefab path '{prefabEntityPath}' or map name '{mapName}' already used!}}");
				}
			}
		}
#endif

#if UNITY_EDITOR
		private bool EDITOR_IsVariant(GameObject asset)
		{
			// Not a variant - don't export!
			if (PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Variant)
				return false;

			// It's a variant - check first if the parent is a regular prefab or
			// a variant of a prefab... i.e. NOT a model prefab.
			GameObject parent = PrefabUtility.GetCorrespondingObjectFromSource(asset);
			if (parent)
			{
				PrefabAssetType parentType = PrefabUtility.GetPrefabAssetType(parent);
				return parentType switch
				{
					PrefabAssetType.Regular => true,
					PrefabAssetType.Variant => true,

					_ => false
				};
			}

			return true;
		}
#endif
	}
}