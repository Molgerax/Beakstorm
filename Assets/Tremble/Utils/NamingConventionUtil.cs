//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Text;

namespace TinyGoose.Tremble
{
	public static class NamingConventionUtil
	{
		public static string ToNamingConvention(this string input, NamingConvention convention)
		{
			switch (convention)
			{
				case NamingConvention.SnakeCase:
					return input.ToSnakeCase();
				case NamingConvention.UpperCamelCase:
					return CapitaliseFirstLetter(input, true);
				case NamingConvention.LowerCamelCase:
					return CapitaliseFirstLetter(input, false);
				case NamingConvention.HumanFriendly:
					return ToHuman(input);
				case NamingConvention.PreserveExact:
					return input;
			}

			return input;
		}

		private static readonly StringBuilder s_SnakeBuilder = new(128);
		private static string ToSnakeCase(this string inString)
		{
			s_SnakeBuilder.Clear();

			for (int i = 0; i < inString.Length; i++)
			{
				if (inString[i] == ' ')
				{
					s_SnakeBuilder.Append('_');
					continue;
				}

				if (Char.IsUpper(inString[i]) && i > 0)
				{
					// Not if we just output an underscore!
					if (s_SnakeBuilder[^1] != '_')
					{
						// If the previous character wasn't uppercase
						if (!Char.IsUpper(inString[i - 1]))
						{
							s_SnakeBuilder.Append('_');
						}
						// Or the next character isn't uppercase
						else if (i < inString.Length - 1 && Char.IsLower(inString[i + 1]))
						{
							s_SnakeBuilder.Append('_');
						}
					}
				}

				s_SnakeBuilder.Append(Char.ToLowerInvariant(inString[i]));
			}
			return s_SnakeBuilder.ToString();
		}

		private static string ToHuman(this string inString)
		{
			s_SnakeBuilder.Clear();

			int firstIndex = inString.IndexOf('_') + 1;

			for (int i = firstIndex; i < inString.Length; i++)
			{
				// Straight up convert underscores to spaces
				if (inString[i] == '_')
				{
					s_SnakeBuilder.Append(' ');
					continue;
				}

				if (Char.IsUpper(inString[i]) && i > firstIndex)
				{
					// Not if we just output a space!
					if (s_SnakeBuilder[^1] != ' ' && s_SnakeBuilder[^1] != '_')
					{
						// If the previous character wasn't uppercase
						if (!Char.IsUpper(inString[i - 1]))
						{
							s_SnakeBuilder.Append(' ');
						}
						// Or the next character isn't uppercase
						else if (i < inString.Length - 1 && Char.IsLower(inString[i + 1]))
						{
							s_SnakeBuilder.Append(' ');
						}
					}
				}

				if (i == firstIndex || s_SnakeBuilder[^1] == ' ')
				{
					s_SnakeBuilder.Append(Char.ToUpperInvariant(inString[i]));
				}
				else
				{
					s_SnakeBuilder.Append(Char.ToLowerInvariant(inString[i]));
				}
			}
			return s_SnakeBuilder.ToString();
		}

		public static string FromHuman(ReadOnlySpan<char> inString)
		{
			s_SnakeBuilder.Clear();

         int inBrackets = 0;

         for (int i = 0; i < inString.Length; i++)
         {
         	if (inString[i] == '(') { inBrackets++; continue; }
         	if (inString[i] == ')') { inBrackets--; continue; }


         	if (!Char.IsLetter(inString[i]) && !Char.IsNumber(inString[i]) && inString[i] != '_')
         		continue;

         	if (inBrackets > 0)
         		continue;

         	if (i == 0 || inString[i - 1] == ' ')
         	{
         		s_SnakeBuilder.Append(Char.ToUpperInvariant(inString[i]));
         	}
         	else
         	{
         		s_SnakeBuilder.Append(inString[i]);
         	}
         }

         return s_SnakeBuilder.ToString().Trim();
		}
		public static string FromHuman(string inString) => FromHuman(inString.AsSpan());

		private static string CapitaliseFirstLetter(string input, bool capitalise)
		{
			if (input.IsNullOrEmpty())
				return "";

			if (input.Length == 1)
				return capitalise ? input.ToUpperInvariant() : input.ToLowerInvariant();

			return (capitalise ? Char.ToUpperInvariant(input[0]) : Char.ToLowerInvariant(input[0]))
				+ input.Substring(1);
		}
	}
}