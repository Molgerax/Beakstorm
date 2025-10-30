//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public static class PrefabUtil
	{
		public static GameObject InstantiatePrefab(GameObject prefab, Transform parent = null)
		{
#if UNITY_EDITOR
			return Application.isPlaying
				? GameObject.Instantiate(prefab, parent)
				: (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#else
			return GameObject.Instantiate(prefab, parent);
#endif
		}
	}
}