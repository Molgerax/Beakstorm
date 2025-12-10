using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public class MoveFollowRecorder : MovementBehaviour
    {
        [SerializeField] private MoveRecorder recorder;
        [SerializeField] private float distance = 10;
        
        public override void ApplyMovement(Transform t)
        {
            if (!recorder)
                return;

            if (recorder.GetPositionAtDistance(distance, out Vector3 pos, out Quaternion rot))
            {
               t.SetPositionAndRotation(pos, rot);
            }
        }
    }
}