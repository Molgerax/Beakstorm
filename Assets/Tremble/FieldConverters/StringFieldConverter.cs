//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[TrembleFieldConverter(typeof(string))]
	public class StringFieldConverter : TrembleFieldConverter<string>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out string value)
		{
			return entity.TryGetString(key, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out string[] values)
		{
			if (!entity.TryGetString(key, out string stringValues))
			{
				values = default;
				return false;
			}

			values = stringValues.Split(',').ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, string defaultValue, MemberInfo target)
		{
			target.GetCustomAttributes(
				out TooltipAttribute tooltip
			);

			entityClass.AddField(new FgdStringField
			{
				Name = fieldName,
				Description = tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}",
				DefaultValue = defaultValue
			});
		}
	}
}