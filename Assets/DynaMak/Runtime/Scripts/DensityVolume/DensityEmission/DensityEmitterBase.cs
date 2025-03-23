using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes
{
    [System.Serializable]
    public abstract class DensityEmitterBase : MonoBehaviour
    {
        #region Defines
        protected const int ThreadGroupSize = 2;
        #endregion

        #region SerializeFields

        [Header("Emission Settings")] 
        [SerializeField] protected bool _enabled = false;
        [SerializeField] [Range(0f, 5f)] protected float _strength = 1f;
        [SerializeField] [Range(0f, 10f)] protected float _speed = 1f;
        [SerializeField] protected float _maxDensity = 1f;

        [Header("References")] 
        [SerializeField] private VolumeComponent _targetVolumeComponent;
        [SerializeField] protected ComputeShader _computeShader;
        #endregion
        
        #region Protected Fields

        protected Vector3 _currentPosition;
        protected Vector3 _oldPosition;

        #endregion
    
        #region Shader Property IDs
        
        protected int volumeID = Shader.PropertyToID("_Volume");
        protected int volumeResolutionID = Shader.PropertyToID("_VolumeResolution");
    
        protected int volumeCenterID = Shader.PropertyToID("_VolumeCenter");
        protected int volumeBoundsID = Shader.PropertyToID("_VolumeBounds");
    
        protected int dtID = Shader.PropertyToID("_dt");
    
        protected int emissionStrengthID = Shader.PropertyToID("_EmissionStrength");
        protected int emissionSpeedID = Shader.PropertyToID("_EmissionSpeed");
        protected int emissionMaxDensityID = Shader.PropertyToID("_EmissionMaxDensity");
    
        protected int worldPosID = Shader.PropertyToID("_WorldPos");
        protected int worldPosOldID = Shader.PropertyToID("_WorldPosOld");
    
        #endregion
        
        // -------------------
    
        #region Mono Methods
    
        protected virtual void Awake()
        {
            _currentPosition = transform.position;
            _oldPosition = _currentPosition;
        }

        protected virtual void Update()
        {
            UpdateTransforms();
    
            if (_enabled)
            {
                DispatchEmit();
            }
        }
    
        #endregion
        
        
        
        #region Public Methods
    
        [ContextMenu("Emit")]
        public virtual void DispatchEmit()
        {
            SetOtherComputeValues();
            SetDefaultComputeValues();
            _computeShader.Dispatch(0, _targetVolumeComponent.GetVolumeTexture().Resolution, ThreadGroupSize);
        }
        
        #endregion

        public void SetTargetVolume(VolumeComponent volumeComponent)
        {
            _targetVolumeComponent = volumeComponent;
        }
        
        #region Private Methods
    
        protected virtual void SetOtherComputeValues()
        {
            //To be overridden
        }
    
        protected virtual void UpdateTransforms()
        {
            _oldPosition = _currentPosition;
            _currentPosition = transform.position;
        }
    
        void SetDefaultComputeValues()
        {
            //Set Density Volume
            _computeShader.SetVolume(0, _targetVolumeComponent.GetVolumeTexture(), 
                volumeID, volumeCenterID, volumeBoundsID, volumeResolutionID);
    
            //Add Defaults
            _computeShader.SetFloat(dtID, Time.deltaTime);
            _computeShader.SetFloat(emissionStrengthID, _strength);
            _computeShader.SetFloat(emissionSpeedID, _speed);
            _computeShader.SetFloat(emissionMaxDensityID, _maxDensity);
            
            _computeShader.SetVector(worldPosID, _currentPosition);
            _computeShader.SetVector(worldPosOldID, _oldPosition);
        }
    
        #endregion
    }
}
