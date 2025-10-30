// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[CustomPropertyDrawer(typeof(MapProcessorClass), useForChildren: true)]
	public class MapProcessorClassDrawer : BaseClassPickerDrawer<MapProcessorBase>
	{
		protected override string ClassNamePropertyName => "m_MapClassName";
	}
}
