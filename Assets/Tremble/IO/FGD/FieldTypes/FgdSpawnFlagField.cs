//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdSpawnFlagField : FgdFieldBase
	{
		public int Bit { get; init; }
		public string Description { get; init; }
		public bool DefaultValue { get; init; }

		public override string FieldName => NamingConventionUtil.FromHuman(Description);

		public override void Write(StreamWriter sw)
		{
			int bitDefaultValue = 1 << Bit;

			sw.WriteLine($"{bitDefaultValue} : \"{Description.Replace("\"", "'")}\" : {(DefaultValue ? "1" : "0")}");
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
			=> WriteClassField(sw, indent, "bool", FieldName, Description, DefaultValue ? "true" : null, extraAttributes: $"SpawnFlags({Bit})");

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, "bool", FieldName, Description, attributes: $"SpawnFlags({Bit})");
	}
}