using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Targeting
{
    public class Target : MonoBehaviour
    {
        public static List<Target> Targets = new List<Target>(64);

        [SerializeField] private Faction faction;
        
        public Faction Faction => faction;

        private void OnEnable() => Targets.Add(this);

        private void OnDisable() => Targets.Remove(this);
    }
}