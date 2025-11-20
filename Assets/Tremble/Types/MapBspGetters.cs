// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public enum EntityType
	{
		Brush,
		Point
	}
	
	public static class MapBspGetters
	{
		private static TrembleFieldConverterCollection s_ConverterCollection;

		public static bool FindEntityById(this MapBsp mapBsp, string id, out BspEntity entity)
		{
			string identityProperty = TrembleSyncSettings.Get().IdentityPropertyName;

			foreach (BspEntity ent in mapBsp.Entities)
			{
				if (!ent.TryGetString(identityProperty, out string foundId) || !foundId.EqualsInvariant(id, caseSensitive: true))
					continue;
				
				entity = ent;
				return true;
			}

			entity = default;
			return false;
		}

		public static List<BspEntity> FindEntitiesById(this MapBsp mapBsp, string id)
		{
			string identityProperty = TrembleSyncSettings.Get().IdentityPropertyName;

			List<BspEntity> entities = new();
			foreach (BspEntity ent in mapBsp.Entities)
			{
				if (!ent.TryGetString(identityProperty, out string foundId) || !foundId.EqualsInvariant(id, caseSensitive: true))
					continue;
				
				entities.Add(ent);
			}
			return entities;
		}

		public static BspEntity FindFirstEntityOfClass(this MapBsp mapBsp, string className)
		{
			foreach (BspEntity ent in mapBsp.Entities)
			{
				if (ent.GetClassname().EqualsInvariant(className, caseSensitive: true))
					return ent;
			}

			return default;
		}
		public static List<BspEntity> FindEntitiesOfClass(this MapBsp mapBsp, string className)
		{
			List<BspEntity> entities = new();

			foreach (BspEntity ent in mapBsp.Entities)
			{
				if (!ent.GetClassname().EqualsInvariant(className, caseSensitive: true))
					continue;
				
				entities.Add(ent);
			}

			return entities;
		}

		public static TReturnType GetBaseClassData<TReturnType>(this BspEntity entity, ITrembleBaseClass instance, string name)
		{
			PropertyInfo defaultProperty = instance
				.GetType()
				.GetInterfaces()
				.SelectMany(i => i.GetProperties())
				.FirstOrDefault(p => p.Name.EqualsInvariant(name, caseSensitive: true));

			if (defaultProperty == null)
				return default;

			string fieldName = name.GetFieldNameInMap(TrembleSyncSettings.Get().FieldNamingConvention);

			s_ConverterCollection ??= new();

			TrembleFieldConverter converter = s_ConverterCollection.GetConverterForType(defaultProperty.PropertyType);
			if (converter != null)
			{
				// Use a converter to convert the map value
				if (converter.TryGetValueFromMap(entity, fieldName, null, defaultProperty, out object mapValue))
				{
					if (defaultProperty.GetFieldOrPropertyTypeOrElementType() == typeof(Transform) && mapValue is Component c)
					{
						mapValue = c.transform;
					}

					return (TReturnType)mapValue;
				}

				// Couldn't convert - read the default value from the interface base
				return (TReturnType)defaultProperty.GetValue(instance);
			}

			// Nothing worked - log a warning
			Debug.LogWarning($"Saw field '{defaultProperty.Name}' of type {defaultProperty.GetFieldOrPropertyType()}, which is not yet supported. Try adding a custom TrembleFieldConverter for it, if you like!");

			return (TReturnType)defaultProperty.GetValue(instance);
		}


		public static BspEntity GetWorldspawnEntity(this MapBsp mapBsp)
		{
			return FindFirstEntityOfClass(mapBsp, "worldspawn");
		}

		public static string GetClassname(this BspEntity entity)
			=> entity.GetString(FgdConsts.PROPERTY_CLASSNAME);

		public static string GetID(this BspEntity entity)
			=> entity.GetString(TrembleSyncSettings.Get().IdentityPropertyName);

		public static bool HasID(this BspEntity entity)
			=> entity.Entries.ContainsKey(TrembleSyncSettings.Get().IdentityPropertyName);

		public static bool HasKey(this BspEntity entity, string key) => entity.Entries.ContainsKey(key);

		public static IEnumerable<string> GetAllNumberedKeys(this BspEntity entity, string prefix)
		{
			return entity.Entries.Keys
				.Where(k => k.StartsWithInvariant(prefix, caseSensitive: true))
				.OrderBy(k => k);
		}

		public static bool TryGetString(this BspEntity entity, string key, out string value) => entity.Entries.TryGetValue(key, out value);
		public static string GetString(this BspEntity entity, string key, string defaultValue = null)
			=> entity.Entries.GetValueOrDefault(key, defaultValue);
		public static string[] GetStrings(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetString(key)).ToArray();

		public static bool TryGetInt(this BspEntity entity, string key, out int value)
		{
			if (!entity.Entries.TryGetValue(key, out string strValue))
			{
				value = default;
				return false;
			}

			return int.TryParse(strValue, out value);
		}
		public static int GetInt(this BspEntity entity, string key, int defaultValue)
			=> TryGetInt(entity, key, out int value) ? value : defaultValue;
		public static int[] GetInts(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetInt(key, 0)).ToArray();

		public static bool TryGetFloat(this BspEntity entity, string key, out float value)
		{
			if (!entity.Entries.TryGetValue(key, out string strValue))
			{
				value = default;
				return false;
			}

			return strValue.TryParseFloat(out value);
		}
		public static float GetFloat(this BspEntity entity, string key, float defaultValue)
			=> TryGetFloat(entity, key, out float value) ? value : defaultValue;
		public static float[] GetFloats(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetFloat(key, 0f)).ToArray();
		
		public static bool TryGetBoolean(this BspEntity entity, string key, out bool value)
		{
			if (!entity.Entries.TryGetValue(key, out string strValue)
				|| !int.TryParse(strValue, out int intValue))
			{
				value = default;
				return false;
			}

			value = (intValue > 0);
			return true;
		}
		public static bool GetBoolean(this BspEntity entity, string key, bool defaultValue)
			=> TryGetBoolean(entity, key, out bool value) ? value : defaultValue;
		public static bool[] GetBooleans(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetBoolean(key, false)).ToArray();

		
		public static bool TryGetSpawnFlag(this BspEntity entity, int bit, out bool value)
		{
			int bitValue = 1 << bit;
			
			if (!entity.Entries.TryGetValue(FgdConsts.PROPERTY_SPAWNFLAGS, out string strValue)
			    || !int.TryParse(strValue, out int intValue))
			{
				value = default;
				return false;
			}

			value = (intValue & bitValue) > 0;
			return true;
		}
		public static bool GetSpawnFlag(this BspEntity entity, int bit, bool defaultValue)
			=> TryGetSpawnFlag(entity, bit, out bool value) ? value : defaultValue;

		public static bool TryGetUntransformedVector(this BspEntity entity, string key, out Vector3 value)
		{
			if (!entity.Entries.TryGetValue(key, out string strValue))
			{
				value = default;
				return false;
			}

			return strValue.TryParseQ3Vector(out value);
		}
		public static Vector3 GetUntransformedVector(this BspEntity entity, string key, Vector3 defaultValue)
			=> TryGetUntransformedVector(entity, key, out Vector3 value) ? value : defaultValue;
		public static Vector3[] GetUntransformedVectors(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetUntransformedVector(key, Vector3.zero)).ToArray();


		public static bool TryGetVector(this BspEntity entity, string key, out Vector3 value)
		{
			if (!TryGetUntransformedVector(entity, key, out Vector3 rawValue))
			{
				value = rawValue;
				return false;
			}

			value = rawValue.Q3ToUnityVector(scale: entity.ImportScale);
			return true;
		}
		public static Vector3 GetVector(this BspEntity entity, string key, Vector3 defaultValue)
			=> TryGetVector(entity, key, out Vector3 value) ? value : defaultValue;
		public static Vector3[] GetVectors(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetVector(key, Vector3.zero)).ToArray();

		public static bool TryGetRotation(this BspEntity entity, string key, out Quaternion value)
		{
			if (entity.TryGetUntransformedVector(key, out Vector3 rawAngles))
			{
				// From TB Docs: "angles" is interpreted as "pitch yaw roll" (if the entity model is a Quake MDL, pitch is inverted)
				Quaternion quat = Quaternion.identity;
				if (rawAngles.z != 0) quat = Quaternion.AngleAxis(-rawAngles.z, Vector3.right) * quat;		// pitch
				if (rawAngles.x != 0) quat = Quaternion.AngleAxis(-rawAngles.x, Vector3.forward) * quat;	// roll
				if (rawAngles.y != 0) quat = Quaternion.AngleAxis(-rawAngles.y, Vector3.up) * quat;			// yaw

				value = quat;
				return true;
			}

			value = default;
			return false;
		}
		public static Quaternion GetRotation(this BspEntity entity, string key, Quaternion defaultValue)
			=> TryGetRotation(entity, key, out Quaternion value) ? value : defaultValue;
		public static Quaternion[] GetRotations(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetRotation(key, Quaternion.identity)).ToArray();


		public static Vector3 GetPosition(this BspEntity entity)
			=> TryGetVector(entity, FgdConsts.PROPERTY_ORIGIN, out Vector3 value) ? value : Vector3.zero;


		public static bool TryGetColour(this BspEntity entity, string key, out Color value)
		{
			if (!entity.Entries.TryGetValue(key, out string strValue))
			{
				value = default;
				return false;
			}

			return strValue.TryParseQ3Colour(out value);
		}
		public static Color GetColour(this BspEntity entity, string key, Color defaultValue)
			=> TryGetColour(entity, key, out Color value) ? value : defaultValue;
		public static Color[] GetColours(this BspEntity entity, string keyPrefix)
			=> entity.GetAllNumberedKeys(keyPrefix).Select(key => entity.GetColour(key, Color.white)).ToArray();

	}
}