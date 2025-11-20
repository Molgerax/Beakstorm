//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdChoicesStringField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public string[] Choices { get; init; }
		public string DefaultValue { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			sw.WriteLine($"{Name}(choices) : \"{Description.Replace("\"", "'")}\" : \"{DefaultValue ?? Choices[0]}\" = ");
			sw.WriteLine("\t[");
			{
				foreach (string choice in Choices)
				{
					if (choice.IsNullOrEmpty())
					{
						sw.WriteLine("\t\t\"\" : \"(none)\"");
					}
					else
					{
						sw.WriteLine($"\t\t\"{choice}\" : \"{choice.ToNamingConvention(NamingConvention.HumanFriendly)}\"");
					}
				}
			}
			sw.WriteLine("\t]");
		}

		//TODO(jwf): support default values?
		public override void WriteCSharpClass(StreamWriter sw, int indent)
			=> WriteClassField(sw, indent, "string", Name, Description);

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, "string", Name, Description);
	}
}