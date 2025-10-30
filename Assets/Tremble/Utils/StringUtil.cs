//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace TinyGoose.Tremble
{
	public static class StringUtil
	{
		public static bool Split(this string input, char splitter, out string first, out string rest) 
			=> Split(input.AsSpan(), splitter, out first, out rest);

		public static bool Split(this ReadOnlySpan<char> input, char splitter, out string first, out string rest)
		{
			bool insideQuotes = false;
			int index = -1;
			for (int c = 0; c < input.Length; c++)
			{
				if (input[c] == '"')
				{
					insideQuotes = !insideQuotes;
				}

				if (insideQuotes || input[c] != splitter)
					continue;
				
				index = c;
				break;
			}
			
			if (index == -1)
			{
				first = null;
				rest = input.ToString();
				return false;
			}

			first = input[..index].ToString().Trim();
			rest = input[(index + 1)..].ToString().Trim();

			return true;
		}

		public static bool IsNullOrEmpty(this string input) => string.IsNullOrEmpty(input);

		public static bool EqualsInvariant(this string input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.Equals(other, compareType);
		}
		public static bool ContainsInvariant(this string input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.Contains(other, compareType);
		}

		public static bool StartsWithInvariant(this string input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.StartsWith(other, compareType);
		}
		public static bool EndsWithInvariant(this string input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.EndsWith(other, compareType);
		}
		public static int IndexOfInvariant(this string input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.IndexOf(other, compareType);
		}
		public static int LastIndexOfInvariant(this string input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.LastIndexOf(other, compareType);
		}

		public static bool EqualsInvariant(this ReadOnlySpan<char> input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.Equals( other, compareType);
		}
		public static bool ContainsInvariant(this ReadOnlySpan<char> input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.Contains( other, compareType);
		}

		public static bool StartsWithInvariant(this ReadOnlySpan<char> input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.StartsWith( other, compareType);
		}
		public static bool EndsWithInvariant(this ReadOnlySpan<char> input, string other, bool caseSensitive = false)
		{
			StringComparison compareType = caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
			return input.EndsWith( other, compareType);
		}

		private static readonly HashSet<char> DEFAULT_ILLEGAL_CHARS = new() { '{', '}', ' ', '[', ']', '"', '\'', '.', ',' };
		public static string Sanitise(this string input, HashSet<char> illegalCharacters = null, char? replacement = null)
		{
			if (input == null)
				return null;

			illegalCharacters ??= DEFAULT_ILLEGAL_CHARS;

			StringBuilder newName = new(input.Length);
			foreach (char c in input)
			{
				if (illegalCharacters.Contains(c))
				{
					if (replacement.HasValue)
					{
						newName.Append(replacement.Value);
					}

					continue;
				}

				newName.Append(c);
			}

			return newName.ToString();
		}
	}
}