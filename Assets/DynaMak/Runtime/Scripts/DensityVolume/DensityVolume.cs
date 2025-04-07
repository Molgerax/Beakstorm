using System;
using UnityEngine;

namespace DynaMak.Volumes
{
    [System.Serializable]
    public class DensityVolume : VolumeComponent
    {
        #region Serialize Fields
        [SerializeField] private Vector3Int _resolution = Vector3Int.one * 16;
        #endregion

        #region Public Fields
        public VolumeTexture VolumeTexture;
        #endregion
        
        
        #region Override Functions
        public override VolumeTexture GetVolumeTexture()
        {
            return VolumeTexture;
        }

        public override Vector3 VolumeCenter => GetVolumeTexture().IsInitialized ? base.VolumeCenter : transform.position;
        public override Vector3 VolumeBounds => GetVolumeTexture().IsInitialized ? base.VolumeBounds : transform.localScale;
        public override Vector3Int VolumeResolution => GetVolumeTexture().IsInitialized ? base.VolumeResolution : _resolution;
        
        #endregion
        

        #region Mono Methods

        private void Awake()
        {
            VolumeTexture = new VolumeTexture(RenderTextureFormat.RHalf, _resolution, transform.position,
                transform.localScale);
            VolumeTexture.Initialize();
        }

        private void Update()
        {
            UpdateTransform();
        }

        private void OnDestroy()
        {
            VolumeTexture.Release();
        }
        private void OnDisable()
        {
            VolumeTexture.Release();
        }

        #endregion

        #region Private Methods

        void UpdateTransform()
        {
            VolumeTexture.SetTransforms(transform.position, transform.localScale);
        }

        #endregion
    }
}