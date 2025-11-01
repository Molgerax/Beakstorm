using Beakstorm.Simulation.Collisions.SDF.Shapes;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Beakstorm.Mapping.Tremble
{
    public class SdfTextureMapProcessor : MapProcessorBase
    {
        private ComputeShader _sdfCompute;
        private ComputeShader _sdfCombineCompute;
    
        public override void OnProcessingStarted(GameObject root, MapBsp mapBsp)
        {
#if UNITY_EDITOR
            _sdfCompute = FindComputeShader("MeshToSDF");
            _sdfCombineCompute = FindComputeShader("SdfCombine");
#endif
            _sdfCompute = Addressables.LoadAssetAsync<ComputeShader>("MeshToSDF.compute").WaitForCompletion();
            _sdfCombineCompute = Addressables.LoadAssetAsync<ComputeShader>("SdfCombine.compute").WaitForCompletion();
        }

        public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
        {
            MapWorldSpawn worldSpawn = GetWorldspawn<MapWorldSpawn>();
            
            SdfTextureField sdf = root.GetComponent<SdfTextureField>();
            if (!sdf)
                sdf = root.AddComponent<SdfTextureField>();

            if (sdf)
                sdf.InitializeFromScript(_sdfCompute, _sdfCombineCompute, worldSpawn.SdfResolution, root, true);
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