using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DynaMak.Utility;

namespace DynaMak.Volumes.Voxelizer
{
    [System.Serializable]
    public class MeshVoxelizer : VolumeComponent
    {
        #region Defines
        private const int ThreadGroupSize = 2;
        #endregion

        #region Serialize Fields

        [Header("Voxelizer Settings")] 
        [SerializeField] private bool _enableVoxelizer;

        [SerializeField] private Vector3Int _resolution = Vector3Int.one * 16;

        [SerializeField] private Renderer[] meshRenderers;

        [Header("References")] 
        [SerializeField] private Shader _voxelizeShader;
        [SerializeField] private ComputeShader _computeShader;

        #endregion

        // ---------------------

        #region Private Fields

        public VolumeTexture VoxelTexture;
        private RenderTexture SliceMap;

        private CommandBuffer _cmdBuffer;
        private RenderTexture _dummyRenderTarget;

        private Material _voxelMaterial;

        private List<Material> _cachedMaterials = new List<Material>(8);

        private const string k_ComputeShaderPath = "Voxelizer/SceneVoxelizer";
        private const string k_ShaderName = "DynaMak/SceneVoxelizer256";

        public Vector3 VoxelCenter
        {
            get { return transform.position; }
        }

        public Vector3 VoxelBounds
        {
            get { return transform.localScale; }
        }

        #endregion
        
        
        #region Override Functions
        public override VolumeTexture GetVolumeTexture()
        { 
            return VoxelTexture;
        }
        
        public override Vector3 VolumeCenter => GetVolumeTexture().IsInitialized ? base.VolumeCenter : transform.position;
        public override Vector3 VolumeBounds => GetVolumeTexture().IsInitialized ? base.VolumeBounds : transform.localScale;
        public override Vector3Int VolumeResolution => GetVolumeTexture().IsInitialized ? base.VolumeResolution : _resolution;
        #endregion
        

        #region Shader Property IDs

        private int voxelizerResolutionID = Shader.PropertyToID("_VoxelizerResolution");
        private int voxelTextureID = Shader.PropertyToID("_VoxelTexture");
        private int slicemapID = Shader.PropertyToID("_SliceMap");


        #endregion

        // ----------------------

        #region Mono Methods

        private void Awake()
        { 
            InitializeBuffers();
        }

        private void Update()
        {
            if (_enableVoxelizer)
            {
                VoxelTexture.SetTransforms(VoxelCenter, VoxelBounds);
                Voxelize(meshRenderers);
            }
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        private void Reset()
        {
            _computeShader = Resources.Load<ComputeShader>(k_ComputeShaderPath);
            _voxelizeShader = Shader.Find(k_ShaderName);

            meshRenderers = GetComponentsInChildren<Renderer>();
        }

        #endregion

        // ----------------------

        #region Public Methods

        public void InitializeBuffers()
        {
            ReleaseBuffers();

            VoxelTexture = new VolumeTexture(RenderTextureFormat.RHalf, _resolution, transform.position, transform.localScale);
            VoxelTexture.Initialize();

            SliceMap = new RenderTexture(_resolution.x * 4, _resolution.z * 2, 0, RenderTextureFormat.RInt,
                RenderTextureReadWrite.Linear);
            SliceMap.dimension = TextureDimension.Tex2D;
            SliceMap.enableRandomWrite = true;
            SliceMap.filterMode = FilterMode.Point;
            SliceMap.Create();

            _dummyRenderTarget = new RenderTexture(_resolution.x, _resolution.z, 0, RenderTextureFormat.R8);
            _dummyRenderTarget.Create();


            _cmdBuffer = new CommandBuffer();
            
            if(_voxelMaterial is null) _voxelMaterial = new Material(_voxelizeShader);
        }

        public void Voxelize(Renderer[] renderers)
        {
            DispatchClear();
            
            _cmdBuffer.SetRandomWriteTarget(1, SliceMap);
            _cmdBuffer.SetRenderTarget( _dummyRenderTarget);
            
            _cmdBuffer.SetGlobalVector(voxelizerResolutionID, new Vector4(_resolution.x, _resolution.y, _resolution.z));
            
            SetCommandBufferMatrix(_cmdBuffer, VoxelCenter, VoxelBounds);

            for (int i = 0; i < renderers.Length; i++)
            {
                if(renderers[i] is null) continue;

                int subMeshCount = 1;
                if (renderers[i] is SkinnedMeshRenderer smr) subMeshCount = smr.sharedMesh.subMeshCount;
                else
                {
                    renderers[i].GetSharedMaterials(_cachedMaterials);
                    subMeshCount = _cachedMaterials.Count;
                }

                for (int j = 0; j < subMeshCount; j++)
                {
                    _cmdBuffer.DrawRenderer(renderers[i], _voxelMaterial, j);
                }
            }
            
            
            
            _cmdBuffer.ClearRandomWriteTargets();
            
            Graphics.ExecuteCommandBuffer(_cmdBuffer);
            _cmdBuffer.Clear();
            
            DispatchFill();
        }

        public static void SetCommandBufferMatrix(CommandBuffer cmdBuffer, Vector3 center, Vector3 boundsExtents)
        {
            Matrix4x4 proj = Matrix4x4.identity;
            Matrix4x4 view = Matrix4x4.identity;
            
            Bounds voxelBounds = new Bounds(Vector3.zero, boundsExtents);
            
            proj = Matrix4x4.Ortho(voxelBounds.min.x,voxelBounds.max.x,
                voxelBounds.min.z,voxelBounds.max.z,
                voxelBounds.max.y,voxelBounds.min.y);

            //proj.SetColumn(2, proj.GetColumn(2) * -1);
            
            view = Matrix4x4.TRS(center, Quaternion.LookRotation(Vector3.down, Vector3.forward), Vector3.one);
            view = view.inverse;
            
            cmdBuffer.SetViewProjectionMatrices(view, proj);
        }
        

        public void ReleaseBuffers()
        {
            if (VoxelTexture != null) VoxelTexture.Release();
            if (SliceMap) SliceMap.Release();
            if (_dummyRenderTarget) _dummyRenderTarget.Release();
            if (_cmdBuffer is not null) _cmdBuffer.Release();
        }

        #endregion


        #region Private Methods
        

        void DispatchClear()
        {
            _computeShader.SetTexture(0, voxelTextureID, VoxelTexture.Texture);
            _computeShader.SetTexture(0, slicemapID, SliceMap);
            _computeShader.Dispatch(0, _resolution, ThreadGroupSize);
        }

        void DispatchFill()
        {
            _computeShader.SetTexture(1, voxelTextureID, VoxelTexture.Texture);
            _computeShader.SetTexture(1, slicemapID, SliceMap);
            _computeShader.Dispatch(1, _resolution, ThreadGroupSize);
        }

        #endregion
    }
}