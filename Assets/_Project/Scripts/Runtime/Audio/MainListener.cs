using UnityEngine;

namespace Beakstorm.Audio
{
    public class MainListener : MonoBehaviour
    {
        public static MainListener Instance;

        private Transform _t;
        public static Transform Transform => Instance._t;

        private Camera _camera;
        public static Camera Camera => Instance._camera;
        
        private void Awake()
        {
            Instance = this;
            _t = transform;
            _camera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}
