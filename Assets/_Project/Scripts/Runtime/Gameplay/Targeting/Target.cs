using System;
using System.Collections.Generic;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Targeting
{
    public class Target : MonoBehaviour, IComparable<Target>
    {
        public static List<Target> Targets = new(64);

        [SerializeField] private Faction faction;

        [NonSerialized] public float Weight;

        [SerializeField] private UltEvent<bool> onSetCurrentTarget;
        [SerializeField] private UltEvent<bool> onSetActiveTarget;
        
        public Faction Faction => faction;

        public Vector3 Position => transform.position;

        private void OnEnable()
        {
            onSetCurrentTarget?.Invoke(false);
            onSetActiveTarget?.Invoke(false);
            
            Targets ??= new(64);
            Targets.Add(this);
        }

        private void OnDisable() => Targets.Remove(this);

        public void SetTarget(bool value)
        {
            onSetCurrentTarget?.Invoke(value);
        }

        public void SetActiveTarget(bool value)
        {
            onSetActiveTarget?.Invoke(value);
        }

        
        public int CompareTo(Target other)
        {
            if (this.Weight < other.Weight) return -1;
            if (this.Weight > other.Weight) return 1;
            return 0;
        }
    }
}