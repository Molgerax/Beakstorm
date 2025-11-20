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
        [SerializeField] private TMP_Text ammoCount;
        [SerializeField] private Image cooldownBar;
        [SerializeField] private Image reloadBar;

        private PheromoneWeaponInstance _currentWeapon;
        private PheromoneWeaponInventory _inventory => PheromoneWeaponInventory.Instance;

        private bool _initialized = false;
        
        public void OnEnable()
        {
            PlayerInputs.Instance.selectPheromoneAction.performed += OnSelectPheromone;

            _initialized = false;
            CycleItems(0);
        }

        private void OnDisable()
        {
            PlayerInputs.Instance.selectPheromoneAction.performed -= OnSelectPheromone;
        }

        private void Update()
        {
            if (!_initialized)
                CycleItems(0);
            
            UpdateData();
        }

        private void UpdateData()
        {
            if (_currentWeapon == null)
                return;

            if (ammoCount)
                ammoCount.text = $"{_currentWeapon.Count}";

            if (cooldownBar)
            {
                if (_currentWeapon.Count == 0)
                    cooldownBar.fillAmount = 1;
                else 
                    cooldownBar.fillAmount = _currentWeapon.Cooldown01;
            }
            
            if (reloadBar)
                reloadBar.fillAmount = _currentWeapon.ReloadTime01;
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
            if (!_inventory)
            {
                image.sprite = null;
                text.text = null;
                _initialized = false;
                return;
            }

            _initialized = true;
            
            _inventory.SelectedIndex += value;
            _currentWeapon = _inventory.SelectedWeapon;

            if (_currentWeapon != null)
            {
                image.sprite = _currentWeapon.Weapon.DisplaySprite;
                text.text = _currentWeapon.Weapon.DisplayName;
            }
            else
            {
                image.sprite = null;
                text.text = null;
            }
            
            UpdateData();
        }
    }
}
