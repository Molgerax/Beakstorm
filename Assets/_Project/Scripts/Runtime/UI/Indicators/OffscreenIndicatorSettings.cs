using PrimeTween;
using UnityEngine;

namespace Beakstorm.UI.Indicators
{
    [CreateAssetMenu(menuName = "Beakstorm/UI/IndicatorSettings", fileName = "IndicatorSettings")]
    public class OffscreenIndicatorSettings : ScriptableObject
    {
        [Header("Offscreen Indicator")]
        [SerializeField] private Sprite indicatorTexture;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float scale = 1f;

        [SerializeField] private float indicatorRadius = 384;

        [Header("Onscreen Outline")]
        [SerializeField] private Sprite outlineTexture;
        [SerializeField] private Color outlineColor = Color.white;

        [SerializeField] private bool adjustToBounds = true;
        [SerializeField] private float outlineBoundsSize = 64;

        [SerializeField] private TweenSettings<float> selectSizeTween = new(1f, 0.5f);

        public Color Color => color;
        public Sprite IndicatorTexture => indicatorTexture;
        public float Scale => scale;

        public float IndicatorRadius => indicatorRadius;
        
        public Color OutlineColor => outlineColor;
        public Sprite OutlineTexture => outlineTexture;

        public bool AdjustToBounds => adjustToBounds;
        public float OutlineBoundsSize => outlineBoundsSize;
        
        public TweenSettings<float> SelectSizeTween => selectSizeTween;
    }
}
