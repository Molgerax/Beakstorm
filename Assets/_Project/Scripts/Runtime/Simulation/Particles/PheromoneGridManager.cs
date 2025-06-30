using System.Collections.Generic;
using Beakstorm.ComputeHelpers;
using Beakstorm.Pausing;
using Beakstorm.Simulation.Collisions.SDF;
using Beakstorm.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    [DefaultExecutionOrder(-90)]
    public class PheromoneGridManager : MonoBehaviour, IGridParticleSimulation
    {
        private const int THREAD_GROUP_SIZE = 256;

        public static List<PheromoneEmitter> Emitters = new(32);
        
        [SerializeField] private int maxCount = 256;
        [SerializeField] private ComputeShader pheromoneComputeShader;

        [Header("Rendering")] 
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [SerializeField] private ComputeShader sortShader;

        [Header("Attractor")] 
        [SerializeField] private bool useAttractor = false;

        [Header("Collision")] 
        [SerializeField] private Vector3 simulationSpace = Vector3.one;
        [SerializeField] private float targetDensity = 1;
        [SerializeField] private float pressureMultiplier = 1;

        [SerializeField, Range(0.1f, 16f)] private float smoothingRadius = 8;

        [SerializeField] [Range(0.5f, 2f)] private float cellSizeRatio = 1f;
        
        [SerializeField] private ComputeShader cellShader;


        private GraphicsBuffer _pheromoneBuffer;
        private GraphicsBuffer _pheromoneBufferRead;

        private GraphicsBuffer _pheromoneSorted;
        
        private GraphicsBuffer _instancedDrawingArgsBuffer;
        
        private MaterialPropertyBlock _propertyBlock;

        private int _particlesPerEmit = 1;
        private int[] _counterArray;
        
        private int _capacity;
        private bool _initialized;

        public static PheromoneGridManager Instance;

        public bool Initialized => _initialized;
        public GraphicsBuffer GridOffsetsBuffer => _hash?.GridOffsetBuffer;

        public GraphicsBuffer InstancedArgsBuffer => _instancedDrawingArgsBuffer;
        
        public int AgentCount => _capacity;
        public float CellSize => smoothingRadius * cellSizeRatio;

        public float SmoothingRadius => smoothingRadius;
        public Vector3 SimulationCenter => transform.position;
        public Vector3 SimulationSize => simulationSpace;

        private bool _swapBuffers;

        public void SwapBuffers() => _swapBuffers = !_swapBuffers;
        
        public GraphicsBuffer AgentBufferWrite => _swapBuffers ? _pheromoneBufferRead : _pheromoneBuffer;
        public GraphicsBuffer AgentBufferRead => _swapBuffers ? _pheromoneBuffer : _pheromoneBufferRead;
        public int[] CellDimensions => _hash.Dimensions;
        public int AgentBufferStride => 12; 
        
        private SpatialGrid _hash;
        public SpatialGrid Hash => _hash;

        private Transform _mainCamera;

        private List<EmissionRequest> _emissionRequests = new List<EmissionRequest>(128);

        public void AddEmissionRequest(int count, Vector3 pos, Vector3 oldPos, float lifeTime)
        {
            if (count <= 0 || lifeTime <= 0)
                return;
            
            _emissionRequests.Add(new EmissionRequest(count, pos, oldPos, lifeTime));
        }

        private void Awake()
        {
            Instance = this;
            if (Camera.main is not null) _mainCamera = Camera.main.transform;
        }

        private void Start()
        {
            InitializeBuffers();
        }

        private void Update()
        {
            if (_initialized)
            {
                if (!PauseManager.IsPaused)
                {
                    Clear();
                    int updateKernel = pheromoneComputeShader.FindKernel("Update");
                    RunSimulation(updateKernel, SimulationTime.DeltaTime);
                    
                    ResetAttractor();
                    
                    ApplyEmitters(Time.deltaTime);
                    ApplyEmissionRequests(SimulationTime.DeltaTime);
                    SwapBuffers();
                    
                    _hash?.Update();
                    SwapBuffers();
                    
                    TransferAttractorData();
                }
                
                RenderMeshes();
            }
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }


        [ContextMenu("Re-Init")]
        private void InitializeBuffers()
        {
            _capacity = maxCount;
            ReleaseBuffers();
            
            _pheromoneBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, _capacity, AgentBufferStride * sizeof(float));
            _pheromoneBufferRead = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Counter, _capacity, AgentBufferStride * sizeof(float));

            _pheromoneSorted = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 2 * sizeof(float));

            _instancedDrawingArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            GraphicsBuffer.IndirectDrawIndexedArgs[] indexedArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            indexedArgs[0].indexCountPerInstance = mesh.GetIndexCount(0);
            _instancedDrawingArgsBuffer.SetData(indexedArgs);
            
            InitializeAttractor();

            _hash = new SpatialGrid(cellShader, this, _instancedDrawingArgsBuffer);
            
            _pheromoneBuffer.SetCounterValue(0);
            _pheromoneBufferRead.SetCounterValue(0);
            
            int initKernel = pheromoneComputeShader.FindKernel("Init");
            RunSimulation(initKernel, SimulationTime.DeltaTime);

            _initialized = true;
        }

        private void ReleaseBuffers()
        {
            _pheromoneBuffer?.Release();
            _pheromoneBuffer = null;

            _pheromoneBufferRead?.Release();
            _pheromoneBufferRead = null;

            _instancedDrawingArgsBuffer?.Release();
            _instancedDrawingArgsBuffer = null;

            _pheromoneSorted?.Release();
            _pheromoneSorted = null;
            
            _hash?.Dispose();
            
            _attractorBuffer?.Release();
            _attractorBuffer = null;
        }


        private void RunSimulation(int kernelId, float timeStep)
        {
            if (PauseManager.IsPaused)
                return;
            
            if (kernelId < 0)
            {
                Debug.LogError($"Kernel for ComputeShader {pheromoneComputeShader} is invalid", this);
                return;
            }

            pheromoneComputeShader.SetFloat(PropertyIDs.HashCellSize, CellSize);
            
            pheromoneComputeShader.SetVector(PropertyIDs.WorldPos, transform.position);
            pheromoneComputeShader.SetMatrix(PropertyIDs.WorldMatrix, transform.localToWorldMatrix);
            
            pheromoneComputeShader.SetFloat(PropertyIDs.Time, Time.time);
            pheromoneComputeShader.SetFloat(PropertyIDs.DeltaTime, timeStep);
            pheromoneComputeShader.SetFloat(PropertyIDs.LifeTime, 1f);

            pheromoneComputeShader.SetFloat(PropertyIDs.TargetDensity, targetDensity);
            pheromoneComputeShader.SetFloat(PropertyIDs.PressureMultiplier, pressureMultiplier);
            pheromoneComputeShader.SetFloat(PropertyIDs.SmoothingRadius, smoothingRadius);
            
            if (SdfShapeManager.Instance)
            {
                pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.NodeBuffer, SdfShapeManager.Instance.NodeBuffer);
                pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.SdfBuffer, SdfShapeManager.Instance.SdfBuffer);
                pheromoneComputeShader.SetInt(PropertyIDs.NodeCount, SdfShapeManager.Instance.NodeCount);
            }
            
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, _hash.GridOffsetBuffer);

            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneBufferRead, AgentBufferRead);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneBuffer, AgentBufferWrite);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.InstancedArgsBuffer, _instancedDrawingArgsBuffer);

            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneSortingBuffer, _pheromoneSorted);

            if (_mainCamera)
            {
                pheromoneComputeShader.SetVector(PropertyIDs.CameraPos, _mainCamera.position);
                pheromoneComputeShader.SetVector(PropertyIDs.CameraForward, _mainCamera.forward);
            }
            
            _hash.SetShaderProperties(pheromoneComputeShader);
            
            pheromoneComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
            GraphicsBuffer.CopyCount(AgentBufferWrite, _instancedDrawingArgsBuffer, 1 * sizeof(uint));
        }


        private void ApplyEmitters(float timeStep)
        {
            if (PauseManager.IsPaused)
                return;
            if (timeStep == 0)
                return;
            
            if (Emitters == null)
                return;
            
            for (int i = Emitters.Count - 1; i >= 0; i--)
            {
                var emitter = Emitters[i];
                if (emitter == null)
                {
                    Emitters.RemoveAt(i);
                    continue;
                }
                
                emitter.EmitOverTime(timeStep);
            }
        }
        
        private void ApplyEmissionRequests(float timeStep)
        {
            if (PauseManager.IsPaused)
                return;
            if (timeStep == 0)
                return;
            
            if (_emissionRequests == null)
                return;
            
            for (int i = _emissionRequests.Count - 1; i >= 0; i--)
            {
                var request = _emissionRequests[i];
                EmitParticles(request.Count, request.Position, request.OldPosition, request.LifeTime);
                _emissionRequests.RemoveAt(i);
            }
        }

        private void Clear()
        {
            int kernel = pheromoneComputeShader.FindKernel("Clear");
            
            _hash.SetShaderProperties(pheromoneComputeShader);
            pheromoneComputeShader.SetBuffer(kernel, PropertyIDs.PheromoneBuffer, AgentBufferWrite);
            
            pheromoneComputeShader.DispatchExact(kernel, AgentCount);
            AgentBufferWrite.SetCounterValue(0);
        }

        public void EmitParticles(int count, Vector3 pos, Vector3 oldPos, float lifeTime)
        {
            if (PauseManager.IsPaused)
                return;
            
            if (count <= 0 || lifeTime <= 0)
                return;
        
            AddAttractor(pos, count / SimulationTime.DeltaTime, lifeTime);

            if (useAttractor)
            {
                count *= 4;
                lifeTime = 0.5f;
            }
            
            int emissionKernel = pheromoneComputeShader.FindKernel("Emit");
            
            pheromoneComputeShader.SetFloat(PropertyIDs.Time, Time.time);
            pheromoneComputeShader.SetFloat(PropertyIDs.DeltaTime, SimulationTime.DeltaTime);
            pheromoneComputeShader.SetFloat(PropertyIDs.LifeTime, lifeTime);
            
            pheromoneComputeShader.SetFloat(PropertyIDs.TargetDensity, targetDensity);
            pheromoneComputeShader.SetFloat(PropertyIDs.PressureMultiplier, pressureMultiplier);
            pheromoneComputeShader.SetVector(PropertyIDs.SpawnPos, pos);
            pheromoneComputeShader.SetVector(PropertyIDs.SpawnPosOld, oldPos);
            
            pheromoneComputeShader.SetInt(PropertyIDs.TargetEmitCount, count);
            pheromoneComputeShader.SetInt(PropertyIDs.ParticlesPerEmit, _particlesPerEmit);
            
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.PheromoneBuffer, AgentBufferWrite);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.InstancedArgsBuffer, _instancedDrawingArgsBuffer);

            
            pheromoneComputeShader.Dispatch(emissionKernel, Mathf.CeilToInt((float)count / THREAD_GROUP_SIZE), 1, 1);
            GraphicsBuffer.CopyCount(AgentBufferWrite, _instancedDrawingArgsBuffer, 1 * sizeof(uint));
        }
        
        private void PrepareSort()
        {
            int kernel = pheromoneComputeShader.FindKernel("PrepareSort");
            
            pheromoneComputeShader.SetBuffer(kernel, PropertyIDs.PheromoneBufferRead, AgentBufferRead);
            pheromoneComputeShader.SetBuffer(kernel, PropertyIDs.PheromoneSortingBuffer, _pheromoneSorted);
            pheromoneComputeShader.SetBuffer(kernel, PropertyIDs.InstancedArgsBuffer, _instancedDrawingArgsBuffer);

            pheromoneComputeShader.DispatchExact(kernel, AgentCount);
        }

        private void SortForRendering()
        {
            PrepareSort();
            BitonicMergeSort.SortBuffer(sortShader, _pheromoneSorted);
        }

        private void RenderMeshes()
        {
            if (!mesh || !material)
                return;
            
            SortForRendering();

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetBuffer(PropertyIDs.PheromoneBuffer, AgentBufferRead);
            _propertyBlock.SetBuffer(PropertyIDs.PheromoneSortingBuffer, _pheromoneSorted);

            RenderParams rp = new RenderParams(material)
            {
                camera = null,
                instanceID = GetInstanceID(),
                layer = gameObject.layer,
                lightProbeUsage = LightProbeUsage.Off,
                lightProbeProxyVolume = null,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.Off,
                worldBounds = new Bounds(transform.position, simulationSpace * 100),
                matProps = _propertyBlock,
            };

            
            Graphics.RenderMeshIndirect(in rp, mesh, _instancedDrawingArgsBuffer);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, simulationSpace);
        }

        private struct EmissionRequest
        {
            public int Count;
            public Vector3 Position;
            public Vector3 OldPosition;
            public float LifeTime;

            public EmissionRequest(int count, Vector3 pos, Vector3 oldPos, float lifeTime)
            {
                Count = count;
                Position = pos;
                OldPosition = oldPos;
                LifeTime = lifeTime;
            }
        }



        #region Attractors

        struct AttractorData
        {
            public Vector3 Position;
            public uint Data;

            public AttractorData(Vector3 pos, float radius, float strength)
            {
                Position = pos;
                Data = (uint)Mathf.RoundToInt(Mathf.Max(1, radius));
                uint str = (uint) Mathf.RoundToInt(strength * 256);
                Data |= str << 16;
            }
        }

        private GraphicsBuffer _attractorBuffer;

        public GraphicsBuffer AttractorBuffer => _attractorBuffer;
        private List<AttractorData> _attractorList = new List<AttractorData>(16);
        private int _attractorCount;
        public int AttractorCount => _attractorCount;
        public bool UseAttractors => useAttractor;
        private void InitializeAttractor()
        {
            if (!useAttractor)
                return;
            
            _attractorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 16, 4 * sizeof(float));
        }
        
        private void ResetAttractor()
        {
            if (!useAttractor)
                return;
            _attractorCount = 0;
            _attractorList.Clear();
        }

        private void AddAttractor(Vector3 pos, float emissionRate, float lifeTime)
        {
            if (!useAttractor)
                return;
            
            float strength = Mathf.Sqrt(emissionRate);
            float radius = SmoothingRadius * 2;
            _attractorList.Add(new AttractorData(pos, radius, strength));
        }

        private void TransferAttractorData()
        {
            if (!useAttractor)
                return;
            
            _attractorCount = _attractorList.Count;
            if (_attractorCount == 0)
                return;
            
            if (AttractorBuffer == null || AttractorBuffer.count < _attractorCount)
            {
                _attractorBuffer?.Release();
                _attractorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 
                    Mathf.NextPowerOfTwo(_attractorCount), 4 * sizeof(float));
            }
            
            _attractorBuffer.SetData(_attractorList);
        }


        #endregion
        
        
        
        public static class PropertyIDs
        {
            public static readonly int HashCellSize            = Shader.PropertyToID("_HashCellSize");
            public static readonly int WorldPos                = Shader.PropertyToID("_WorldPos");
            public static readonly int WorldMatrix             = Shader.PropertyToID("_WorldMatrix");
            
            public static readonly int Time                    = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime               = Shader.PropertyToID("_DeltaTime");
            public static readonly int LifeTime        = Shader.PropertyToID("_LifeTime");
            
            public static readonly int SpawnPos        = Shader.PropertyToID("_SpawnPos");
            public static readonly int SpawnPosOld        = Shader.PropertyToID("_SpawnPosOld");
            
            public static readonly int TargetEmitCount        = Shader.PropertyToID("_TargetEmitCount");
            public static readonly int ParticlesPerEmit        = Shader.PropertyToID("_ParticlesPerEmit");
                  
            public static readonly int CameraPos              = Shader.PropertyToID("_CameraPos");
            public static readonly int CameraForward              = Shader.PropertyToID("_CameraForward");
            
            public static readonly int TargetDensity           = Shader.PropertyToID("_TargetDensity");
            public static readonly int PressureMultiplier      = Shader.PropertyToID("_PressureMultiplier");
            public static readonly int SmoothingRadius      = Shader.PropertyToID("_SmoothingRadius");
            
            public static readonly int SpatialOffsets              = Shader.PropertyToID("_PheromoneSpatialOffsets");
            
            public static readonly int NodeBuffer = Shader.PropertyToID("_NodeBuffer");
            public static readonly int SdfBuffer = Shader.PropertyToID("_SdfBuffer");
            public static readonly int NodeCount = Shader.PropertyToID("_NodeCount");

            public static readonly int PheromoneBuffer              = Shader.PropertyToID("_PheromoneBuffer");
            public static readonly int PheromoneBufferRead              = Shader.PropertyToID("_PheromoneBufferRead");            
            public static readonly int PheromoneSortingBuffer              = Shader.PropertyToID("_PheromoneSortingBuffer");
            public static readonly int InstancedArgsBuffer              = Shader.PropertyToID("_InstancedArgsBuffer");

        }
    }
}
