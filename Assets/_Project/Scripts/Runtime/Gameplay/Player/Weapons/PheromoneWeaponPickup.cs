using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [PrefabEntity("weapon_pickup")]
    public class PheromoneWeaponPickup : MonoBehaviour
    {
        [SerializeField] private PheromoneWeapon weapon;
        [SerializeField, NoTremble] private SpriteRenderer spriteRenderer;


        private void Awake()
        {
            if (spriteRenderer && weapon)
                spriteRenderer.sprite = weapon.DisplaySprite;
        }

        private void Update()
        {
            if (spriteRenderer)
                spriteRenderer.transform.Rotate(Vector3.up, 90 * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!weapon)
                return;
        
            if (other.TryGetComponent(out PheromoneWeaponInventory inventory))
            {
                inventory.GiveWeapon(weapon);
                gameObject.SetActive(false);
            }
        }
    }
}