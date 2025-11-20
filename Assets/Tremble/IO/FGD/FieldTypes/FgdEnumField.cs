//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdEnumField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public string EnumClassName { get; init; }
		public int DefaultValue { get; init; }
		public string[] Values { get; set; }

		public override string FieldName => Name;

		private string[] EnumValuesOrCached
		{
			get
			{
				if (Values == null || Values.Length == 0)
				{
					if (EnumClassName.ContainsInvariant("+"))
					{
						EnumClassName.Split('+', out string className, out string enumName);

						Debug.LogError($"Couldn't use {enumName} enum. Enums embedded in classes are "
											+ $"not supported by Tremble :(. Move your '{enumName}' enum "
											+ $"outside of {className} to use it!");
						return Array.Empty<string>();
					}

					Type enumType = TypeDatabase.GetType(EnumClassName);
					string[] names = Enum.GetNames(enumType);

					int[] values = Enum.GetValues(enumType).Cast<int>().ToArray();
					int maxValue = values.Max();

					Values = new string[maxValue + 1];
					for (int i = 0; i <= maxValue; i++)
					{
						int nameIdx = Array.FindIndex(values, v => v == i);
						if (nameIdx == -1)
						{
							Values[i] = "_";
						}
						else
						{
							Values[i] = names[nameIdx];
						}
					}
				}

				return Values;
			}
		}

		public override void Write(StreamWriter sw)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			if (syncSettings.SyncEnumsAsStringValue)
			{
				string[] values = EnumValuesOrCached;
				string defaultString = values.Length > DefaultValue && !values[DefaultValue].EqualsInvariant("_") ? values[DefaultValue] : "(none)";
				sw.WriteLine($"{Name}(choices) : \"{Description.Replace("\"", "'")}\" : \"{defaultString}\" = ");
			}
			else
			{
				if (DefaultValue == 0)
				{
					sw.WriteLine($"{Name}(choices) : \"{Description.Replace("\"", "'")}\"");
				}
				else
				{
					sw.WriteLine($"{Name}(choices) : \"{Description.Replace("\"", "'")}\" : {DefaultValue} = ");
				}
			}

			sw.WriteLine("\t[");
			{
				for (int i = 0; i < EnumValuesOrCached.Length; i++)
				{
					string value = EnumValuesOrCached[i];
					if (value.EqualsInvariant("_"))
						continue;

					if (syncSettings.SyncEnumsAsStringValue)
					{
						sw.WriteLine($"\t\t\"{value}\" : \"{value}\"");
					}
					else
					{
						sw.WriteLine($"\t\t{i} : \"{value}\"");
					}
				}
			}
			sw.WriteLine("\t]");
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
		{
			string enumTypeName = EnumClassName;
			int lastDot = enumTypeName.LastIndexOf('.');
			if (lastDot != -1)
			{
				enumTypeName = enumTypeName.Substring(lastDot + 1);
			}

			string defaultValueString = $"{EnumClassName}.{EnumValuesOrCached[DefaultValue]}";
			WriteClassField(sw, indent, enumTypeName, Name, Description, defaultValueString);
		}

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, nameof(Vector3), Name, Description);
	}
}