using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace DynaMak.Utility
{
    [RequireComponent(typeof(Camera))]
    public class LowerResolutionCamera : MonoBehaviour
    {
        [SerializeField] private Resolution resolution;
        [SerializeField] private LayerMask layerMask;

        [SerializeField] private Material blitMaterial;

        [SerializeField] private RenderTexture lowerRenderTexture;
        private Camera _lowerCamera;

        private Camera _originalCamera;

        private Vector2Int _pixelResolution;

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Release();
        }


        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            _lowerCamera.Render();
            Graphics.Blit(lowerRenderTexture, src, blitMaterial);

            Graphics.Blit(src, dest);
        }

        private void Initialize()
        {
            Release();

            _originalCamera = GetComponent<Camera>();

            var cullingMask = _originalCamera.cullingMask;
            cullingMask = cullingMask & ~layerMask;
            _originalCamera.cullingMask = cullingMask;

            var targetTexture = _originalCamera.activeTexture;
            if (targetTexture == null) targetTexture = RenderTexture.active;

            if (targetTexture != null)
                _pixelResolution = new Vector2Int(targetTexture.width / (int) resolution,
                    targetTexture.height / (int) resolution);
            else
                _pixelResolution = new Vector2Int(_originalCamera.pixelWidth / (int) resolution,
                    _originalCamera.pixelHeight / (int) resolution);

            lowerRenderTexture = new RenderTexture(_pixelResolution.x, _pixelResolution.y,
                GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D24_UNorm_S8_UInt);
            lowerRenderTexture.enableRandomWrite = true;
            lowerRenderTexture.Create();

            CreateCamera();
        }

        private void CreateCamera()
        {
            if (_lowerCamera)
                return;

            var lowerCameraGameObject = new GameObject("LowResCamera");
            lowerCameraGameObject.transform.parent = transform;
            lowerCameraGameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _lowerCamera = lowerCameraGameObject.AddComponent<Camera>();

            _lowerCamera.CopyFrom(_originalCamera);
            _lowerCamera.targetTexture = lowerRenderTexture;
            _lowerCamera.enabled = false;

            _lowerCamera.backgroundColor = new Color(0f, 0f, 0f, 1f);
            _lowerCamera.depthTextureMode = DepthTextureMode.None;
            _lowerCamera.cullingMask = layerMask;
            _lowerCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void Release()
        {
            if (lowerRenderTexture)
            {
                lowerRenderTexture.Release();
                lowerRenderTexture = null;
            }
        }

        private enum Resolution
        {
            Full = 1,
            Half = 2,
            Quarter = 4,
            Eighth = 8
        }
    }
}