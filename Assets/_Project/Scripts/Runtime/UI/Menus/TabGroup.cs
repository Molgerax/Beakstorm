using System.Collections.Generic;
using Beakstorm.Inputs;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

namespace Beakstorm.UI.Menus
{
    public class TabGroup : MonoBehaviour
    {
        [SerializeField, HideInInspector] private List<TabButton> _tabButtons;
        private TabButton _selectedTab;

        [Header("Sprites")]
        [SerializeField] private Sprite tabIdle;
        [SerializeField] private Sprite tabHover;
        [SerializeField] private Sprite tabActive;

        [Header("Colors")]
        [SerializeField] private Color colorIdle = Color.white;
        [SerializeField] private Color colorHover = Color.white;
        [SerializeField] private Color colorActive = Color.white;

        private void OnEnable()
        {
            PlayerInputs.Instance.cycleTabsAction.performed += CycleTabsAction;
            
            ResetTabs(true);
            CycleTabs(0);
        }
        
        private void OnDisable()
        {
            PlayerInputs.Instance.cycleTabsAction.performed -= CycleTabsAction;
        }

        public void Subscribe(TabButton button)
        {
            _tabButtons ??= new List<TabButton>();
            if (!_tabButtons.Contains(button))
                _tabButtons.Add(button);
        }

        private void OnValidate()
        {
            if (_tabButtons == null)
                return;

            for (int i = _tabButtons.Count - 1; i >= 0; i--)
            {
                if (!_tabButtons[i])
                    _tabButtons.RemoveAt(i);
            }
        }

        public void OnTabEnter(TabButton button)
        {
            ResetTabs();

            if (!_selectedTab || button != _selectedTab)
            {
                button.SetSprite(tabHover);
                button.SetColor(colorHover);
            }
        }
        
        public void OnTabExit(TabButton button)
        {
            ResetTabs(); 
        }

        public void OnTabSelected(TabButton button)
        {
            _selectedTab = button;
            ResetTabs(true);
            button.SetSprite(tabActive);            
            button.SetColor(colorActive);
        }

        public void ResetTabs(bool setActive = false)
        {
            foreach (TabButton tabButton in _tabButtons)
            {
                if (!tabButton)
                    continue;
                
                if (setActive)
                    tabButton.SetChildrenActive(tabButton == _selectedTab);
                
                if (_selectedTab == tabButton)
                    continue;
                
                tabButton.SetSprite(tabIdle);
                tabButton.SetColor(colorIdle);
            }
        }

        private void CycleTabs(int increment)
        {
            if (_tabButtons == null || _tabButtons.Count == 0)
                return;

            if (!_selectedTab)
            {
                OnTabSelected(_tabButtons[0]);
                return;
            }

            int selectedIndex = _tabButtons.IndexOf(_selectedTab);
            selectedIndex = (selectedIndex + increment);
            selectedIndex = (selectedIndex % _tabButtons.Count + _tabButtons.Count) % _tabButtons.Count;
            
            OnTabSelected(_tabButtons[selectedIndex]);
        }

        private void CycleTabsAction(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            if (value != 0)
            {
                int indexShift = value > 0 ? 1 : -1;
                CycleTabs(indexShift);
            }
        }
    }
}
