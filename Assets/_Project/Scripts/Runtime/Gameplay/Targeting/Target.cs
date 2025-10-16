using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Targeting
{
    public class Target : MonoBehaviour, IComparable<Target>
    {
        public static List<Target> Targets = new List<Target>(64);

        [SerializeField] private Faction faction;

        public float Weight;
        
        public Faction Faction => faction;

        private void OnEnable() => Targets.Add(this);

        private void OnDisable() => Targets.Remove(this);


        public int CompareTo(Target other)
        {
            if (this.Weight < other.Weight) return -1;
            if (this.Weight > other.Weight) return 1;
            return 0;
        }
    }
}