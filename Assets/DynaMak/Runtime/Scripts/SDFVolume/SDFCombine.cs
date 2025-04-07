using System;
using UnityEngine;
using UnityEngine.Rendering;
using DynaMak.Utility;

namespace DynaMak.Volumes.SDF
{
    [System.Serializable]
    public class SDFCombine : VolumeComponent
    {
        #region Defines
        private const int ThreadGroupSize = 2;
        
        private const string ComputeShaderPath = "SDFVolume/SDFCombine";
        #endregion

        #region Serialize Fields

        [Header("SDF Operator Settings")] 
        [SerializeField] private VolumeComponent firstInputVolume;
        [SerializeField] private VolumeComponent secondInputVolume;
        [SerializeField] private SDFOperation operation = SDFOperation.Union;
        [SerializeField, Range(0f, 10f)] private float smoothness = 0f;

        [Header("References")] 
        [SerializeField] private ComputeShader computeShader;

        #endregion

        // ---------------------

        #region Private Fields

        public VolumeTexture ResultTexture;


        public enum SDFOperation
        {
            Union = 0, Subtraction = 1, Intersection = 2,
        }

        private bool _isInitialized;
        
        #endregion
        
        
        #region Override Functions
        public override VolumeTexture GetVolumeTexture()
        { 
            return ResultTexture;
        }
        
        public override Vector3 VolumeCenter 
        {
            get
            {
                if(GetVolumeTexture().IsInitialized) return base.VolumeCenter;
                return firstInputVolume != null ? firstInputVolume.VolumeCenter : Vector3.zero;
            }
        }
        public override Vector3 VolumeBounds 
        {
            get
            {
                if(GetVolumeTexture().IsInitialized) return base.VolumeBounds;
                return firstInputVolume != null ? firstInputVolume.VolumeBounds : Vector3.zero;
            }
        }
        public override Vector3Int VolumeResolution 
        {
            get
            {
                if(GetVolumeTexture().IsInitialized) return base.VolumeResolution;
                return firstInputVolume != null ? firstInputVolume.VolumeResolution : Vector3Int.zero;
            }
        }
        #endregion
        

        #region Shader Property IDs

        private int resultVolumeID =        Shader.PropertyToID("_ResultVolume");
        private int resultCenterID =        Shader.PropertyToID("_ResultCenter");
        private int resultBoundsID =        Shader.PropertyToID("_ResultBounds");
        private int resultResolutionID =    Shader.PropertyToID("_ResultResolution");
        
        private int firstVolumeID = Shader.PropertyToID("_FirstVolume");
        private int firstCenterID = Shader.PropertyToID("_FirstCenter");
        private int firstBoundsID = Shader.PropertyToID("_FirstBounds");
        private int firstResolutionID = Shader.PropertyToID("_FirstResolution");
        
        private int secondVolumeID =      Shader.PropertyToID("_SecondVolume");
        private int secondCenterID =      Shader.PropertyToID("_SecondCenter");
        private int secondBoundsID =      Shader.PropertyToID("_SecondBounds");
        private int secondResolutionID =  Shader.PropertyToID("_SecondResolution");

        
        private int smoothnessID =      Shader.PropertyToID("_Smoothness");

        private int _normalsKernel;
        

        #endregion

        // ----------------------

        #region Mono Methods

        private void Start()
        { 
            InitializeBuffers();
        }

        private void Update()
        {
            TryInitialize();
            
            if(!_isInitialized) return;
            
            if(!firstInputVolume || ! secondInputVolume) return;
            if(!firstInputVolume.GetVolumeTexture().IsInitialized || !secondInputVolume.GetVolumeTexture().IsInitialized) return;

            ResultTexture.SetTransforms(firstInputVolume.VolumeCenter, firstInputVolume.VolumeBounds);
            
            PerformOperation((int)operation );
            CalculateNormals();
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        private void Reset()
        {
            computeShader = Resources.Load<ComputeShader>(ComputeShaderPath);
        }

        #endregion

        // ----------------------

        #region Public Methods

        public void TryInitialize()
        {
            if(!firstInputVolume.GetVolumeTexture().IsInitialized || _isInitialized) return;
            _isInitialized = true;
            
            InitializeBuffers();
        }
        
        public void InitializeBuffers()
        {
            ReleaseBuffers();

            _normalsKernel = computeShader.FindKernel("CalculateNormals");

            Vector3Int resolution = firstInputVolume.VolumeResolution;
            Vector3 center = firstInputVolume.VolumeCenter;
            Vector3 bounds = firstInputVolume.VolumeBounds;

            ResultTexture = new VolumeTexture(RenderTextureFormat.ARGBHalf, resolution, center, bounds);
            ResultTexture.Initialize();
            
            CopyInput();
        }

        public void ReleaseBuffers()
        {
            if (ResultTexture != null) ResultTexture.Release();
        }

        #endregion


        #region Private Methods

        private void PerformOperation(int kernelIndex)
        {
            computeShader.SetVolume(kernelIndex, ResultTexture, resultVolumeID, resultCenterID, resultBoundsID, resultResolutionID);
            computeShader.SetVolume(kernelIndex, firstInputVolume.GetVolumeTexture(), firstVolumeID, firstCenterID, firstBoundsID, firstResolutionID);
            computeShader.SetVolume(kernelIndex, secondInputVolume.GetVolumeTexture(), secondVolumeID, secondCenterID, secondBoundsID, secondResolutionID);
            computeShader.SetFloat(smoothnessID, smoothness);
            computeShader.Dispatch(kernelIndex, VolumeResolution, ThreadGroupSize);
        }
        
        /// <summary>
        /// Fills the SDF with a large positive distance, as fallback in case the voxel texture isn't supplied in time. 
        /// </summary>
        private void CopyInput()
        { 
            if( !firstInputVolume || !firstInputVolume.GetVolumeTexture().IsInitialized ) return;
            
            int copyKernel = computeShader.FindKernel("Copy");
            computeShader.SetTexture(copyKernel, resultVolumeID, ResultTexture.Texture);
            computeShader.SetTexture(copyKernel, firstVolumeID, firstInputVolume.GetVolumeTexture().Texture);
            computeShader.Dispatch(copyKernel,VolumeResolution, ThreadGroupSize);
        }

        private void CalculateNormals()
        {
            computeShader.SetVolume(_normalsKernel, ResultTexture, resultVolumeID, resultCenterID, resultBoundsID, resultResolutionID);
            computeShader.Dispatch(_normalsKernel, VolumeResolution, ThreadGroupSize);
        }

        #endregion
    }
}