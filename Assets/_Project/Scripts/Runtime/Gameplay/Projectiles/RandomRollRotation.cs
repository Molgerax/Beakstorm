using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class RandomRollRotation : MonoBehaviour
    {
        public void ApplyRandomRoll()
        {
            float r = Random.Range(0f, 360f);
            transform.Rotate(Vector3.forward * r, Space.Self);
        }
    }
}
