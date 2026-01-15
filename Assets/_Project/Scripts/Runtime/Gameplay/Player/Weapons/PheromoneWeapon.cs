using UnityEngine;
using UnityEngine.Serialization;

namespace Beakstorm.Gameplay.Player.Weapons
{
    public abstract class PheromoneWeapon : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] protected Sprite displaySprite;
        [SerializeField] protected string displayName;

        [Header("Crosshair")] 
        [SerializeField] protected CrosshairSettings crosshairSettings = new(null);

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

        public CrosshairSettings CrosshairSettings => crosshairSettings;

        public abstract void FireProjectile(FireInfo fireInfo);
    }

    [System.Serializable]
    public struct CrosshairSettings
    {
        [FormerlySerializedAs("Sprite")] public Sprite sprite;
        [FormerlySerializedAs("Count")] [Range(1, 3)] public int count;
        [FormerlySerializedAs("Scale")] public float scale;
        [FormerlySerializedAs("Distance")] public float distance;

        public Sprite Sprite => sprite;
        public int Count => sprite ? count : 0;
        public float Scale => sprite ? scale : 1;
        public float Distance => sprite ? distance : 50;
        
        public CrosshairSettings(Sprite sprite, int count = 1, float scale = 1f, float distance = 50)
        {
            this.sprite = sprite;
            this.count = count;
            this.scale = scale;
            this.distance = distance;
        }
    }
}
