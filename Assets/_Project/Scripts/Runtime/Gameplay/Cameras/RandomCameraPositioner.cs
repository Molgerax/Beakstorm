using UnityEngine;

namespace Beakstorm.Gameplay.Cameras
{
    public class RandomCameraPositioner : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 500;

        private void Update()
        {
            float dist = Vector3.Distance(target.position, transform.position);

            if (dist > distance)
            {
                SetNewPosition();
            }
        }

        public void SetNewPosition()
        {
            Vector3 newPos = target.position + target.forward * (distance * 0.85f) + Random.insideUnitSphere * (distance * 0.15f);
            transform.position = newPos;
        }
    }
}