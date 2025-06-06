using UnityEngine;

namespace Beakstorm.Utility
{
    [ExecuteInEditMode]
    public class FramerateLimiter : MonoBehaviour
    {
        [SerializeField] [Range(1, 120)] private int targetFrameRate = 60;
        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            if(isActiveAndEnabled) 
                Apply();
        }

        private void Apply()
        {
            Application.targetFrameRate = targetFrameRate;
        }
        
        private void OnDisable()
        {
            Application.targetFrameRate = -1;
        }
    }
}
