//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Text.RegularExpressions;

namespace TinyGoose.Tremble
{
	public static class SimpleRegexUtil
	{
		public static Regex ToSimpleRegex(this string pattern)
		{
			// This is overly simplistic. But essentially, changes a pattern like
			// "my*thing" into a Regex like "^my.+thing$"
			return new($"^{pattern.Replace("*", ".+")}$");
		}
	}
}