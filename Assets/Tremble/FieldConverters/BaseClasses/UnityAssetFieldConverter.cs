// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble
{
	public static class UnityObjectFieldConverterStatics
	{
		public static readonly Dictionary<Type, string[]> s_CachedAssetLists = new();
	}
	
	public abstract class UnityAssetFieldConverter<TObjectType> : TrembleFieldConverter<Object>
		where TObjectType : Object
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Object value)
		{
			if (!entity.TryGetString(key, out string objectName))
			{
				value = null;
				return false;
			}

			return TryLoadObjectWithName(objectName, target.GetFieldOrPropertyTypeOrElementType(), key, entity, target, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out Object[] values)
		{
			if (!entity.TryGetString(key, out string objectNameString))
			{
				values = default;
				return false;
			}

			string[] objectNames = objectNameString.Split(',').ToArray();
			values = new Object[objectNames.Length];

			for (int objIdx = 0; objIdx < objectNames.Length; objIdx++)
			{
				if (!TryLoadObjectWithName(objectNames[objIdx], target.GetFieldOrPropertyTypeOrElementType(), key, entity, target, out Object foundObject))
					continue;

				values[objIdx] = foundObject;
			}

			return true;
		}

		private bool TryLoadObjectWithName(string name, Type objectType, string key, BspEntity entity, MemberInfo target, out Object obj)
		{
			string[] assetPaths = GetCachedAssetsPaths(objectType)
				.Where(p => Path.GetFileNameWithoutExtension(p).EqualsInvariant(name))
				.ToArray();

			if (assetPaths.Length > 1)
			{
				Debug.LogWarning($"Warning - importing {objectType.Name} value '{name}' for field '{key}', but multiple {objectType.Name}s match that name. Ensure your {target.GetFieldOrPropertyType().Name}s are named uniquely. Selecting the first available one!");

				foreach (string assetPath in assetPaths)
				{
					Debug.LogWarning($"Could be: {assetPath}?", TrembleAssetLoader.LoadAssetByPath<Object>(assetPath));
				}
			}

			if (assetPaths.Length == 0 || assetPaths[0].IsNullOrEmpty())
			{
				string entityName = entity.GetID().IsNullOrEmpty() ? entity.GetClassname() : $"'{entity.GetID()}' ({entity.GetClassname()})";

				Debug.LogWarning($"Couldn't find a {objectType.Name} asset named '{name}' in the project! Using default for entity {entityName}.");

				obj = null;
				return false;
			}

			obj = TrembleAssetLoader.LoadAssetByPath<Object>(assetPaths[0]);
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, Object defaultValue, MemberInfo target)
		{
			target.GetCustomAttributes(out TooltipAttribute tooltip);
	
			string[] choices = GetCachedAssetsPaths(target.GetFieldOrPropertyTypeOrElementType())
				.Select(Path.GetFileNameWithoutExtension)
				.ToArray();

			string defaultString = Path.GetFileNameWithoutExtension(TrembleAssetLoader.GetPath(defaultValue));

			entityClass.AddField(new FgdChoicesStringField
			{
				Name = fieldName,
				Choices = choices,
				Description = tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType()} {target.Name}",
				DefaultValue = defaultString
			});
		}

		private string[] GetCachedAssetsPaths(Type actualType)
		{
			if (!UnityObjectFieldConverterStatics.s_CachedAssetLists.TryGetValue(actualType, out string[] assetPaths))
			{
				List<string> assetPathList = TrembleAssetLoader.FindAssetPaths(actualType).ToList();
				assetPathList.Insert(0, "");

				assetPaths = assetPathList.ToArray();
				UnityObjectFieldConverterStatics.s_CachedAssetLists[actualType] = assetPaths;
			}

			return assetPaths;
		}
	}
}