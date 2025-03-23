using UnityEngine;
using UnityEngine.Rendering;

namespace DynaMak.Volumes.MarchingCubes
{
    public class MarchingCubesComponent : MonoBehaviour
    {
        #region Serialize Fields
        
        [Header("Marching Settings")]
        [SerializeField] [Range(-5f, 5f)] private float surfaceLevel = 0f;
        [SerializeField] private bool fillEdge = false;
        [SerializeField] private bool invert = true;

        [Header("References")] 
        [SerializeField] private VolumeComponent volumeComponent;
        [SerializeField] private Material material;
        [SerializeField] private ComputeShader computeShader;

        #endregion

        // ------------------
        
        #region Private Fields

        private const string k_ComputeShaderPath = "MarchingCubes/MarchingCubes";
        
        private MarchingCubes _marchingCubes;
        private bool _isVolumeInitialized;
        
        #endregion

        #region Mono Methods

        private void Start()
        {
            computeShader = Instantiate(computeShader);
            InitializeVolumeTest();
        }

        void Update()
        {
            if (_isVolumeInitialized)
            {
                    _marchingCubes.MarchTexture(volumeComponent.GetVolumeTexture(), surfaceLevel, fillEdge, invert);
                    _marchingCubes.DrawCubesProcedural(material);
            }
            else
            {
                InitializeVolumeTest();
            }
        }

        private void OnDestroy()
        {
            _marchingCubes?.ReleaseBuffers();
        }

        private void Reset()
        {
            computeShader = Resources.Load<ComputeShader>(k_ComputeShaderPath);
            volumeComponent = GetComponent<VolumeComponent>();
        }
        
        #endregion

        #region Private Methods

        private void InitializeVolumeTest()
        {
            if (!_isVolumeInitialized)
            {
                if (volumeComponent.GetVolumeTexture().IsInitialized)
                {
                    SetTextureFormatKeyword();
                    
                    _marchingCubes = new MarchingCubes(computeShader, surfaceLevel, volumeComponent.GetVolumeTexture().Resolution, volumeComponent.GetVolumeTexture().Center,
                        volumeComponent.GetVolumeTexture().Bounds, fillEdge, invert);
                    _isVolumeInitialized = true;
                }
                
            }

        }

        private void SetTextureFormatKeyword()
        {
             bool useHalf4 = volumeComponent.GetVolumeTexture().Texture.format == RenderTextureFormat.ARGBHalf;
             
             LocalKeyword textureFormatKeyword = computeShader.keywordSpace.FindKeyword("HALF4");
             
             if (textureFormatKeyword.isValid)
             {
                 computeShader.SetKeyword(textureFormatKeyword, useHalf4);
             }
        }

        #endregion
    }
}