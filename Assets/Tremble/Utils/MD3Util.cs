//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
// This class based on BSP Map Tools for Unity by John Evans (evans3d512@gmail.com)
//

using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace TinyGoose.Tremble
{
	public static class MD3Util
	{
		private const int MAX_SURF_INDICES = 12000;

		private const string IDP3_SIGNATURE = "IDP3";
		private const int IDP3_VERSION = 15;

		public static bool HasModel(GameObject prefab, out Bounds bounds)
		{
			bool result = false;
			bounds = new();

			foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>())
			{
				MeshRenderer mr = mf.GetComponent<MeshRenderer>();
				if (!mr)
					continue;

				if (!mf.sharedMesh)
					continue;

				result = true;
				GetTransformedMeshVertices(mr.transform, isSkinnedMesh: false, mf.sharedMesh, ref bounds);
			}

			foreach (SkinnedMeshRenderer smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				if (!smr.sharedMesh)
					continue;

				result = true;
				GetTransformedMeshVertices(smr.transform, isSkinnedMesh: true, smr.sharedMesh, ref bounds);
			}

			return result;
		}

		private static Bounds GetPrefabBounds(GameObject prefab)
		{
			Bounds boundingBox = new();

			foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>())
			{
				MeshRenderer mr = mf.GetComponent<MeshRenderer>();
				if (!mr)
					continue;

				if (!mf.sharedMesh)
					continue;

				GetTransformedMeshVertices(mr.transform, isSkinnedMesh: false, mf.sharedMesh, ref boundingBox);
			}

			foreach (SkinnedMeshRenderer smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				if (!smr.sharedMesh)
					continue;
				GetTransformedMeshVertices(smr.transform, isSkinnedMesh: true, smr.sharedMesh, ref boundingBox);
			}

			return boundingBox;
		}

		public static Vector3 GetPrefabUnitySpawnOffset(GameObject prefab, float importScale)
		{
			Bounds boundingBox = GetPrefabBounds(prefab);

			Vector3 boundsMin = boundingBox.min.UnityToQ3Vector(scale: importScale);
			Vector3 boundsMax = boundingBox.max.UnityToQ3Vector(scale: importScale);
			Vector3 boundsCentre = (boundsMin + boundsMax) / 2f;

			return -boundsCentre.Q3ToUnityVector(importScale);
		}

		public static bool SaveMD3(GameObject prefab, string md3Path, MaterialNameLookup materialNameLookup, float importScale, out Bounds boundingBox)
		{
			// Gather MD3 surfaces to write from renderers
			List<MD3Surface> surfaces = GatherMD3SurfacesFromRenderers(prefab, materialNameLookup, out boundingBox, out int allSurfacesSize);
			if (surfaces.Count == 0)
				return false;

			// Now write the MD3 file
			int fileSize = allSurfacesSize + 164;
			string modelName = GetPathInFolder(md3Path, "baseq3");

			using FileStream stream = new(md3Path, FileMode.Create, FileAccess.Write, FileShare.None, 32768);
			using StreamWriteIO writer = new(stream);

			// IDP3 signature & version
			writer.WriteString(IDP3_SIGNATURE);
			writer.WriteInt32(IDP3_VERSION);

			// Model name (64 chars)
			writer.WriteString(modelName, 64);

			// Flags, frames, tags, surfaces, skins
			writer.WriteInt32(0); // flags 0
			writer.WriteInt32(1); // frames
			writer.WriteInt32(0); // tags 0
			writer.WriteInt32(surfaces.Count); // surfaces
			writer.WriteInt32(0); // skins 0

			// Write metrics
			const int FRAME_OFFSET = 108;
			writer.WriteInt32(FRAME_OFFSET);
			const int TAGS_OFFSET = 164;
			writer.WriteInt32(TAGS_OFFSET);
			const int SURFACE_OFFSET = 164;
			writer.WriteInt32(SURFACE_OFFSET);

			writer.WriteInt32(fileSize); // file size bytes

			// Write first frame
			Vector3 boundsMin = boundingBox.min.UnityToQ3Vector(scale: importScale);
			Vector3 boundsMax = boundingBox.max.UnityToQ3Vector(scale: importScale);
			Vector3 boundsCentre = (boundsMin + boundsMax) / 2f;
			FixBounds(ref boundsMin, ref boundsMax);

			writer.WriteFloat(boundsMin.x); // bounds (min)
			writer.WriteFloat(boundsMin.y);
			writer.WriteFloat(boundsMin.z);

			writer.WriteFloat(boundsMax.x); // bounds (max)
			writer.WriteFloat(boundsMax.y);
			writer.WriteFloat(boundsMax.z);

			writer.WriteFloat(0f); // origin
			writer.WriteFloat(0f);
			writer.WriteFloat(0f);

			writer.WriteFloat((boundsMax - boundsMin).magnitude); // radius

			writer.WriteString("frame1", 16); // frame name

			// Write surfaces
			for (int surfIdx = 0; surfIdx < surfaces.Count; surfIdx++)
			{
				MD3Surface surface = surfaces[surfIdx];
				int surfVertCount = surface.Vertices.Length;

				int triangleCount = surfVertCount / 3;
				int triangleOffset = 176;
				int shaderOffset = 108;
				int uvsOffset = triangleOffset + triangleCount * 12;
				int vtxOffset = uvsOffset + surfVertCount * 8;

				writer.WriteString(IDP3_SIGNATURE); // IDP3

				// Surface
				writer.WriteString(surface.Name, 64); // surf name
				writer.WriteInt32(0); // flags 0
				writer.WriteInt32(1); // frames
				writer.WriteInt32(1); // shaders

				writer.WriteInt32(surfVertCount);
				writer.WriteInt32(triangleCount);
				writer.WriteInt32(triangleOffset);
				writer.WriteInt32(shaderOffset);
				writer.WriteInt32(uvsOffset);
				writer.WriteInt32(vtxOffset);
				writer.WriteInt32(surface.DataSize);

				// Shader
				writer.WriteString($"models/{surface.Texture}.png", 64); // shader name
				writer.WriteInt32(surfIdx); // Shader index

				// Tris
				for (int triIdx = 0; triIdx < surfVertCount; triIdx++)
				{
					writer.WriteInt32(triIdx);
				}

				// Surfs
				for (int uvIdx = 0; uvIdx < surface.Uvs.Length; uvIdx++)
				{
					writer.WriteFloat(surface.Uvs[uvIdx].x);
					writer.WriteFloat(1f - surface.Uvs[uvIdx].y);
				}

				// Verts
				for (int vertIdx = 0; vertIdx < surfVertCount; vertIdx++)
				{
					Vector3 md3Vector = surface.Vertices[vertIdx].UnityToQ3Vector(scale: importScale);
					md3Vector -= boundsCentre;

					writer.WriteInt16((short)(md3Vector.x * 64f));
					writer.WriteInt16((short)(md3Vector.y * 64f));
					writer.WriteInt16((short)(md3Vector.z * 64f));
					writer.WriteNormalAsLatLong(surface.Normals[vertIdx]);
				}
			}

			return true;
		}

		private static List<MD3Surface> GatherMD3SurfacesFromRenderers(GameObject prefab, MaterialNameLookup materialNameLookup, out Bounds boundingBox, out int allSurfacesSize)
		{
			List<MD3Surface> surfaces = new();
			allSurfacesSize = 0;

			boundingBox = new();

			foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>())
			{
				MeshRenderer mr = mf.GetComponent<MeshRenderer>();
				if (!mr)
					continue;

				AddMeshToMD3(mr.transform, isSkinnedMesh: false, mf.sharedMesh, mr.sharedMaterials, surfaces, materialNameLookup, ref allSurfacesSize, ref boundingBox);
			}

			foreach (SkinnedMeshRenderer smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				AddMeshToMD3(smr.transform, isSkinnedMesh: true, smr.sharedMesh, smr.sharedMaterials, surfaces, materialNameLookup, ref allSurfacesSize, ref boundingBox);
			}

			return surfaces;
		}

		private static void FixBounds(ref Vector3 min, ref Vector3 max)
		{
			Vector3 newMin = new(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Min(min.z, max.z));
			Vector3 newMax = new(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y), Mathf.Max(min.z, max.z));

			min = newMin;
			max = newMax;
		}

		private static Vector3[] GetTransformedMeshVertices(Transform meshTransform, bool isSkinnedMesh, Mesh unityMesh, ref Bounds allBounds)
		{
			// Gather mesh data
			Vector3[] meshVertices = unityMesh.vertices;

			Vector3 min = allBounds.min;
			Vector3 max = allBounds.max;

			Matrix4x4 ltwMatrix = meshTransform.localToWorldMatrix;
			for (int i = 0; i < meshVertices.Length; i++)
			{
				// Transform vertex according to the prefab root it's from
				if (isSkinnedMesh)
				{
					meshVertices[i] = meshTransform.TransformVector(meshVertices[i]);
				}
				else
				{
					meshVertices[i] = ltwMatrix.MultiplyPoint(meshVertices[i]);
				}

				// Clamp vertices, MD3 uses Int16 vectors for vertex positions, so each vertex can be in range from -12.75 to 12.75
				float meshVertX = meshVertices[i].x; // = Mathf.Clamp(meshVertices[i].x, -12.75f, +12.75f);
				float meshVertY = meshVertices[i].y; // = Mathf.Clamp(meshVertices[i].y, -12.75f, +12.75f);
				float meshVertZ = meshVertices[i].z; // = Mathf.Clamp(meshVertices[i].z, -12.75f, +12.75f);

				// Inlined Mathf.Min and Mathf.Max
				min.x = min.x < meshVertX ? min.x : meshVertX;
				min.y = min.y < meshVertY ? min.y : meshVertY;
				min.z = min.z < meshVertZ ? min.z : meshVertZ;

				max.x = max.x > meshVertX ? max.x : meshVertX;
				max.y = max.y > meshVertY ? max.y : meshVertY;
				max.z = max.z > meshVertZ ? max.z : meshVertZ;
			}
			allBounds.SetMinMax(min, max);

			return meshVertices;
		}

		private static void AddMeshToMD3(Transform meshTransform, bool isSkinnedMesh, Mesh unityMesh, Material[] materials, List<MD3Surface> surfaces, MaterialNameLookup materialNameLookup, ref int allSurfacesSize, ref Bounds allBounds)
		{
			// Gather triangles per submesh
			List<int[]> submeshTriIndices = new(unityMesh.subMeshCount);
			for (int i = 0; i < unityMesh.subMeshCount; i++)
			{
				submeshTriIndices.Add(unityMesh.GetTriangles(i));
			}

			// Gather mesh data
			Vector3[] meshVertices = GetTransformedMeshVertices(meshTransform, isSkinnedMesh, unityMesh, ref allBounds);
			Vector3[] meshNormals = unityMesh.normals;
			Vector2[] meshUVs = unityMesh.uv;

			// Write one surface per Unity submesh
			for (int submeshIdx = 0; submeshIdx < unityMesh.subMeshCount; submeshIdx++)
			{
				if (submeshTriIndices[submeshIdx].Length >= MAX_SURF_INDICES)
				{
					Debug.LogError("Mesh has too many surfaces - skipping!");
					continue;
				}

				// Add verts and triangles
				int vertIndexCount = submeshTriIndices[submeshIdx].Length;

				Vector3[] surfaceVertices = new Vector3[vertIndexCount];
				Vector3[] surfaceNormals = new Vector3[vertIndexCount];
				Vector2[] surfaceUVs = new Vector2[vertIndexCount];

				int[] thisSubmeshTriIndices = submeshTriIndices[submeshIdx];
				for (int vIdx = 0; vIdx < vertIndexCount; vIdx++)
				{
					int index = thisSubmeshTriIndices[vIdx];

					surfaceVertices[vIdx] = meshVertices[index];
					surfaceNormals[vIdx] = meshNormals[index];

					// Some meshes have no UVs (uh-oh...)
					if (meshUVs.Length > 0)
					{
						surfaceUVs[vIdx] = meshUVs[index];
					}
				}

				// Write material names
				string textureName;
				if (materials[submeshIdx] && materialNameLookup != null)
				{
					string path = TrembleAssetLoader.GetPath(materials[submeshIdx]);
					textureName = materialNameLookup.GetPrefabNameFromMaterialPath(path);
				}
				else
				{
					textureName = "missing";
				}

				// Create surface
				MD3Surface currentSurface = new()
				{
					Name = $"surface{surfaces.Count}",
					Texture = textureName,

					Vertices = surfaceVertices,
					Normals = surfaceNormals,
					Uvs = surfaceUVs,
				};

				surfaces.Add(currentSurface);
				allSurfacesSize += currentSurface.DataSize;
			}
		}

		private static string GetPathInFolder(string path, string folder)
		{
			int i = path.IndexOfInvariant(folder + "/");
			return path.Substring(i + folder.Length + 1);
		}
	}
}