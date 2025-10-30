//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public enum CheckerboardStyle
	{
		None,
		Light,
		Dark,
	}

	public enum BrushType
	{
		// Solid brush with a texture (default)
		Solid,

		// Liquid brush (trigger collision) with a texture
		Liquid,

		// An invisible brush which is a Unity trigger
		Trigger,

		// An invisible (but solid) brush
		Invisible
	}

	/// <summary>
	/// Marks this MonoBehaviour as a component which can be added to arbitrary Brush entities from a map.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class BrushEntityAttribute : EntityAttributeBase
	{
		/// <summary>
		/// A Tremble-compatible Brush entity.
		/// </summary>
		/// <param name="trenchBroomName">The name in TrenchBroom. e.g. "cool_entity".</param>
		/// <param name="category">The category for TrenchBroom. e.g. "ent".</param>
		/// <param name="type">The brush type. e.g. Trigger, Solid, Liquid.</param>
		/// <param name="colour">The colour of this brush's texture.</param>
		/// <param name="checkerStyle">The type of checkerboard for this brush's texture.</param>
		/// <param name="layer">The layer name to put this brush on.</param>
		public BrushEntityAttribute(
				string trenchBroomName = null,
				string category = null,
            BrushType type = BrushType.Solid,
            string colour = "",
            CheckerboardStyle checkerStyle = CheckerboardStyle.None,
				string layer = "")
			: base(trenchBroomName, category)
		{
			m_BrushType = type;
			m_Colour = colour.TryParseQ3Colour(out Color value) ? value : null;
			m_CheckerStyle = checkerStyle;
			m_LayerName = layer;
		}

		private readonly BrushType m_BrushType;
		private readonly Color? m_Colour;
		private readonly CheckerboardStyle m_CheckerStyle;
		private readonly string m_LayerName;

		public BrushType BrushType => m_BrushType;
		public Color? Colour => m_Colour;
		public CheckerboardStyle CheckerStyle => m_CheckerStyle;
		public string LayerName => m_LayerName;
	}
}