//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;

namespace TinyGoose.Tremble.Editor
{
	public record ManualEntry
	{
		public string Title { get; init; }
		public string PagePath { get; init; }
		public bool ShowInTree { get; init; } = true;

		public List<ManualEntry> Children { get; } = new();

		public ManualEntry FindEntryByPath_Recursive(string path)
		{
			if (PagePath != null && PagePath.EqualsInvariant(path))
				return this;

			foreach (ManualEntry child in Children)
			{
				ManualEntry result = child.FindEntryByPath_Recursive(path);
				if (result != null)
					return result;
			}

			return null;
		}
	}
}