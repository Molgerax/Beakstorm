//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TinyGoose.Tremble
{
	public class TrembleFieldConverterCollection
	{
		private readonly List<TrembleFieldConverter> m_Converters = new();
		private static readonly Dictionary<Type, TrembleFieldConverter> s_LookupCache = new();
		private static readonly Dictionary<TrembleFieldConverter, TrembleFieldConverterAttribute> s_ConverterSetupCache = new();

		public TrembleFieldConverterCollection()
		{
			m_Converters.AddRange(TypeDatabase
				.GetTypesWithAttribute<TrembleFieldConverterAttribute>()
				.Select(t => (TrembleFieldConverter)Activator.CreateInstance(t)));
		}

		public TrembleFieldConverter GetConverterForType(Type t)
		{
			// Array special case - expose as element type of array
			if (t.IsArray)
			{
				t = t.GetElementType();
			}

			// Enum special case!
			if (t.IsEnum)
			{
				return new EnumFieldConverter();
			}

			// First see if we've seen this type before
			if (s_LookupCache.TryGetValue(t, out TrembleFieldConverter existingConverter))
			{
				return existingConverter;
			}

			// Exact match first
			foreach (TrembleFieldConverter converter in m_Converters)
			{
				if (!s_ConverterSetupCache.TryGetValue(converter, out TrembleFieldConverterAttribute tfca))
				{
					tfca = converter.GetType().GetCustomAttribute<TrembleFieldConverterAttribute>();
					s_ConverterSetupCache.Add(converter, tfca);
				}

				if (tfca.Type != t)
					continue;

				s_LookupCache[t] = converter;
				return converter;
			}

			// Nothing exactly matches - look for subclasses too (for Components)
			foreach (TrembleFieldConverter converter in m_Converters)
			{
				if (!s_ConverterSetupCache.TryGetValue(converter, out TrembleFieldConverterAttribute tfca))
				{
					tfca = converter.GetType().GetCustomAttribute<TrembleFieldConverterAttribute>();
					s_ConverterSetupCache.Add(converter, tfca);
				}

				if (!t.IsSubclassOf(tfca.Type))
					continue;

				s_LookupCache[t] = converter;
				return converter;
			}

			return null;
		}
	}
}