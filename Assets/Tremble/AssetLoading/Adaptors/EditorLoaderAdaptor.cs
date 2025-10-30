//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR

namespace TinyGoose.Tremble
{
	public class EditorLoaderAdaptor : IAssetLoaderAdaptor
	{
		public bool SupportsPreloading => false;

		private static readonly Dictionary<string, string[]> s_CachedQueryResults = new(1024);

		public string[] FindAssetPaths(Type type, string name = "")
		{
			string typeString = type == typeof(GameObject) ? "prefab" : type.Name;
			string fullQuery = $"t:{typeString} {name}";

			if (!s_CachedQueryResults.TryGetValue(fullQuery, out string[] results))
			{
				results = AssetDatabase
					.FindAssets(fullQuery, new[] { "Assets" })
					.Select(AssetDatabase.GUIDToAssetPath)
					.ToArray();

				if (results.Length > 0)
				{
					s_CachedQueryResults[fullQuery] = results;
				}
			}

			return results;
		}

		public TAsset LoadAssetByPath<TAsset>(string path) where TAsset : Object
			=> AssetDatabase.LoadAssetAtPath<TAsset>(path);

		public Task PreloadAssetsByPaths(IList<string> paths)
		{
			// We don't need to preload in Editor ;)
			return Task.CompletedTask;
		}

		public async Task PreloadAssetsByPathsAsync(IList<string> paths)
		{
			// We don't need to preload in Editor ;)
			await Task.CompletedTask;
		}

		public string GetPath(Object obj) => AssetDatabase.GetAssetPath(obj);


		public TrembleSyncSettings GetSyncSettings() => TrembleSyncSettings.Get();

		public Material LoadClipMaterial()
		{
			string clipPath = Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Materials", "M_NullRender.mat");
			return LoadAssetByPath<Material>(clipPath);
		}

		public TrembleSyncSettings LoadSyncSettings()
		{
			string[] paths = FindAssetPaths(typeof(TrembleSyncSettings));

			switch (paths.Length)
			{
				case 0:
					return null;

				case >1:
					Debug.LogError("More than one Tremble Sync Settings file found!");
					foreach (string path in paths)
					{
						Debug.LogWarning($"Found at: {path}", AssetDatabase.LoadAssetAtPath<TrembleSyncSettings>(path));
					}
					Debug.LogError("There should be only one! Picking one arbitrarily for now.");

					goto default;
				default:
					return AssetDatabase.LoadAssetAtPath<TrembleSyncSettings>(paths[0]);
			}
		}

		public void ReleaseAll() { }


		public static void DropCachedAssetPaths()
		{
			s_CachedQueryResults.Clear();
		}
	}
}

#endif