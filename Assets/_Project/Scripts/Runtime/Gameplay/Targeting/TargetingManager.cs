using System;
using System.Collections.Generic;
using Beakstorm.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Targeting
{
    public class TargetingManager : MonoBehaviour
    {
        [SerializeField] private TargetingStrategy strategy;
        [SerializeField] private Transform viewAnchor;

        [Header("Sound")]
        [SerializeField] private AK.Wwise.Event onSelectSound;
        [SerializeField] private AK.Wwise.Event onDeselectSound;
        
        public Transform ViewAnchor => viewAnchor;

        private HashSet<Target> _previouslyActiveTargets = new (8);
        [NonSerialized] public List<Target> ActiveTargets = new (8);

        
        
        private Target _currentTarget;

        private Target _previousTarget;

        public Target CurrentTarget
        {
            get => _currentTarget;
            private set
            {
                if (value == _currentTarget)
                    return;
                
                OnDeselectTarget(_currentTarget);
                _currentTarget = value;
                OnSelectTarget(_currentTarget);
            }
        }

        private void CacheActiveTargets()
        {
            _previouslyActiveTargets.Clear();
            foreach (Target target in ActiveTargets)
            {
                _previouslyActiveTargets.Add(target);
            }
        }

        private void SelectionCallbackToActiveTargets()
        {
            foreach (Target target in ActiveTargets)
            {
                if (_previouslyActiveTargets.Remove(target))
                    target.SetActiveTarget(true);
            }

            foreach (Target target in _previouslyActiveTargets)
            {
                target.SetActiveTarget(false);
            }
        }

        private void OnEnable()
        {
            PlayerInputs.Instance.lockOnAction.performed += OnLockOnAction;
        }


        private void OnDisable()
        {
            PlayerInputs.Instance.lockOnAction.performed -= OnLockOnAction;
        }
        
        
        private void OnLockOnAction(InputAction.CallbackContext context)
        {
            SelectTarget();
        }


        private void SelectTarget()
        {
            if (ActiveTargets.Count == 0)
            {
                CurrentTarget = null;
                return;
            }

            if (!CurrentTarget)
            {
                CurrentTarget = ActiveTargets[0];
                return;
            }

            foreach (Target target in ActiveTargets)
            {
                if (target == CurrentTarget)
                    continue;

                CurrentTarget = target;
                return;
            }

            CurrentTarget = null;
        }

        private void OnSelectTarget(Target target)
        {
            if (!target)
                return;
            target.SetTarget(true);

            onSelectSound.Post(gameObject);
        }

        private void OnDeselectTarget(Target target)
        {
            if (!target)
                return;
            target.SetTarget(false);
        }
        
        private void Start()
        {
            if (viewAnchor) 
                return;

            var main = Camera.main;
            viewAnchor = main != null ? main.transform : transform;
        }

        private void Update()
        {
            if (!CurrentTarget && _previousTarget)
            {
                onDeselectSound.Post(gameObject);
            }
                
            if (strategy)
            {
                CacheActiveTargets();
                strategy.Tick(this);
                SelectionCallbackToActiveTargets();
            }
            _previousTarget = CurrentTarget;
        }
    }
}
