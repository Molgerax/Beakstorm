using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public class MoveRecorder : MovementBehaviour
    {
        [SerializeField] private float distanceBetween = 5f;
        [SerializeField, Min(4)] private int maxCount = 32;

        private TransformHistoryData _currentData;
        private TransformHistoryData _secondData;
        
        private List<TransformHistoryData> _transformData = new List<TransformHistoryData>(16);

        private float _cachedLength;

        public float Length => GetDistance(_currentData, _secondData) + _cachedLength;

        public bool GetPositionAtDistance(float distance, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            if (_currentData == null)
                return false;

            float beginDist = GetDistance(_currentData, _secondData);

            if (distance < beginDist)
            {
                pos = Vector3.Lerp(_currentData.Position, _secondData.Position, distance / beginDist);
                rot = Quaternion.Slerp(_currentData.Rotation, _secondData.Rotation, distance / beginDist);
                return true;
            }

            distance -= beginDist;
            
            float offset = distance % distanceBetween;
            int index = Mathf.FloorToInt(distance / distanceBetween) + 1;
            if (index >= _transformData.Count - 1)
                return false;

            float t = offset / distanceBetween;
            pos = Vector3.Lerp(_transformData[index].Position, _transformData[index + 1].Position, t);
            rot = Quaternion.Slerp(_transformData[index].Rotation, _transformData[index + 1].Rotation, t);
            return true;
        }
        
        
        public override void Initialize(Transform t)
        {
            FillAll(t);
        }

        private void ResetData(Transform t)
        {
            _secondData = null;
            _currentData = null;
        
            _transformData ??= new List<TransformHistoryData>(16);
            _transformData.Clear();

            _secondData = new(t);
            _currentData = new(t);
            
            _transformData.Insert(0, _secondData);
            _transformData.Insert(0, _currentData);

            _cachedLength = 0;
        }

        private void FillAll(Transform t)
        {
            ResetData(t);
            Vector3 forward = t.forward;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;
            
            for (int i = 2; i < maxCount; i++)
            {
                pos -= forward * distanceBetween;
                _transformData.Add(new(pos, rot));
            }
        }
        
        public override void ApplyMovement(Transform t)
        {
            _currentData.Update(t);

            float currentDistance = GetDistance(_currentData, _secondData);
            if (currentDistance > distanceBetween)
            {
                Vector3 diff = _currentData.Position - _secondData.Position;
                _currentData.Position = _secondData.Position + Vector3.ClampMagnitude(diff, distanceBetween);

                _cachedLength += distanceBetween;
                _secondData = _currentData;

                if (_transformData.Count == maxCount)
                {
                    _currentData = _transformData[^1];
                    _currentData.Update(t);
                    _transformData.RemoveAt(_transformData.Count - 1);
                }
                else
                {
                    _currentData = new(t);
                }

                _transformData.Insert(0, _currentData);
            }
        }


        private class TransformHistoryData
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public TransformHistoryData(Transform t)
            {
                Position = t.position;
                Rotation = t.rotation;
            }
            
            public TransformHistoryData(Vector3 pos, Quaternion rot)
            {
                Position = pos;
                Rotation = rot;
            }

            public void Update(Transform t)
            {
                Position = t.position;
                Rotation = t.rotation;
            }
        }

        private float GetDistance(TransformHistoryData a, TransformHistoryData b)
        {
            if (a != null && b != null)
                return Vector3.Distance(a.Position, b.Position);
            return 0;
        }
    }
}