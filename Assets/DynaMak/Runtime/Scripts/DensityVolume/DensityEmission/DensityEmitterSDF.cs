using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes
{
    [System.Serializable]
    public class DensityEmitterSDF : DensityEmitterBase
    {
        #region Serialize Fields

        [Header("Emission Settings")] 
        [SerializeField] protected VolumeComponent _sdfVolumeComponent;
        [SerializeField] [Range(0f, 5.0f)] protected float _surfaceLevel = 1f;
        [SerializeField] protected bool _fillInside;
        

        #endregion

        #region Shader Property IDs

        private int surfaceLevelID = Shader.PropertyToID("_SurfaceLevel");
        private int fillInsideID = Shader.PropertyToID("_FillInside");

        private int sdfVolumeID = Shader.PropertyToID("_SDFVolume");
        private int sdfCenterID = Shader.PropertyToID("_SDFCenter");
        private int sdfBoundsID = Shader.PropertyToID("_SDFBounds");

        #endregion

        // -------------------------------

        #region Override Functions

        protected override void SetOtherComputeValues()
        {
            _computeShader.SetBool(fillInsideID, _fillInside);
            _computeShader.SetFloat(surfaceLevelID, _surfaceLevel);
            _computeShader.SetVolume(0, _sdfVolumeComponent.GetVolumeTexture(), sdfVolumeID, sdfCenterID, sdfBoundsID);
        }

        #endregion


        #region Editor

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_sdfVolumeComponent.GetVolumeTexture().Center, _sdfVolumeComponent.GetVolumeTexture().Bounds * 2);
        }

        #endregion
    }
}