using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    [System.Serializable]
    public class FluidFieldAddSDF : FluidFieldAddBase
    {
        #region Serialize Fields

        [Header("Addition Settings")]
        [SerializeField, SerializeReference] protected VolumeComponent sdfVolumeComponent;

        [SerializeField] [Range(0f, 1f)] private float density = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float velocityStrength = 0.1f;
        [SerializeField] private Vector2 minMaxSurface = Vector2.up;
        [SerializeField] private bool mirroredEmission = false;
        [SerializeField] private Falloff falloff = Falloff.SmoothStep;

        #endregion

        private enum Falloff
        {
            Linear = 0, SmoothStep = 1, Quadratic = 2, Quartic = 3, One = 4
        }

        #region Shader Property IDs

        private int _sdfVolumeID = Shader.PropertyToID("_SDFVolume");
        private int _sdfCenterID = Shader.PropertyToID("_SDFCenter");
        private int _sdfBoundsID = Shader.PropertyToID("_SDFBounds");
        
        private int _addDensityID = Shader.PropertyToID("_AddDensity");
        private int _addStrengthID = Shader.PropertyToID("_AddStrength");
        private int _falloffID = Shader.PropertyToID("_Falloff");
        private int _minMaxSurfaceID = Shader.PropertyToID("_MinMaxSurface");
        private int _mirrorID = Shader.PropertyToID("_Mirror");
        
        #endregion

        // -------------------------------

        #region Override Functions
        
        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddSDF";

        protected override void SetProperties()
        {
            base.SetProperties();
            
            _computeShader.SetVolume(0, sdfVolumeComponent.GetVolumeTexture(), _sdfVolumeID, _sdfCenterID, _sdfBoundsID);
            
            _computeShader.SetInt(_mirrorID, mirroredEmission ? 1 : 0);
            _computeShader.SetInt(_falloffID,  (int) falloff);
            _computeShader.SetVector(_minMaxSurfaceID, minMaxSurface);
            
            _computeShader.SetFloat(_addDensityID, density);
            _computeShader.SetFloat(_addStrengthID, velocityStrength);
        }

        #endregion


        #region Editor

        #endregion
    }
}