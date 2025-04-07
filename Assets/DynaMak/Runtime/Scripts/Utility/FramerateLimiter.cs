using System;
using UnityEngine;

namespace DynaMak.Utility
{
    [ExecuteInEditMode]
    public class FramerateLimiter : MonoBehaviour
    {
        [SerializeField] [Range(1, 120)] private int targetFrameRate = 60;

        private void OnEnable()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        private void OnValidate()
        {
            if(isActiveAndEnabled) 
                Application.targetFrameRate = targetFrameRate;
        }

        private void OnDisable()
        {
            Application.targetFrameRate = -1;
        }
    }
}
