//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public record BspOptions(float UV2Padding = 0.02f);
	
	public class MapBsp
	{
		public string MapName { get; init; }
		public float ImportScale { get; init; }
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		public TexInfo[] TexInfos { get; init; }
		public Plane[] Planes { get; init; }
		public BspNode[] Nodes { get; init; }
		public BspLeaf[] Leaves { get; init; }
		public int[] LeafFaces { get; init; }
		public bool[] UsedFaces { get; init; }

		public BspModel[] Models { get; init; }
		public BspVertex[] Vertices { get; init; }
		public BspFace[] Faces { get; init; }
		public int[] MeshVertices { get; init; }

		public List<BspEntity> Entities { get; init; }

		public BspOptions Options { get; init; }

		public MapGroupData[] Groups { get; init; }

		public void GatherResourceList(List<string> outPaths)
		{
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			MaterialNameLookup materialNameLookup = new(syncSettings);
			PrefabNameLookup prefabNameLookup = new(syncSettings);

			GatherResourceList(materialNameLookup, prefabNameLookup, outPaths);
		}

		public void GatherResourceList(MaterialNameLookup materialNameLookup, PrefabNameLookup prefabNameLookup, List<string> outPaths)
		{
			foreach (TexInfo texInfo in TexInfos)
			{
				if (!materialNameLookup.TryGetMaterialPathFromMapName(texInfo.Name, out string materialPath))
					continue;

				outPaths.Add(materialPath);
			}

			foreach (BspEntity entity in Entities)
			{
				if (!prefabNameLookup.TryGetPrefabPathFromMapName(entity.GetClassname(), out string prefabPath))
					continue;

				outPaths.Add(prefabPath);
			}
		}
	}

	// -----------------------------------------------------------------------------------------------------------------------------
	//		Map Types
	// -----------------------------------------------------------------------------------------------------------------------------
	public readonly struct MapGroupData
	{
		public string Name { get; init; }
		public bool IsNoExport { get; init; }
		public bool IsLayer { get; init; }

		public string LabelName => (IsLayer ? "layer: " : "group: ") + Name;
	}

	// -----------------------------------------------------------------------------------------------------------------------------
	//		BSPTypes
	// -----------------------------------------------------------------------------------------------------------------------------
	public readonly struct BspEntity : IEquatable<BspEntity>
	{
		public int SerialNumber { get; init; }
		public Dictionary<string, string> Entries { get; init; }
		public float ImportScale { get; init; }

		public bool Equals(BspEntity other)
		{
			return other.SerialNumber == SerialNumber;
		}

		public override bool Equals(object obj)
		{
			return obj is BspEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			return SerialNumber;
		}

		public static bool operator ==(BspEntity left, BspEntity right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(BspEntity left, BspEntity right)
		{
			return !left.Equals(right);
		}
	}

	public readonly struct TexInfo
	{
		public string Name { get; init; }
		public int Flags { get; init; }
		public int Contents { get; init; }
	}

	public readonly struct BspVertex
	{
		public Vector3 Position { get; init; }
		public Vector3 Normal { get; init; }
		public Color32 Color { get; init; }
		public Vector2 TexCoord { get; init; }
		public Vector2 LightmapCoord { get; init; }
	}

	public readonly struct BspFace
	{
		public int TexID { get; init; }
		public int Effect { get; init; }
		public int Type { get; init; } // 1=polygon, 2=patch, 3=mesh, 4=billboard
		
		public int VertexStartIdx { get; init; }
		public int NumVertices { get; init; }
		
		public int MeshVertexStartIdx { get; init; }
		public int NumMeshVertices { get; init; }
		
		public int LightmapIndex { get; init; }
		public Vector2Int LightmapStart { get; init; }
		public Vector2Int LightmapSize { get; init; }
		
		public Vector3 LightmapOrigin { get; init; }
		public Vector3 LightmapVector1 { get; init; }
		public Vector3 LightmapVector2 { get; init; }
		public Vector3 Normal { get; init; }
		
		public Vector2Int Size { get; init; }
	}

	public readonly struct BspModel
	{
		public Vector3 Min { get; init; }
		public Vector3 Max { get; init; }
		
		public int StartFaceIdx { get; init; }
		public int NumFaces { get; init; }
		
		public int StartBrushIdx { get; init; }
		public int NumBrushes { get; init; }
	}
	
	public readonly struct BspMeshVertex
	{
		public Vector3 Position { get; init; }
		public Vector3 Normal { get; init; }
		public Vector2 UV { get; init; }
		
		public int BaseIndex { get; init; }
	}

	public struct BspSubMesh
	{
		public int TexID { get; init; }
		public List<int> Triangles { get; init; }
	}

	public readonly struct BspNode
	{
		public int PlaneIndex { get; init; }
		
		public int Child1 { get; init; } // negative numbers are leafs
		public int Child2 { get; init; }
		public Vector3Int Min { get; init; }
		public Vector3Int Max { get; init; }
	}

	public readonly struct BspLeaf
	{
		public int VisCluster { get; init; }
		public int PortalArea { get; init; }
		
		public Vector3Int Min { get; init; }
		public Vector3Int Max { get; init; }
		
		public int LeafFaceStartIdx { get; init; }
		public int NumLeafFaces { get; init; }
		
		public int LeafBrushStartIdx { get; init; }
		public int NumLeafBrushes { get; init; }
	}

	// -----------------------------------------------------------------------------------------------------------------------------
	//		MD3 Types
	// -----------------------------------------------------------------------------------------------------------------------------
	public readonly struct MD3Surface
	{
		public Vector3[] Vertices { get; init; }
		public Vector2[] Uvs { get; init; }
		public Vector3[] Normals { get; init; }
		public string Texture { get; init; }
		public string Name { get; init; }

		public int DataSize => Vertices.Length * 16 + Vertices.Length * 4 + 176;
	}

	// -----------------------------------------------------------------------------------------------------------------------------
	//		Vector Conversions
	// -----------------------------------------------------------------------------------------------------------------------------
	public static class Q3VectorExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 Q3ToUnityVector(this Vector3 q3Vector, float scale = 1f)
			=> new Vector3(q3Vector.x, q3Vector.z, q3Vector.y) * scale;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 UnityToQ3Vector(this Vector3 unityVector, float scale = 1f)
			=> new Vector3(unityVector.x, unityVector.z, unityVector.y) / scale;

		public static bool TryParseQ3Vector(this string inVector, out Vector3 value)
		{
			string[] parts = inVector.Split(" ");

			// Vector, e.g. "x y z"
			if (parts.Length == 3 &&
			    parts[0].TryParseFloat(out float x) &&
			    parts[1].TryParseFloat(out float y) &&
			    parts[2].TryParseFloat(out float z))
			{
				value = new(x, y, z);
				return true;
			}

			// Float, e.g. "x"
			if (parts.Length == 1 && parts[0].TryParseFloat(out float f))
			{
				value = new(f, f, f);
				return true;
			}

			value = Vector3.zero;
			return false;
		}
	}

	// -----------------------------------------------------------------------------------------------------------------------------
	//		Colour Conversions
	// -----------------------------------------------------------------------------------------------------------------------------
	public static class Q3ColourExtensions
	{
		public static bool TryParseQ3Colour(this string inColour, out Color value)
		{
			if (inColour.IsNullOrEmpty())
			{
				value = Color.white;
				return false;
			}

			string[] parts = inColour.Split(" ");
			if (parts.Length != 3)
			{
				value = default;
				return false;
			}

			if (!parts[0].TryParseFloat(out float r)
		       || !parts[1].TryParseFloat(out float g)
		       || !parts[2].TryParseFloat(out float b))
			{
				value = default;
				return false;
			}

			if (r > 1f || g > 1f || b > 1f)
			{
				// Assume BYTE values, 0-255
				value = new(r / 255f, g / 255f, b / 255f);
				return true;
			}
			else
			{
				// Assume FLOAT values, 0-1
				value = new(r, g, b);
				return true;
			}
		}
	}
}