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
	public class FgdVectorField : FgdFieldBase
	{
		public string Name { get; init; }
		public string Description { get; init; }
		public Vector3 DefaultValue { get; init; }
		public bool ConvertToQ3 { get; init; }
		public float ImportScale { get; init; }

		public override string FieldName => Name;

		public override void Write(StreamWriter sw)
		{
			Vector3 value = DefaultValue;

			if (ConvertToQ3)
			{
				value = new Vector3(-value.x, -value.z, value.y) / ImportScale;
			}

			if (Mathf.Approximately(value.x, value.y) && Mathf.Approximately(value.y, value.z))
			{
				sw.WriteLine($"{Name}(float) : \"{Description.Replace("\"", "'")}\" : \"{value.x.ToStringInvariant()}\"");
			}
			else
			{
				sw.WriteLine($"{Name}(float) : \"{Description.Replace("\"", "'")}\" : \"{value.ToStringInvariant()}\"");
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