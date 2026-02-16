using System.Collections.Generic;
using Beakstorm.Mapping;
using UnityEngine;

namespace Beakstorm.Utility.Extensions
{
    public static class TriggerTargetExtensions
    {
        #region ITriggerTarget

        public static void TryTrigger(this ITriggerTarget target, TriggerData data = default)
        {
            if (target != null)
                target.Trigger(data);
        }
        
        public static void TryTrigger(this ITriggerTarget[] targets, TriggerData data = default)
        {
            if (targets == null)
                return;
            foreach (var target in targets)
            {
                target.TryTrigger(data);
            }
        }
        
        public static void TryTrigger(this List<ITriggerTarget> targets, TriggerData data = default)
        {
            if (targets == null)
                return;
            foreach (var target in targets)
            {
                target.TryTrigger(data);
            }
        }
        
        #endregion

        #region Component

        public static void TryTrigger(this Component component, TriggerData data = default)
        {
            if (component && component is ITriggerTarget target)
                target.Trigger(data);
        }

        public static void TryTrigger(this Component[] components, TriggerData data = default)
        {
            if (components == null)
                return;
            foreach (var component in components)
            {
                component.TryTrigger(data);
            }
        }

        public static void TryTrigger(this List<Component> components, TriggerData data = default)
        {
            if (components == null)
                return;
            foreach (var component in components)
            {
                component.TryTrigger(data);
            }
        }

        #endregion
        
        
        #region Conversion

        public static Component TriggerToComponent(this ITriggerTarget target) => target as Component;

        public static Component[] TriggerToComponent(this ITriggerTarget[] targets)
        {
            if (targets == null)
                return null;
            
            Component[] c = new Component[targets.Length];
            for (var index = 0; index < targets.Length; index++)
            {
                ITriggerTarget target = targets[index];
                c[index] = target.TriggerToComponent();
            }
            return c;
        }
        
        #endregion
    }
}