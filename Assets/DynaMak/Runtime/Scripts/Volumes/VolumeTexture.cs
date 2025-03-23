using UnityEngine;
using UnityEngine.Rendering;

namespace DynaMak.Volumes
{
    [System.Serializable]
    public class VolumeTexture
    {
        #region Initialize Fields

        protected readonly RenderTextureFormat _renderTextureFormat;
        protected readonly FilterMode _filterMode;
        protected Vector3Int _volumeResolution;
        protected Vector3 _volumeCenter;
        protected Vector3 _volumeBounds;

        protected int[] _resolutionInts = new int[3];
        protected bool _initialized = false;

        #endregion

        #region Public Fields

        public RenderTexture Texture;

        public Vector3 Center { get => _volumeCenter; }
        public Vector3 Bounds { get => _volumeBounds; }
        public Vector3Int Resolution { get => _volumeResolution; }
        public bool IsInitialized => _initialized;
        

        public int[] ResolutionArray
        {
            get
            {
                _resolutionInts[0] = _volumeResolution.x;
                _resolutionInts[1] = _volumeResolution.y;
                _resolutionInts[2] = _volumeResolution.z;
                return _resolutionInts;
            }
        }

        #endregion
        
        // -----------------

        #region Constructor

        public VolumeTexture(RenderTextureFormat renderTextureFormat, Vector3Int resolution, Vector3 center, Vector3 bounds, FilterMode filterMode = FilterMode.Bilinear)
        {
            _renderTextureFormat = renderTextureFormat;
            _filterMode = filterMode;
            _volumeResolution = resolution;
            _volumeCenter = center;
            _volumeBounds = bounds;
        }

        #endregion

        #region Public Functions

        public virtual void Initialize()
        {
            Release();

            Texture = new RenderTexture(_volumeResolution.x, _volumeResolution.y, 0, _renderTextureFormat,
                RenderTextureReadWrite.Linear);
            Texture.dimension = TextureDimension.Tex3D;
            Texture.volumeDepth = _volumeResolution.z;
            Texture.enableRandomWrite = true;
            Texture.filterMode = _filterMode;
            Texture.Create();

            _initialized = true;
        }

        public virtual void SetTransforms(Vector3 center, Vector3 bounds)
        {
            _volumeCenter = center;
            _volumeBounds = bounds;
        }
        
        public virtual void SetCenter(Vector3 center)
        {
            _volumeCenter = center;
        }

        public virtual void Release()
        {
            if (Texture) Texture.Release();
        }

        #endregion
    }
}
