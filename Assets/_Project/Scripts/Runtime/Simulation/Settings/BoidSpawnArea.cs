using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Settings
{
    [BrushEntity("boid_spawn", category:"trigger", type: BrushType.Trigger)]
    public class BoidSpawnArea : MonoBehaviour, IOnImportFromMapEntity
    {
        private static BoidSpawnArea _instance;

        public static Bounds SpawnBounds => _instance ? _instance.Bounds
            : new Bounds(new(0, -128, 0), new Vector3(256, 1, 256));

        [SerializeField, HideInInspector, NoTremble] private MeshCollider _meshCollider;
        [SerializeField, HideInInspector, NoTremble] private Bounds _bounds;
        
        private void OnEnable()
        {
            if (!_instance)
                _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this)
                _instance = null;
        }

        public Bounds Bounds => _bounds.size.magnitude > 0 ? _bounds : new Bounds(transform.position, transform.lossyScale);
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new(0.25f, 0.25f, 1f, 0.5f);
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            _meshCollider = GetComponent<MeshCollider>();
            _bounds = new Bounds(Vector3.zero, Vector3.zero);
            if (_meshCollider)
            {
                _bounds = _meshCollider.bounds;
                CoreUtils.Destroy(_meshCollider);
            }
        }
    }
}
