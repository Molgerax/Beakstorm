using System;
using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Volumes.Voxelizer
{
    [ExecuteAlways]
    [System.Serializable]
    public class SetBoundsToSkinnedMesh : MonoBehaviour
    {
        #region Serialize Fields
        [SerializeField] private Renderer trackedRenderer;
        [SerializeField] private Vector3 multiplier = Vector3.one;
        #endregion

        #region Private Fields
        private Bounds _meshBounds;
        private Transform _transform;
        #endregion

        #region Mono Methods

        private void Awake()
        {
            _transform = transform;
        }

        private void LateUpdate()
        {
            SetBounds();
        }

        #endregion

        #region Private Methods

        void SetBounds()
        {
            if (trackedRenderer)
            {
                _meshBounds = trackedRenderer.bounds;
                _transform.position = _meshBounds.center;
                _transform.localScale = _meshBounds.size.PointWiseProduct(multiplier);
            }
        }

        #endregion
    }
}