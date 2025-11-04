// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace TinyGoose.Tremble
{
	[AttributeUsage(AttributeTargets.Class)]
	public class TrembleFieldConverterAttribute : PreserveAttribute
	{
		public TrembleFieldConverterAttribute(Type t)
		{
			m_Type = t;
		}

		private readonly Type m_Type;
		public Type Type => m_Type;
	}

	[Preserve]
	public abstract class TrembleFieldConverter
	{
		public abstract bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out object value);

		public abstract bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out object[] value);

		public abstract void AddFieldToFgd(FgdClass entityClass, string fieldName, object defaultValue, MemberInfo target);
	}

	public abstract class TrembleFieldConverter<T> : TrembleFieldConverter
	{
		public sealed override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out object value)
		{
			Type targetType = target.GetFieldOrPropertyType();

			if (targetType.IsArray)
			{
				value = entity
					.GetAllNumberedKeys(key)
					.Select(k => TryGetValueFromMap(entity, k, gameObject, target, out T typedValue) ? typedValue : default)
					.ToArray();
				return true;
			}
			else
			{
				bool success = TryGetValueFromMap(entity, key, gameObject, target, out T typedValue);
				value = success ? typedValue : default;
				return success;
			}
		}

		public sealed override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out object[] values)
		{
			string[] numberedKeys = entity.GetAllNumberedKeys(key).ToArray();
			if (numberedKeys is { Length: 0 })
			{
				values = default;
				return false;
			}

			if (key == "target")
				numberedKeys = new[] {key};

			List<object> outValues = new(numberedKeys.Length);

			foreach (string numberedKey in numberedKeys)
			{
				// First get multiple and add if possible
				if (TryGetValuesFromMap(entity, numberedKey, gameObject, target, out T[] tValues))
				{
					outValues.AddRange(tValues.Cast<object>());
				}
				else if (TryGetValueFromMap(entity, numberedKey, gameObject, target, out T value))
				{
					outValues.Add(value);
				}
			}

			values = outValues.ToArray();
			return true;
		}

		public sealed override void AddFieldToFgd(FgdClass entityClass, string fieldName, object defaultValue, MemberInfo target)
		{
			if (defaultValue is Array { Length: > 0 } defaultArray)
			{
				defaultValue = defaultArray.GetValue(0);
			}

			AddFieldToFgd(entityClass, fieldName, (T)defaultValue, target);
		}

		protected abstract bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out T value);

		protected virtual bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out T[] values)
		{
			if (!TryGetValueFromMap(entity, key, gameObject, target, out T value))
			{
				values = default;
				return false;
			}

			values = new T[1];
			values[0] = value;
			return true;
		}

		protected abstract void AddFieldToFgd(FgdClass entityClass, string fieldName, T defaultValue, MemberInfo target);
	}
}