using System;
using Beakstorm.Inputs;
using Unity.Cinemachine;
using UnityEngine;

namespace Beakstorm.Gameplay.Cameras
{
    public class CameraCycle : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera[] cameras;

        private int _currentIndex = 0;
        
        private void OnEnable()
        {
            PlayerInputs.Instance.CameraViewPoint += OnCameraViewPointInput;

            if (Camera.main)
                Camera.main.cullingMask = Int32.MaxValue;

            _currentIndex = 0;
            Cycle(0);
        }
        
        private void OnDisable()
        {
            PlayerInputs.Instance.CameraViewPoint -= OnCameraViewPointInput;
        }

        private void OnCameraViewPointInput(bool performed)
        {
            if (!performed)
                return;

            Cycle(1);
        }

        private void Cycle(int increment)
        {
            _currentIndex += increment;
            _currentIndex = (cameras.Length + _currentIndex) % cameras.Length;

            for (var index = 0; index < cameras.Length; index++)
            {
                CinemachineCamera cam = cameras[index];
                cam.Priority.Value = index == _currentIndex ? 1 : 0;

                if (_currentIndex == index)
                {
                    if (cam.TryGetComponent(out CameraLayerMaskSetter setter))
                        setter.SetLayerMask();
                    
                    if (cam.TryGetComponent(out CameraCullUI cullUI))
                        cullUI.Activate();
                }
            }
        }
    }
}
