using Beakstorm.Mapping.Tremble.Properties;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("door", category:"func")]
    public class FuncDoor : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] private float speed = 5f;
        [SerializeField, NoTremble] private float distance = 16;
        [SerializeField, NoTremble] private bool toggle;
        
        
        [Tremble("lip")] private float _trembleLip = 0;
        [Tremble("speed")] private float _trembleSpeed = 64;
        [Tremble("angle")] private QuakeAngle _angle;

        [Tremble("toggle"), SpawnFlags()] private bool _toggle;

        private float _timer;
        private float Duration => speed > 0 ? distance / speed : 0;

        private bool _triggered;

        private Vector3 _initPos;
        private Vector3 _targetPos;

        private DoorState _state = DoorState.Idle;
        
        private void Awake()
        {
            _state = DoorState.Idle;
            _initPos = transform.position;
            _targetPos = _initPos + transform.right * distance;
        }

        private enum DoorState
        {
            Idle = 0,
            Triggered = 1,
            Finished = 2
        }

        public void Trigger(TriggerData data)
        {
            if (_state != DoorState.Idle)
                return;
            
            _timer = 0;
            _state = DoorState.Triggered;
        }

        private void Update()
        {
            if (_state != DoorState.Triggered)
                return;

            _timer += Time.deltaTime;
            
            if (_timer > Duration)
            {
                _state = DoorState.Finished;
                transform.position = _targetPos;
                return;
            }
            
            transform.position = Vector3.Lerp(_initPos, _targetPos, _timer / Duration);
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            Vector3 direction = _angle;
            
            Bounds bounds = GetComponent<MeshCollider>().bounds;
            distance = Vector3.Dot(direction, bounds.size) - _trembleLip * entity.ImportScale;

            speed = (_trembleSpeed * entity.ImportScale);

            transform.right = direction;
        }
    }
}