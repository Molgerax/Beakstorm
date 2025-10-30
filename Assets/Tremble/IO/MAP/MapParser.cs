//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public class MapBrushFace
	{
		// public Vector3 Position { get; init; }
		// public Vector3 Normal { get; init; }
		public Vector3 P1 { get; set; }
		public Vector3 P2 { get; set; }
		public Vector3 P3 { get; set; }
		public string TextureName { get; set; }
		public float[] TexU { get; init; }
		public float[] TexV { get; init; }
		public float Rotation { get; init; }
		public Vector2 Scale { get; init; }
	}

	public class MapBrush
	{
		public List<MapBrushFace> Faces { get; init; }
	}

	public class MapEntity
	{
		public Dictionary<string, string> Entries { get; init; }
		public List<MapBrush> Brushes { get; init; }

		public string Classname => Entries.GetValueOrDefault("classname", null);
	}

	public class Map
	{
		public Dictionary<string, string> Metadata { get; init; }
		public List<MapEntity> Entities { get; init; }

		public void WriteTo(string path)
		{
			using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
			using StreamWriter sw = new(fs);

			sw.WriteLine($"// Game: {TrembleConsts.GAME_NAME}");
			sw.WriteLine("// Format: Quake3 (Valve)");

			for (int entityIdx = 0; entityIdx < Entities.Count; entityIdx++)
			{
				sw.WriteLine($"// entity {entityIdx}");
				sw.WriteLine("{");
				foreach ((string key, string value) in Entities[entityIdx].Entries)
				{
					sw.WriteLine($"\"{key}\" \"{value}\"");
				}

				if (Entities[entityIdx].Brushes != null)
				{
					for (int brushIdx = 0; brushIdx < Entities[entityIdx].Brushes.Count; brushIdx++)
					{
						MapBrush brush = Entities[entityIdx].Brushes[brushIdx];

						sw.WriteLine($"// brush {brushIdx}");
						sw.WriteLine("{");
						foreach (MapBrushFace face in brush.Faces)
						{
							//Vector3[] points = new Vector3[3];

							// Vector3 arb = Vector3.Normalize(face.Normal + new Vector3(2f, 3f, 1f));
							// points[0] = face.Position;
							// points[1] = face.Position + Vector3.Cross(face.Normal, arb);
							// points[2] = face.Position + Vector3.Cross(points[1], face.Normal);
							// foreach (Vector3 p in points)
							// {
							// 	sw.Write($"( {p.x} {p.y} {p.z} ) ");
							// }


							sw.Write($"( {face.P1.ToStringInvariant()} ) ");
							sw.Write($"( {face.P2.ToStringInvariant()} ) ");
							sw.Write($"( {face.P3.ToStringInvariant()} ) ");

							sw.Write(face.TextureName);
							sw.Write(" [ ");
							foreach (float f in face.TexU)
							{
								sw.Write(f);
								sw.Write(" ");
							}

							sw.Write("] [ ");
							foreach (float f in face.TexV)
							{
								sw.Write(f);
								sw.Write(" ");
							}

							sw.WriteLine($"] {face.Rotation.ToStringInvariant()} {face.Scale.ToStringInvariant()}");
						}

						sw.WriteLine("}");
					}
				}

				sw.WriteLine("}");
			}
		}
	}

	public static class MapParser
	{
		private const string TOKEN_START = "{";
		private const string TOKEN_END = "}";

		public static Map Parse(string mapPath)
		{
			List<MapEntity> entities = new(512);
			Dictionary<string, string> metadata = new(16);

			// Quicker than using streams (wtf?)
			string mapContents = File.ReadAllText(mapPath).Replace("\r", "");

			// Read metadata
			string remaining = mapContents;
			while (remaining.StartsWithInvariant("//") && remaining.ContainsInvariant(":"))
			{
				remaining.Split('\n', out string thisLine, out remaining);

				thisLine.Substring(2).Split(':', out string key, out string value);
				if (key == null || value == null)
					break;

				metadata[key] = value;
			}

			// Parse map
			TokenParser parser = new(mapContents);
			while (!parser.IsAtEnd)
			{
				entities.Add(parser.ReadEntity());
			}

			return new() { Metadata = metadata, Entities = entities };
		}
		public static Dictionary<string, string> ParseMetadataOnly(string mapPath)
		{
			Dictionary<string, string> metadata = new(16);

			// Quicker than using streams (wtf?)
			using FileStream file = new(mapPath, FileMode.Open, FileAccess.Read);
			using StreamReader sr = new(file, Encoding.ASCII);

			// Read metadata
			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();
				if (line == null)
					break;

				if (!line.StartsWithInvariant("//") || !line.ContainsInvariant(":"))
					break;

				line.Substring(2).Split(':', out string key, out string value);
				if (key == null || value == null)
					break;

				metadata[key] = value;
			}

			return metadata;
		}

		private static MapEntity ReadEntity(this TokenParser parser)
		{
			if (!parser.MatchToken(TOKEN_START))
				return new();

			Dictionary<string, string> entries = new(64);

			while (!parser.IsAtEnd)
			{
				ReadOnlySpan<char> token = parser.PeekToken();

				if (token.EqualsInvariant(TOKEN_START, caseSensitive: true))
				{
					// Found brushes, parse them
					List<MapBrush> brushes = ParseBrushes(parser);
					parser.MatchToken(TOKEN_END); // Consume end brace of entity

					return new() { Brushes = brushes, Entries = entries };
				}
				else if (token.EqualsInvariant(TOKEN_END, caseSensitive: true))
				{
					// Found end - with no brushes
					parser.MatchToken(TOKEN_END); // Consume end brace of entity
					return new() { Brushes = new(), Entries = entries };
				}
				else
				{
					// Still parsing entries
					string key = parser.ReadToken().ToString();
					string value = parser.ReadToken().ToString();
					entries.Add(key, value);
				}
			}

			return new();
		}

		private static List<MapBrush> ParseBrushes(in TokenParser parser)
		{
			List<MapBrush> brushes = new(64);

			while (true)
			{
				brushes.Add(parser.ReadBrush());

				if (parser.PeekToken(TOKEN_END))
					break;
			}

			return brushes;
		}

		private static MapBrush ReadBrush(this TokenParser parser)
		{
			parser.MatchToken(TOKEN_START); // Consume open brace

			List<MapBrushFace> faces = new(64);
			while (!parser.IsAtEnd)
			{
				ReadOnlySpan<char> token = parser.PeekToken();
				if (token.EqualsInvariant(TOKEN_END))
				{
					// End of brush faces
					break;
				}

				faces.Add(parser.ReadBrushFace());
			}

			parser.MatchToken(TOKEN_END); // Consume close brace

			return new() { Faces = faces };
		}
		private static MapBrushFace ReadBrushFace(this TokenParser parser)
		{
			Vector3 v0 = parser.ReadVector3(false);
			Vector3 v1 = parser.ReadVector3(false);
			Vector3 v2 = parser.ReadVector3(false);

			ReadOnlySpan<char> textureName = parser.ReadToken(disableNumLock: true);
			float[] texU, texV;
			if (parser.PeekToken("["))
			{
				// Valve format
				texU = parser.ReadMatrix(4);
				texV = parser.ReadMatrix(4);
			}
			else
			{
				// Standard format
				Vector3 u = Vector3.Normalize(v0 - v1);
				Vector3 v = Vector3.Normalize(v2 - v1);
				texU = new [] { u.x, u.y, u.z, parser.ReadFloat() };
				texV = new [] { v.x, v.y, v.z, parser.ReadFloat() };
			}
			//Vector3 planeNormal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

			float rotation = parser.ReadFloat();
			Vector2 scale = new() { x = parser.ReadFloat(), y = parser.ReadFloat() };

			return new()
			{
				// Position = v0,
				// Normal = planeNormal,
				P1 = v0,
				P2 = v1,
				P3 = v2,
				TextureName = textureName.ToString(),
				TexU = texU,
				TexV = texV,
				Rotation = rotation,
				Scale = scale,
			};
		}
	}
}