using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Beakstorm.UI.Indicators
{
    public class OffscreenIndicatorManager : MonoBehaviour
    {
        public static List<OffscreenTarget> Targets = new(32);

        public static OffscreenIndicatorManager Instance;
        
        [SerializeField] private OffscreenIndicator offscreenIndicatorPrefab;

        private IObjectPool<OffscreenIndicator> _indicatorPool;
        private Transform _poolParentTransform;
        
        private Camera _camera;
        private Canvas _canvas;

        private Plane[] _frustumPlanes = new Plane[6];
        
        private void Awake()
        {
            Instance = this;

            _camera = Camera.main;
            _poolParentTransform = GetComponent<RectTransform>();

            _canvas = GetComponent<Canvas>();
            
            _indicatorPool = new ObjectPool<OffscreenIndicator>(CreateOffscreenIndicator, OnGetFromPool,
                OnReleaseToPool, OnDestroyPooledObject,
                false, 32, 256);

            foreach (OffscreenTarget target in Targets)
            {
                SpawnIndicator(target);
            }
        }

        private void Update()
        {
            GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
            
            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                var target = Targets[i];
                if (!target)
                {
                    Targets.RemoveAt(i);
                    continue;
                }
                
                target.UpdateIndicator(_frustumPlanes);
            }
        }


        public static void Register(OffscreenTarget target)
        {
            if (Targets.Contains(target))
                return;
                
            Targets.Add(target);

            if (Instance)
                Instance.SpawnIndicator(target);
        }
        
        public static void Unregister(OffscreenTarget target)
        {
            if (!Targets.Contains(target))
                return;

            Targets.Remove(target);
            target.DeactivateIndicator();
        }

        public void SpawnIndicator(OffscreenTarget target)
        {
            var offscreenIndicator = _indicatorPool.Get();
            offscreenIndicator.ObjectPool = _indicatorPool;
            offscreenIndicator.Initialize(target, _camera, _canvas);
        }
        
        
        private OffscreenIndicator CreateOffscreenIndicator()
        {
            var offscreenIndicator = Object.Instantiate(offscreenIndicatorPrefab, _poolParentTransform, false);
            offscreenIndicator.transform.localScale = Vector3.one;
            offscreenIndicator.ObjectPool = _indicatorPool;
            return offscreenIndicator;
        }

        private void OnGetFromPool(OffscreenIndicator offscreenIndicator)
        {
            offscreenIndicator.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(OffscreenIndicator offscreenIndicator)
        {
            offscreenIndicator.gameObject.SetActive(false);
        }

        private void OnDestroyPooledObject(OffscreenIndicator offscreenIndicator)
        {
            if (offscreenIndicator)
                Object.Destroy(offscreenIndicator.gameObject);
        }
    }
}
