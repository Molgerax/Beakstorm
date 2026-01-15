using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class ButtonFeedback : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private ButtonFeedbackProfile profile;

        public static Selectable LastSelected { get; private set; }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            LastSelected = GetComponent<Selectable>();
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

        public void OnSelect(BaseEventData eventData)
        {
            if (!profile)
                return;
            
            profile.OnPointerEnter(this, eventData);
        }
    }
}
