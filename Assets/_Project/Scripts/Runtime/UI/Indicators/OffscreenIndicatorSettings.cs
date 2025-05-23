using UnityEngine;

namespace Beakstorm.UI.Indicators
{
    [CreateAssetMenu(menuName = "Beakstorm/UI/IndicatorSettings", fileName = "IndicatorSettings")]
    public class OffscreenIndicatorSettings : ScriptableObject
    {
        [SerializeField] private Sprite indicatorTexture;
        [SerializeField] private Color color = Color.white;

        public Color Color => color;
        public Sprite IndicatorTexture => indicatorTexture;
    }
}
