using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Simulation.Settings
{
    [BrushEntity("boid_spawn", category:"trigger", type: BrushType.Trigger)]
    public class BoidSpawnArea : MonoBehaviour, IOnImportFromMapEntity
    {
        private static BoidSpawnArea _instance;

        public static Bounds SpawnBounds => _instance ? _instance.Bounds
            : new Bounds(new(0, -128, 0), new Vector3(256, 1, 256));

        [SerializeField, HideInInspector, NoTremble] private MeshCollider _meshCollider;
        
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

        public Bounds Bounds => _meshCollider ? _meshCollider.bounds : new Bounds(transform.position, transform.lossyScale);
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new(0.25f, 0.25f, 1f, 0.5f);
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            _meshCollider = GetComponent<MeshCollider>();
        }
    }
}
