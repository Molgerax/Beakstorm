using Beakstorm.Mapping.Tremble.Properties;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("door", category:"func")]
    public class TrembleDoor : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] private float speed = 5f;
        [SerializeField, NoTremble] private float distance = 16;
        
        [Tremble("distance")] private float _trembleDistance = 64;
        [Tremble("speed")] private float _trembleSpeed = 64;
        [Tremble("angle")] private QuakeAngle _angle;

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

        public void Trigger()
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
            distance = (_trembleDistance * TrembleSyncSettings.Get().ImportScale);
            speed = (_trembleSpeed * TrembleSyncSettings.Get().ImportScale);

            transform.right = _angle;
        }
    }
}