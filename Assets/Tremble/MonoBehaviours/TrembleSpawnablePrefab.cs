// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Add this component to an existing Prefab which doesn't contain a
	/// MonoBehaviour to allow it to be placed in TrenchBroom easily. 
	/// </summary>
	[PrefabEntity]
	public sealed class TrembleSpawnablePrefab : MonoBehaviour
	{
		[SerializeField, NoTremble] private string m_Category;
		[SerializeField, NoTremble] private Texture2D m_OverrideSprite;
		[SerializeField, NoTremble] private bool m_OnlyVariants;

		public string Category => m_Category;
		public Texture2D OverrideSprite => m_OverrideSprite;
		public bool OnlyVariants => m_OnlyVariants;
	}
}