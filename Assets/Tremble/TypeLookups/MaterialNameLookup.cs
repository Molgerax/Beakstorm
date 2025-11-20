// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public class MaterialNameLookup
	{
		public MaterialNameLookup(TrembleSyncSettings syncSettings)
		{
			m_NamingConvention = syncSettings.MaterialNamingConvention;

#if UNITY_EDITOR
			GenerateLookup(syncSettings);
#endif
		}
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly NamingConvention m_NamingConvention;

		private readonly TwoWayDictionary<string, string> m_MapNameToMaterialPath = new();

		public IEnumerable<string> AllMaterialPaths => m_MapNameToMaterialPath.Values;
		
		public bool TryGetMaterialPathFromMapName(string mapName, out string foundMaterialPath)
		{
#if !UNITY_EDITOR
			if (TrembleAssetLoader.TryGetAssetDataFromMapName(mapName, out AssetMetadata data))
			{
				foundMaterialPath = data.Path;
				return true;
			}
			
			foundMaterialPath = mapName;
			return false;
#else
			Debug.Assert(m_MapNameToMaterialPath != null, nameof(m_MapNameToMaterialPath) + " != null");
			return m_MapNameToMaterialPath.TryGetValue(mapName, out foundMaterialPath);
#endif
		}
		public bool TryGetMapNameFromMaterialPath(string materialPath, out string foundName)
		{
			Debug.Assert(m_MapNameToMaterialPath != null, nameof(m_MapNameToMaterialPath) + " != null");
			return m_MapNameToMaterialPath.TryGetKey(materialPath, out foundName);
		}

		public string GetPrefabNameFromMaterialPath(string materialPath)
		{
			string mapName = Path.GetFileNameWithoutExtension(materialPath);

			if (m_NamingConvention != NamingConvention.PreserveExact && mapName.StartsWith("M_"))
			{
				mapName = mapName.Substring(2);
			}

			return mapName.ToNamingConvention(m_NamingConvention);
		}

#if UNITY_EDITOR
		private void GenerateLookup(TrembleSyncSettings syncSettings)
		{
			foreach (MaterialGroup mg in syncSettings.GetMaterialGroupsOrDefault())
			{
				foreach (Material mat in mg.Materials)
				{
					if (!mat)
						continue;

					string materialPath = TrembleAssetLoader.GetPath(mat);
					string mapName = mat.name;

					if (m_NamingConvention != NamingConvention.PreserveExact && mapName.StartsWith("M_"))
					{
						if (mapName.StartsWith("M_"))
						{
							mapName = mapName.Substring(2);
						}
					}

					mapName = mapName.ToNamingConvention(m_NamingConvention);

					// Replace spaces with underscores
					mapName = mapName.Sanitise(new() { ' ' }, '_');

					// Prepend group name
					mapName = mg.Name + "/" + mapName;

					// Already seen, and this is not a .mat - skip
					if (m_MapNameToMaterialPath.ContainsValue(mapName) && !materialPath.EndsWith(".mat"))
					{
						continue;
					}

					m_MapNameToMaterialPath[mapName] = materialPath;
				}
			}
		}
#endif
	}
}