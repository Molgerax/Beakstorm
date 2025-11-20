//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Type = System.Type;

namespace TinyGoose.Tremble
{
	public static class FieldUtil
	{
		private static readonly string[] KNOWN_PREFIXES = { "m_", "_" };

		private static readonly Dictionary<Type, object[]> s_CachedTypeAttributes = new();
		private static readonly Dictionary<MemberInfo, object[]> s_CachedFieldAttributes = new();
		private static readonly Dictionary<Type, FieldInfo[]> s_AllInstanceFieldsIncludingPrivateAndBaseClasses = new();

		public static bool TryGetCustomAttribute<TAttribute>(this Type type, out TAttribute att)
			where TAttribute : Attribute
		{
			att = type.GetCachedAttribute<TAttribute>();
			return att != null;
		}
		
		public static bool HasCustomAttribute<TAttribute>(this FieldInfo field)
			where TAttribute : Attribute
		{
			return field.GetCachedAttribute<TAttribute>() != null;
		}
		
		public static bool HasCustomAttribute<TAttribute>(this Type type)
			where TAttribute : Attribute
		{
			return type.GetCachedAttribute<TAttribute>() != null;
		}

		public static void GetCustomAttributes<TAttribute1>(this MemberInfo member, out TAttribute1 attr1)
			where TAttribute1 : Attribute
		{
			attr1 = default;

			foreach (Attribute attribute in member.GetCachedAttributes())
			{
				if (attribute is TAttribute1 tAttribute1)
					attr1 = tAttribute1;
			}
		}

		public static void GetCustomAttributes<TAttribute1, TAttribute2>(this MemberInfo member, out TAttribute1 attr1, out TAttribute2 attr2)
			where TAttribute1 : Attribute
			where TAttribute2 : Attribute
		{
			attr1 = default;
			attr2 = default;

			foreach (Attribute attribute in member.GetCachedAttributes())
			{
				if (attribute is TAttribute1 tAttribute1)
					attr1 = tAttribute1;
				if (attribute is TAttribute2 tAttribute2)
					attr2 = tAttribute2;
			}
		}

		public static void GetCustomAttributes<TAttribute1, TAttribute2, TAttribute3>(this MemberInfo member, out TAttribute1 attr1, out TAttribute2 attr2, out TAttribute3 attr3)
			where TAttribute1 : Attribute
			where TAttribute2 : Attribute
			where TAttribute3 : Attribute
		{
			attr1 = default;
			attr2 = default;
			attr3 = default;

			foreach (Attribute attribute in member.GetCachedAttributes())
			{
				if (attribute is TAttribute1 tAttribute1)
					attr1 = tAttribute1;
				if (attribute is TAttribute2 tAttribute2)
					attr2 = tAttribute2;
				if (attribute is TAttribute3 tAttribute3)
					attr3 = tAttribute3;
			}
		}

		public static void GetCustomAttributes<TAttribute1, TAttribute2, TAttribute3, TAttribute4>(this MemberInfo member, out TAttribute1 attr1, out TAttribute2 attr2, out TAttribute3 attr3, out TAttribute4 attr4)
			where TAttribute1 : Attribute
			where TAttribute2 : Attribute
			where TAttribute3 : Attribute
			where TAttribute4 : Attribute
		{
			attr1 = default;
			attr2 = default;
			attr3 = default;
			attr4 = default;

			foreach (Attribute attribute in member.GetCachedAttributes())
			{
				if (attribute is TAttribute1 tAttribute1)
					attr1 = tAttribute1;
				if (attribute is TAttribute2 tAttribute2)
					attr2 = tAttribute2;
				if (attribute is TAttribute3 tAttribute3)
					attr3 = tAttribute3;
				if (attribute is TAttribute4 tAttribute4)
					attr4 = tAttribute4;
			}
		}

		public static void GetCustomAttributes<TAttribute1, TAttribute2, TAttribute3, TAttribute4, TAttribute5>(this MemberInfo member, out TAttribute1 attr1, out TAttribute2 attr2, out TAttribute3 attr3, out TAttribute4 attr4, out TAttribute5 attr5)
			where TAttribute1 : Attribute
			where TAttribute2 : Attribute
			where TAttribute3 : Attribute
			where TAttribute4 : Attribute
			where TAttribute5 : Attribute
		{
			attr1 = default;
			attr2 = default;
			attr3 = default;
			attr4 = default;
			attr5 = default;

			foreach (Attribute attribute in member.GetCachedAttributes())
			{
				if (attribute is TAttribute1 tAttribute1)
					attr1 = tAttribute1;
				if (attribute is TAttribute2 tAttribute2)
					attr2 = tAttribute2;
				if (attribute is TAttribute3 tAttribute3)
					attr3 = tAttribute3;
				if (attribute is TAttribute4 tAttribute4)
					attr4 = tAttribute4;
				if (attribute is TAttribute5 tAttribute5)
					attr5 = tAttribute5;
			}
		}

		public static void GetCustomAttributes<TAttribute1, TAttribute2, TAttribute3, TAttribute4, TAttribute5, TAttribute6>(this MemberInfo member, out TAttribute1 attr1, out TAttribute2 attr2, out TAttribute3 attr3, out TAttribute4 attr4, out TAttribute5 attr5, out TAttribute6 attr6)
			where TAttribute1 : Attribute
			where TAttribute2 : Attribute
			where TAttribute3 : Attribute
			where TAttribute4 : Attribute
			where TAttribute5 : Attribute
			where TAttribute6 : Attribute
		{
			attr1 = default;
			attr2 = default;
			attr3 = default;
			attr4 = default;
			attr5 = default;
			attr6 = default;

			foreach (Attribute attribute in member.GetCachedAttributes())
			{
				if (attribute is TAttribute1 tAttribute1)
					attr1 = tAttribute1;
				if (attribute is TAttribute2 tAttribute2)
					attr2 = tAttribute2;
				if (attribute is TAttribute3 tAttribute3)
					attr3 = tAttribute3;
				if (attribute is TAttribute4 tAttribute4)
					attr4 = tAttribute4;
				if (attribute is TAttribute5 tAttribute5)
					attr5 = tAttribute5;
				if (attribute is TAttribute6 tAttribute6)
					attr6 = tAttribute6;
			}
		}

		private static object[] GetCachedAttributes(this MemberInfo member)
		{
			if (s_CachedFieldAttributes.TryGetValue(member, out object[] attributes))
				return attributes;

			attributes = member.GetCustomAttributes(true);
			s_CachedFieldAttributes[member] = attributes;

			return attributes;
		}

		private static object[] GetCachedAttributes(this Type type)
		{
			if (s_CachedTypeAttributes.TryGetValue(type, out object[] attributes))
				return attributes;

			attributes = type.GetCustomAttributes(true);
			s_CachedTypeAttributes[type] = attributes;

			return attributes;
		}

		private static TAttribute GetCachedAttribute<TAttribute>(this MemberInfo member)
		{
			foreach (object availableAttribute in member.GetCachedAttributes())
			{
				if (availableAttribute is TAttribute castedAttribute)
					return castedAttribute;
			}

			return default;
		}

		private static TAttribute GetCachedAttribute<TAttribute>(this Type type)
		{
			foreach (object availableAttribute in type.GetCachedAttributes())
			{
				if (availableAttribute is TAttribute castedAttribute)
					return castedAttribute;
			}

			return default;
		}

		public static string GetFieldNameInMap(this string fieldName, NamingConvention fnc)
		{
			// Remove prefixes
			if (fnc != NamingConvention.PreserveExact)
			{
				foreach (string prefix in KNOWN_PREFIXES)
				{
					if (!fieldName.StartsWithInvariant(prefix, caseSensitive: true))
						continue;

					fieldName = fieldName.Substring(prefix.Length);
				}
			}

			return fieldName.ToNamingConvention(fnc);
		}

		public static int GetBitForSpawnFlagsField(this SpawnFlagsAttribute spawnFlags, TrembleSyncSettings syncSettings, MemberInfo mi)
		{
			if (spawnFlags is { Bit: >= 0 })
				return spawnFlags.Bit;
			
			int bit = -1;
			foreach (FieldInfo availableField in mi.DeclaringType.GetAllInstanceFieldsIncludingPrivateAndBaseClasses())
			{
				// This isn't a boolean!
				if (availableField.FieldType != typeof(Boolean))
					continue;

				availableField.GetCustomAttributes(
					out SpawnFlagsAttribute sfa,
					out NoSpawnFlagsAttribute noSfa,
					out TrembleAttribute ta,
					out NoTrembleAttribute nta,
					out SerializeField sf
				);

				// This is categorically NOT a spawnflags var
				if (noSfa != null)
					continue;

				// Not for Tremble
				if (nta != null)
					continue;
				
				// This field doesn't have spawn flags - doesn't count!
				if (sfa == null && !syncSettings.AlwaysPackBoolsIntoSpawnFlags)
					continue;

				// No tremble attribute, or no SerializeField/we're not considering SFs
				if (ta == null && (sf == null || !syncSettings.SyncSerializedFields))
					continue;

				// It's a spawnflags field, increment
				bit++;

				// It's the one we're looking for - stop!
				if (availableField == mi)
					break;
			}

			return bit;
		}

		public static Type GetFieldOrPropertyType(this MemberInfo mi)
		{
			switch (mi)
			{
				case PropertyInfo pi:
					return pi.PropertyType;

				case FieldInfo fi:
					return fi.FieldType;
			}

			Debug.LogError($"Unsupported field type {mi.GetType()}!");
			return null;
		}

		public static Type GetFieldOrPropertyTypeOrElementType(this MemberInfo mi)
		{
			Type type = mi.GetFieldOrPropertyType();
			if (type is { IsArray: true })
			{
				type = type.GetElementType();
			}

			return type;
		}

		public static FieldInfo[] GetAllInstanceFieldsIncludingPrivateAndBaseClasses(this Type t)
		{
			Type originalType = t;

			if (!s_AllInstanceFieldsIncludingPrivateAndBaseClasses.TryGetValue(originalType, out FieldInfo[] fields))
			{
				List<FieldInfo> fieldList = new(64);
				while (t != null && (t.Namespace == null || !t.Namespace.ContainsInvariant("UnityEngine", caseSensitive: true)))
				{
					fieldList.InsertRange(0, t.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public));
					t = t.BaseType;
				}

				fields = fieldList.ToArray();
				s_AllInstanceFieldsIncludingPrivateAndBaseClasses[originalType] = fields;
			}

			return fields;
		}
	}
}