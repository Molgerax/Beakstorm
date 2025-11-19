using Beakstorm.Inputs;
using UnityEngine;
using UnityEngine.UI;

namespace Beakstorm.UI.Icons
{
    public class ButtonPrompt : MonoBehaviour
    {
        [SerializeField] private string actionName;
        [SerializeField] private ButtonIcons icons;
        [SerializeField] private Image image;

        private void Reset()
        {
            if (!image)
                image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            PlayerInputs.ActiveDeviceChangeEvent += SetText;
            SetText();
        }

        private void OnDisable()
        {
            PlayerInputs.ActiveDeviceChangeEvent -= SetText;
        }

        [ContextMenu("Set Text")]
        private void SetText()
        {
            image.sprite =
                CompleteTextWithButtonPromptSprite.GetSpriteFromBinding(actionName, icons, out Vector4 uv);
            //image.uvRect = Rect.MinMaxRect(uv.x, uv.y, uv.x + uv.z, uv.y + uv.w);
        }
    }
}