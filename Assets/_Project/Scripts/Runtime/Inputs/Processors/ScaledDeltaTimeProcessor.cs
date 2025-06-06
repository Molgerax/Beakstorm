using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Inputs.Processors
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class ScaledDeltaTimeProcessor : InputProcessor<Vector2>
    {
        [Tooltip("When true, divides by the target framerate")]
        public int rectifyByTargetFrameRate = 60;
        
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            value /= Time.unscaledDeltaTime;

            rectifyByTargetFrameRate = Mathf.Max(0, rectifyByTargetFrameRate);

            if (rectifyByTargetFrameRate > 0)
                value /= rectifyByTargetFrameRate;
            
            return value;
        }

#if UNITY_EDITOR
        static ScaledDeltaTimeProcessor() => Initialize();
#endif
 
        [RuntimeInitializeOnLoadMethod]
        static void Initialize() => InputSystem.RegisterProcessor<ScaledDeltaTimeProcessor>();
    }
}