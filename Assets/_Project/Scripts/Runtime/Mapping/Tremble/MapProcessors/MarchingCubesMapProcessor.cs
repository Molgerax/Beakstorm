using System.Collections.Generic;
using Beakstorm.Rendering.MarchingCubes;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble.MapProcessors
{
    public class MarchingCubesMapProcessor : MapProcessorBase
    {
        private ComputeShader _sdfCompute;
        private ComputeShader _sdfCombineCompute;
    
        public override void OnProcessingStarted(GameObject root, MapBsp mapBsp)
        {
#if UNITY_EDITOR
            _sdfCompute = FindComputeShader("MeshToSDF");
            _sdfCombineCompute = FindComputeShader("SdfCombine");
#endif
            
            if (!_sdfCompute) _sdfCompute = Addressables.LoadAssetAsync<ComputeShader>("MeshToSDF.compute").WaitForCompletion();
            if (!_sdfCombineCompute) _sdfCombineCompute = Addressables.LoadAssetAsync<ComputeShader>("SdfCombine.compute").WaitForCompletion();
        }

        public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
        {
            MapWorldSpawn worldSpawn = GetWorldspawn<MapWorldSpawn>();

            float noiseStrength = worldSpawn.CloudNoiseStrength;

            var marchingCubes = root.GetComponentsInChildren<TrembleMarchingCubes>();
            
            if (marchingCubes == null || marchingCubes.Length == 0)
                return;


            List<GameObject> layersAndGroups = new();

            foreach (TrembleMarchingCubes cube in marchingCubes)
            {
                GameObject parentObject = cube.transform.parent.gameObject;
                if (parentObject == root)
                    continue;
                
                if (!layersAndGroups.Contains(parentObject))
                    layersAndGroups.Add(parentObject);
            }


            foreach (GameObject layer in layersAndGroups)
            {
                var cubesLocal = layer.GetComponentsInChildren<TrembleMarchingCubes>();

                List<MeshCollider> colliders = new();

                Material cloudMaterial = null;

                float surface = 0;
                float unionSmoothing = 0;
                int count = 0;
                foreach (var cube in cubesLocal)
                {
                    colliders.Add(cube.GetComponent<MeshCollider>());
                    
                    count++;
                    surface += cube.surface;
                    unionSmoothing = Mathf.Max(cube.smoothing, unionSmoothing);

                    if (cube.TryGetComponent(out MeshRenderer mr))
                    {
                        cloudMaterial = mr.sharedMaterial;
                        CoreUtils.Destroy(mr);
                    }
                    if (cube.TryGetComponent(out MeshFilter mf))
                    {
                        CoreUtils.Destroy(mf.sharedMesh);
                        CoreUtils.Destroy(mf);
                    }
                }
                surface /= count;
                
                SdfTextureField sdf = layer.GetOrAddComponent<SdfTextureField>();
                
                var tex = sdf.InitializeFromScript(_sdfCompute, _sdfCombineCompute, worldSpawn.SdfMaterialType, 
                    worldSpawn.SdfResolution, layer, true, false, colliders.ToArray(), unionSmoothing,
                    noiseStrength);
                
                if (!tex || !tex.isReadable)
                    continue;

                sdf.GetBounds(out Vector3 min, out Vector3 max);

                Marching marching = new MarchingCubes(surface);

                List<Vector3> vertices = new();
                List<int> indices = new();
                List<Vector3> normals = null;

                MeshFilter meshFilter = layer.GetOrAddComponent<MeshFilter>();
                MeshRenderer meshRenderer = layer.GetOrAddComponent<MeshRenderer>();

                if (meshRenderer.sharedMaterial == null)
                    meshRenderer.sharedMaterial = cloudMaterial;

                Mesh mesh = meshFilter.sharedMesh;

                bool saveToAsset = false;

                if (mesh == null)
                {
                    mesh = new Mesh();
                    mesh.name = layer.name + "_cloudMesh";
                    saveToAsset = true;
                }
                mesh.indexFormat = IndexFormat.UInt32;

                marching.Generate(tex, vertices, indices, normals);

                Vector3 center = sdf.transform.position;
                //max -= center - Vector3.one * 2;
                //min -= center - Vector3.one * 2;

                Vector3 resolution = new(tex.width, tex.height, tex.depth);
                Vector3 cornerA = Vector3.zero;
                Vector3 cornerB = resolution - Vector3.one;
                
                for (var index = 0; index < vertices.Count; index++)
                {
                    Vector3 vertex = vertices[index];
                    vertex = Remap(vertex, cornerA, cornerB, min, max);
                    vertices[index] = vertex;
                }

                mesh.SetVertices(vertices);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                if (normals is {Count: > 0})
                    mesh.SetNormals(normals);
                else
                    mesh.RecalculateNormals();

                //CalculateUVs(mesh, min, max);
                
                CalculateTangents(mesh);
                //mesh.RecalculateTangents();
                
                mesh.RecalculateBounds();

                meshFilter.sharedMesh = mesh;
                
                if (saveToAsset)
                    TrembleMapImportSettings.Current.SaveObjectInMap(mesh.name, mesh);
                
                
                //TrembleMapImportSettings.Current.SaveObjectInMap(tex.name, tex);
                CoreUtils.Destroy(tex);
                CoreUtils.Destroy(sdf);
                
                for (var index = colliders.Count - 1; index >= 0; index--)
                {
                    var collider = colliders[index];
                    
                    CoreUtils.Destroy(collider.sharedMesh);
                    CoreUtils.Destroy(collider);
                }

                for (var index = cubesLocal.Length - 1; index >= 0; index--)
                {
                    var cube = cubesLocal[index];
                    CoreUtils.Destroy(cube.gameObject);
                }

                var meshCollider = layer.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = false;
            }
        }

        float Remap(float x, float minA, float maxA, float minB, float maxB)
        {
            float t = (x - minA) / (maxA - minA);
            return Mathf.LerpUnclamped(minB, maxB, t);
        }
        
        private Vector3 Remap(Vector3 vector3, Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            Vector3 o = new();
            o.x = Remap(vector3.x, minA.x, maxA.x, minB.x, maxB.x);
            o.y = Remap(vector3.y, minA.y, maxA.y, minB.y, maxB.y);
            o.z = Remap(vector3.z, minA.z, maxA.z, minB.z, maxB.z);
            return o;
        }

        private void CalculateUVs(Mesh mesh, Vector3 min, Vector3 max)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            
            List<Vector2> uvs = new List<Vector2>();

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 normal = normals[i];

                vertex = Remap(vertex, min, max, Vector3.zero, Vector3.one);
                
                normal = new(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                //normal /= Vector3.Dot(normal, Vector3.one);
                
                Vector2 zy = new(vertex.z, vertex.y);
                Vector2 xz = new(vertex.x, vertex.z);
                Vector2 xy = new(vertex.x, vertex.y);

                zy *= 0.5f;
                xz *= 0.5f;
                xy *= 0.5f;

                xz.x += 0.5f;
                xy.y += 0.5f;
                
                Vector2 uv = 
                    zy * normal.x + 
                    xz * normal.y +
                    xy * normal.z;

                uv = xz;
                if (normal.x > normal.y)
                {
                    uv = zy;
                    if (normal.z > normal.x)
                        uv = xy;
                }

                uvs.Add(uv);
            }
            
            mesh.SetUVs(0, uvs);
        }
        
        private void CalculateTangents(Mesh mesh)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            
            List<Vector4> tangents = new();

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 normal = normals[i];

                Vector3 tangent = Vector3.Cross(normal, Vector3.up);
                if (tangent.magnitude == 0)
                    tangent = Vector3.Cross(normal, Vector3.right);
                
                tangent.Normalize();

                Vector4 t = new(tangent.x, tangent.y, tangent.z, 1);
                tangents.Add(t);
            }
            
            mesh.SetTangents(tangents);
        }
        
#if UNITY_EDITOR
        private ComputeShader FindComputeShader(string name)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(name + $"t:{nameof(ComputeShader)}");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ComputeShader cs = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                if (cs)
                    return cs;
            }
            return null;
        }
#endif
    }
}