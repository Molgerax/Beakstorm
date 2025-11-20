//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public static class MaterialGroupUtil
	{
		public static bool RemoveMaterialsFromGroups(Material[] materials)
			=> MoveMaterialsToGroupIdx(materials, -1);

		public static bool MoveMaterialsToGroup(Material[] materials, string groupName, bool createIfNotExist = false)
		{
			int index = GetMaterialGroupIndex(groupName);
			if (index == -1 && !createIfNotExist)
			{
				Debug.LogError($"Could not move materials to non-existent group '{groupName}'!");
				return false;
			}

			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			// New group - create and add
			if (index == -1)
			{
				syncSettings.Modify(so =>
				{
					SerializedProperty materialGroupsArray = so.FindBackedProperty(nameof(TrembleSyncSettings.MaterialGroups));
					materialGroupsArray.AppendArrayElement();
				});

				MaterialGroup[] mgs = syncSettings.MaterialGroups;
				mgs[^1].Name = groupName;
				mgs[^1].Materials = Array.Empty<Material>();

				index = mgs.Length - 1;
			}

			return MoveMaterialsToGroupIdx(materials, index);
		}

		public static int GetMaterialGroupIndexForMaterial(Material material)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			for (int materialGroupIdx = 0; materialGroupIdx < syncSettings.MaterialGroups.Length; materialGroupIdx++)
			{
				MaterialGroup mg = syncSettings.MaterialGroups[materialGroupIdx];

				foreach (Material m in mg.Materials)
				{
					if (material == m)
						return materialGroupIdx;
				}
			}

			return -1;
		}

		public static bool IsMaterialPartOfAnyMaterialGroup(Material material)
			=> GetMaterialGroupIndexForMaterial(material) != -1;

		private static bool MoveMaterialsToGroupIdx(Material[] materials, int index)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			MaterialGroup[] mgs = syncSettings.MaterialGroups;

			for (int groupIdx = 0; groupIdx < mgs.Length; groupIdx++)
			{
				if (groupIdx == index)
				{
					// Our target group - remove these mats, and add to end
					mgs[groupIdx].Materials = mgs[groupIdx].Materials
						.Where(m => !materials.Contains(m))
						.Concat(materials)
						.ToArray();
				}
				else
				{
					// Not our target group - remove these mats
					mgs[groupIdx].Materials = mgs[groupIdx].Materials
						.Where(m => !materials.Contains(m))
						.ToArray();
				}
			}

			CleanupMissingMaterials(mgs);
			CleanupEmptyGroups(mgs);

			return true;
		}

		private static void CleanupMissingMaterials(MaterialGroup[] mgs)
		{
			for (int groupIdx = 0; groupIdx < mgs.Length; groupIdx++)
			{
				mgs[groupIdx].Materials = mgs[groupIdx].Materials
					.Where(m => m)
					.ToArray();
			}
		}

		private static void CleanupEmptyGroups(MaterialGroup[] mgs)
		{
			// Now check for empty groups
			List<int> removeIndexes = new();
			for (int groupIdx = 0; groupIdx < mgs.Length; groupIdx++)
			{
				if (mgs[groupIdx].Materials.Length != 0)
					continue;

				bool remove = EditorUtility.DisplayDialog("Empty Material Group", $"Material Group '{mgs[groupIdx].Name}' is empty - delete it?", "Yup", "No - keep it!");
				if (!remove)
					continue;

				removeIndexes.Add(groupIdx);
			}

			int numRemoved = 0;
			foreach (int idx in removeIndexes)
			{
				int removeIdx = idx - numRemoved;
				numRemoved++;

				TrembleSyncSettings.Get().Modify(so =>
				{
					SerializedProperty groupsProp = so.FindBackedProperty(nameof(TrembleSyncSettings.MaterialGroups));
					groupsProp.DeleteArrayElementAtIndex(removeIdx);
				});
			}
		}


		private static int GetMaterialGroupIndex(string groupName)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			for (int mgIdx = 0; mgIdx < syncSettings.MaterialGroups.Length; mgIdx++)
			{
				if (syncSettings.MaterialGroups[mgIdx].Name.EqualsInvariant(groupName))
					return mgIdx;
			}

			return -1;
		}
	}
}