//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdColourField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public Color DefaultColour { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			sw.WriteLine($"{Name}(color) : \"{Description.Replace("\"", "'")}\" : \"{DefaultColour.ToStringInvariant()}\"");
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
		{
			string defaultValueString = DefaultColour == Color.white
				? "Color.white"
				: $"new Color({DefaultColour.r.ToStringInvariant()}, {DefaultColour.g.ToStringInvariant()}, {DefaultColour.b.ToStringInvariant()})";

			WriteClassField(sw, indent, nameof(Color), Name, Description, DefaultColour == Color.white ? null : defaultValueString);
		}

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, nameof(Color), Name, Description);
	}
}