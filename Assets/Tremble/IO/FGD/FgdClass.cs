//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public enum FgdClassType
	{
		Base,

		Point,
		Brush,
	}

	public class FgdClass
	{
		public FgdClass(FgdClassType type, string name, string description)
		{
			Type = type;
			Name = name;
			Description = description;
		}

		public FgdClassType Type { get; }
		public string Name { get; }
		public string Description { get; set; }
		public bool HasModel { get; init; }
		public Color? Colour { get; init; }
		public string Sprite { get; init; }
		public Bounds? Box { get; init; }

		private readonly List<string> m_BaseClasses = new();
		private readonly List<FgdFieldBase> m_Fields = new();
		private readonly List<FgdSpawnFlagField> m_SpawnFlags = new();

		public void AddBaseClass(string baseClass) => m_BaseClasses.Add(baseClass);

		public void AddBaseClassInterfaces(Type entityType)
		{
			foreach (Type iface in entityType.GetInterfaces())
			{
				if (!iface.GetInterfaces().Contains(typeof(ITrembleBaseClass)))
					continue;

				AddBaseClass(iface.Name.Substring(1)); // Remove 'I'
				break;
			}
		}

		public void AddField(FgdFieldBase fgdField)
		{
			if (fgdField is FgdSpawnFlagField flag)
			{
				m_SpawnFlags.Add(flag);
			}
			else
			{
				m_Fields.Add(fgdField);
			}
		}

		public List<FgdFieldBase> AllFields => m_Fields;
		public List<FgdSpawnFlagField> AllSpawnFlags => m_SpawnFlags;
		public List<string> BaseClassNames => m_BaseClasses;

		public bool HasField(string name) => m_Fields.Any(m => m.FieldName.EqualsInvariant(name, caseSensitive: true));

		public bool HasSpawnFlag(int bit) => m_SpawnFlags.Any(sf => sf.Bit == bit);

		public void AddComment(string comment)
		{
			m_Fields.Add(new FgdComment { Comment = comment });
		}

		public void WriteFgd(StreamWriter stream, float importScale)
		{
			string typeString = Type switch
			{
				FgdClassType.Base => "@BaseClass",
				FgdClassType.Point => "@PointClass",
				FgdClassType.Brush => "@SolidClass",
				_ => throw new ArgumentOutOfRangeException()
			};

			stream.WriteLine(typeString);

			if (m_BaseClasses.Count > 0)
			{
				string baseList = String.Join(", ", m_BaseClasses);
				stream.WriteLine($"\tbase({baseList})");
			}

			if (HasModel)
			{
				stream.WriteLine($"\tmodel({{ path: \"models/{Name}.md3\", scale: scale }})");
			}

			if (Colour.HasValue)
			{
				stream.WriteLine($"\tcolor({Colour.Value.ToStringInvariant()})");
			}

			if (!Sprite.IsNullOrEmpty())
			{
				stream.WriteLine($"\tsprite(\"models/{Sprite}.png\")");
			}

			if (Box.HasValue)
			{
				Vector3 q3Min = Box.Value.min.UnityToQ3Vector(scale: importScale);
				Vector3 q3Max = Box.Value.max.UnityToQ3Vector(scale: importScale);

				// From xage on TB Discord: bboxes need to be symmetrical
				Vector3 extents = new()
				{
					x = (Mathf.Abs(q3Min.x) + Mathf.Abs(q3Max.x)),
					y = (Mathf.Abs(q3Min.y) + Mathf.Abs(q3Max.y)),
					z = (Mathf.Abs(q3Min.z) + Mathf.Abs(q3Max.z))
				};

				stream.WriteLine($"\tsize({Mathf.RoundToInt(extents.x)} {Mathf.RoundToInt(extents.y)} {Mathf.RoundToInt(extents.z)})");
			}

			stream.Write($" = {Name} : \"{Description}\"");

			// Write fields, and then spawnflags fields
			if (m_Fields.Count == 0 && m_SpawnFlags.Count == 0)
			{
				stream.WriteLine(" []");
			}
			else
			{
				stream.WriteLine();
				stream.WriteLine("[");

				foreach (FgdFieldBase variable in m_Fields)
				{
					stream.Write("\t");
					variable.Write(stream);
				}

				if (m_SpawnFlags.Count > 0)
				{
					stream.WriteLine("\tspawnflags(Flags) = ");
					stream.WriteLine("\t[");
					foreach (FgdSpawnFlagField flag in m_SpawnFlags.OrderBy(sf => sf.Bit))
					{
						stream.Write("\t\t");
						flag.Write(stream);
					}

					stream.WriteLine("\t]");
				}

				stream.WriteLine("]");
			}
		}
	}
}