//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[TrembleFieldConverter(typeof(float))]
	public class FloatFieldConverter : TrembleFieldConverter<float>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out float value)
		{
			return entity.TryGetFloat(key, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out float[] values)
		{
			if (!entity.TryGetString(key, out string stringValues))
			{
				values = default;
				return false;
			}

			values = stringValues
				.Split(',')
				.Select(s => s.TryParseFloat(out float result) ? result : default)
				.ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, float defaultValue, MemberInfo target)
		{
			target.GetCustomAttributes(
				out TooltipAttribute tooltip,
				out RangeAttribute range
			);

			// Read range hint, if available
			string rangeHint = "";
			if (range != null)
			{
				rangeHint = $" (valid range: {range.min} - {range.max})";
			}

			entityClass.AddField(new FgdFloatField
			{
				Name = fieldName,
				Description = (tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}") + rangeHint,
				DefaultValue = defaultValue
			});
		}
	}
}