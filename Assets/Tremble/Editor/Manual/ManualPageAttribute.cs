//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEngine.Scripting;

namespace TinyGoose.Tremble.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ManualPageAttribute : PreserveAttribute
	{
		public ManualPageAttribute(string path, string title = null, bool showInTree = true, bool isHomePage = false)
		{
			m_Path = path;
			m_Title = title ?? path.Split('/')[^1];
			m_ShowInTree = showInTree;
			m_IsHomePage = isHomePage;
		}

		private readonly string m_Title;
		private readonly string m_Path;
		private readonly bool m_ShowInTree;
		private readonly bool m_IsHomePage;

		public string Title => m_Title;
		public string Path => m_Path;
		public bool ShowInTree => m_ShowInTree;
		public bool IsHomePage => m_IsHomePage;
	}
}