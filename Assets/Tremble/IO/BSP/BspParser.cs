// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
// This class based on BSP Map Tools for Unity by John Evans (evans3d512@gmail.com)
//

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public static class BspParser
	{
		public static MapBsp Parse(string bspPath, float importScale, BspOptions options = null, MapGroupData[] foundGroups = null)
		{
			StreamReadIO reader = new(bspPath);

			string signature = new (reader.ReadChars(4));
			int version = reader.ReadInt32();
			if (signature != "IBSP" || version != 46)
			{
				Debug.LogWarning($"BSP Importer: Unknown signature or version. Expected IBSP, 46. Got {signature}, {version}");
			}

			int entitiesOffset = reader.ReadInt32();
			int entitiesLength = reader.ReadInt32(); // 16
			int texturesOffset = reader.ReadInt32();
			int texturesLength = reader.ReadInt32(); // 24

			int planesOffset = reader.ReadInt32();
			int planesLength = reader.ReadInt32();
			int nodesOffset = reader.ReadInt32();
			int nodesLength = reader.ReadInt32();
			int leavesOffset = reader.ReadInt32();
			int leavesLength = reader.ReadInt32();
			int leafFacesOffset = reader.ReadInt32();
			int leaffacesLength = reader.ReadInt32();

			reader.Seek(64);
			int modelsOffset = reader.ReadInt32();
			int modelsLength = reader.ReadInt32();
			reader.Seek(88); // Vertices
			int verticesOffset = reader.ReadInt32();
			int verticesLength = reader.ReadInt32();
			int mVerticesOffset = reader.ReadInt32();
			int mVerticesLength = reader.ReadInt32();
			reader.Seek(112); // Faces
			int facesOffset = reader.ReadInt32();
			int facesLength = reader.ReadInt32();

			int vertexCount = verticesLength / 44;
			int mVertexCount = mVerticesLength / 4;
			int faceCount = facesLength / 104;
			int texCount = texturesLength / 72;
			int modelCount = modelsLength / 40;

			int planesCount = planesLength / 16;
			int nodesCount = nodesLength / 36;
			int leavesCount = leavesLength / 48;
			int leafFacesCount = leaffacesLength / 4;

			MapBsp newMapBsp = new() {
				MapName = Path.GetFileNameWithoutExtension(bspPath),
				ImportScale = importScale,
				Options = options ?? new(),

				TexInfos = new TexInfo[texCount],
				Planes = new Plane[planesCount],
				Nodes = new BspNode[nodesCount],
				Leaves = new BspLeaf[leavesCount],
				LeafFaces = new int[leafFacesCount],

				Models = new BspModel[modelCount],
				Vertices = new BspVertex[vertexCount],
				Faces = new BspFace[faceCount],
				MeshVertices = new int[mVertexCount],
				Entities = new(),

				UsedFaces = new bool[faceCount],

				Groups = foundGroups,
			};


			reader.Seek(texturesOffset);
			for (int i = 0; i < texCount; i++)
			{
				string tempName = reader.ReadString(64);
				if (tempName.StartsWithInvariant("textures/"))
				{
					tempName = tempName.Substring("textures/".Length);
				}

				newMapBsp.TexInfos[i] = new()
				{
					Name = tempName,
					Flags = reader.ReadInt32(),
					Contents = reader.ReadInt32(),
				};
			}

			reader.Seek(planesOffset);
			for (int i = 0; i < planesCount; i++)
			{
				Vector3 planeNormal = reader.ReadVector3(convertToQ3: true);

				newMapBsp.Planes[i] = new()
				{
					normal = planeNormal,
					distance = reader.ReadSingle() * importScale,
				};
			}

			reader.Seek(leavesOffset);
			for (int i = 0; i < leavesCount; i++)
			{
				newMapBsp.Leaves[i] = new()
				{
					VisCluster = reader.ReadInt32(),
					PortalArea = reader.ReadInt32(),
					Min = reader.ReadVector3Int(),
					Max = reader.ReadVector3Int(),
					LeafFaceStartIdx = reader.ReadInt32(),
					NumLeafFaces = reader.ReadInt32(),
					LeafBrushStartIdx = reader.ReadInt32(),
					NumLeafBrushes = reader.ReadInt32(),
				};
			}

			reader.Seek(nodesOffset);
			for (int i = 0; i < nodesCount; i++)
			{
				newMapBsp.Nodes[i] = new()
				{
					PlaneIndex = reader.ReadInt32(),
					Child1 = reader.ReadInt32(),
					Child2 = reader.ReadInt32(),
					Min = reader.ReadVector3Int(),
					Max = reader.ReadVector3Int(),
				};
			}

			reader.Seek(leafFacesOffset);
			for (int i = 0; i < leafFacesCount; i++)
			{
				newMapBsp.LeafFaces[i] = reader.ReadInt32();
			}

			reader.Seek(modelsOffset);
			for (int i = 0; i < modelCount; i++)
			{
				newMapBsp.Models[i] = new()
				{
					Min = reader.ReadVector3(convertToQ3: true, scale: importScale),
					Max = reader.ReadVector3(convertToQ3: true, scale: importScale),
					StartFaceIdx = reader.ReadInt32(),
					NumFaces = reader.ReadInt32(),
					StartBrushIdx = reader.ReadInt32(),
					NumBrushes = reader.ReadInt32(),
				};
			}

			reader.Seek(verticesOffset);
			for (int i = 0; i < vertexCount; i++)
			{
				newMapBsp.Vertices[i] = new()
				{
					Position = reader.ReadVector3(convertToQ3: true, scale: importScale),
					TexCoord = reader.ReadUV(),
					LightmapCoord = reader.ReadUV(),
					Normal = Vector3.Normalize(reader.ReadVector3(convertToQ3: true)),
					Color = reader.ReadColour(),
				};
			}

			reader.Seek(facesOffset);
			for (int i = 0; i < faceCount; i++)
			{
				newMapBsp.Faces[i] = new()
				{
					TexID = reader.ReadInt32(),
					Effect = reader.ReadInt32(),
					Type = reader.ReadInt32(),
					VertexStartIdx = reader.ReadInt32(),
					NumVertices = reader.ReadInt32(),
					MeshVertexStartIdx = reader.ReadInt32(),
					NumMeshVertices = reader.ReadInt32(),
					LightmapIndex = reader.ReadInt32(),
					LightmapStart = reader.ReadVector2Int(),
					LightmapSize = reader.ReadVector2Int(),
					LightmapOrigin = reader.ReadVector3Int(),
					LightmapVector1 = reader.ReadVector3(),
					LightmapVector2 = reader.ReadVector3(),
					Normal = reader.ReadVector3(convertToQ3: true),
					Size = reader.ReadVector2Int(),
				};
			}

			reader.Seek(mVerticesOffset);
			for (int i = 0; i < mVertexCount; i++)
			{
				newMapBsp.MeshVertices[i] = reader.ReadInt32();
			}
			
			// Read entity key/value pairs
			reader.Seek(entitiesOffset);

			List<string> kvPairs = new();

			int entityNumber = 0;
			int endOfEntities = entitiesOffset + entitiesLength - 1;
			int charsToRead = endOfEntities - reader.Position;
			string entities = new(reader.ReadChars(charsToRead));

			TokenParser tokenParser = new(entities);
			do
			{
				ReadOnlySpan<char> token = tokenParser.ReadToken();
				if (token == null || token.Length == 0)
				{
					kvPairs.Add("");
					continue;
				}

				switch (token[0])
				{
					case '{':
					{
						continue;
					}
					case '}':
					{
						Dictionary<string, string> entries = new();
						for (int kvIdx = 0; kvIdx < kvPairs.Count; kvIdx += 2)
						{
							entries.Add(kvPairs[kvIdx], kvPairs[kvIdx+1]);
						}
					
						newMapBsp.Entities.Add(new() { SerialNumber = entityNumber++, Entries = entries, ImportScale = newMapBsp.ImportScale});
						kvPairs.Clear();
					
						continue;
					}
					default:
					{
						kvPairs.Add(token.ToString());
						break;
					}
				}
			} while (!tokenParser.IsAtEnd);

			return newMapBsp;
		}

		private static float GetTriangleAreaSqr(in Vector3 v1, in Vector3 v2, in Vector3 v3)
		{
			// Sqr Magnitudes inlined!
			float ax = v1.x - v2.x;
			float ay = v1.y - v2.y;
			float az = v1.z - v2.z;
			float a = ax * ax + ay * ay + az * az;

			float bx = v2.x - v3.x;
			float by = v2.y - v3.y;
			float bz = v2.z - v3.z;
			float b = bx * bx + by * by + bz * bz;

			float cx = v2.x - v3.x;
			float cy = v2.y - v3.y;
			float cz = v2.z - v3.z;
			float c = cx * cx + cy * cy + cz * cz;

			float s = (a + b + c) * 0.25f;

			float surfAreaSqr = Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
			return float.IsNaN(surfAreaSqr) ? 0f : surfAreaSqr;
		}

		public static void GetFaceList(MapBsp mapBsp, BspLeaf leaf, List<int> receiver)
		{
			int faceMin = mapBsp.Models[0].StartFaceIdx;
			int faceMax = faceMin + mapBsp.Models[0].NumFaces;

			for (int faceOffset = leaf.LeafFaceStartIdx; faceOffset < leaf.LeafFaceStartIdx + leaf.NumLeafFaces; faceOffset++)
			{
				int leafFaceIdx = mapBsp.LeafFaces[faceOffset];

				if (leafFaceIdx < faceMin || leafFaceIdx > faceMax)
					continue;

				if (mapBsp.UsedFaces[leafFaceIdx])
					continue;

				receiver.Add(leafFaceIdx);
				mapBsp.UsedFaces[leafFaceIdx] = true;
			}
		}

		public static void GetFaceList(MapBsp mapBsp, BspNode node, List<int> receiver)
		{
			if (node.Child1 < 0)
			{
				GetFaceList(mapBsp, mapBsp.Leaves[-node.Child1 - 1], receiver);
			}
			else
			{
				GetFaceList(mapBsp, mapBsp.Nodes[node.Child1], receiver);
			}

			if (node.Child2 < 0)
			{
				GetFaceList(mapBsp, mapBsp.Leaves[-node.Child2 - 1], receiver);
			}
			else
			{
				GetFaceList(mapBsp, mapBsp.Nodes[node.Child2], receiver);
			}
		}

		private static void GetSurfaceAreaSqr(MapBsp mapBsp, BspLeaf leaf, ref float receiver, HashSet<int> faceList)
		{
			int faceMin = mapBsp.Models[0].StartFaceIdx;
			int faceMax = faceMin + mapBsp.Models[0].NumFaces;

			for (int faceOffset = leaf.LeafFaceStartIdx; faceOffset < leaf.LeafFaceStartIdx + leaf.NumLeafFaces; faceOffset++)
			{
				if (mapBsp.LeafFaces[faceOffset] < faceMin || mapBsp.LeafFaces[faceOffset] > faceMax)
					continue;

				int leafFaceIdx = mapBsp.LeafFaces[faceOffset];
				if (faceList.Contains(leafFaceIdx))
					continue;

				BspFace meshFace = mapBsp.Faces[leafFaceIdx];
				for (int vertexOffset = meshFace.MeshVertexStartIdx; vertexOffset < meshFace.MeshVertexStartIdx + meshFace.NumMeshVertices; vertexOffset += 3)
				{
					receiver += GetTriangleAreaSqr
					(
						mapBsp.Vertices[mapBsp.MeshVertices[vertexOffset + 0] + meshFace.VertexStartIdx].Position,
						mapBsp.Vertices[mapBsp.MeshVertices[vertexOffset + 1] + meshFace.VertexStartIdx].Position,
						mapBsp.Vertices[mapBsp.MeshVertices[vertexOffset + 2] + meshFace.VertexStartIdx].Position
					);
				}

				faceList.Add(leafFaceIdx);
			}
		}

		public static void GetSurfaceAreaSqr(MapBsp mapBsp, BspNode node, ref float receiver, HashSet<int> faceList)
		{
			if (node.Child1 < 0)
			{
				GetSurfaceAreaSqr(mapBsp, mapBsp.Leaves[-node.Child1 - 1], ref receiver, faceList);
			}
			else
			{
				GetSurfaceAreaSqr(mapBsp, mapBsp.Nodes[node.Child1], ref receiver, faceList);
			}

			if (node.Child2 < 0)
			{
				GetSurfaceAreaSqr(mapBsp, mapBsp.Leaves[-node.Child2 - 1], ref receiver, faceList);
			}
			else
			{
				GetSurfaceAreaSqr(mapBsp, mapBsp.Nodes[node.Child2], ref receiver, faceList);
			}
		}


		public static void DumpToFile(this MapBsp mapBsp, string file)
		{
			using FileStream fs = new(file, FileMode.Create, FileAccess.Write);
			using StreamWriter sw = new(fs);

			sw.WriteLine($"m_TexInfos[{mapBsp.TexInfos.Length}]:");
			for (int i = 0; i < mapBsp.TexInfos.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.TexInfos[i].Contents} {mapBsp.TexInfos[i].Flags} {mapBsp.TexInfos[i].Name}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Planes[{mapBsp.Planes.Length}]:");
			for (int i = 0; i < mapBsp.Planes.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.Planes[i].normal} {mapBsp.Planes[i].distance}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Nodes[{mapBsp.Nodes.Length}]:");
			for (int i = 0; i < mapBsp.Nodes.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.Nodes[i].Child1} {mapBsp.Nodes[i].Child2} {mapBsp.Nodes[i].PlaneIndex} {mapBsp.Nodes[i].Min} {mapBsp.Nodes[i].Max}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Leaves[{mapBsp.Leaves.Length}]:");
			for (int i = 0; i < mapBsp.Leaves.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.Leaves[i].Min} {mapBsp.Leaves[i].Max} {mapBsp.Leaves[i].LeafBrushStartIdx} {mapBsp.Leaves[i].NumLeafBrushes} {mapBsp.Leaves[i].LeafFaceStartIdx} {mapBsp.Leaves[i].NumLeafFaces}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.LeafFaces[{mapBsp.LeafFaces.Length}]:");
			for (int i = 0; i < mapBsp.LeafFaces.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.LeafFaces[i]}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.UsedFaces[{mapBsp.UsedFaces.Length}]:");
			for (int i = 0; i < mapBsp.UsedFaces.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.UsedFaces[i]}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Models[{mapBsp.Models.Length}]:");
			for (int i = 0; i < mapBsp.Models.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.Models[i].Min} {mapBsp.Models[i].Max} {mapBsp.Models[i].StartBrushIdx} {mapBsp.Models[i].NumBrushes} {mapBsp.Models[i].StartFaceIdx} {mapBsp.Models[i].NumFaces}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Vertices[{mapBsp.Vertices.Length}]:");
			for (int i = 0; i < mapBsp.Vertices.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.Vertices[i].Position} {mapBsp.Vertices[i].Normal} {mapBsp.Vertices[i].Color} {mapBsp.Vertices[i].TexCoord} {mapBsp.Vertices[i].LightmapCoord}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Faces[{mapBsp.Faces.Length}]:");
			for (int i = 0; i < mapBsp.Faces.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.Faces[i].VertexStartIdx} {mapBsp.Faces[i].NumVertices} {mapBsp.Faces[i].MeshVertexStartIdx} {mapBsp.Faces[i].NumMeshVertices} {mapBsp.Faces[i].Type}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.MeshVertices[{mapBsp.MeshVertices.Length}]:");
			for (int i = 0; i < mapBsp.MeshVertices.Length; i++)
			{
				sw.WriteLine($"  [{i}]: {mapBsp.MeshVertices[i]}");
			}
			sw.WriteLine();

			sw.WriteLine($"bsp.Entities[{mapBsp.Entities.Count}]:");
			for (int i = 0; i < mapBsp.Entities.Count; i++)
			{
				sw.Write($"  [{i}]: {mapBsp.Entities[i].GetClassname()} {{");
				foreach (string key in mapBsp.Entities[i].Entries.Keys)
				{
					sw.Write($" {key}=\"{mapBsp.Entities[i].Entries[key]}\"");
				}
				sw.WriteLine(" }");
			}
			sw.WriteLine();
		}
	}
}