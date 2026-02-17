//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

#if ADDRESSABLES_INSTALLED

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble.Editor
{
	public static class TrembleSyncAddressables
	{
		private const string GROUP_TREMBLE_DATA = "Tremble Data";
		private const string GROUP_TREMBLE_PREFABS = "Tremble Prefabs";
		private const string GROUP_TREMBLE_MATERIALS = "Tremble Materials";

		public static async Task SetupAddressables(TrembleSyncSettings settings, MaterialNameLookup materialNameLookup, List<string> allMaterialPaths, PrefabNameLookup prefabNameLookup, List<string> allPrefabPaths, List<Type> allAssetTypes)
		{
			using TrembleTimerScope _ = new(TrembleTimer.Context.ExportAddressableDataForStandalone);

			AddressableAssetSettings addressables = AddressableAssetSettingsDefaultObject.GetSettings(true);
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			// Data/scriptable objects
			AddressableAssetGroup dataGroup = addressables.FindOrCreateGroup(GROUP_TREMBLE_DATA);
			addressables.AddEntryIfNotExist(dataGroup, settings, TrembleSyncSettings.ADDRESSABLE_KEY);

			foreach (string assetPath in allAssetTypes.SelectMany(t => TrembleAssetLoader.FindAssetPaths(t)))
			{
				addressables.AddEntryIfNotExist(dataGroup, assetPath);
			}

			// Prefabs
			AddressableAssetGroup prefabsGroup = addressables.FindOrCreateGroup(GROUP_TREMBLE_PREFABS);
			foreach (string prefabPath in allPrefabPaths)
			{
				string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
				addressables.AddEntryIfNotExist(prefabsGroup, prefabPath, prefabName);
			}

			// Materials
			AddressableAssetGroup materialsGroup = addressables.FindOrCreateGroup(GROUP_TREMBLE_MATERIALS);
			addressables.AddEntryIfNotExist(materialsGroup, TrembleAssetLoader.LoadAssetByName<Material>("M_NullRender"));
			foreach (string materialPath in allMaterialPaths)
			{
				if (!materialNameLookup.TryGetMapNameFromMaterialPath(materialPath, out string mapName))
					continue;

				addressables.AddEntryIfNotExist(materialsGroup, materialPath, mapName);
			}

			// Add in any unexpected addressables
			List<AssetMetadata> assets = new(syncSettings.AssetMetadatas);

			List<AddressableAssetEntry> trembleEntries = new(1024);
			addressables.GetAllAssets(trembleEntries, false, entryFilter: (e) => e.labels.Contains("Tremble"));

			foreach (AddressableAssetEntry entry in trembleEntries)
			{
				if (syncSettings.AssetMetadatas.Any(am => am.AddressableName == entry.address))
					continue;

				if (entry.MainAssetType == typeof(TrembleSyncSettings))
					continue;
				
				assets.Add(new()
				{
					AddressableName = entry.address,
					MapName = entry.address,
					Path = entry.AssetPath,
					
					FullTypeName = entry.MainAssetType.FullName,
				});
			}

			// Write out resources
			syncSettings.AssetMetadatas = assets.ToArray();
			EditorUtility.SetDirty(syncSettings);
			AssetDatabase.SaveAssetIfDirty(syncSettings);

			await PackedResourceUtil.PackFolderIntoResource(TrembleConsts.BASEQ3_PATH, TrembleRuntimeAPI.TREMBLE_BASEQ3_RESOURCE);
			await PackedResourceUtil.PackFolderIntoResource(TrenchBroomUtil.GetGameFolder(), TrembleRuntimeAPI.TREMBLE_GAME_RESOURCE);

			AssetDatabase.Refresh();
		}

		public static AddressableAssetEntry GetExistingEntryForPath(string assetPath)
		{
			AddressableAssetSettings addressables = AddressableAssetSettingsDefaultObject.GetSettings(true);

			string assetGuid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();
			return addressables.FindAssetEntry(assetGuid, true);
		}

		private static AddressableAssetGroup FindOrCreateGroup(this AddressableAssetSettings settings, string name, bool clear = false)
		{
			AddressableAssetGroup matchingGroup = settings.FindGroup(name);
			if (matchingGroup)
			{
				if (clear)
				{
					List<AddressableAssetEntry> entries = new(matchingGroup.entries);
					foreach (AddressableAssetEntry entry in entries)
					{
						matchingGroup.RemoveAssetEntry(entry);
					}
				}

				return matchingGroup;
			}

			return settings.CreateGroup(name, false, false, true, settings.DefaultGroup.Schemas, settings.DefaultGroup.SchemaTypes.ToArray());
		}

		private static AddressableAssetEntry AddEntryIfNotExist(this AddressableAssetSettings settings, AddressableAssetGroup group, string newObjectPath, string address = null)
			=> AddEntryIfNotExist(settings, group, AssetDatabase.LoadAssetAtPath<Object>(newObjectPath), address);

		private static AddressableAssetEntry AddEntryIfNotExist(this AddressableAssetSettings settings, AddressableAssetGroup group, Object newObject, string address = null)
		{
			if (!newObject)
			{
				Debug.LogWarning("Could not add an Addressable entry for a NULL object!");
				return null;
			}

			string assetPath = AssetDatabase.GetAssetPath(newObject);
			string guid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();

			if (assetPath.IsNullOrEmpty())
			{
				Debug.LogWarning($"Could not add an Addressable entry for {newObject.name} - could not find path!");
				return null;
			}

			AddressableAssetEntry entry = GetExistingEntryForPath(assetPath);

			if (entry == null)
			{
				entry = settings.CreateOrMoveEntry(guid, group);
				entry.SetAddress(address != null ? address.Sanitise(new() { '[', ']' }) : newObject.name);
			}

			entry.SetLabel("Tremble", true, force: true);

			return entry;
		}
	}
}
#endif