// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class WorldspawnScript
	{
		private static readonly int DEFAULT_TYPE_HASHCODE = typeof(Worldspawn).FullName?.GetHashCode() ?? 0;
		private static readonly int EMPTY_STRING_HASHCODE = "".GetHashCode();

		public WorldspawnScript(string scriptName = null)
		{
			m_ScriptName = scriptName;
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Serialised
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField] private string m_ScriptName;

		public string ScriptName => m_ScriptName;
		public Type Class => TypeDatabase.GetType(m_ScriptName);

		public override int GetHashCode()
		{
			int hash = m_ScriptName.GetHashCode();
			return hash == DEFAULT_TYPE_HASHCODE ? EMPTY_STRING_HASHCODE : hash;
		}
	}
}