using UnityEngine;

namespace Beakstorm.UI.Indicators
{
    [CreateAssetMenu(menuName = "Beakstorm/UI/IndicatorSettings", fileName = "IndicatorSettings")]
    public class OffscreenIndicatorSettings : ScriptableObject
    {
        [SerializeField] private Sprite indicatorTexture;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float scale = 1f;

        [SerializeField] private float indicatorRadius = 384;

        [SerializeField] private Sprite outlineTexture;
        [SerializeField] private Color outlineColor = Color.white;

        public Color Color => color;
        public Sprite IndicatorTexture => indicatorTexture;
        public float Scale => scale;

        public float IndicatorRadius => indicatorRadius;
        
        public Color OutlineColor => outlineColor;
        public Sprite OutlineTexture => outlineTexture;
    }
}
