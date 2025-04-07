using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class FluidFieldAddMoveSphere : FluidFieldAddBase
    {
        #region Serialize Fields

        [Header("Addition Settings")] 
        [SerializeField] private float radius = 1f;
        [SerializeField] [Range(0f, 3f)] private float density = 0f;
        [SerializeField] private float strength = 1f;
        [SerializeField] [Range(0f, 10f)] private float velocityToDensity = 1;
        [SerializeField] private bool useCurl;

        public float Strength => strength;
        public float Radius => radius;
        #endregion

        #region Shader Property IDs
        private int addRadiusID = Shader.PropertyToID("_AddRadius");
        private int addStrengthID = Shader.PropertyToID("_AddStrength");
        private int addDensityID = Shader.PropertyToID("_AddDensity");
        private int velocityToDensityID = Shader.PropertyToID("_VelocityToDensity");
        private int useCurlID = Shader.PropertyToID("_UseCurl");
        #endregion

        
        
        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddMoveSphere";
        

        protected override void SetProperties()
        {
            base.SetProperties();
            
            _computeShader.SetFloat(addRadiusID, radius);
            _computeShader.SetFloat(addDensityID, density);
            _computeShader.SetFloat(addStrengthID, strength);
            _computeShader.SetFloat(velocityToDensityID, velocityToDensity);
            _computeShader.SetFloat(useCurlID, useCurl ? 1 : 0);
        }

        #endregion
    }
}