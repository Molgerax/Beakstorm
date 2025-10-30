//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Globalization;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public static class FloatFormatUtil
	{
		private static readonly char[] s_FormatArr = { 'F', '0' };

		public static string ToStringInvariant(this float f, int decimals = 2)
		{
			if (decimals > 9)
				throw new ArgumentException("ToStringInvariant only supports up to 9 decimal places :(");

			s_FormatArr[1] = (char)('0' + decimals);
			return f.ToString(new(s_FormatArr), CultureInfo.InvariantCulture);
		}

		public static string ToStringInvariant(this Vector2 v, int decimals = 2)
			=> $"{v.x.ToStringInvariant(decimals)} {v.y.ToStringInvariant(decimals)}";

		public static string ToStringInvariant(this Vector3 v, int decimals = 2)
			=> $"{v.x.ToStringInvariant(decimals)} {v.y.ToStringInvariant(decimals)} {v.z.ToStringInvariant(decimals)}";

		public static string ToStringInvariant(this Color c, int decimals = 2)
			=> $"{c.r.ToStringInvariant(decimals)} {c.g.ToStringInvariant(decimals)} {c.b.ToStringInvariant(decimals)}";

		public static bool TryParseFloat(this string s, out float f)
			=> float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
		public static bool TryParseFloat(this ReadOnlySpan<char> s, out float f)
			=> float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
	}
}