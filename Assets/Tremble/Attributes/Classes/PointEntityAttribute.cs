//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEngine;

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Marks this MonoBehaviour as a component which can be added to empty GameObjects as a Point entity.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PointEntityAttribute : EntityAttributeBase
	{
		/// <summary>
		/// A Tremble-compatible Point entity.
		/// </summary>
		/// <param name="trenchBroomName">The name in TrenchBroom. e.g. "cool_entity".</param>
		/// <param name="category">The category for TrenchBroom. e.g. "ent".</param>
		/// <param name="colour">The colour of the entity in TrenchBroom. e.g. green is "0.0 1.0 0.0".</param>
		/// <param name="sprite">The sprite to show in TrenchBroom. Must be the name of a texture in your project. e.g. "T_MyTexture".</param>
		/// <param name="size">The size of the cube for this entity in TrenchBroom. e.g. 5.</param>
		/// <param name="prefab">The prefab to show in TrenchBroom. Must be the name of a prefab in your project. e.g. "T_MyPrefab".</param>
		public PointEntityAttribute(string trenchBroomName = null, string category = null, string colour = null, string sprite = null, float size = 0.25f, string prefab = null)
			: base(trenchBroomName, category)
		{
			m_Size = size;
			m_Sprite = sprite;
			m_Colour = colour.TryParseQ3Colour(out Color value) ? value : Color.white;
			m_Prefab = prefab;
		}

		private readonly Color? m_Colour;
		private readonly string m_Sprite;
		private readonly float m_Size;
		private readonly string m_Prefab;

		public Color? Colour => m_Colour;
		public string Sprite => m_Sprite;
		public float Size => m_Size;
		public string Prefab => m_Prefab;
	}
}