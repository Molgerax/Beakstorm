//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Marks this MonoBehaviour as being usable as a Point entity in TrenchBroom, using one or more prefabs in the project.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PrefabEntityAttribute : EntityAttributeBase
	{
		/// <summary>
		/// A Tremble-compatible Prefab entity.
		/// </summary>
		/// <param name="trenchBroomName">The name in TrenchBroom. e.g. "P_MyPrefab".</param>
		/// <param name="category">The category for TrenchBroom. e.g. "game".</param>
		/// <param name="excludePrefabs">Which prefabs containing this script to exclude. e.g. "P_BaseCoinPrefab".</param>
		/// <param name="onlyIncludePrefabs">Which prefabs containing this script to include. e.g. "P_TrembleCoinPrefab".</param>
		/// <param name="excludePrefab">Which prefabs containing this script to exclude. e.g. "P_BasePrefab".</param>
		/// <param name="onlyIncludePrefab">Which prefabs containing this script to include. e.g. "P_TrembleCoinPrefab".</param>
		/// <param name="onlyVariants">Only export variants, not root prefabs</param>
		public PrefabEntityAttribute(string trenchBroomName = null, string category = null, string[] excludePrefabs = null, string[] onlyIncludePrefabs = null, string excludePrefab = null, string onlyIncludePrefab = null, bool onlyVariants = false, string overrideSprite = null)
			: base(trenchBroomName: trenchBroomName, category: category)
		{
			m_ExcludedPrefabNames = excludePrefab != null ? new [] { excludePrefab } : excludePrefabs;
			m_OnlyIncludedPrefabNames = onlyIncludePrefab != null ? new [] { onlyIncludePrefab } : onlyIncludePrefabs;

			m_OverrideSprite = overrideSprite;
			m_OnlyVariants = onlyVariants;
		}

		private readonly string[] m_ExcludedPrefabNames;
		private readonly string[] m_OnlyIncludedPrefabNames;

		private readonly string m_OverrideSprite;
		private readonly bool m_OnlyVariants;

		public bool IsPrefabIncluded(string prefabName)
		{
			// Is excluded explicitly?
			if (m_ExcludedPrefabNames != null)
			{
				foreach (string excluded in m_ExcludedPrefabNames)
				{
					if (excluded.ToSimpleRegex().IsMatch(prefabName))
						return true;
				}
			}

			// Include everything?
			if (m_OnlyIncludedPrefabNames == null)
				return true;

			// See if it's on the whitelist
			foreach (string included in m_OnlyIncludedPrefabNames)
			{
				if (included.ToSimpleRegex().IsMatch(prefabName))
					return true;
			}

			return false;
		}

		public string OverrideSprite => m_OverrideSprite;
		public bool OnlyVariants => m_OnlyVariants;
	}
}