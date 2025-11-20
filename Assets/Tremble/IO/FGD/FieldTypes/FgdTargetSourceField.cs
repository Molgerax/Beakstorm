//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdTargetSourceField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public string DefaultValue { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			if (DefaultValue.IsNullOrEmpty())
			{
				sw.WriteLine($"{Name}(target_source) : \"{Description.Replace("\"", "'")}\"");
			}
			else
			{
				sw.WriteLine($"{Name}(target_source) : \"{Description.Replace("\"", "'")}\" : \"{DefaultValue}\"");
			}
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
			=> WriteClassField(sw, indent, "string", Name, Description, DefaultValue.IsNullOrEmpty() ? null : $"\"{DefaultValue}\"");

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, "string", Name, Description);
	}
}