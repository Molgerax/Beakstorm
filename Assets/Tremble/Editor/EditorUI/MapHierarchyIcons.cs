//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[InitializeOnLoad]
	public static class MapHierarchyIcons
	{
		static MapHierarchyIcons()
		{
			EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItem;
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItem;
		}
		public static void ResetModifiedTimes() => s_CachedModifiedTimes.Clear();

		private static readonly Dictionary<GameObject, ulong> s_CachedModifiedTimes = new();

		private static void OnHierarchyItem(int instanceID, Rect selectionRect)
		{
			Rect r = new(selectionRect);
			r.x -= 28;
			r.width = 20;

			GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (!go)
				return;

			bool areDependenciesUpToDate = TrembleSyncSettings.Get().AutomaticallyReimportWhenDependencyChanges;

			if (go.TryGetComponent(out MapDocument thisMap))
			{
				if (!areDependenciesUpToDate)
				{
					ulong maxTimestamp = 0ul;
					List<string> outOfDate = new(512);

					for (int childIdx = 0; childIdx < thisMap.transform.childCount; childIdx++)
					{
						Transform child = thisMap.transform.GetChild(childIdx);
						if (!IsGameObjectOutOfDate(thisMap, child.gameObject, out ulong time))
							continue;

						outOfDate.Add(child.name);
						maxTimestamp = Math.Max(time, maxTimestamp);
					}

					if (outOfDate.Count > 0)
					{
						const string PREFIX = "\n * ";
						string oodPrefabs = String.Join(PREFIX, outOfDate);
						GUI.Label(r, MakeWarningIcon($"Map may require reimport ({FormatTimespanSince(maxTimestamp)} out of date)! Possible out of date entities are:{PREFIX}{oodPrefabs}"));
					}
				}

				// Edit button
				Rect buttonRect = new(selectionRect);
				buttonRect.width = 62;
				buttonRect.x = selectionRect.width;

				if (GUI.Button(buttonRect, "edit map", EditorStyles.miniButton))
				{
					string mapPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(thisMap.OriginalAssetGuid));
					TrenchBroomUtil.OpenWithTrenchBroom(mapPath);
				}
			}
			else if (!areDependenciesUpToDate)
			{
				MapDocument parentMap = go.GetComponentInParent<MapDocument>();
				if (!parentMap)
					return;

				if (!IsGameObjectOutOfDate(parentMap, go, out ulong timestamp))
					return;

				string tooltip = $"This entity may be using an older version of its prefab, from {FormatTimespanSince(timestamp)} ago - reimport the map!";
				GUI.Label(r, MakeWarningIcon(tooltip));
			}
		}

		private static GUIContent MakeWarningIcon(string tooltip) => new(EditorGUIUtility.IconContent("console.warnicon.sml").image, tooltip);

		private static bool IsGameObjectOutOfDate(MapDocument map, GameObject objectInstance, out ulong timestamp)
		{
			foreach (Transform child in objectInstance.transform)
			{
				if (IsGameObjectOutOfDate(map, child.gameObject, out timestamp))
					return true;
			}

			GameObject prefab = objectInstance;
			while (prefab)
			{
				prefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab);

				if (!prefab)
				{
					timestamp = 0ul;
					return false;
				}

				ulong mapTime = map.MapImportedTime;

				if (!s_CachedModifiedTimes.TryGetValue(prefab, out ulong time))
				{
					time = ModTimeDB.CalcModelModifiedTime(prefab);
					s_CachedModifiedTimes[prefab] = time;
				}

				if (mapTime < time)
				{
					timestamp = time;
					return true;
				}
			}

			timestamp = 0ul;
			return false;
		}

		private static string FormatTimespanSince(ulong timestamp)
		{
			DateTime fileTime = new DateTime((long)timestamp).ToLocalTime();
			DateTime now = DateTime.Now;

			TimeSpan span = now - fileTime;
			if (span.TotalDays >= 1.0)
				return $"{(int)span.TotalDays} day(s)";

			if (span.TotalHours >= 1.0)
				return $"{(int)span.TotalHours} hour(s)";

			if (span.TotalMinutes >= 1.0)
				return $"{(int)span.TotalMinutes} min(s)";

			return "mere moments";
		}
	}
}