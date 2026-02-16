// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public class MeshBuilder
	{
		public delegate Material FindMaterialFunc(string name);

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Setup
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly TrembleMapImportSettings m_ImportSettings;
		private readonly string m_WorldspawnName;
		private readonly float m_SmoothingAngle;
		private readonly float m_MaxMeshSurfaceArea;
		private readonly FindMaterialFunc m_MaterialFinder;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private int m_MeshCounter;

		private readonly List<int> m_FaceIndicesBuffer = new();
		private readonly List<BspMeshVertex> m_MeshVerticesBuffer = new(2048);
		private readonly List<BspSubMesh> m_MeshSubmeshesBuffer = new(16);
		private readonly Dictionary<int, int> m_BaseIndexToIndex = new(2048);

		public MeshBuilder(TrembleMapImportSettings importSettings, FindMaterialFunc materialFinder, string worldspawnName, float smoothingAngle, float maxMeshSurfaceArea)
		{
			m_ImportSettings = importSettings;
			m_WorldspawnName = worldspawnName;
			m_SmoothingAngle = smoothingAngle;
			m_MaxMeshSurfaceArea = maxMeshSurfaceArea;
			m_MaterialFinder = materialFinder;
		}

		private static readonly HashSet<int> s_InnerFaceList = new(2048);
		public void BuildMesh_Recursive(MapBsp mapBsp, Transform parent, int nodeID, HashSet<int> faceList, float importScale, int unityLayer, List<GameObject> intoListOrNull = null)
		{
			// Not a leaf - recurse unless smol
			if (nodeID >= 0)
			{
				bool subdivide = TrembleSyncSettings.Get().SubdivideAllLeafs;
				ref BspNode node = ref mapBsp.Nodes[nodeID];

				if (!subdivide)
				{
					float sqrSurfArea = 0;
					BspParser.GetSurfaceAreaSqr(mapBsp, node, ref sqrSurfArea, faceList);

					subdivide = (sqrSurfArea > m_MaxMeshSurfaceArea * m_MaxMeshSurfaceArea);
				}

				if (subdivide)
				{
					s_InnerFaceList.Clear();
					BuildMesh_Recursive(mapBsp, parent, node.Child1, s_InnerFaceList, importScale, unityLayer, intoListOrNull);
					BuildMesh_Recursive(mapBsp, parent, node.Child2, s_InnerFaceList, importScale, unityLayer, intoListOrNull);

					// Don't build this one
					return;
				}
			}
			
			// This is a leaf, or as small as we want to go - create!
			Bounds bounds = new();
			if (nodeID < 0)
			{
				bounds.min = new Vector3(mapBsp.Leaves[-nodeID - 1].Min.x, mapBsp.Leaves[-nodeID - 1].Min.z, mapBsp.Leaves[-nodeID - 1].Min.y) * importScale;
				bounds.max = new Vector3(mapBsp.Leaves[-nodeID - 1].Max.x, mapBsp.Leaves[-nodeID - 1].Max.z, mapBsp.Leaves[-nodeID - 1].Max.y) * importScale;
			}
			else
			{
				bounds.min = new Vector3(mapBsp.Nodes[nodeID].Min.x, mapBsp.Nodes[nodeID].Min.z, mapBsp.Nodes[nodeID].Min.y) * importScale;
				bounds.max = new Vector3(mapBsp.Nodes[nodeID].Max.x, mapBsp.Nodes[nodeID].Max.z, mapBsp.Nodes[nodeID].Max.y) * importScale;
			}

			GameObject meshGo = BuildMesh(mapBsp, parent, nodeID, true, m_WorldspawnName, true, bounds.center, true);
			if (meshGo)
			{
				meshGo.layer = unityLayer;
				intoListOrNull?.Add(meshGo);
			}
		}

		public GameObject BuildMesh(MapBsp mapBsp, Transform parent, int sourceID, bool isBspRoot, string modelName, bool isFromNode, Vector3 origin, bool isStatic, Quaternion? rotation = null)
		{
			// Thank you again John Evans. This method would have been awful to figure out by myself!
		
			// Collect face indices
			m_FaceIndicesBuffer.Clear();
			if (isFromNode)
			{
				if (sourceID < 0)
				{
					BspParser.GetFaceList(mapBsp, mapBsp.Leaves[-sourceID - 1], m_FaceIndicesBuffer);
				}
				else
				{
					BspParser.GetFaceList(mapBsp,  mapBsp.Nodes[sourceID], m_FaceIndicesBuffer);
				}
			}
			else
			{
				for (int i = 0; i < mapBsp.Models[sourceID].NumFaces; i++)
				{
					m_FaceIndicesBuffer.Add(mapBsp.Models[sourceID].StartFaceIdx + i);
				}
			}

			if (m_FaceIndicesBuffer.Count == 0)
				return null;

			// Gather vertices and submeshes
			m_MeshVerticesBuffer.Clear();
			m_MeshSubmeshesBuffer.Clear();
			m_BaseIndexToIndex.Clear();

			// Get faces for this mesh
			foreach (BspFace face in m_FaceIndicesBuffer.Select(faceIdx => mapBsp.Faces[faceIdx]))
			{
				// Get each vertex for this face
				for (int faceVertIdx = face.MeshVertexStartIdx; faceVertIdx < face.MeshVertexStartIdx + face.NumMeshVertices; faceVertIdx++)
				{
					int baseIdx = face.VertexStartIdx + mapBsp.MeshVertices[faceVertIdx];
					
					// Find or add a vertex to this mesh
					int localVertIndex = FindOrAddVertexForBaseIndex(baseIdx, mapBsp, origin, rotation);
					AddVertexToSubmeshWithTexID(m_MeshSubmeshesBuffer, face.TexID, localVertIndex);
				}
			}

			// Discover and apply materials
			List<Material> meshMaterials = new(m_MeshSubmeshesBuffer.Count);
			{
				for (int materialIdx = 0; materialIdx < m_MeshSubmeshesBuffer.Count; materialIdx++)
				{
					string matName = mapBsp.TexInfos[m_MeshSubmeshesBuffer[materialIdx].TexID].Name;

					// Skip? Skip!
					if (matName.ContainsInvariant(TBConsts.SKIP_TEXTURE, caseSensitive: true))
					{
						m_MeshSubmeshesBuffer.RemoveAt(materialIdx);
						materialIdx--;

						continue;
					}

					Material foundMaterial = m_MaterialFinder.Invoke(matName);
					meshMaterials.Add(foundMaterial);

					if (foundMaterial)
					{
						m_ImportSettings.AddDependency(foundMaterial);
					}
				}
			}

			// Create Unity mesh
			Mesh mesh = CreateUnityMesh(isBspRoot, ref modelName, m_MeshVerticesBuffer, m_MeshSubmeshesBuffer, origin);
			m_ImportSettings.SaveObjectInMap(mesh.name, mesh);

			// Create model GameObject and attach mesh
			GameObject modelObject = new(modelName);
			modelObject.transform.parent = parent;
			modelObject.transform.position = origin;
			modelObject.AddComponent<MeshFilter>().mesh = mesh;

			modelObject.AddComponent<MeshRenderer>().sharedMaterials = meshMaterials.ToArray();

			bool isTinyMesh = mesh.triangles.Length == 0 || Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.y, mesh.bounds.extents.z) < 0.1f;
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			// Generate mesh collider
			if (!isTinyMesh)
			{
				Mesh collisionMesh;
				if (syncSettings.PipelineSimplifyCollisionMeshes)
				{
					using TrembleTimerScope _ = new(TrembleTimer.Context.SimplifyCollisionMeshes);
					collisionMesh = GenerateSimplifiedUnityCollisionMesh(mesh);
				}
				else
				{
					collisionMesh = Mesh.Instantiate(mesh);
				}

				collisionMesh.name = mesh.name.Replace("_mesh", "_collision");

				m_ImportSettings.SaveObjectInMap(collisionMesh.name, collisionMesh);
				modelObject.AddComponent<MeshCollider>().sharedMesh = collisionMesh;
			}
			// Calculate lightmap UVs
#if UNITY_EDITOR
			if (isStatic && syncSettings.PipelineUnwrapUV2 && !isTinyMesh)
			{
				using TrembleTimerScope _ = new(TrembleTimer.Context.GenerateUV2);
				Unwrapping.GenerateSecondaryUVSet(mesh, new()
				{
					hardAngle = 70.0f,
					angleError = 8.0f,
					areaError = 15.0f,
					packMargin = mapBsp.Options.UV2Padding,
				});
			}
#endif

			modelObject.isStatic = isStatic;
			return modelObject;
		}

		private Mesh CreateUnityMesh(bool isBspRoot, ref string modelName, List<BspMeshVertex> meshVertices, List<BspSubMesh> meshSubmeshes, Vector3 origin)
		{
			IndexFormat meshFormat = IndexFormat.UInt16;
			foreach (BspSubMesh submesh in meshSubmeshes)
			{
				if (submesh.Triangles.Count * 3 > 65535)
				{
					Debug.Log($"Note: Mesh '{modelName}' using 32-bit indices due to high index count. Render performance may be reduced!");
					meshFormat = IndexFormat.UInt32;
					break;
				}
			}

			// Create Unity mesh
			Mesh mesh = new()
			{
				indexFormat = meshFormat,
				subMeshCount = meshSubmeshes.Count,
				vertices = meshVertices.Select(mv => mv.Position).ToArray(),
				normals = meshVertices.Select(mv => mv.Normal).ToArray(),
				uv = meshVertices.Select(mv => mv.UV).ToArray()
			};
			
			for (int i = 0; i < meshSubmeshes.Count; i++)
			{
				mesh.SetTriangles(meshSubmeshes[i].Triangles.ToArray(), i);
			}
			mesh.RecalculateBounds();
			mesh.RecalculateTangents();

			//CalculateUVScale(mesh, origin);
			
			// Name the mesh
			if (isBspRoot)
			{
				modelName += $"_{++m_MeshCounter}";
				mesh.name = $"{modelName}_mesh";
			}
			else
			{
				mesh.name = $"{modelName}_{++m_MeshCounter}_mesh";
			}

			// Smooth out normals if required
			if (m_SmoothingAngle > 0 && TrembleSyncSettings.Get().PipelineSmoothMeshNormals)
			{
				using TrembleTimerScope _ = new(TrembleTimer.Context.SmoothMeshNormals);
				SmoothMeshNormals(mesh, m_SmoothingAngle);
			}

			return mesh;
		}

		private void CalculateUVScale(Mesh mesh, Vector3 origin)
		{
			List<Vector2> oldUVs = new();
			List<Vector2> newUVs = new();
			mesh.GetUVs(0, oldUVs);
			mesh.GetUVs(0, newUVs);

			Dictionary<int, Vector2> processedUVs = new();

			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				for (uint j = mesh.GetIndexStart(i); j < mesh.GetIndexCount(i); j += 3)
				{
					int i0 = mesh.triangles[j + 0];
					int i1 = mesh.triangles[j + 1];
					int i2 = mesh.triangles[j + 2];

					Vector3 v0 = mesh.vertices[i0];
					Vector3 v1 = mesh.vertices[i1];
					Vector3 v2 = mesh.vertices[i2];

					Vector2 uv0 = mesh.uv[i0];
					Vector2 uv1 = mesh.uv[i1];
					Vector2 uv2 = mesh.uv[i2];

					Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
					Vector4 t = mesh.tangents[i0];
					Vector3 tangent = ((Vector3) t).normalized;
					Vector3 bitangent = Vector3.Cross(normal, tangent).normalized * t.w;

					int uCount = 0;
					float uDerivative = 0;

					Vector3 diff10 = v1 - v0;
					Vector3 diff20 = v2 - v0;
					Vector3 diff21 = v2 - v1;
					Vector2 uvDiff10 = uv1 - uv0;
					Vector2 uvDiff20 = uv2 - uv0;
					Vector2 uvDiff21 = uv2 - uv1;

					if (uvDiff10.x != 0)
					{
						uDerivative += Mathf.Abs(Vector3.Dot(tangent, diff10) / uvDiff10.x);
						uCount++;
					}

					if (uvDiff20.x != 0)
					{
						uDerivative += Mathf.Abs(Vector3.Dot(tangent, diff20) / uvDiff20.x);
						uCount++;
					}

					if (uvDiff21.x != 0)
					{
						uDerivative += Mathf.Abs(Vector3.Dot(tangent, diff21) / uvDiff21.x);
						uCount++;
					}

					int vCount = 0;
					float vDerivative = 0;
					if (uvDiff10.y != 0)
					{
						vDerivative += Mathf.Abs(Vector3.Dot(bitangent, diff10) / uvDiff10.y);
						vCount++;
					}

					if (uvDiff20.y != 0)
					{
						vDerivative += Mathf.Abs(Vector3.Dot(bitangent, diff20) / uvDiff20.y);
						vCount++;
					}

					if (uvDiff21.y != 0)
					{
						vDerivative += Mathf.Abs(Vector3.Dot(bitangent, diff21) / uvDiff21.y);
						vCount++;
					}


					Vector2 scale = new Vector2(uDerivative / uCount, vDerivative / vCount);
					
					Vector3 center = Vector3.Min(Vector3.Min(v0, v1), v2);
					Vector2 uvCenter = Vector2.Min(Vector2.Min(uv0, uv1), uv2);
					
					Vector3 projectedOnPlane = center + origin;
					float u = Vector3.Dot(tangent, projectedOnPlane) * scale.x;
					float v = Vector3.Dot(bitangent, projectedOnPlane) * scale.y;

					u = Mathf.Floor(u);
					v = Mathf.Floor(v);

					uvCenter.x = Mathf.Floor(uvCenter.x);
					uvCenter.y = Mathf.Floor(uvCenter.y);
					
					Vector2 uvOffset = -uvCenter + new Vector2(u, v);

					for (int k = 0; k < 3; k++)
					{
						int index = mesh.triangles[j + k];
						Vector2 uv = oldUVs[index];
						
						if (processedUVs.TryGetValue(index, out var offset))
							uvOffset = offset;
						
						Vector2 newUv = uv;
						newUv = uv + uvOffset;

						newUVs[index] = newUv;

						processedUVs.TryAdd(index, uvOffset);
					}
				}
			}
			mesh.SetUVs(0, newUVs);
		}
		
		private int FindOrAddVertexForBaseIndex(int baseIdx, MapBsp mapBsp, Vector3 origin, Quaternion? rotation)
		{
			// Find existing value
			if (m_BaseIndexToIndex.TryGetValue(baseIdx, out int currentMeshVertIdx))
				return currentMeshVertIdx;

			Vector3 position = mapBsp.Vertices[baseIdx].Position - origin;
			Vector3 normal = mapBsp.Vertices[baseIdx].Normal;

			if (rotation != null)
			{
				position = rotation.Value * position;
				normal = rotation.Value * normal;
			}

			// Not found, add a new one
			BspMeshVertex currentMeshVertex = new()
			{
				Position = position,
				Normal = normal,
				UV = mapBsp.Vertices[baseIdx].TexCoord + new Vector2(0.5f, 0.5f),
				BaseIndex = baseIdx
			};
			m_MeshVerticesBuffer.Add(currentMeshVertex);
			m_BaseIndexToIndex.Add(baseIdx, m_MeshVerticesBuffer.Count - 1);
			return m_MeshVerticesBuffer.Count - 1;
		}

		private static void AddVertexToSubmeshWithTexID(List<BspSubMesh> meshSubmeshes, int texID, int localVertexIdx)
		{
			// Find a submesh that uses this texture ID
			foreach (BspSubMesh subMesh in meshSubmeshes)
			{
				if (texID != subMesh.TexID)
					continue;

				subMesh.Triangles.Add(localVertexIdx);
				return;
			}

			// Not found, make a new submesh for this face
			meshSubmeshes.Add(new()
			{
				TexID = texID,
				Triangles = new() { localVertexIdx },
			});
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Utilities
		// -----------------------------------------------------------------------------------------------------------------------------
		private static void SmoothMeshNormals(Mesh mesh, float maxAngle = 45.0f)
		{
			Vector3[] newVertices = mesh.vertices;
			Vector3[] newNormals = mesh.normals;

			Vector3[] originalNormals = new Vector3[newNormals.Length];
			Array.Copy(newNormals, originalNormals, newNormals.Length);

			float cosThreshold = Mathf.Cos(Mathf.Deg2Rad * maxAngle);

			for (int i = 0; i < newVertices.Length; i++)
			{
				int groupCount = 1;
				Vector3 currentNormal = originalNormals[i];
				
				for (int j = 0; j < newVertices.Length; j++)
				{
					if (i == j)
						continue;

					// Inlined SqrMagnitude to avoid Vector3 .ctor
					ref Vector3 from = ref newVertices[i];
					ref Vector3 to = ref newVertices[j];

					float xDiff = from.x - to.x;
					float yDiff = from.y - to.y;
					float zDiff = from.z - to.z;
					float sqrDistance = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;

					if (sqrDistance >= 0.01f * 0.01f)
						continue;
					
					// Merge verts!
					to = from;

					// Also inline AngleBetween and avoid Acos for speed up... these 3 changes reduce from 700ms to about 20ms ;)
					float cosBetween = Vector3.Dot(originalNormals[i], originalNormals[j]);

					if (cosBetween > cosThreshold)
					{
						currentNormal += originalNormals[j];
						groupCount++;
					}
				}

				if (groupCount >= 2)
				{
					newNormals[i] = currentNormal / groupCount;
				}
			}
			
			mesh.vertices = newVertices;
			mesh.normals = newNormals;
			mesh.RecalculateBounds();
			mesh.RecalculateTangents();
		}
		
		private static Mesh GenerateSimplifiedUnityCollisionMesh(Mesh inputMesh, float threshold = 0.01f)
		{
			Mesh outputMesh = new() { name = inputMesh.name };

			Vector3[] inputVertices = inputMesh.vertices;
			int inputVertexCount = inputMesh.vertexCount;
			int[] inputTriangles = inputMesh.triangles;
			int[] vertexRemap = new int[inputVertexCount];

			List<Vector3> newVertices = new() { inputVertices[0] };

			float sqrThreshold = threshold * threshold;

			for (int inputVertIdx = 0; inputVertIdx < inputVertexCount; inputVertIdx++)
			{
				bool wasRemapped = false;

				float newVertCount = newVertices.Count;
				for (int newVertIdx = 0; newVertIdx < newVertCount; newVertIdx++)
				{
					Vector3 from = inputVertices[inputVertIdx];
					Vector3 to = newVertices[newVertIdx];

					float xDiff = from.x - to.x;
					float yDiff = from.y - to.y;
					float zDiff = from.z - to.z;
					float sqrDistance = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;

					if (sqrDistance > sqrThreshold)
						continue;

					vertexRemap[inputVertIdx] = newVertIdx;
					wasRemapped = true;
					break;
				}

				if (!wasRemapped)
				{
					newVertices.Add(inputVertices[inputVertIdx]);
					vertexRemap[inputVertIdx] = newVertices.Count - 1;
				}
			}

			// Create triangles array
			int[] newTriangles = new int[inputTriangles.Length];
			int triPtr = 0;
			
			for (int i = 0; i < inputTriangles.Length; i += 3)
			{
				if (vertexRemap[inputTriangles[i]] == vertexRemap[inputTriangles[i + 1]] || vertexRemap[inputTriangles[i]] == vertexRemap[inputTriangles[i + 2]] || vertexRemap[inputTriangles[i + 1]] == vertexRemap[inputTriangles[i + 2]])
					continue;
				
				newTriangles[triPtr++] = vertexRemap[inputTriangles[i + 0]];
				newTriangles[triPtr++] = vertexRemap[inputTriangles[i + 1]];
				newTriangles[triPtr++] = vertexRemap[inputTriangles[i + 2]];
			}

			outputMesh.vertices = newVertices.ToArray();
			outputMesh.triangles = newTriangles;
			return outputMesh;
		}
	}
}