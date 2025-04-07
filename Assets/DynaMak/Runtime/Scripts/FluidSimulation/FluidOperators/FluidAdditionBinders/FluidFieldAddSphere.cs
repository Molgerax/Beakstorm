using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class FluidFieldAddSphere : FluidFieldAddBase
    {
        #region Serialize Fields

        [Header("Addition Settings")] 
        [SerializeField] private Transform target;
        [SerializeField] private float _radius = 1f;
        [SerializeField] [Min(0f)] private float _density = 0f;
        [SerializeField] private float _strength = 1f;

        public float Strength => _strength;
        public float Radius => _radius;
        public float Density => _density;
        #endregion

        #region Shader Property IDs
        private int addPositionID = Shader.PropertyToID("_AddPosition");
        private int addDirectionID = Shader.PropertyToID("_AddDirection");
        private int addRadiusID = Shader.PropertyToID("_AddRadius");
        private int addStrengthID = Shader.PropertyToID("_AddStrength");
        private int addDensityID = Shader.PropertyToID("_AddDensity");
        #endregion

        
        
        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddSphere";
        
        protected override void Initialize()
        {
            base.Initialize();
            
            if(target == null) target = transform;
        }

        protected override void SetProperties()
        {
            base.SetProperties();
            
            _computeShader.SetVector(addPositionID, target.position);
            _computeShader.SetVector(addDirectionID, target.forward);
            _computeShader.SetFloat(addRadiusID, _radius);
            _computeShader.SetFloat(addDensityID, _density);
            _computeShader.SetFloat(addStrengthID, _strength);
        }

        #endregion
    }
}