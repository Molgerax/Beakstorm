using Unity.Cinemachine;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class CameraFOV : MonoBehaviour
    {
        [SerializeField] private float min = 60;
        [SerializeField] private float max = 70;

        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        
        [SerializeField] private CinemachineCamera cam;
        [SerializeField] private CinemachinePositionComposer composer;

        [SerializeField] private Vector2 distances = new(6, 10);
        
        public static CameraFOV Instance;

        public void Awake()
        {
            Instance = this;
        }


        public void SetFoV(float value01)
        {
            if (!cam)
                return;

            cam.Lens.FieldOfView = Mathf.Lerp(min, max, curve.Evaluate(value01));
        }

        public void SetCameraDistance(float value01)
        {
            if (!composer)
                return;

            composer.CameraDistance = Mathf.Lerp(distances.x, distances.y, value01);
        }
    }
}