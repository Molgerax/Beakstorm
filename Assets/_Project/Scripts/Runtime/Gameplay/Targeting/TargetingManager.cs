using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Targeting
{
    public class TargetingManager : MonoBehaviour
    {
        [SerializeField] private TargetingStrategy strategy;
        [SerializeField] private Transform viewAnchor;

        public Transform ViewAnchor => viewAnchor;
        
        public List<Target> ActiveTargets = new List<Target>(8);
        
        private void Start()
        {
            if (!viewAnchor)
            {
                if (Camera.main != null)
                    viewAnchor = Camera.main.transform;
                else
                    viewAnchor = transform;
            }
        }

        private void Update()
        {
            if (strategy)
                strategy.Tick(this);
        }
    }
}
