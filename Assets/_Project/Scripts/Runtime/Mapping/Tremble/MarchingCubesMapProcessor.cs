using System.Collections.Generic;
using Beakstorm.Rendering.MarchingCubes;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
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

            var marchingCubes = root.GetComponentsInChildren<TrembleMarchingCubes>();
            
            if (marchingCubes == null || marchingCubes.Length == 0)
                return;


            foreach (var cube in marchingCubes)
            {
                MeshFilter meshFilter = cube.GetComponent<MeshFilter>();
                if (!meshFilter || !meshFilter.sharedMesh)
                    continue;
                
                SdfTextureField sdf = cube.gameObject.GetComponent<SdfTextureField>();
                if (!sdf)
                    sdf = cube.gameObject.AddComponent<SdfTextureField>();
                
                var tex = sdf.InitializeFromScript(_sdfCompute, _sdfCombineCompute, worldSpawn.SdfMaterialType, worldSpawn.SdfResolution, cube.gameObject, true, false);

                if (!tex || !tex.isReadable)
                    continue;

                sdf.GetBounds(out Vector3 min, out Vector3 max);

                Marching marching = new MarchingCubes(cube.surface);

                List<Vector3> vertices = new();
                List<int> indices = new();

                Mesh mesh = meshFilter.sharedMesh;

                marching.Generate(tex, vertices, indices);

                Vector3 center = sdf.transform.position;
                max -= center - Vector3.one * 2;
                min -= center - Vector3.one * 2;

                Vector3 resolution = new(tex.width, tex.height, tex.depth);
                Vector3 cornerA = Vector3.zero;
                Vector3 cornerB = resolution;
                
                for (var index = 0; index < vertices.Count; index++)
                {
                    Vector3 vertex = vertices[index];
                    vertex = Remap(vertex, cornerA, cornerB, min, max);
                    vertices[index] = vertex;
                }

                mesh.SetVertices(vertices);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                
                CoreUtils.Destroy(tex);
                CoreUtils.Destroy(sdf);
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