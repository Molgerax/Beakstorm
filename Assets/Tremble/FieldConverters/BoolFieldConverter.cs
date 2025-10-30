//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[TrembleFieldConverter(typeof(bool))]
	public class BoolFieldConverter : TrembleFieldConverter<bool>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out bool value)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			target.GetCustomAttributes(
				out SpawnFlagsAttribute sfa,
				out NoSpawnFlagsAttribute noSfa
			);

			bool useSpawnFlag = sfa != null || syncSettings.AlwaysPackBoolsIntoSpawnFlags;
			if (noSfa != null)
			{
				useSpawnFlag = false;
			}
			
			// No spawn flag - easy!			
			if (!useSpawnFlag)
				return entity.TryGetBoolean(key, out value);

			// Spawn flag
			int bit = sfa.GetBitForSpawnFlagsField(syncSettings, target);
			return entity.TryGetSpawnFlag(bit, out value);
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out bool[] values)
		{
			if (!entity.TryGetString(key, out string stringValues))
			{
				values = default;
				return false;
			}

			values = stringValues
				.Split(',')
				.Select(s => int.TryParse(s, out int result) ? result == 1 : default)
				.ToArray();
			return true;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, bool defaultValue, MemberInfo target)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			target.GetCustomAttributes(
				out SpawnFlagsAttribute sfa,
				out NoSpawnFlagsAttribute noSfa,
				out TooltipAttribute tooltip
			);

			bool useSpawnFlag = sfa != null || syncSettings.AlwaysPackBoolsIntoSpawnFlags;
			if (noSfa != null)
			{
				useSpawnFlag = false;
			}

			if (useSpawnFlag)
			{
				string spawnFlagName = target.Name.GetFieldNameInMap(syncSettings.SpawnFlagNamingConvention);
				if (syncSettings.SpawnFlagNamingConvention == NamingConvention.HumanFriendly)
				{
					spawnFlagName += "?";
				}

				int bit = sfa.GetBitForSpawnFlagsField(syncSettings, target);
				entityClass.AddField(new FgdSpawnFlagField
				{
					Description = spawnFlagName,
					Bit = bit,
					DefaultValue = defaultValue
				});
			}
			else
			{
				string friendlyName = target.Name.GetFieldNameInMap(NamingConvention.HumanFriendly) + "?";

				entityClass.AddField(new FgdBooleanField
				{
					Name = fieldName,
					Description = tooltip?.tooltip ?? friendlyName,
					DefaultValue = defaultValue
				});
			}
		}
	}
}