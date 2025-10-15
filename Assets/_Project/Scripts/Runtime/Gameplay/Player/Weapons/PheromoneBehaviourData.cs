using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [CreateAssetMenu(menuName = "Beakstorm/Pheromone/BehaviourData", fileName = "PheromoneData")]
    public class PheromoneBehaviourData : ScriptableObject
    {
        [Header("Pheromone")] 
        [SerializeField] private int pheromoneEmission = 128;
        [SerializeField] private AnimationCurve pheromoneEmissionCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        
        [SerializeField] private float pheromoneLife = 3f;
        [SerializeField] private AnimationCurve pheromoneLifeCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        [SerializeField] private float velocityFactor = 1f;
        
        [Header("Projectile")]
        [SerializeField] private float projectileLifeTime = 5f;
        [SerializeField] private float gravity = 5;

        public float ProjectileLifeTime => projectileLifeTime;
        public float Gravity => gravity;

        public float GetLifeTime01(float currentLife) => currentLife / projectileLifeTime;

        public float GetPheromoneEmission(float currentLife) =>
            pheromoneEmissionCurve.Evaluate(GetLifeTime01(currentLife)) * pheromoneEmission;
        
        public float GetPheromoneLife(float currentLife) =>
            pheromoneLifeCurve.Evaluate(GetLifeTime01(currentLife)) * pheromoneLife;

        public float GetPheromoneVelocity() => velocityFactor;
    }
}
