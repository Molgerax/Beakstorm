//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Marks a field as being serialised for maps (even if [SerializeField] is absent)
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class TrembleAttribute : Attribute
	{
		public TrembleAttribute(string name = null, bool isRotation = false)
		{
			//NOTE: Change MapBspGetters if this changes!!
			m_OverrideName = name;
			m_IsRotation = isRotation;
		}

		private readonly string m_OverrideName;
		private readonly bool m_IsRotation;

		public string OverrideName => m_OverrideName;
		public bool IsRotation => m_IsRotation;
	}


	/// <summary>
	/// Marks a field as NEVER being serialised for maps (even if [SerializeField] is on)
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class NoTrembleAttribute : Attribute
	{
		//NOTE: Change MapBspGetters if this changes!!
	}
}