using Beakstorm.Mapping;
using UnityEngine;

namespace Beakstorm.Utility.Extensions
{
    public static class GameObjectExtensions
    {
        public static bool IsTriggerable(this GameObject gameObject)
        {
            if (!gameObject)
                return false;

            return (gameObject.TryGetComponent(out TriggerBehaviour triggerable));
        }

        public static bool IsTriggerable(this Component component) => component && component.gameObject.IsTriggerable();
        
        public static bool TryTrigger(this GameObject gameObject)
        {
            if (!gameObject)
                return false;
            
            if (gameObject.TryGetComponent(out TriggerBehaviour triggerable))
            {
                triggerable.Trigger();
                return true;
            }

            return false;
        }

        public static void TryTrigger(this TriggerBehaviour[] components)
        {
            if (components == null)
                return;
            foreach (var component in components)
            {
                if (component)
                    component.Trigger();
            }
        }
        
        public static bool TryTrigger(this Component component) => component && component.gameObject.TryTrigger();
        

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (!component)
                component = gameObject.AddComponent<T>();
            return component;
        }
    }
}
