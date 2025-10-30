//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdAnglesField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public Vector3 DefaultValue { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			if (Mathf.Approximately(DefaultValue.x, DefaultValue.y) && Mathf.Approximately(DefaultValue.y, DefaultValue.z))
			{
				sw.WriteLine($"{Name}(float) : \"{Description.Replace("\"", "'")}\" : \"{DefaultValue.x.ToStringInvariant()}\"");
			}
			else
			{
				sw.WriteLine($"{Name}(float) : \"{Description.Replace("\"", "'")}\" : \"{DefaultValue.ToStringInvariant()}\"");
			}
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
		{
			string defaultValueString = DefaultValue == Vector3.zero
				? "Vector3.zero"
				: $"new Vector3({DefaultValue.x.ToStringInvariant()}, {DefaultValue.y.ToStringInvariant()}, {DefaultValue.z.ToStringInvariant()})";
			WriteClassField(sw, indent, nameof(Vector3), Name, Description, defaultValueString);
		}

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
			=> WriteInterfaceProperty(sw, indent, nameof(Vector3), Name, Description);
	}
}