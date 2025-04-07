using System;
using UnityEngine;
using UnityEngine.Rendering;
using DynaMak.Utility;

namespace DynaMak.Volumes.Voxelizer
{
    [System.Serializable]
    public class VoxelToSDF : VolumeComponent
    {
        #region Defines
        private const int ThreadGroupSize = 2;
        
        private const string ComputeShaderPath = "Voxelizer/VoxelToSDF";
        #endregion

        #region Serialize Fields

        [Header("SDF Baker Settings")] 
        [SerializeField] private bool enableSDFBaker;
        [SerializeField] private bool bakeOnceOnStart;
        [SerializeField] private VolumeComponent voxelVolume;
        [SerializeField] private bool invertSDF;

        [Header("References")] 
        [SerializeField] private ComputeShader computeShader;

        #endregion

        // ---------------------

        #region Private Fields

        public VolumeTexture SDFTexture;
        private VolumeTexture _voxelTexture;

        private bool _hasBaked = false;
        private int _bakeDelay = 3;

        private int[] _resolutionArray = new int[3];
        
        #endregion
        
        
        #region Override Functions
        public override VolumeTexture GetVolumeTexture()
        { 
            return SDFTexture;
        }
        
        public override Vector3 VolumeCenter 
        {
            get
            {
                if(GetVolumeTexture().IsInitialized) return base.VolumeCenter;
                return voxelVolume != null ? voxelVolume.VolumeCenter : Vector3.zero;
            }
        }
        public override Vector3 VolumeBounds 
        {
            get
            {
                if(GetVolumeTexture().IsInitialized) return base.VolumeBounds;
                return voxelVolume != null ? voxelVolume.VolumeBounds : Vector3.zero;
            }
        }
        public override Vector3Int VolumeResolution 
        {
            get
            {
                if(GetVolumeTexture().IsInitialized) return base.VolumeResolution;
                return voxelVolume != null ? voxelVolume.VolumeResolution : Vector3Int.zero;
            }
        }
        #endregion
        

        #region Shader Property IDs

        private int voxelizerResolutionID = Shader.PropertyToID("_VoxelizerResolution");
        private int voxelTextureID = Shader.PropertyToID("_VoxelTexture");
        private int slicemapID = Shader.PropertyToID("_SliceMap");


        #endregion

        // ----------------------

        #region Mono Methods

        private void Start()
        { 
            InitializeBuffers();
        }

        private void Update()
        {
            if(!_voxelTexture.IsInitialized) return;
            if(bakeOnceOnStart && _hasBaked) return;
            
            if (enableSDFBaker)
            {
                SDFTexture.SetTransforms(_voxelTexture.Center, _voxelTexture.Bounds);
                
                
                BitmapTextureToSDF(_voxelTexture.Texture, SDFTexture.Texture, computeShader, _voxelTexture.Resolution);


                if (!_hasBaked)
                {
                    _bakeDelay--;
                    _hasBaked = _bakeDelay <= 0;
                }
            }
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

        public void InitializeBuffers()
        {
            ReleaseBuffers();

            _voxelTexture = voxelVolume.GetVolumeTexture();

            Vector3Int resolution = voxelVolume.GetVolumeTexture().Resolution;
            Vector3 center = voxelVolume.GetVolumeTexture().Center;
            Vector3 bounds = voxelVolume.GetVolumeTexture().Bounds;

            SDFTexture = new VolumeTexture(RenderTextureFormat.ARGBHalf, resolution, center, bounds);
            SDFTexture.Initialize();
            
            FillEmpty(SDFTexture.Texture, resolution);
        }

        /// <summary>
        /// Fills the SDF with a large positive distance, as fallback in case the voxel texture isn't supplied in time. 
        /// </summary>
        private void FillEmpty(RenderTexture sdfTexture, Vector3Int resolution)
        {
            int emptyKernel = computeShader.FindKernel("Empty");
            computeShader.SetTexture(emptyKernel, "_SDFVolume_Write", sdfTexture);
            computeShader.Dispatch(emptyKernel,resolution, ThreadGroupSize);
        }

        /// <summary>
        /// Generates a signed distance field of a bitmap texture.
        /// </summary>
        /// <param name="src">Bitmap texture input</param>
        /// <param name="dest">Output texture to render to</param>
        public void BitmapTextureToSDF(RenderTexture src, RenderTexture dest, ComputeShader computeShader, Vector3Int resolution)
        {
            int initKernel = computeShader.FindKernel("Initialize");
            int iterateKernel = computeShader.FindKernel("Iterate");
            int combineKernel = computeShader.FindKernel("CombineInOut");
            int swapKernel = computeShader.FindKernel("Swap");
            int normalsKernel = computeShader.FindKernel("CalculateNormals");
            
            int highestSide = Math.Max(resolution.x, Math.Max(resolution.y, resolution.z));
            int maxIterations = Mathf.CeilToInt(Mathf.Log(highestSide, 2) + 1);

            //maxIterations = iterations;

            RenderTexture temp = RenderTexture.GetTemporary(dest.descriptor);
            RenderTexture innerField = RenderTexture.GetTemporary(dest.descriptor);

            
            computeShader.SetVector("_VoxelCenter", _voxelTexture.Center);
            computeShader.SetVector("_VoxelBounds", _voxelTexture.Bounds);
            

            computeShader.SetInt("_MaxIterations", maxIterations);
            // Flip Sign is used for calculating the outer / inner field respectively
            computeShader.SetInt("_FlipSign", 0);
            
            computeShader.SetInt("_Invert", invertSDF ? 1 : 0);

            // First pass: Marks all filled pixels as seeds
            
            computeShader.SetTexture(initKernel, "_VoxelVolume", src);
            computeShader.SetTexture(initKernel, "_SDFVolume_Write", dest);
            computeShader.SetInts("_VoxelResolution", resolution.ToArray());
            computeShader.Dispatch(initKernel,resolution, ThreadGroupSize);
            
            // Second Pass: Do the Jump Flood Algorithm on the outer field
            for (int i = 0; i < maxIterations; i++)
            {
                computeShader.SetInt("_Iteration", i);
                
                computeShader.SetTexture(iterateKernel, "_SDFVolume_Read", dest);
                computeShader.SetTexture(iterateKernel, "_SDFVolume_Write", temp);
                computeShader.Dispatch(iterateKernel,resolution, ThreadGroupSize);
                
                
                // Swap back
                computeShader.SetTexture(swapKernel, "_SDFVolume_Read", temp);
                computeShader.SetTexture(swapKernel, "_SDFVolume_Write", dest);
                computeShader.Dispatch(swapKernel,resolution, ThreadGroupSize);

            }
            
            
            
            // Second Pass: Do the Jump Flood Algorithm, now on the inner field
            computeShader.SetInt("_FlipSign", 1);
            computeShader.SetTexture(initKernel, "_VoxelVolume", src);
            computeShader.SetTexture(initKernel, "_SDFVolume_Write", innerField);
            computeShader.SetInts("_VoxelResolution", resolution.ToArray());
            computeShader.Dispatch(initKernel,resolution, ThreadGroupSize);
            
            for (int i = 0; i < maxIterations; i++)
            {
                computeShader.SetInt("_Iteration", i);
                
                computeShader.SetTexture(iterateKernel, "_SDFVolume_Read", innerField);
                computeShader.SetTexture(iterateKernel, "_SDFVolume_Write", temp);
                computeShader.Dispatch(iterateKernel,resolution, ThreadGroupSize);
                
                
                // Swap back
                computeShader.SetTexture(swapKernel, "_SDFVolume_Read", temp);
                computeShader.SetTexture(swapKernel, "_SDFVolume_Write", innerField);
                computeShader.Dispatch(swapKernel,resolution, ThreadGroupSize);
            }



            // Third Pass: Calculate the distances from the JFA pass, subtract inner from outer field and normalize
            computeShader.SetTexture(combineKernel, "_SDFVolume_Read", dest);
            computeShader.SetTexture(combineKernel, "_SDFVolume_Write", temp);
            computeShader.SetTexture(combineKernel, "_SDFVolume_InnerField", innerField);
            computeShader.Dispatch(combineKernel,resolution, ThreadGroupSize);
            
            // Swap back
            computeShader.SetTexture(swapKernel, "_SDFVolume_Read", temp);
            computeShader.SetTexture(swapKernel, "_SDFVolume_Write", dest);
            computeShader.Dispatch(swapKernel,resolution, ThreadGroupSize);
            
            // Calculate normals
            computeShader.SetTexture(normalsKernel, "_SDFVolume_Write", dest);
            computeShader.Dispatch(normalsKernel,resolution, ThreadGroupSize);
            
            // Release temporary textures
            RenderTexture.ReleaseTemporary(temp);
            RenderTexture.ReleaseTemporary(innerField);
        }

        public void ReleaseBuffers()
        {
            if (SDFTexture != null) SDFTexture.Release();
        }

        #endregion


        #region Private Methods
        
        

        #endregion
    }
}