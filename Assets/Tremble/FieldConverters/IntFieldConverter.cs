// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[TrembleFieldConverter(typeof(int))]
	public class IntFieldConverter : TrembleFieldConverter<int>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out int value)
		{
			return entity.TryGetInt(key, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out int[] values)
		{
			if (!entity.TryGetString(key, out string stringValues))
			{
				values = default;
				return false;
			}

			values = stringValues
				.Split(',')
				.Select(s => int.TryParse(s, out int result) ? result : default)
				.ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, int defaultValue, MemberInfo target)
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

			entityClass.AddField(new FgdIntegerField
			{
				Name = fieldName,
				Description = (tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}") + rangeHint,
				DefaultValue = defaultValue
			});
		}
	}
}