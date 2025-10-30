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
	[TrembleFieldConverter(typeof(Color))]
	public class ColourFieldConverter : TrembleFieldConverter<Color>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Color value)
		{
			return entity.TryGetColour(key, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Color[] values)
		{
			if (!entity.TryGetString(key, out string stringValues))
			{
				values = default;
				return false;
			}

			values = stringValues
				.Split(',')
				.Select(s => s.TryParseQ3Colour(out Color result) ? result : Color.white)
				.ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, Color defaultValue, MemberInfo target)
		{
			string tooltip = target.GetCustomAttribute<TooltipAttribute>()?.tooltip;

			entityClass.AddField(new FgdColourField 
			{
				Name = fieldName,
				Description = tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}",
				DefaultColour = defaultValue
			});
		}
	}
}