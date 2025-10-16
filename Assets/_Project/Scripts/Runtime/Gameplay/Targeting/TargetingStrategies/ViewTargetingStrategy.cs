using UnityEngine;

namespace Beakstorm.Gameplay.Targeting.TargetingStrategies
{
    [CreateAssetMenu(fileName = "ViewTargetingStrategy", menuName = "Beakstorm/Targeting/Strategies/ViewTargeting", order = 0)]
    public class ViewTargetingStrategy : TargetingStrategy
    {
        [SerializeField] private float maxDistance = 500;
        [SerializeField] private float maxAngle = 25;
        [SerializeField, Min(1)] private int maxTargets = 1;
        
        public override void Begin(TargetingManager targetingManager)
        {
            
        }

        public override void Tick(TargetingManager targetingManager)
        {
            targetingManager.ActiveTargets.Clear();
        
            foreach (Target target in Target.Targets)
            {
                if (!IsTargetFaction(target))
                    continue;
                
                Vector3 diff = target.transform.position - targetingManager.ViewAnchor.position;

                float angle = Vector3.Angle(targetingManager.ViewAnchor.forward, diff.normalized);

                if (diff.magnitude < maxDistance && angle < maxAngle)
                {
                    targetingManager.ActiveTargets.Add(target);
                    target.Weight = angle;
                }
            }
            
            targetingManager.ActiveTargets.Sort();

            for (int i = targetingManager.ActiveTargets.Count - 1; i >= maxTargets; i--)
            {
                targetingManager.ActiveTargets.RemoveAt(i);
            }
        }
    }
}