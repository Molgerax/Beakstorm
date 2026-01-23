using System.Collections.Generic;
using Beakstorm.Mapping;
using UnityEngine;

namespace Beakstorm.Utility.Extensions
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (!component)
                component = gameObject.AddComponent<T>();
            return component;
        }
    }
}
