using Beakstorm.Simulation.Collisions.SDF;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
{
    [BrushEntity("box", category:"collision", type: BrushType.Solid)]
    public class TrembleBoxCollider : MonoBehaviour, IOnImportFromMapEntity
    {
        [Tremble("sdfMaterial")] private SdfMaterialType _sdfMaterialType = SdfMaterialType.None;
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            Bounds bounds = meshCollider.bounds;
            
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            CoreUtils.Destroy(meshCollider);

            SdfBox sdfBox = gameObject.AddComponent<SdfBox>();
            sdfBox.SetDimensions(boxCollider.center, boxCollider.size);
            sdfBox.SetMaterialType(_sdfMaterialType);
        }
    }
}