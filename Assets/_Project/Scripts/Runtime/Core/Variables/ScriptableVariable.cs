using UnityEngine;

namespace Beakstorm.Core.Variables
{
    public abstract class ScriptableVariable<T> : ScriptableObject
    {
        public T Value;
    }
}
