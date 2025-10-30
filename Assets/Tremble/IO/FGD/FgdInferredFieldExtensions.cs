//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Globalization;

namespace TinyGoose.Tremble
{
	public static class FgdInferredFieldExtensions
	{
		public static void TryAddInferredField(this FgdClass fgdClass, string key, string value)
		{
			// ID property (or "targetname")
			string identityProperty = TrembleSyncSettings.Get().IdentityPropertyName;
			if (key.EqualsInvariant("target") || key.EqualsInvariant(identityProperty, caseSensitive: true))
			{
				return;
			}

			// "Target"
			if (key.ContainsInvariant(FgdConsts.PROPERTY_TARGET))
			{
				fgdClass.AddField(new FgdTargetDestinationField
				{
					Name = key,
					Description = $"{key.ToNamingConvention(NamingConvention.UpperCamelCase)} of this Entity"
				});

				return;
			}

			// "Origin" - skip
			if (key.EqualsInvariant(FgdConsts.PROPERTY_ORIGIN))
			{
				return;
			}

			// "Scale" - skip
			if (key.EqualsInvariant(FgdConsts.PROPERTY_SCALE))
			{
				return;
			}

			// "Parent" - skip
			if (key.EqualsInvariant(FgdConsts.PROPERTY_PARENT))
			{
				return;
			}

			// Angles - skip
			if (key.EqualsInvariant(FgdConsts.PROPERTY_ANGLES))
			{
				return;
			}

			// _tb_textures, group, etc - skip
			if (key.StartsWithInvariant(TBConsts.TB_PREFIX))
			{
				return;
			}

			// Spawnflags
			if (key.EqualsInvariant(FgdConsts.PROPERTY_SPAWNFLAGS))
			{
				if (!int.TryParse(value, out int flags))
					return;

				int bit = 0;
				while (flags > 0)
				{
					if (!fgdClass.HasSpawnFlag(bit))
					{
						fgdClass.AddField(new FgdSpawnFlagField
						{
							Bit = bit,
							Description = $"Unknown spawnflag bit #{bit}"
						});
					}

					bit++;
					flags >>= 1;
				}

				return;
			}

			// Anything else? Try to infer type.
			if (IsValueVector(value))
			{
				fgdClass.AddField(new FgdVectorField
				{
					Name = key,
					Description = $"Vector {key}",
				});
			}
			else if (IsValueFloat(value))
			{
				fgdClass.AddField(new FgdFloatField
				{
					Name = key,
					Description = $"Float {key}",
				});
			}
			else
			{
				fgdClass.AddField(new FgdStringField
				{
					Name = key,
					Description = $"String {key}",
				});
			}
		}

		private static bool IsValueFloat(string value) => value.TryParseFloat(out float _);

		private static bool IsValueVector(string value)
		{
			if (value.IsNullOrEmpty())
				return false;

			string[] parts = value.Split(' ');
			if (parts.Length != 3)
				return false;

			return IsValueFloat(parts[0]) && IsValueFloat(parts[1]) && IsValueFloat(parts[2]);
		}
	}
}