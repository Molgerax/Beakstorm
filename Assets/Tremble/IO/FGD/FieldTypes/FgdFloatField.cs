//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdFloatField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public float DefaultValue { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			if (DefaultValue == 0f)
			{
				sw.WriteLine($"{Name}(float) : \"{Description.Replace("\"", "'")}\"");
			}
			else
			{
				sw.WriteLine($"{Name}(float) : \"{Description.Replace("\"", "'")}\" : \"{DefaultValue.ToStringInvariant()}\"");
			}
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
			=> WriteClassField(sw, indent, "float", Name, Description, DefaultValue == 0f ? null : DefaultValue.ToStringInvariant() + "f");

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, "float", Name, Description);
	}
}