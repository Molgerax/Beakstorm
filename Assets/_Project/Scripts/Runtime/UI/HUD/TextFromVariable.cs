using Beakstorm.Core.Variables;
using TMPro;
using UnityEngine;

namespace Beakstorm.UI.HUD
{
    public class TextFromVariable : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private FloatVariable variable;

        private void Update()
        {
            if (!text)
                return;
            
            float value = variable.Get(); 
            text.text = $"{value:F2}";
        }
    }
}