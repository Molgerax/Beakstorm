using Unity.Cinemachine;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class CameraFOV : MonoBehaviour
    {
        [SerializeField] private float min = 60;
        [SerializeField] private float max = 70;

        [SerializeField] private CinemachineCamera cam;
        
        public static CameraFOV Instance;

        public void Awake()
        {
            Instance = this;
        }


        public void SetFoV(float value01)
        {
            if (!cam)
                return;

            cam.Lens.FieldOfView = Mathf.Lerp(min, max, value01);
        }
    }
}