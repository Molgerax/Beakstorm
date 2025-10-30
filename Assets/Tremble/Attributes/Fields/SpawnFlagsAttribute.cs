//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Marks this boolean as being a spawnflags field.
	/// You can either provide an explicit bit to use, or omit and let the system guess based on order. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SpawnFlagsAttribute : Attribute
	{
		public SpawnFlagsAttribute(int bit = -1)
		{
			//NOTE: Change MapBspGetters if this changes!!
			m_Bit = bit;
		}

		private readonly int m_Bit;
		public int Bit => m_Bit;
	}
	
	/// <summary>
	/// Marks this boolean as never being a spawnflags field.
	/// Use this is you have the auto-spawnflags flag set for booleans. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class NoSpawnFlagsAttribute : Attribute
	{
		//NOTE: Change MapBspGetters if this changes!!
	}
}