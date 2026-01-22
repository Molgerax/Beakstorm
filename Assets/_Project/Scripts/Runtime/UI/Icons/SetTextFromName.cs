using TMPro;
using UnityEngine;

namespace Beakstorm.UI.Icons
{
    public class SetTextFromName : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private string prefix = "Player/";

        private void Awake()
        {
            SetText();
        }

        private void OnValidate()
        {
            SetText();
        }

        private void SetText()
        {
            if (text)
                text.text = @$"{{{prefix}{gameObject.name}}}";
        }
    }
}