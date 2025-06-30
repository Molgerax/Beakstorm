using UnityEngine;

namespace Beakstorm.Utility
{
    public class CameraDepthTextureMode : MonoBehaviour
    {
        [SerializeField] private DepthTextureMode depthTextureMode = DepthTextureMode.Depth;
        
        private void Awake()
        {
            if (TryGetComponent(out Camera cam))
            {
                cam.depthTextureMode = depthTextureMode;
            }
        }
    }
}
