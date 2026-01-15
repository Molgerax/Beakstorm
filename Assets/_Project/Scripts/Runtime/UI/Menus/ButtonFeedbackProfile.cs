using UnityEngine;
using UnityEngine.EventSystems;

namespace Beakstorm.UI.Menus
{
    [CreateAssetMenu(fileName = "ButtonFeedbackProfile", menuName = "Beakstorm/UI/Button Feedback Profile")]
    public class ButtonFeedbackProfile : ScriptableObject
    {
        [SerializeField] private string pointerEnterEvent = "play_menuChoose";
        [SerializeField] private string pointerClickEvent = "play_menuConfirm";
        
        
        public void OnPointerEnter(ButtonFeedback feedback, BaseEventData eventData)
        {
            if (!string.IsNullOrEmpty(pointerEnterEvent))
                AkUnitySoundEngine.PostEvent(pointerEnterEvent, feedback.gameObject);
        }

        public void OnPointerClick(ButtonFeedback feedback, BaseEventData eventData)
        {
            if (!string.IsNullOrEmpty(pointerClickEvent))
                AkUnitySoundEngine.PostEvent(pointerClickEvent, feedback.gameObject);
        }

        public void OnClickDisabled(ButtonFeedback feedback, BaseEventData eventData)
        {
        }
    }
}
