using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [CreateAssetMenu(menuName = "Beakstorm/PlayerWeaponData/MultiShotProjectile", fileName = "MultiShotPlayerWeapon")]
    public class MultiShotPlayerWeapon : SimplePlayerWeapon
    {
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Range(1f, 360f)] private float spreadAngle = 25f;
        [SerializeField, Range(0f, 3f)] private float offset = 1f;

        [SerializeField] private bool useSpiral = true;
        
        protected override void FireSingleProjectile(FireInfo fireInfo)
        {
            FireInfo infoCopy = fireInfo;
            for (int i = 0; i < count; i++)
            {
                fireInfo = infoCopy;
                fireInfo.InitialDirection = useSpiral ? 
                    GetSpiralPattern(i, infoCopy.InitialDirection) : 
                    GetShotDirection(i, infoCopy.InitialDirection);

                fireInfo.LookDirection = useSpiral ? 
                    GetSpiralPattern(i, infoCopy.LookDirection) : 
                    GetShotDirection(i, infoCopy.LookDirection);
                
                fireInfo.InitialPosition += fireInfo.InitialDirection * offset;
                
                base.FireSingleProjectile(fireInfo);
            }
        }

        private Vector3 GetShotDirection(int index, Vector3 initDirection)
        {
            Vector3 right = Vector3.Cross(Vector3.up, initDirection);
            Vector3 rotationAxis = Vector3.Cross(initDirection, right).normalized;

            float angle = GetAngle(index);
            return Quaternion.AngleAxis(angle, rotationAxis) * initDirection;
        }

        private Vector3 GetSpiralPattern(int index, Vector3 initDirection)
        {
            float turnFraction = 1.618033988f;

            float angleIncrement = 2 * Mathf.PI * turnFraction;
            
            float t = index / (count - 1f);

            t *= (spreadAngle / 360f);
            
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = angleIncrement * index;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);

            Vector3 target = new Vector3(x, y, z);
            Quaternion dir = Quaternion.LookRotation(target);
            Quaternion initDir = Quaternion.LookRotation(initDirection);

            return initDir * target;
        }

        private float GetAngle(int index)
        {
            if (count == 1) 
                return 0;
            
            float t = (float)index / (count - 1) - 0.5f;
            return t * spreadAngle;
        }
    }
}