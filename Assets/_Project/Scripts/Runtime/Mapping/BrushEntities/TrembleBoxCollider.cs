using Beakstorm.Simulation.Collisions.SDF;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("collision_box", category:"func", type: BrushType.Solid)]
    public class TrembleBoxCollider : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity
    {
        [Tremble("parent")] private Transform _parent;

        [Tremble("targetname")] private string _id;
        
        [Tremble("sdfMaterial")] private SdfMaterialType _sdfMaterialType = SdfMaterialType.None;

        [SerializeField, HideInInspector, NoTremble] private Rigidbody _rigidbody;
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            if (!string.IsNullOrEmpty(_id))
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }
            
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            Bounds bounds = meshCollider.bounds;
            
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            CoreUtils.Destroy(meshCollider);

            SdfBox sdfBox = gameObject.AddComponent<SdfBox>();
            sdfBox.SetDimensions(boxCollider.center, boxCollider.size);
            sdfBox.SetMaterialType(_sdfMaterialType);
        }

        public void Trigger()
        {
            if (_rigidbody)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
            }
        }
    }
}