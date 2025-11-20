//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Globalization;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public static class FgdFieldFactory
	{
		public static FgdFieldBase CreateField(string typename, string name, string description, string defaultValue)
		{
			switch (typename.ToLowerInvariant())
			{
				case "float":
					return new FgdFloatField
					{
						Name = name,
						Description = description,
						DefaultValue = defaultValue != null ? float.Parse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture) : 0f
					};
				case "integer":
					return new FgdIntegerField
					{
						Name = name,
						Description = description,
						DefaultValue = defaultValue != null ? int.Parse(defaultValue, NumberStyles.Any, CultureInfo.InvariantCulture) : 0
					};
				case "vector":
					if (defaultValue == null || !defaultValue.TryParseQ3Vector(out Vector3 vector))
					{
						vector = Vector3.zero;
					}

					return new FgdVectorField
					{
						Name = name,
						Description = description,
						DefaultValue = vector
					};
				case "color":
					if (!defaultValue.TryParseQ3Colour(out Color colour))
					{
						colour = Color.white;
					}

					return new FgdColourField
					{
						Name = name,
						Description = description,
						DefaultColour = colour
					};
				case "string":
					return new FgdStringField
					{
						Name = name,
						Description = description,
						DefaultValue = defaultValue
					};
				case "target_source":
					return new FgdTargetSourceField
					{
						Name = name,
						Description = description,
						DefaultValue = defaultValue
					};
				case "target_destination":
					return new FgdTargetDestinationField
					{
						Name = name,
						Description = description,
						DefaultValue = defaultValue
					};
			}

			return new FgdStringField
			{
				Name = name,
				Description = description,
				DefaultValue = defaultValue
			};
		}
	}
}