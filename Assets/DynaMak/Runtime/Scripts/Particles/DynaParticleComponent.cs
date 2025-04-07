using System;
using System.Collections.Generic;
using UnityEngine;
using DynaMak.Utility;
using UnityEditor;
using Object = UnityEngine.Object;
using DynaMak.Properties;

namespace DynaMak.Particles
{
    public class DynaParticleComponent : MonoBehaviour, IDynaPropertyUser
    {
        private const int ThreadBlockSize = 256;
        
        
        #region Serialize Fields

        [Header("Emission")] 
        [SerializeField] private bool emitAllOnAwake = false;
        [SerializeField] private bool enableEmission = false;
        [SerializeField] private float particlesPerSecond = 60f;

        [Header("Settings")] 
        [SerializeField] private bool usePingPong = false;
        [SerializeField, Range(1, 10)] private int iterations = 1;

        [Header("Particle Count Info")] 
        [RequireInterface(typeof(IParticleCountInfo))]
        public Object particleCountInfo;

        [SerializeField, Min(0)] private int maxParticles = 65536;
        [SerializeField, Min(1)] private int renderTrianglesPerParticle = 2;
        [SerializeField, Min(1)] private int particlesPerEmit = 1;
        //[SerializeField, Min(1)] private int maxRenderTriangles = 65536;
        
        [Header("Shader File")]
        [SerializeField] private ComputeShader computeShader;
        public ComputeShader ComputeShader => computeShader;
        
        [Header("Properties")] 
        [SerializeField] 
        [ArrayElementTitle] private DynaPropertyBinderBase[] dynaProperties;
        #endregion


        #region Public Fields

        [HideInInspector] public DynaParticle ParticleSystem;

        #endregion
        
        
        #region Private Fields

        [SerializeField, HideInInspector] private Object particleStructFile;
        public Object ParticleStructFile => particleStructFile;
        
        [SerializeField, HideInInspector] private int structLengthBytes;


        private Vector3 _currentPosition;
        private Vector3 _previousPosition;

        private float _emissionTimeCounter;
        
        #endregion


        #region Shader Property IDs

        protected int _initializeKernel, _killKernel, _updateKernel, _renderKernel, _emitKernel;

        public int EmitKernel => _emitKernel;
        
        protected readonly int _particleBufferID = Shader.PropertyToID("_ParticleBuffer");
        protected readonly int _deadIndexBufferID = Shader.PropertyToID("_DeadIndexBuffer");

        protected readonly int _timeID = Shader.PropertyToID("_time");
        protected readonly int _dtID = Shader.PropertyToID("_DeltaTime");
        protected readonly int _worldPosID = Shader.PropertyToID("_WorldPosition");
        protected readonly int _worldPosOldID = Shader.PropertyToID("_WorldPositionOld");
        protected readonly int _worldMatrixID = Shader.PropertyToID("_WorldMatrix");
        protected readonly int _worldMatrixInverseID = Shader.PropertyToID("_WorldMatrixInverse");

        #endregion



        #region DynaProperties


        [SerializeField] private List<DynaPropertyBinderBase> _subscribedProperties = new List<DynaPropertyBinderBase>();
        public List<DynaPropertyBinderBase> SubscribedProperties => _subscribedProperties;
        
        
        public void AddProperty(DynaPropertyBinderBase property)
        {
            _subscribedProperties.Add(property);
        }

        public void RemoveProperty(DynaPropertyBinderBase property)
        {
            _subscribedProperties.Remove(property);
        }

        private void SetPropertiesFromListeners(int kernelId)
        {
            for (int i = _subscribedProperties.Count - 1; i >= 0; i--)
            {
                if (_subscribedProperties[i])
                {
                    if(_subscribedProperties[i].isActiveAndEnabled)
                        _subscribedProperties[i].SetProperty(computeShader, kernelId);
                }
                else
                    _subscribedProperties.RemoveAt(i);
            }
        }

        #endregion
        

        
        #region Mono Methods

        private void Update()
        {
            UpdateTransforms();
            SetUpdateProperties();

            if (enableEmission)
            {
                int emissionCount = EmissionTimer(particlesPerSecond);
                
                if (emissionCount > 0)
                    DispatchEmit(emissionCount);
            }
            
            
            for (int i = 0; i < iterations; i++)
            {
                DispatchUpdate();
            }
            
            DispatchRender();

        }


        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if(emitAllOnAwake) EmitAll();
        }

        private void OnDestroy()
        {
            Release();
        }

        #endregion
        
        
        
        
        #region Public Functions

        public void Initialize()
        {
            Release();

            bool usedInfo = GetParticleCountInfo(out int maxParticleCount, out int maxRenderedTriangles);
            
            int groupCount = Mathf.CeilToInt((float) maxParticleCount / ThreadBlockSize);
            int bufferSize = groupCount * ThreadBlockSize;
            if(usedInfo == false) maxRenderedTriangles = bufferSize * renderTrianglesPerParticle;
            
            ParticleSystem = new DynaParticle(computeShader, maxParticleCount, maxRenderedTriangles, structLengthBytes, ThreadBlockSize, usePingPong, particlesPerEmit);
            
            ParticleSystem.Initialize();
            
            _initializeKernel = computeShader.FindKernel("InitializeKernel");
            _updateKernel = computeShader.FindKernel("UpdateKernel");
            _renderKernel = computeShader.FindKernel("RenderKernel");
            _killKernel = computeShader.FindKernel("KillKernel");
            _emitKernel = computeShader.FindKernel("EmitKernel");


            if(usePingPong)
                computeShader.EnableKeyword("PARTICLE_PONG_BUFFER");
            else computeShader.DisableKeyword("PARTICLE_PONG_BUFFER");
            
            
            _currentPosition = transform.position;
            _previousPosition = _currentPosition;

            DispatchInitialize();
        }


        public void EmitAll()
        {
            DispatchEmit(Mathf.CeilToInt(ParticleSystem.GetBufferSize()));
        }


        public virtual void DispatchInitialize()
        {
            computeShader.SetBuffer(_initializeKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_initializeKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            ParticleSystem.InitializeParticles();
        }

        public virtual void DispatchKill()
        {
            computeShader.SetBuffer(_killKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_killKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            ParticleSystem.DispatchCompute(computeShader, _killKernel);
        }
        
        #endregion


        #region Private Functions

        protected virtual void SetPropertiesFromArray(int kernelIndex)
        {
            if (dynaProperties is not null)
            {
                foreach (var property in dynaProperties)
                {
                    if (!property) continue;
                    if(property.isActiveAndEnabled) property.SetProperty(computeShader, kernelIndex);
                }
            }
            
            SetPropertiesFromListeners(kernelIndex);
        }
        

        protected virtual void DispatchUpdate()
        {
            computeShader.SetBuffer(_updateKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            ParticleSystem.SetParticleBuffers(_updateKernel);
            ParticleSystem.SwitchBuffers();

            computeShader.SetBuffer(_updateKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            computeShader.SetBuffer(_updateKernel, ParticleSystem._aliveIndexBufferID, ParticleSystem.DeadIndexBuffer);
            
            
            computeShader.SetFloat(_dtID, Time.deltaTime / iterations);
            
            ParticleSystem.DispatchCompute(computeShader, _updateKernel);
            
            computeShader.SetFloat(_dtID, Time.deltaTime);
        }
        
        protected virtual void DispatchRender()
        {
            SetPropertiesFromArray(_renderKernel);

            ParticleSystem.RenderTriangleBuffer.SetCounterValue(0);

            computeShader.SetBuffer(_renderKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_renderKernel, ParticleSystem._triangleRenderBufferID, ParticleSystem.RenderTriangleBuffer);
            ParticleSystem.DispatchCompute(computeShader, _renderKernel);
        }
        
        
        public virtual void DispatchEmit(int count, bool setPropertiesFromComponent = true)
        {
            if(setPropertiesFromComponent) SetPropertiesFromArray(_emitKernel);

            computeShader.SetBuffer(_emitKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_emitKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            computeShader.SetBuffer(_emitKernel, ParticleSystem._aliveIndexBufferID, ParticleSystem.DeadIndexBuffer);
            
            ParticleSystem.DispatchEmission(count, computeShader, _emitKernel);
        }

        

        protected int EmissionTimer(float emissionPerSecond)
        {
            _emissionTimeCounter += Time.deltaTime;
            if (emissionPerSecond <= 0f) return 0;
            
            int o = Mathf.FloorToInt(_emissionTimeCounter * emissionPerSecond);
            if (_emissionTimeCounter * emissionPerSecond >= 1f)
            {
                _emissionTimeCounter = 0f;
            }
            return o;
        }


        protected virtual void SetUpdateProperties()
        {
            computeShader.SetFloat(_timeID, Time.time);
            computeShader.SetFloat(_dtID, Time.deltaTime);
            
            
            computeShader.SetVector(_worldPosID, _currentPosition);
            computeShader.SetVector(_worldPosOldID, _previousPosition);
            Matrix4x4 worldMat = transform.localToWorldMatrix;
            
            computeShader.SetMatrix(_worldMatrixID, worldMat);
            computeShader.SetMatrix(_worldMatrixInverseID, worldMat.inverse);
            
            
            SetPropertiesFromArray(_updateKernel);
        }

        protected virtual void UpdateTransforms()
        {
            _previousPosition = _currentPosition;
            _currentPosition = transform.position;
        }
        
        protected virtual void Release()
        {
            ParticleSystem?.Release();
        }
        
        
        
        private bool GetParticleCountInfo(out int maxParticleCount, out int maxRenderTriangleCount)
        {
            if (!particleCountInfo || (particleCountInfo is not IParticleCountInfo countInfo))
            {
                maxParticleCount = maxParticles;
                maxRenderTriangleCount = renderTrianglesPerParticle * maxParticles;
                return false;
            }
            
            
            maxParticleCount = countInfo!.MaxParticleCount;
            maxRenderTriangleCount = countInfo.RenderTriangleCount;
            return true;
        }

        
        #endregion
    }
}