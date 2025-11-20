using Beakstorm.Inputs;
using TMPro;
using UnityEngine;

namespace Beakstorm.UI.Icons
{
    public class TextBoxButtonPrompts : MonoBehaviour
    {
        [SerializeField] private ButtonIcons icons;
        [SerializeField] private TMP_Text textBox;

        private string _text;

        private void Reset()
        {
            if (!textBox)
                textBox = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            PlayerInputs.ActiveDeviceChangeEvent += SetText;
            _text = textBox.text;
            
            SetText();
        }

        private void OnDisable()
        {
            PlayerInputs.ActiveDeviceChangeEvent -= SetText;
            textBox.text = _text;
        }

        [ContextMenu("Set Text")]
        private void SetText()
        {
            textBox.text =
                CompleteTextWithButtonPromptSprite.ReplaceActiveBindings(_text, PlayerInputs.Instance, icons);
            textBox.spriteAsset = icons.GetAssetByDevice(PlayerInputs.LastActiveDevice);
        }
    }
}