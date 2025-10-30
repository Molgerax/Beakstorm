//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[TrembleFieldConverter(typeof(Component))]
	public class ComponentReferenceFieldConverter : TrembleFieldConverter<Component>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Component value)
		{
			if (entity.TryGetString(key, out string id))
			{
				if (TrembleMapImportSettings.Current.TryGetGameObjectsForID(id, out List<GameObject> objs) && objs.Count > 0)
				{
					value = objs[0].GetComponent(target.GetFieldOrPropertyTypeOrElementType());
					if (!value)
					{
						Debug.LogWarning($"Entity '{objs[0].name}' reference '{id}' is of unexpected type. (expected: {target.GetFieldOrPropertyTypeOrElementType().Name}). Check the targeted entity in the map is of the correct type.");
					}

					return value;
				}
			}

			value = default;
			return false;
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Component[] values)
		{
			if (entity.TryGetString(key, out string id) && TrembleMapImportSettings.Current.TryGetGameObjectsForID(id, out List<GameObject> objs))
			{
				values = new Component[objs.Count];
				for (int objIdx = 0; objIdx < objs.Count; objIdx++)
				{
					Component component = objs[objIdx].GetComponent(target.GetFieldOrPropertyTypeOrElementType());
					if (!component)
					{
						Debug.LogWarning($"Entity '{objs[objIdx].name}' reference '{id}' is of unexpected type. (expected: {target.GetFieldOrPropertyTypeOrElementType().Name}). Check the targeted entity in the map is of the correct type.");

						continue;
					}

					values[objIdx] = component;
				}

				return true;
			}

			values = default;
			return false;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, Component defaultValue, MemberInfo target)
		{
			target.GetCustomAttributes(
				out TooltipAttribute tooltip
			);

			entityClass.AddField(new FgdTargetDestinationField
			{
				Name = fieldName,
				Description = tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}",
				DefaultValue = default
			});
		}
	}
}