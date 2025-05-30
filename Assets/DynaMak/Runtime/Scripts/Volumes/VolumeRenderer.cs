using System;
using UnityEngine;
using DynaMak.Utility;
using UnityEngine.Rendering;

namespace DynaMak.Volumes
{
    [System.Serializable]
    public class VolumeRenderer : MonoBehaviour
    {
        #region Serialize Fields

        [Header("Ray-Marching Settings")] 
        [SerializeField] [Range(1, 30)] private int maxSteps = 15;
        [SerializeField] [Range(1, 10)] private int maxStepsLight = 5;
        [SerializeField] [Range(0, 1)] private float randomSampleOffset = 0;

        [Header("Renderer Settings")] 
        [SerializeField] private VolumeComponent volumeComponent;
        [SerializeField] private Material material;
        [SerializeField, HideInInspector] private Mesh cubeMesh;

        #endregion

        #region Private Fields

        private MaterialPropertyBlock _propBlock;
        private Matrix4x4 _volumeMatrix;

        private Material _materialInstance;
        private bool _isInitialized = false;
        private int _materialHash;

        #endregion

        #region Property IDs

        private int _volumeTextureID = Shader.PropertyToID("_Volume");
        private int _volumeCenterID = Shader.PropertyToID("_VolumeCenter");
        private int _volumeBoundsID = Shader.PropertyToID("_VolumeBounds");
        
        private int _numStepsID = Shader.PropertyToID("_NumSteps");
        private int _numStepsLightID = Shader.PropertyToID("_NumStepsLight");
        private int _randomSampleOffsetID = Shader.PropertyToID("_RandomSampleOffset");


        #endregion

        #region Mono Methods

        private void Start()
        {
            _propBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            TryInitialize();
            if (_isInitialized)
            {
                CreateMaterialInstanceOnChange();
                RenderVolume();
            }
        }

        private void Reset()
        {
            volumeComponent = GetComponent<VolumeComponent>();
            cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }

        #endregion

        #region Private Methods

        void RenderVolume()
        {
            _propBlock.Clear();
            _propBlock.SetVolume(volumeComponent.GetVolumeTexture(), _volumeTextureID, _volumeCenterID, _volumeBoundsID);

            _propBlock.SetInt(_numStepsID, maxSteps);
            _propBlock.SetInt(_numStepsLightID, maxStepsLight);
            _propBlock.SetFloat(_randomSampleOffsetID, randomSampleOffset);
            _volumeMatrix = Matrix4x4.TRS(volumeComponent.GetVolumeTexture().Center, Quaternion.identity, volumeComponent.GetVolumeTexture().Bounds);
            
            //Graphics.DrawMesh(cubeMesh, _volumeMatrix, _materialInstance, gameObject.layer, null, 0, _propBlock);

            RenderParams renderParams = new RenderParams(_materialInstance);
            renderParams.camera = null;
            renderParams.layer = gameObject.layer;
            renderParams.matProps = _propBlock;
            renderParams.receiveShadows = true;
            renderParams.worldBounds = new Bounds(volumeComponent.GetVolumeTexture().Center,
                volumeComponent.GetVolumeTexture().Bounds);
            renderParams.rendererPriority = 0;
            renderParams.motionVectorMode = MotionVectorGenerationMode.ForceNoMotion;
            renderParams.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderParams.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderParams.renderingLayerMask = UInt32.MaxValue;
            renderParams.shadowCastingMode = ShadowCastingMode.Off;

            Graphics.RenderMesh(renderParams, cubeMesh, 0, _volumeMatrix);
        }

        private void TryInitialize()
        {
            if(_isInitialized) return;
            if(!volumeComponent) return;
            if((volumeComponent.GetVolumeTexture()?.IsInitialized ?? false) == false) return;
            _isInitialized = true;
        }

        private void CreateMaterialInstanceOnChange()
        {   
            int newMaterialHash = material.ComputeCRC();
            if (_materialHash != newMaterialHash)
            {
                _materialHash = newMaterialHash;
                
                if(_materialInstance)
                {
                    if (material.shader == _materialInstance.shader)
                        _materialInstance.CopyPropertiesFromMaterial(material);
                }
                else
                {
                    _materialInstance = new Material(material);
                }
                SetTextureFormatKeyword();
            }
        }
        
        private void SetTextureFormatKeyword()
        {
            bool useHalf4 = volumeComponent.GetVolumeTexture().Texture.format == RenderTextureFormat.ARGBHalf;
            
            LocalKeyword textureFormatKeyword = _materialInstance.shader.keywordSpace.FindKeyword("HALF4");
            if(textureFormatKeyword.isValid)
                _materialInstance.SetKeyword(textureFormatKeyword, useHalf4);
        }
        
        #endregion
    }
}