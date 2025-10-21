using Beakstorm.Core.Variables;
using UnityEngine;

namespace Beakstorm.UI.HUD
{
    public class GaugeSliderUI : MonoBehaviour
    {
        [SerializeField] private RangeVariable variable;
        
        [SerializeField] private RectTransform child;

        private void Update()
        {
            RectTransform rect = (RectTransform) transform;

            if (variable && child)
            {
                var childAnchoredPosition = child.anchoredPosition;
                childAnchoredPosition.y = rect.rect.height * variable.Get01;
                child.anchoredPosition = childAnchoredPosition;
            }
        }
    }
}
