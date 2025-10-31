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

            return (gameObject.TryGetComponent(out ITriggerable triggerable));
        }

        public static bool IsTriggerable(this Component component) => component && component.gameObject.IsTriggerable();
        
        public static bool TryTrigger(this GameObject gameObject)
        {
            if (!gameObject)
                return false;
            
            if (gameObject.TryGetComponent(out ITriggerable triggerable))
            {
                triggerable.Trigger();
                return true;
            }

            return false;
        }
        
        
        public static bool TryTrigger(this Component component) => component && component.gameObject.TryTrigger();
    }
}
