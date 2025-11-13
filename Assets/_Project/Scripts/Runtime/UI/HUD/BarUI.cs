using Beakstorm.Core.Variables;
using UnityEngine;
using UnityEngine.UI;

namespace Beakstorm.UI.HUD
{
    public class BarUI : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private FloatVariable variable;

        private void Update()
        {
            if (variable)
                image.fillAmount = variable.GetValue;
        }
    }
}
