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
	[TrembleFieldConverter(typeof(Vector3Int))]
	public class Vector3IntConverter : TrembleFieldConverter<Vector3Int>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Vector3Int value)
		{
			if (entity.TryGetUntransformedVector(key, out Vector3 v3Value))
			{
				value = new((int)v3Value.x, (int)v3Value.z, (int)v3Value.y);
				return true;
			}

			value = default;
			return false;
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Vector3Int[] values)
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

					return new Vector3Int((int)result.x, (int)result.z, (int)result.y);
				})
				.ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, Vector3Int defaultValue, MemberInfo target)
		{
         string tooltip = target.GetCustomAttribute<TooltipAttribute>()?.tooltip;

			entityClass.AddField(new FgdVectorField 
			{
				Name = fieldName,
				Description = tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}",
				DefaultValue = defaultValue
			});
		}
	}
}