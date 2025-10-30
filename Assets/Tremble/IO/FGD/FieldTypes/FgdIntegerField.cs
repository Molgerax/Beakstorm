//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdIntegerField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public int DefaultValue { get; init; }

		public override string FieldName => Name;

		public override void  Write(StreamWriter sw)
		{
			if (DefaultValue == 0)
			{
				sw.WriteLine($"{Name}(integer) : \"{Description.Replace("\"", "'")}\"");
			}
			else
			{
				sw.WriteLine($"{Name}(integer) : \"{Description.Replace("\"", "'")}\" : {DefaultValue}");
			}
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
			=> WriteClassField(sw, indent, "int", Name, Description, DefaultValue == 0 ? null : DefaultValue.ToString());

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, "int", Name, Description);
	}
}