using Beakstorm.Mapping.Waypoints;
using Beakstorm.Simulation.Collisions.SDF;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
{
    //[PointEntity("platform", category:"func", colour:"0 0.5 1.0", size: 16)]
    [BrushEntity("platform", category:"func", type: BrushType.Solid, colour:"0 0.5 1.0")]
    public class TrembleMovingPlatform : TriggerBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] private float speed = 5f;

        [SerializeField, Tremble("target")] private Waypoint waypoint;
        [Tremble("speed")] private float _trembleSpeed = 64;
        
        [Tremble("sdfMaterial")] private SdfMaterialType _sdfMaterialType = SdfMaterialType.None;
        
        [SerializeField] private bool autoStart;

        private bool _triggered;

        private Vector3 _initPos;
        private Vector3 _targetPos;

        private DoorState _state;

        private Waypoint _nextWaypoint;

        private Vector3 _offset;
        
        private void Awake()
        {
            if (autoStart)
                Trigger();
        }

        private enum DoorState
        {
            Idle = 0,
            Triggered = 1,
            Finished = 2,
            Stopped = 3,
        }

        public override void Trigger()
        {
            if (_state == DoorState.Idle)
            {
                _nextWaypoint = waypoint;
                if (!_nextWaypoint)
                {
                    _state = DoorState.Finished;
                    return;
                }

                _offset = _nextWaypoint.transform.position - transform.position;
                _state = DoorState.Triggered;
            }

            if (_state == DoorState.Stopped)
            {
                _state = DoorState.Triggered;
            }
        }

        private void Update()
        {
            if (_state != DoorState.Triggered)
                return;
            
            if (!_nextWaypoint)
                return;

            Vector3 posA = _offset + transform.position;
            Vector3 posB = _nextWaypoint.transform.position;

            posA = Vector3.MoveTowards(posA, posB, Time.deltaTime * speed);
            
            if (Vector3.Distance(posA, posB) < 0.1f)
            {
                OnReachWaypoint(_nextWaypoint);
                posA = posB;
            }
            transform.position = posA - _offset;
        }

        private void OnReachWaypoint(Waypoint wp)
        {
            Waypoint newWp = wp.GetNextWaypoint();

            if (_nextWaypoint && _nextWaypoint.IsStop)
                _state = DoorState.Stopped;
            
            _nextWaypoint = newWp;
            if (!_nextWaypoint)
                _state = DoorState.Finished;
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            speed = (_trembleSpeed * TrembleSyncSettings.Get().ImportScale);
            
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            Bounds bounds = meshCollider.bounds;
            
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

            SdfBox sdfBox = gameObject.AddComponent<SdfBox>();
            sdfBox.SetDimensions(boxCollider.center, boxCollider.size);
            sdfBox.SetMaterialType(_sdfMaterialType);
            
            CoreUtils.Destroy(boxCollider);
        }
    }
}