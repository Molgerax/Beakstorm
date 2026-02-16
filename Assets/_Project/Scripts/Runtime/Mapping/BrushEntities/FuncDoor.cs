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
        [SerializeField, NoTremble] private float wait;
        
        [SerializeField, NoTremble] private Vector3 moveDirection;
        
        [Tremble("lip")] private float _trembleLip = 0;
        [Tremble("speed")] private float _trembleSpeed = 64;
        [Tremble("angle")] private QuakeAngle _angle;
        [Tremble("wait")] private float _wait = 3;

        [Tremble("toggle"), SpawnFlags()] private bool _toggle;

        private float _timer;
        private float _waitTimer;
        private float Duration => speed > 0 ? distance / speed : 0;

        private float MoveTime01 => Duration > 0 ? Mathf.Clamp01(_timer / Duration) : Mathf.Clamp01(_timer * 10000f);
        
        private bool _triggered;

        private Vector3 _initPos;
        private Vector3 _targetPos;

        private DoorState _state = DoorState.Idle;
        
        private void Awake()
        {
            _state = DoorState.Idle;
            _initPos = transform.position;
            _targetPos = _initPos + moveDirection * distance;
        }

        private enum DoorState
        {
            Idle = 0,
            Triggered = 1,
            Finished = 2,
            Retract = 3,
            Waiting = 4,
        }

        public void Trigger(TriggerData data)
        {
            if (_state == DoorState.Idle)
            {
                _timer = 0;
                _state = DoorState.Triggered;
            }
        }

        private void Update()
        {
            if (_state == DoorState.Triggered)
                TickPressed(Time.deltaTime);
            else if (_state == DoorState.Retract)
                TickUnPressed(Time.deltaTime);
            else if (_state == DoorState.Waiting)
                TickWait(Time.deltaTime);
        }

        private void TickPressed(float deltaTime)
        {
            _timer += deltaTime;

            if (_timer >= Duration)
            {
                _timer = 0;
                _state = wait < 0 ? DoorState.Finished : DoorState.Waiting;
            }
            MoveDoor();
        }
        
        
        private void TickUnPressed(float deltaTime)
        {
            _timer -= deltaTime;
            
            if (_timer <= Duration)
            {
                _timer = 0;
                _state = DoorState.Idle;
            }
            MoveDoor();
        }

        private void MoveDoor()
        {
            if (MoveTime01 <= 0)
                transform.position = _initPos;
            else if (MoveTime01 >= 1)
                transform.position = _targetPos;
            else
                transform.position = Vector3.Lerp(_initPos, _targetPos, MoveTime01);
        }

        private void TickWait(float deltaTime)
        {
            _waitTimer += deltaTime;
            if (_waitTimer >= wait)
            {
                _waitTimer = 0;
                _state = DoorState.Retract;
            }
        }


        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            Vector3 direction = _angle;
            
            Bounds bounds = GetComponent<MeshCollider>().bounds;
            Vector3 positiveDirection = new(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            distance = Vector3.Dot(positiveDirection, bounds.size) - _trembleLip * entity.ImportScale;

            speed = (_trembleSpeed * entity.ImportScale);

            moveDirection = _angle;
            wait = _wait;
        }
    }
}