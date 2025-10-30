//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdBooleanField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public bool DefaultValue { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			string description = Description != null ? Description.Replace("\"", "'") : Name;

			sw.WriteLine($"{Name}(choices) : \"{description}\" : {(DefaultValue ? "1" : "0")} = ");
			sw.WriteLine("\t[");
			{
				sw.WriteLine("\t\t0 : \"False\"");
				sw.WriteLine("\t\t1 : \"True\"");
			}
			sw.WriteLine("\t]");
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
			=> WriteClassField(sw, indent, "bool", Name, Description, DefaultValue ? "true" : null);

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, "bool", Name, Description);
	}
}