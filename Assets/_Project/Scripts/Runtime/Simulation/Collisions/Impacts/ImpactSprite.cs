using UnityEngine;

namespace Beakstorm.Simulation.Collisions.Impacts
{
    [CreateAssetMenu(fileName = "ImpactSprite", menuName = "Beakstorm/Impact/Impact Sprite")]
    public class ImpactSprite : ScriptableObject
    {
        [SerializeField] private Texture2D[] sprites;
        [SerializeField] private int frameCount;

        public Texture2D Sprite => sprites[0];
        public Texture2D[] Sprites => sprites;
        public int FrameCount => sprites?.Length ?? 0;
    }
}
