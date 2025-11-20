//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[TrembleFieldConverter(typeof(Vector3))]
	public class Vector3Converter : TrembleFieldConverter<Vector3>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Vector3 value)
		{
			target.GetCustomAttributes(out TrembleAttribute tremble);
			bool? isRotation = tremble?.IsRotation;
			
			// Keep raw value if requested, or it's a "rotation"
			isRotation ??= key.ContainsInvariant("rotation") || key.ContainsInvariant("rotate");

			return isRotation.Value 
				? entity.TryGetUntransformedVector(key, out value) 
				: entity.TryGetVector(key, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Vector3[] values)
		{
			if (!entity.TryGetString(key, out string stringValues))
			{
				values = default;
				return false;
			}

			target.GetCustomAttributes(out TrembleAttribute tremble);
			bool? isRotation = tremble?.IsRotation;

			// Keep raw value if requested, or it's a "rotation"
			isRotation ??= key.ContainsInvariant("rotation") || key.ContainsInvariant("rotate");

			values = stringValues
				.Split(',')
				.Select(s =>
				{
					s.TryParseQ3Vector(out Vector3 result);

					if (!isRotation.GetValueOrDefault(false))
					{
						result.Q3ToUnityVector(entity.ImportScale);
					}

					return result;
				})
				.ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, Vector3 defaultValue, MemberInfo target)
		{
			target.GetCustomAttributes(
				out TrembleAttribute tremble,
				out TooltipAttribute tooltip
			);

			// Keep raw value if requested, or it's a "rotation"
			bool? isRotation = tremble?.IsRotation;
			isRotation ??= fieldName.ContainsInvariant("rotation") || fieldName.ContainsInvariant("rotate");

			entityClass.AddField(new FgdVectorField 
			{
				Name = fieldName,
				Description = tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}",
				DefaultValue = defaultValue,
				ImportScale = isRotation.Value ? 1f : TrembleSyncSettings.Get().ImportScale,
				ConvertToQ3 = true
			});
		}
	}
}