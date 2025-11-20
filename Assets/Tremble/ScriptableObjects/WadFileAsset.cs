//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble
{
	public class WadFileAsset : ScriptableObject
	{
		[SerializeField] private string[] m_MaterialNames;
		public string[] MaterialNames => m_MaterialNames;
	}
}