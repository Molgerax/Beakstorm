using System;
using System.Data;
using UnityEngine;
using DynaMak.Utility;
using DynaMak.Properties;

namespace DynaMak.Particles
{
    [System.Serializable]
    public class DynaParticle
    {
        #region Initialize Fields
        
        [SerializeField, HideInInspector] protected int _maxParticles;
        [SerializeField, HideInInspector] protected int _structLengthBytes;
        [SerializeField, HideInInspector] protected int _threadBlockSize;
        [SerializeField, HideInInspector] protected int _maxRenderTriangles = 65536;
        [SerializeField, HideInInspector] protected bool _usePingPong = false;
        
        [SerializeField, HideInInspector] protected int _particlesPerEmit = 1;
        
        protected readonly ComputeShader _computeShader;
        
        #endregion

        
        #region Public Fields

        private ComputeBuffer PingParticleBuffer;
        private ComputeBuffer PongParticleBuffer;
        
        public ComputeBuffer DeadIndexBuffer;
        public ComputeBuffer CounterBuffer;
        public GraphicsBuffer RenderTriangleBuffer;

        public ComputeBuffer ParticleBuffer
        {
            get
            {
                return _usePingPong ? (_ping ? PingParticleBuffer : PongParticleBuffer) : PingParticleBuffer;
            }
        }

        public ComputeBuffer BackParticleBuffer => _ping ? PongParticleBuffer : PingParticleBuffer;

        #endregion

        
        
        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="computeShader">Generic compute shader that must contain Initialize and Kill kernels.</param>
        /// <param name="maxParticles">Maximum number of particles.</param>
        /// <param name="maxRenderTriangles">Maximum number of rendered triangles</param>
        /// <param name="structLengthBytes"> Must be a multiple of 4.</param>
        /// <param name="threadBlockSize"></param>
        /// <param name="usePingPong">Use a second particle buffer to avoid read/write race conditions.</param>
        public DynaParticle(ComputeShader computeShader, int maxParticles, int maxRenderTriangles, int structLengthBytes, int threadBlockSize = 256, bool usePingPong = false, int particlesPerEmit = 1)
        {
            if (structLengthBytes % 4 != 0)
            {
                Debug.LogError($"Struct length ({structLengthBytes}) must be multiple of 4. " +
                               $"Will be rounded up to {(structLengthBytes / 4) * 4 + 4}.");
                
                structLengthBytes = (structLengthBytes / 4) * 4 + 4;
            }

            _computeShader = computeShader;
            _maxParticles = Math.Max(0, maxParticles);
            _structLengthBytes = structLengthBytes;
            _threadBlockSize = threadBlockSize;
            _maxRenderTriangles = maxRenderTriangles;
            _usePingPong = usePingPong;
            _particlesPerEmit = particlesPerEmit;

            _initializeKernel = _computeShader.FindKernel("InitializeKernel");
            _killKernel = _computeShader.FindKernel("KillKernel");
            _deadCountKernel = _computeShader.FindKernel("EmissionCountKernel");
            _emitKernel = _computeShader.FindKernel("EmitKernel");
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxParticles"></param>
        /// <param name="structLengthBytes"> Must be a multiple of 4.</param>
        /// <param name="threadBlockSize"></param>
        public DynaParticle(int maxParticles, int structLengthBytes, int maxRenderTriangles = 65536, int threadBlockSize = 256)
        {
            if (structLengthBytes % 4 != 0)
            {
                Debug.LogError($"Struct length ({structLengthBytes}) must be multiple of 4. " +
                               $"Will be rounded up to {(structLengthBytes / 4) * 4 + 4}.");
                
                structLengthBytes = (structLengthBytes / 4) * 4 + 4;
            }

            _maxParticles = Math.Max(0, maxParticles);
            _structLengthBytes = structLengthBytes;
            _threadBlockSize = threadBlockSize;
            _maxRenderTriangles = maxRenderTriangles;
        }

        #endregion



        #region Private Fields

        // Buffer Sizing
        private int _bufferSize = 100096;
        private int _groupCount;
        private int[] _counterArray;
        private int _deadCount = 0;
        
        private bool _ping = true;
        
        #endregion

        #region Shader Property IDs

        private int _initializeKernel, _killKernel, _updateKernel, _deadCountKernel, _emitKernel, _renderKernel;
        
        public readonly int _particleBufferID = Shader.PropertyToID("_ParticleBuffer");
        public readonly int _pongBufferID = Shader.PropertyToID("_PongBuffer");
        public readonly int _deadIndexBufferID = Shader.PropertyToID("_DeadIndexBuffer");
        public readonly int _aliveIndexBufferID = Shader.PropertyToID("_AliveIndexBuffer");
        public readonly int _deadCountBufferID = Shader.PropertyToID("_DeadCountBuffer");
        public readonly int _triangleRenderBufferID = Shader.PropertyToID("_TriangleBuffer");

        private readonly int _particlesPerEmitID = Shader.PropertyToID("_ParticlesPerEmit");
        private readonly int _targetEmitCountID = Shader.PropertyToID("_TargetEmitCount");
        private readonly int _maxParticleCountID = Shader.PropertyToID("_MaxParticleCount");

        #endregion



        #region Getters

        public int GetThreadBlockSize() => _threadBlockSize;
        public int GetMaxParticles() => _maxParticles;
        public int GetBufferSize() => _bufferSize;
        public int GetGroupCount() => _groupCount;
        public int GetDeadCount() => _deadCount;
        public int GetAliveCount() => _bufferSize - _deadCount;

        #endregion
        

        #region Public Functions

        /// <summary>
        /// Initializes values and allocates buffers. Does NOT populate the DeadIndexBuffer yet, must be performed
        /// externally. 
        /// </summary>
        public virtual void Initialize()
        {
            Release();

            

            _groupCount = Mathf.CeilToInt((float) _maxParticles / _threadBlockSize);
            _bufferSize = _groupCount * _threadBlockSize;
            
            
            PingParticleBuffer = new ComputeBuffer(_bufferSize, _structLengthBytes, ComputeBufferType.Counter);
            PingParticleBuffer.SetCounterValue(0);
            if (_usePingPong)
            {
                PongParticleBuffer = new ComputeBuffer(_bufferSize, _structLengthBytes, ComputeBufferType.Counter);
                PongParticleBuffer.SetCounterValue(0);
            }
            _ping = true;
            

            DeadIndexBuffer = new ComputeBuffer(_bufferSize, sizeof(int), ComputeBufferType.Append);
            DeadIndexBuffer.SetCounterValue(0);
            CounterBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
            _counterArray = new int[] {_bufferSize / _particlesPerEmit, 1, 1, _bufferSize};
            CounterBuffer.SetData(_counterArray);
            
            RenderTriangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, 
                    _maxRenderTriangles, sizeof(float) * 16 * 3 + sizeof(int));
            RenderTriangleBuffer.SetCounterValue(0);
        }



        public void SwitchBuffers()
        {
            _ping = !_ping;
        }

        public void SetParticleBuffers(int kernelId)
        {
            _computeShader.SetBuffer(kernelId, _particleBufferID, ParticleBuffer);
            if(_usePingPong) 
                _computeShader.SetBuffer(kernelId, _pongBufferID, BackParticleBuffer);
        }
        
        
        /// <summary>
        /// Sets the count of dead particles. This has CPU overhead / Semaphore wait time.
        /// </summary>
        public int ReadDeadCountFromGPU()
        {
            if (DeadIndexBuffer == null || CounterBuffer == null || _counterArray == null)
            {
                _deadCount = _bufferSize;
                return _deadCount;
            }
            CounterBuffer.SetData(_counterArray);
            ComputeBuffer.CopyCount(DeadIndexBuffer, CounterBuffer, 0);
            CounterBuffer.GetData(_counterArray);
            _deadCount = _counterArray[0];

            return _deadCount;
        }

        public void CopyTriCountToBuffer(ComputeBuffer dst, int dstOffsetBytes)
        {
            GraphicsBuffer.CopyCount(RenderTriangleBuffer, dst, dstOffsetBytes);
        }

        public void InitializeParticles()
        {
            if(_computeShader == null) return;
            
            _computeShader.SetBuffer(_initializeKernel, _particleBufferID, ParticleBuffer);
            _computeShader.SetBuffer(_initializeKernel, _deadIndexBufferID, DeadIndexBuffer);
            _computeShader.Dispatch(_initializeKernel, this);
        }

        public void KillParticles()
        {
            if(_computeShader == null) return;
            
            _computeShader.SetBuffer(_killKernel, _particleBufferID, ParticleBuffer);
            _computeShader.SetBuffer(_killKernel, _deadIndexBufferID, DeadIndexBuffer);
            _computeShader.Dispatch(_killKernel, this);

            _ping = true;
        }
        
        public virtual void DispatchEmissionCount(int count, ComputeShader cs)
        {
            _deadCountKernel = cs.FindKernel("EmissionCountKernel");
            
            ComputeBuffer.CopyCount(DeadIndexBuffer, CounterBuffer, 0);
            cs.SetInt(_targetEmitCountID, count);
            cs.SetInt(_particlesPerEmitID, _particlesPerEmit);

            cs.SetBuffer(_deadCountKernel, _deadCountBufferID, CounterBuffer);
            cs.Dispatch(_deadCountKernel, 1, 1, 1);
        }

        public virtual void DispatchEmission(int count, ComputeShader cs, int kernelId)
        {
            DispatchEmissionCount(count, cs);
            
            cs.SetBuffer(kernelId, _deadCountBufferID, CounterBuffer);
            cs.DispatchIndirect(kernelId, CounterBuffer, 0);
            
            DispatchEmissionCount(0, cs);
        }


        /// <summary>
        /// Dispatches a compute shader with the correct group count.
        /// </summary>
        public void DispatchCompute(ComputeShader computeShader, int kernelIndex)
        {
            computeShader.SetInt(_maxParticleCountID, _maxParticles);
            computeShader.SetBuffer(kernelIndex, _deadCountBufferID, CounterBuffer);
            computeShader.Dispatch(kernelIndex, Math.Max(_groupCount, 1), 1, 1);
        }
        

        /// <summary>
        /// Releases all buffers.
        /// </summary>
        public virtual void Release()
        {
            PingParticleBuffer?.Release();
            PongParticleBuffer?.Release();
            
            CounterBuffer?.Release();
            DeadIndexBuffer?.Release();
            RenderTriangleBuffer?.Release();
        }

        #endregion
    }
}
