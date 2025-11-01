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
            {
                var tex = sdf.InitializeFromScript(_sdfCompute, _sdfCombineCompute, worldSpawn.SdfMaterialType, worldSpawn.SdfResolution, root, true);
                if (tex)
                    TrembleMapImportSettings.Current.SaveObjectInMap("sdf_texture", tex);
            }
            
            
        }
    }
}