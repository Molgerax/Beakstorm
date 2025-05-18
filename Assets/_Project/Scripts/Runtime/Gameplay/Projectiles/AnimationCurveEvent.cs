using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class AnimationCurveEvent : MonoBehaviour
    {
        [SerializeField] private AnimationCurve curve = new();
        [SerializeField] private UltEvent<float> remappedValue;

        public float EvaluateCurve(float t)
        {
            if (curve == null)
                return 0;

            float value = curve.Evaluate(t);
            remappedValue?.Invoke(value);
            
            return value;
        }
    }
}
