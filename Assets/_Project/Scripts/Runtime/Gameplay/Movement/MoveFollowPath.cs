using Beakstorm.Mapping.PointEntities;
using Beakstorm.Utility.Extensions;
using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public class MoveFollowPath : MovementBehaviour
    {
        [SerializeField] private float speed = 10;

        private Waypoint _waypointA;
        private Waypoint _waypointB;

        private bool _finished;

        private float _waypointT;

        private Vector3 _moveTargetPoint;
        
        private Vector3 _moveTargetForward;

        private float _cachedWaypointDistance;

        public override void Initialize(Transform t)
        {
            _finished = false;
        }

        public void SetWaypoint(Waypoint wp)
        {
            _waypointB = wp;
            if (wp)
            {
                _moveTargetPoint = wp.transform.position;
                _moveTargetForward = wp.GetTangent();
                _waypointT = 0;
            }
        }

        public override void ApplyMovement(Transform t)
        {
            if (_finished)
                return;

            if (!_waypointB)
                return;

            Vector3 posA = t.position;
            Vector3 posB = _waypointB.transform.position;
            float moveDist = Time.deltaTime * speed;

            if (_waypointA)
            {
                while (Vector3.Distance(posA, _moveTargetPoint) < moveDist && _waypointB)
                {
                    float tDelta = _cachedWaypointDistance > 0 ? moveDist / _cachedWaypointDistance : 0.1f;
                    _waypointT = Mathf.MoveTowards(_waypointT, 1, tDelta);
                    _moveTargetPoint = Waypoint.Interpolate(_waypointA, _waypointB, _waypointT, out _moveTargetForward);

                    if (_waypointT > 0.99f)
                    {
                        OnReachWaypoint();
                    }
                }
            }
            else
            {
                _moveTargetPoint = _waypointB.transform.position;
                _moveTargetForward = (_moveTargetPoint - posA).normalized;
            }
            
            posA = Vector3.MoveTowards(posA, _moveTargetPoint, moveDist);
            
            if (Vector3.Distance(posA, posB) == 0)
            {
                OnReachWaypoint();
                posA = _moveTargetPoint;
            }
            t.position = posA;

            _moveTargetForward = _moveTargetForward.With(y: 0);

            if (_moveTargetForward.magnitude == 0)
                return;

            t.rotation = Quaternion.RotateTowards(t.rotation, Quaternion.LookRotation(_moveTargetForward),
                Time.deltaTime * speed);
        }

        private void OnReachWaypoint()
        {
            if (!_waypointB)
                return;
            
            Waypoint newWp = _waypointB.GetNextWaypoint();
            _waypointA = _waypointB;
            _waypointB = newWp;
            if (_waypointB)
            {
                _moveTargetPoint = _waypointA ? _waypointA.transform.position : _waypointB.transform.position;
                _waypointT = 0;

                if (_waypointA && _waypointB)
                    _cachedWaypointDistance = Waypoint.GetDistance(_waypointA, _waypointB);
            }
            else
            {
                _finished = true;
            }
        }

        private void OnDrawGizmos()
        {
            return;
            Gizmos.color = Color.red;
            
            if (_waypointA)
                Gizmos.DrawLine(transform.position, _waypointA.transform.position);
                
            if (_waypointB)
                Gizmos.DrawLine(transform.position, _waypointB.transform.position);

            Gizmos.color = Color.blue;
            Gizmos.DrawCube(_moveTargetPoint, Vector3.one * 4);
            Gizmos.DrawLine(transform.position, _moveTargetPoint);
        }
    }
}