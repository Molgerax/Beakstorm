using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        [SerializeField] private TabGroup tabGroup;
        [SerializeField] private Image background;

        [SerializeField] private GameObject[] children;
        
        private Sprite _defaultSprite;

        private void Reset()
        {
            tabGroup = GetComponentInParent<TabGroup>();
            background = GetComponent<Image>();
            
            if (tabGroup)
                tabGroup.Subscribe(this);
        }

        private void OnValidate()
        {
            if (tabGroup)
                tabGroup.Subscribe(this);
        }

        private void Awake()
        {
            if (!background)
                background = GetComponent<Image>();
            _defaultSprite = background.sprite;
        }

        public void SetChildrenActive(bool value)
        {
            foreach (GameObject child in children)
            {
                if (child)
                    child.SetActive(value);
            }
        }
        
        public void SetSprite(Sprite sprite)
        {
            background.sprite = sprite ? sprite : _defaultSprite;
        }

        public void SetColor(Color color) => background.color = color;

        public void OnPointerEnter(PointerEventData eventData)
        {
            tabGroup.OnTabEnter(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            tabGroup.OnTabSelected(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tabGroup.OnTabExit(this);
        }
    }
}
