using System;
using Beakstorm.Gameplay.Player;
using Beakstorm.Gameplay.Player.Weapons;
using Beakstorm.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Beakstorm.UI.HUD
{
    public class WeaponSelectUI : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private TMP_Text text;

        private SimplePlayerWeapon _currentWeapon;
        
        
        public void OnEnable()
        {
            PlayerInputs.Instance.selectPheromoneAction.performed += OnSelectPheromone;
            
            CycleItems(0);
        }

        private void OnDisable()
        {
            PlayerInputs.Instance.selectPheromoneAction.performed -= OnSelectPheromone;
        }

        private void OnSelectPheromone(InputAction.CallbackContext context)
        {
            Vector2 read = context.ReadValue<Vector2>();

            float value = read.x;
            if (value == 0)
                value = read.y;

            if (value != 0)
            {
                int indexShift = value > 0 ? 1 : -1;
                CycleItems(indexShift);
            }
        }

        private void CycleItems(int value)
        {
            if (!PlayerController.Instance)
            {
                image.sprite = null;
                text.text = null;
                return;
            }
            
            PlayerController.Instance.SelectedWeaponIndex += value;
            _currentWeapon = PlayerController.Instance.SelectedWeapon;

            if (_currentWeapon)
            {
                image.sprite = _currentWeapon.DisplaySprite;
                text.text = _currentWeapon.DisplayName;
            }
            else
            {
                image.sprite = null;
                text.text = null;
            }
        }
    }
}
