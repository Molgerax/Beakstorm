// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	public abstract class FgdFieldBase
	{
		public abstract void Write(StreamWriter sw);
		public abstract void WriteCSharpClass(StreamWriter sw, int indent);
		public abstract void WriteCSharpInterface(StreamWriter sw, int indent);

		public virtual string FieldName => null;

		public string FriendlyTypeName
		{
			get
			{
				string friendlyTypeName = GetType().Name;
				if (friendlyTypeName.StartsWithInvariant("Fgd", caseSensitive: true))
				{
					friendlyTypeName = friendlyTypeName.Remove(0, 3);
				}

				if (friendlyTypeName.EndsWithInvariant("Field", caseSensitive: true))
				{
					friendlyTypeName = friendlyTypeName.Remove(friendlyTypeName.Length - 5, 5);
				}
				return friendlyTypeName;
			}
		}

		protected void WriteClassField(StreamWriter sw, int indent, string unityType, string name, string description, string defaultValue = null, string extraAttributes = null)
		{
			sw.WriteIndent(indent);

			sw.Write("[");
			{
				if (!TrembleSyncSettings.Get().SyncSerializedFields)
				{
					sw.Write("Tremble, ");
				}

				sw.Write($"SerializeField");
				if (!description.IsNullOrEmpty())
				{
					sw.Write($", Tooltip(\"{description}\")");
				}
			}

			if (!extraAttributes.IsNullOrEmpty())
			{
				sw.Write(", ");
				sw.Write(extraAttributes);
			}

			sw.Write("] ");

			sw.Write($"private {unityType} m_{name.ToNamingConvention(NamingConvention.UpperCamelCase)}");

			if (defaultValue == null)
			{
				sw.WriteLine(";");
			}
			else
			{
				sw.WriteLine($" = {defaultValue};");
			}
		}
		protected void WriteInterfaceProperty(StreamWriter sw, int indent, string unityType, string name, string description, string attributes = null)
		{
			sw.WriteIndent(indent);

			if (!attributes.IsNullOrEmpty())
			{
				sw.Write("[");
				sw.Write(attributes);
				sw.Write("] ");
			}

			sw.Write($"public {unityType} {name.ToNamingConvention(NamingConvention.UpperCamelCase)} => default;");
			sw.WriteLine(description.IsNullOrEmpty() ? "" : $"  // \"{description}\"");
		}
	}
}