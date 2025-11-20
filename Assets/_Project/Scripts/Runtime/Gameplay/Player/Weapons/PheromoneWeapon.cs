using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    public abstract class PheromoneWeapon : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] protected Sprite displaySprite;
        [SerializeField] protected string displayName;
        
        [Header("Data")]
        [SerializeField] protected float fireDelay = 0.5f;
        [SerializeField] protected int ammoCost = 1;
        [SerializeField] protected int pickupCount = 5;
        [SerializeField] protected float reloadTime = 1;
        [SerializeField] protected int maxReloadCount = 5;

        public Sprite DisplaySprite => displaySprite;
        public string DisplayName => displayName;
        public float FireDelay => fireDelay;
        public int AmmoCost => ammoCost;
        public int PickupCount => pickupCount;
        public float ReloadTime => reloadTime;
        public int MaxReloadCount => maxReloadCount;

        public abstract void FireProjectile(FireInfo fireInfo);
    }
}
