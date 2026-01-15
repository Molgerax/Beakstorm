using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Beakstorm.Inputs
{
    public class SwitchUiInputs : MonoBehaviour
    {
        private static int _currentInstances = 0;


        private static int CurrentInstances
        {
            get => _currentInstances;
            set
            {
                if (!_asyncIsRunning)
                {
                    _asyncIsRunning = true;
                    _waitForEndOfFrame = EvaluateInputsAfterDelay(_currentInstances);
                }
                _currentInstances = value;
            }
        }

        private static bool _asyncIsRunning;

        private static UniTask _waitForEndOfFrame;

        private static async UniTask EvaluateInputsAfterDelay(int previousCount)
        {
            await UniTask.WaitForEndOfFrame();
            
            if (_currentInstances == 0 && previousCount > 0)
            {
                PlayerInputs.Instance.EnablePlayerInputs();
            }
            else if (_currentInstances > 0 && previousCount == 0)
            {
                PlayerInputs.Instance.EnableUiInputs();
            }

            _asyncIsRunning = false;
        }
        
        private void OnEnable() => CurrentInstances++;

        private void OnDisable() => CurrentInstances--;
    }
}
