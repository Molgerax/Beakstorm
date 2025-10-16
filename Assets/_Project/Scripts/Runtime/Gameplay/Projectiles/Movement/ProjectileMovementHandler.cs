using System;
using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles.Movement
{
    public class ProjectileMovementHandler : MonoBehaviour
    {
        private AbstractProjectileMoveData _moveData;
        
        [NonSerialized] public Vector3 Velocity;
        [NonSerialized] public Vector3 MomentaryForce;

        [NonSerialized] public FireInfo FireInfo;
        
        [NonSerialized] public float Speed;
        [NonSerialized] public float Random1;
        [NonSerialized] public float Random2;
        [NonSerialized] public float ElapsedTime;
        [NonSerialized] public float TargetTime;
        [NonSerialized] public Quaternion RotationFrame;
        
        private void OnEnable()
        {
            ResetVelocity();
        }

        private void Update()
        {
            if (!_moveData)
                return;
            
            _moveData.Tick(this, Time.deltaTime);
        }

        public void SetMovementData(AbstractProjectileMoveData moveData)
        {
            _moveData = moveData;
        }

        public void Initialize(FireInfo fireInfo)
        {
            FireInfo = fireInfo;
            _moveData.Initialize(this, fireInfo);
        }

        public void ResetVelocity()
        {
            Velocity = Vector3.zero;
            MomentaryForce = Vector3.zero;
        }

        public void AddForce(Vector3 force)
        {
            MomentaryForce += force;
        }

        public void SetVelocity(Vector3 velocity)
        {
            Velocity = velocity;
            MomentaryForce = Vector3.zero;
        }

    }
}