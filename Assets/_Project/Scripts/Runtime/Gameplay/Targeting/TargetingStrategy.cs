using UnityEngine;

namespace Beakstorm.Gameplay.Targeting
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        [SerializeField] protected Faction[] targetFactions;
        
        public abstract void Begin(TargetingManager targetingManager);
        public virtual void Tick(TargetingManager targetingManager) { }
        public virtual void Cancel(TargetingManager targetingManager) { }


        protected bool IsTargetFaction(Target target)
        {
            if (targetFactions == null || targetFactions.Length == 0)
                return false;
            
            bool value = false;
            foreach (Faction targetFaction in targetFactions)
            {
                if (target.Faction == targetFaction)
                    value = true;
            }
            return value;
        }
    }
}