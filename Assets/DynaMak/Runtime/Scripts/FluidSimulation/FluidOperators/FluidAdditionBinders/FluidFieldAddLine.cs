using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class FluidFieldAddLine : FluidFieldAddBase
    {
        #region Serialize Fields

        [Header("Addition Settings")] 
        [SerializeField] private Transform target;
        [SerializeField] private float length = 1f;
        [SerializeField] private float radius = 1f;
        [SerializeField] [Range(0f, 3f)] private float density = 0f;
        [SerializeField] private float strength = 1f;
        [SerializeField] private bool useCurl;
        
        public float Strength => strength;
        public float Radius => radius;

        public Transform Target => target != null ? target : transform;
        public Vector3 PointA => Target.position;
        public Vector3 PointB => PointA + Target.forward * length;
        #endregion

        #region Shader Property IDs
        private int addPositionAID = Shader.PropertyToID("_AddPositionA");
        private int addPositionBID = Shader.PropertyToID("_AddPositionB");
        private int addRadiusID = Shader.PropertyToID("_AddRadius");
        private int addStrengthID = Shader.PropertyToID("_AddStrength");
        private int addDensityID = Shader.PropertyToID("_AddDensity");
        private int useCurlID = Shader.PropertyToID("_UseCurl");
        #endregion

        
        
        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddLine";
        
        protected override void Initialize()
        {
            base.Initialize();
            
            if(target == null) target = transform;
        }

        protected override void SetProperties()
        {
            base.SetProperties();
            
            _computeShader.SetVector(addPositionAID, PointA);
            _computeShader.SetVector(addPositionBID, PointB);
            _computeShader.SetFloat(addRadiusID, radius);
            _computeShader.SetFloat(addDensityID, density);
            _computeShader.SetFloat(addStrengthID, strength);
            _computeShader.SetFloat(useCurlID, useCurl ? 1 : 0);
        }

        #endregion
    }
}