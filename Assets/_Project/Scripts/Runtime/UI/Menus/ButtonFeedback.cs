using UnityEngine;
using UnityEngine.EventSystems;

namespace Beakstorm.UI.Menus
{
    public class ButtonFeedback : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] private ButtonFeedbackProfile profile;


        public void OnPointerClick(PointerEventData eventData)
        {
            if (!profile)
                return;
            
            profile.OnPointerClick(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!profile)
                return;
            
            profile.OnPointerEnter(this, eventData);
        }
    }
}
