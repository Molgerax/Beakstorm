//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public class EnumFieldConverter : TrembleFieldConverter
	{
		public override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out object value)
		{
			// Enum special case - parse as int or string
			if (entity.TryGetInt(key, out int enumIntValue))
			{
				value = enumIntValue;
				return true;
			}

			if (entity.TryGetString(key, out string enumStringValue))
			{
				if (Enum.TryParse(target.GetFieldOrPropertyTypeOrElementType(), enumStringValue, true, out object enumObjValue))
				{
					value = (int)enumObjValue;
					return true;
				}

				string entityName = entity.GetID().IsNullOrEmpty() ? entity.GetClassname() : $"'{entity.GetID()}' ({entity.GetClassname()})";
				Debug.LogWarning($"Failed to parse value '{enumStringValue}' for enum {target.GetFieldOrPropertyTypeOrElementType()} in entity {entityName}. Using default.");
			}

			value = null;
			return false;
		}

		public override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out object[] values)
		{
			if (!entity.TryGetString(key, out string commaDelimitedValue))
			{
				values = Array.Empty<object>();
				return false;
			}

			string[] parts = commaDelimitedValue.Split(',');

			values = new object[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				// Enum special case - parse as int or string
				if (int.TryParse(parts[i], out int enumIntValue))
				{
					values[i] = Enum.GetValues(target.GetFieldOrPropertyTypeOrElementType()).GetValue(enumIntValue);
				}
				else if (Enum.TryParse(target.GetFieldOrPropertyTypeOrElementType(), parts[i], true, out object enumObjValue))
				{
					values[i] = enumObjValue;
				}
				else
				{
					string entityName = entity.GetID().IsNullOrEmpty() ? entity.GetClassname() : $"'{entity.GetID()}' ({entity.GetClassname()})";
					Debug.LogWarning($"Failed to parse value '{parts[i]}' for enum {target.GetFieldOrPropertyTypeOrElementType()} in entity {entityName}. Using default.");
				}
			}

			return true;
		}

		public override void AddFieldToFgd(FgdClass entityClass, string fieldName, object defaultValue, MemberInfo target)
		{
			string tooltip = target.GetCustomAttribute<TooltipAttribute>()?.tooltip;

			entityClass.AddField(new FgdEnumField
			{
				EnumClassName = target.GetFieldOrPropertyTypeOrElementType().FullName,
				Name = fieldName,
				Description = tooltip ?? $"{target.GetFieldOrPropertyTypeOrElementType()} {target.Name}",
				DefaultValue = defaultValue == null ? 0 : (int)defaultValue
			});
		}
	}
}