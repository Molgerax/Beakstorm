//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[AttributeUsage(AttributeTargets.Class)]
	public abstract class EntityAttributeBase : SelectionBaseAttribute
	{
		protected EntityAttributeBase(string trenchBroomName = null, string category = null)
		{
			m_TrenchBroomName = trenchBroomName.Sanitise();
			m_Category = category.Sanitise();
		}

		private readonly string m_TrenchBroomName;
		private readonly string m_Category;

		public string TrenchBroomName => m_TrenchBroomName;
		public string Category => m_Category;
	}
}