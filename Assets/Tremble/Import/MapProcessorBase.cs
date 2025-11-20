//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;
using UnityEngine.Scripting;

namespace TinyGoose.Tremble
{
	public abstract class MapProcessorBase
	{
		[Preserve]
		public static string Description => null;

		private Transform m_RootTransform;
		public Transform RootTransform
		{
			get => m_RootTransform;
			set => m_RootTransform = value;
		}

		public virtual void ProcessWorldSpawnProperties(MapBsp mapBsp, BspEntity entity, GameObject rootGameObject) { }
		public virtual void ProcessPrefabEntity(MapBsp mapBsp, BspEntity entity, GameObject prefab) { }
		public virtual void ProcessBrushEntity(MapBsp mapBsp, BspEntity entity, GameObject brush) { }
		public virtual void ProcessPointEntity(MapBsp mapBsp, BspEntity entity, GameObject point) { }

		public virtual void OnProcessingStarted(GameObject root, MapBsp mapBsp) { }
		public virtual void OnProcessingCompleted(GameObject root, MapBsp mapBsp) { }


		public TWorldspawn GetWorldspawn<TWorldspawn>() where TWorldspawn : Worldspawn
			=> m_RootTransform.GetComponentInChildren<TWorldspawn>();

		public GameObject InstantiatePrefab(GameObject prefab, Transform parent = null)
			=> PrefabUtil.InstantiatePrefab(prefab, parent ?? m_RootTransform);
	}
}