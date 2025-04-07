using UnityEngine;
using DynaMak.Utility;
using UnityEngine.Rendering;

namespace DynaMak.Volumes.FluidSimulation
{
    public class FluidFieldAddGravity : FluidFieldAddBase
    {
        #region Serialize Fields

        [Header("Addition Settings")] 
        [SerializeField] private Transform target;
        [SerializeField] private float strength = 0.1f;
        [SerializeField, Min(0f)] private float maxStrength = 1f;
        [SerializeField, Range(0f,1f)] private float useDensityAsMask = 0;

        [Header("Exclude SDF")]
        [SerializeField] private VolumeComponent sdfVolume;
        [SerializeField] private float surface;

        #endregion

        
        #region Shader Property IDs
        private int _sdfVolumeID = Shader.PropertyToID("_SDFVolume");
        private int _sdfCenterID = Shader.PropertyToID("_SDFCenter");
        private int _sdfBoundsID = Shader.PropertyToID("_SDFBounds");
        private int _surfaceID = Shader.PropertyToID("_Surface");
        
        private int addDirectionID = Shader.PropertyToID("_AddDirection");
        private int addStrengthID = Shader.PropertyToID("_AddStrength");
        private int maxStrengthID = Shader.PropertyToID("_MaxStrength");
        private int useWeightID = Shader.PropertyToID("_UseWeight");

        private LocalKeyword _useSdfKeyword;
        #endregion

        
        
        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddGravity";
        
        protected override void Initialize()
        {
            base.Initialize();
            
            if(target == null) target = transform;

            _useSdfKeyword = new LocalKeyword(_computeShader, "SDF_ON");
        }

        protected override void SetProperties()
        {
            base.SetProperties();

            _computeShader.SetKeyword(_useSdfKeyword, sdfVolume);

            if (sdfVolume)
            {
                _computeShader.SetVolume(0, sdfVolume.GetVolumeTexture(), _sdfVolumeID, _sdfCenterID, _sdfBoundsID);
                _computeShader.SetFloat(_surfaceID, surface);
            }
            
            _computeShader.SetVector(addDirectionID, target.forward);
            _computeShader.SetFloat(addStrengthID, strength);
            _computeShader.SetFloat(maxStrengthID, maxStrength);
            _computeShader.SetFloat(useWeightID, useDensityAsMask);
        }

        #endregion
    }
}