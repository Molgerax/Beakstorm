using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Collisions.Impacts
{
    [CreateAssetMenu(fileName = "ImpactSpriteSheet", menuName = "Beakstorm/Impact/Impact Sprite Sheet")]
    public class ImpactSpriteSheet : ScriptableObject
    {
        [SerializeField] private ImpactSprite[] sprites;

        [SerializeField, HideInInspector] private int _maxFrames;
        [SerializeField, HideInInspector] private int _maxResolution;
        [SerializeField, HideInInspector] private int _validSpriteCount;
        
        [SerializeField] public Texture2D _resultTexture;
        
        public Texture2D Result => _resultTexture;

        public int MaxFrames => _maxFrames;
        public int MaxResolution => _maxResolution;
        public int ValidSpriteCount => _validSpriteCount;

        public ImpactSprite[] Sprites => sprites;


        public void SetMaterialPropertyBlock(MaterialPropertyBlock propBlock)
        {
            if (!Result)
                return;

            propBlock.SetTexture("_SpriteSheet", Result);
            propBlock.SetInt("_SpriteFrameCount", MaxFrames);
            propBlock.SetInt("_SpriteHeight", MaxResolution);
            propBlock.SetInt("_SpriteCount", ValidSpriteCount);
        }
        
        public void SetMaterialPropertyBlock(Material material)
        {
            if (!Result)
                return;

            material.SetTexture("_SpriteSheet", Result);
            material.SetInt("_SpriteFrameCount", MaxFrames);
            material.SetInt("_SpriteHeight", MaxResolution);
            material.SetInt("_SpriteCount", ValidSpriteCount);
        }

        public void GetDimensions()
        {
            _validSpriteCount = 0;
            _maxFrames = 1;
            _maxResolution = 16;
            
            foreach (ImpactSprite sprite in sprites)
            {
                if (!sprite)
                    continue;
                if(sprite.Sprites == null || !sprite.Sprite)
                    continue;

                _validSpriteCount++;
                _maxFrames = Mathf.Max(_maxFrames, sprite.FrameCount);
                _maxResolution = Mathf.Max(_maxResolution, sprite.Sprite.height);
            }
        }
    }
}
