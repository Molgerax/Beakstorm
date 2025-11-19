using UnityEngine;

namespace Beakstorm.UI.Indicators
{
    [CreateAssetMenu(menuName = "Beakstorm/UI/IndicatorSettings", fileName = "IndicatorSettings")]
    public class OffscreenIndicatorSettings : ScriptableObject
    {
        [SerializeField] private Sprite indicatorTexture;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float scale = 1f;

        public Color Color => color;
        public Sprite IndicatorTexture => indicatorTexture;
        public float Scale => scale;
    }
}
