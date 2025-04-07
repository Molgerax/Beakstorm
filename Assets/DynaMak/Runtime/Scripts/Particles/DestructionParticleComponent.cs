using DynaMak.Properties;
using UnityEngine;
using DynaMak.Utility;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DynaMak.Particles
{
    public class DestructionParticleComponent : MonoBehaviour
    {
        private const int ThreadBlockSize = 256;
        
        
        #region Serialize Fields

        [Header("Settings")]
        [SerializeField] private bool playOnAwake;
        [SerializeField] private bool enableEmission;
        [SerializeField] private float particlesPerSecond = 60f;
        
        
        
        [Header("Properties")] 
        [SerializeField] private DynaPropertyFracturedMesh fracturedMesh;
        [SerializeField] private DynaPropertyVolumeTexture sdfVolume;
        [SerializeField] private DynaPropertyFloat[] floatProperties;
        
        [Header("References")]
        [SerializeField] private ComputeShader computeShader;
        

        #endregion


        #region Public Fields

        public DynaParticle ParticleSystem;

        #endregion
        
        
        #region Private Fields

        [SerializeField, HideInInspector] private Object particleStructFile;
        [SerializeField, HideInInspector] private int structLengthBytes;
        [SerializeField, HideInInspector] private int maxParticles;
        [SerializeField, HideInInspector] private int maxRenderTriangles;


        private Vector3 _currentPosition;
        private Vector3 _previousPosition;

        private float _emissionTimeCounter;
        
        #endregion

        #region Shader Property IDs

        protected int _initializeKernel, _killKernel, _updateKernel, _renderKernel, _emitKernel;
        
        protected readonly int _particleBufferID = Shader.PropertyToID("_ParticleBuffer");
        protected readonly int _deadIndexBufferID = Shader.PropertyToID("_DeadIndexBuffer");

        protected readonly int _timeID = Shader.PropertyToID("_time");
        protected readonly int _dtID = Shader.PropertyToID("_DeltaTime");
        protected readonly int _worldPosID = Shader.PropertyToID("_WorldPosition");
        protected readonly int _worldPosOldID = Shader.PropertyToID("_WorldPositionOld");
        protected readonly int _worldMatrixID = Shader.PropertyToID("_WorldMatrix");

        #endregion


        #region Mono Methods

        private void Start()
        {
            if(playOnAwake) BurstEmit();
        }

        private void Update()
        {
            UpdateTransforms();
            SetUpdateProperties();
            DispatchUpdate();
            DispatchRender();

            if (enableEmission)
            {
                int emissionCount = EmissionTimer(particlesPerSecond);
                
                if (emissionCount > 0)
                    DispatchEmit(emissionCount);
            }
        }


        private void Awake()
        {
            Initialize();
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
            
            fracturedMesh.Initialize();
            sdfVolume.Initialize();
            foreach (var prop in floatProperties)
            {
                prop.Initialize();
            }

            maxParticles = fracturedMesh.Value.NumberOfPieces + 1;
            maxRenderTriangles = fracturedMesh.Value.MeshTriangles;
            
            ParticleSystem = new DynaParticle(maxParticles, structLengthBytes, maxRenderTriangles, ThreadBlockSize);
            
            ParticleSystem.Initialize();
            
            _initializeKernel = computeShader.FindKernel("InitializeKernel");
            _updateKernel = computeShader.FindKernel("UpdateKernel");
            _renderKernel = computeShader.FindKernel("RenderKernel");
            _killKernel = computeShader.FindKernel("KillKernel");
            _emitKernel = computeShader.FindKernel("EmitKernel");


            _currentPosition = transform.position;
            _previousPosition = _currentPosition;

            DispatchInitialize();
        }


        public void BurstEmit()
        {
            DispatchEmit(Mathf.CeilToInt(ParticleSystem.GetBufferSize()));
        }


        public virtual void DispatchInitialize()
        {
            computeShader.SetBuffer(_initializeKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_initializeKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            ParticleSystem.DispatchCompute(computeShader, _initializeKernel);
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
            fracturedMesh.SetProperty(computeShader, kernelIndex);
            sdfVolume.SetProperty(computeShader, kernelIndex);
            foreach (DynaPropertyFloat prop in floatProperties)
            {
                prop.SetProperty(computeShader, kernelIndex);
            }
        }
        

        protected virtual void DispatchUpdate()
        {
            computeShader.SetBuffer(_updateKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_updateKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            computeShader.SetBuffer(_updateKernel, ParticleSystem._aliveIndexBufferID, ParticleSystem.DeadIndexBuffer);
            ParticleSystem.DispatchCompute(computeShader, _updateKernel);
        }
        
        protected virtual void DispatchRender()
        {
            SetPropertiesFromArray(_renderKernel);

            ParticleSystem.RenderTriangleBuffer.SetCounterValue(0);

            computeShader.SetBuffer(_renderKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_renderKernel, ParticleSystem._triangleRenderBufferID, ParticleSystem.RenderTriangleBuffer);
            ParticleSystem.DispatchCompute(computeShader, _renderKernel);
        }
        
        
        protected virtual void DispatchEmit(int count)
        {
            SetPropertiesFromArray(_emitKernel);

            computeShader.SetBuffer(_emitKernel, _particleBufferID, ParticleSystem.ParticleBuffer);
            computeShader.SetBuffer(_emitKernel, _deadIndexBufferID, ParticleSystem.DeadIndexBuffer);
            computeShader.SetBuffer(_emitKernel, ParticleSystem._aliveIndexBufferID, ParticleSystem.DeadIndexBuffer);
            
            ParticleSystem.DispatchEmission(count, computeShader, _emitKernel);
            //computeShader.Dispatch(_emitKernel, count, 1, 1);
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
            computeShader.SetMatrix(_worldMatrixID, transform.localToWorldMatrix);

            SetPropertiesFromArray(_updateKernel);
        }

        protected virtual void UpdateTransforms()
        {
            _previousPosition = _currentPosition;
            _currentPosition = transform.position;
        }
        
        protected virtual void Release()
        {
            ParticleSystem.Release();
            
            fracturedMesh.Release();
            sdfVolume.Release();
            foreach (var prop in floatProperties)
            {
                prop.Release();
            }
        }
        
        #endregion

        

        #region Editor

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.25f);

            if (fracturedMesh.Value == null) return;
            
            //Gizmos.color = Color.green;
            //Gizmos.DrawWireCube(fracturedMesh.Value.Bounds.center + transform.position, fracturedMesh.Value.Bounds.size);
        }

        private void OnDrawGizmosSelected()
        {
            if (fracturedMesh.Value == null) return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(fracturedMesh.Value.Bounds.center + transform.position, fracturedMesh.Value.Bounds.size);
        }


        private void OnValidate()
        {
            ReadStructLength();
        }
        
        /// <summary>
        /// Reads out the struct length from the Particle Define file.
        /// </summary>
        private void ReadStructLength()
        {
            //particleStructFile = DynaParticleFileReader.ComputeShaderToParticleStruct(computeShader);
            //structLengthBytes = DynaParticleFileReader.GetStructLengthFromFile(computeShader);
        }
        
#endif
        #endregion
    }
}