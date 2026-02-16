using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("rotator", category:"func", size: 16f)]
    public class FuncRotator : MonoBehaviour, ITriggerTarget
    {
        [SerializeField, Tremble] private float amount = 180;
        [SerializeField, Tremble] private float speed = 90f;
        
        private float _timer;
        private float Duration => speed > 0 ? Mathf.Abs(amount) / speed : 0;

        private bool _triggered;

        private Quaternion _initRotation;
        private Quaternion _targetRotation;

        private DoorState _state = DoorState.Idle;
        
        private void Awake()
        {
            _state = DoorState.Idle;
            _initRotation = transform.rotation;
            _targetRotation = Quaternion.AngleAxis(amount, transform.right) * _initRotation;
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
                transform.rotation = _targetRotation;
                return;
            }

            transform.rotation = Quaternion.Slerp(_initRotation, _targetRotation, _timer / Duration);
        }
    }
}