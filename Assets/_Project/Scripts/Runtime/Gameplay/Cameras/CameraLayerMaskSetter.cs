using UnityEngine;

namespace Beakstorm.Gameplay.Cameras
{
    public class CameraLayerMaskSetter : MonoBehaviour
    {
        [SerializeField] private LayerMask layerMask = int.MaxValue;

        private Camera _cam;
        private void OnEnable()
        {
            _cam = Camera.main;
        }

        public void SetLayerMask()
        {
            if (_cam)
                _cam.cullingMask = layerMask;
        }
    }
}