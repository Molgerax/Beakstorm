using UnityEngine;
using UnityEngine.Rendering;
using DynaMak.Utility;

namespace DynaMak.Volumes.Voxelizer
{
    [System.Serializable]
    public class SceneVoxelizer : VolumeComponent
    {
        #region Defines
        private const int ThreadGroupSize = 2;

        private const string ShaderName = "DynaMak/SceneVoxelizer256";
        private const string ComputeShaderPath = "Voxelizer/SceneVoxelizer";
        #endregion

        #region Serialize Fields

        [Header("Voxelizer Settings")] 
        [SerializeField] private bool _enableVoxelizer;

        [SerializeField] private Vector3Int _resolution = Vector3Int.one * 16;
        [SerializeField] private LayerMask _voxelizeLayerMask;

        [Header("References")] 
        [SerializeField] private Shader _voxelizeShader;
        [SerializeField] private ComputeShader _computeShader;

        #endregion

        // ---------------------

        #region Private Fields

        public VolumeTexture VoxelTexture;
        private RenderTexture SliceMap;

        private RenderTexture dummyRenderTarget;
        private Camera voxelCamera;


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
            InitializeCamera();
        }

        private void OnRenderObject()
        {
            if (_enableVoxelizer)
            {
                // Locks Voxelizer in same angle
                if (transform.parent != null)
                {
                    transform.localRotation = Quaternion.Euler(-transform.parent.rotation.eulerAngles + Vector3.right * 90);
                }
                else
                {
                    transform.localRotation = Quaternion.Euler(Vector3.right * 90);
                }

                AdjustCamera();
                
                Voxelize();
            }
        }

        private void OnDisable()
        {
            ReleaseBuffers();
        }

        private void Reset()
        {
            _computeShader = Resources.Load<ComputeShader>(ComputeShaderPath);
            _voxelizeShader = Shader.Find(ShaderName);
        }

        #endregion

        // ----------------------

        #region Public Methods

        public void InitializeBuffers()
        {
            ReleaseBuffers();

            VoxelTexture = new VolumeTexture(RenderTextureFormat.R8, _resolution, transform.position, transform.localScale);
            VoxelTexture.Initialize();

            SliceMap = new RenderTexture(_resolution.x * 4, _resolution.z * 2, 0, RenderTextureFormat.RInt,
                RenderTextureReadWrite.Linear);
            SliceMap.dimension = TextureDimension.Tex2D;
            SliceMap.enableRandomWrite = true;
            SliceMap.filterMode = FilterMode.Point;
            SliceMap.Create();

            dummyRenderTarget = new RenderTexture(_resolution.x, _resolution.z, 0, RenderTextureFormat.R8);
            dummyRenderTarget.Create();
        }

        public void Voxelize()
        {
            DispatchClear();

            Graphics.SetRandomWriteTarget(1, SliceMap);

            voxelCamera.targetTexture = dummyRenderTarget;
            Shader.SetGlobalVector(voxelizerResolutionID, new Vector4(_resolution.x, _resolution.y, _resolution.z));
            voxelCamera.RenderWithShader(_voxelizeShader, "");
            Graphics.ClearRandomWriteTargets();

            DispatchFill();
        }

        public void AdjustCamera()
        {
            float ratio = VoxelBounds.x / VoxelBounds.z;
            voxelCamera.aspect = ratio;
            voxelCamera.orthographicSize = VoxelBounds.z * 0.5f;

            voxelCamera.nearClipPlane = -VoxelBounds.y * 0.5f;
            voxelCamera.farClipPlane = VoxelBounds.y * 0.5f;

            voxelCamera.cullingMask = _voxelizeLayerMask;
            
            VoxelTexture.SetTransforms(transform.position, VoxelBounds);
        }

        public void ReleaseBuffers()
        {
            if (VoxelTexture != null) VoxelTexture.Release();
            if (SliceMap) SliceMap.Release();
            if (dummyRenderTarget) dummyRenderTarget.Release();
        }

        #endregion


        #region Private Methods

        void InitializeCamera()
        {
            voxelCamera = GetComponent<Camera>();

            if (voxelCamera == null) voxelCamera = gameObject.AddComponent<Camera>();

            voxelCamera.enabled = false;
            
            voxelCamera.orthographic = true;
            voxelCamera.renderingPath = RenderingPath.Forward;
            voxelCamera.usePhysicalProperties = false;
            voxelCamera.allowMSAA = false;
            voxelCamera.allowHDR = false;
            voxelCamera.useOcclusionCulling = false;
            voxelCamera.allowDynamicResolution = false;
            voxelCamera.clearFlags = CameraClearFlags.Color;
            
            AdjustCamera();
            
            voxelCamera.targetTexture = dummyRenderTarget;
            
            voxelCamera.hideFlags = HideFlags.HideInInspector;
        }

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