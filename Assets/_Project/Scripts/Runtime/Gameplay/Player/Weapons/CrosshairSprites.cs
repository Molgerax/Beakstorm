using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    public class CrosshairSprites : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        [SerializeField] private PheromoneWeaponInventory inventory;
        
        private void OnEnable()
        {
            inventory.OnSelectWeapon += SetFromWeapon;
            SetFromWeapon(inventory.SelectedWeapon);
        }
        
        private void OnDisable()
        {
            inventory.OnSelectWeapon -= SetFromWeapon;
        }


        public void SetFromWeapon(PheromoneWeapon weapon)
        {
            if (weapon)
                SetCrosshairs(weapon.CrosshairSettings);
            else 
                SetCrosshairs(default);
        }

        private void SetCrosshairs(CrosshairSettings settings)
        {
            for (var index = 0; index < spriteRenderers.Length; index++)
            {
                var spriteRenderer = spriteRenderers[index];
                spriteRenderer.sprite = settings.Sprite;

                var t = spriteRenderer.transform;
                t.localScale = Vector3.one * settings.Scale;
                t.localPosition = Vector3.forward * ((index + 1) * settings.Distance);

                spriteRenderer.enabled = index < settings.Count;
            }
        }
    }
}