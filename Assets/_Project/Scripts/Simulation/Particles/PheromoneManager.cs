using Beakstorm.ComputeHelpers;
using Beakstorm.Simulation.Collisions.SDF;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    [DefaultExecutionOrder(-90)]
    public class PheromoneManager : MonoBehaviour
    {
        private const int THREAD_GROUP_SIZE = 256;

        [SerializeField] private int maxCount = 256;
        [SerializeField] private ComputeShader pheromoneComputeShader;

        [Header("Rendering")] [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;


        [Header("Collision")] [SerializeField] private Vector3 simulationSpace = Vector3.one;
        [SerializeField] private float targetDensity = 1;
        [SerializeField] private float pressureMultiplier = 1;
        [SerializeField, Range(0.1f, 10f)] private float lifeTime = 1;

        [Header("Bitonic Merge Sort")] [SerializeField]
        private ComputeShader sortShader;

        [SerializeField] [Range(0.1f, 10f)] private float hashCellSize = 1f;

        private ComputeBuffer _spatialIndicesBuffer;
        private ComputeBuffer _spatialOffsetsBuffer;


        private GraphicsBuffer _positionBuffer;
        private GraphicsBuffer _oldPositionBuffer;
        private GraphicsBuffer _dataBuffer;
        private GraphicsBuffer _aliveBuffer;
        private GraphicsBuffer _deadIndexBuffer;
        private GraphicsBuffer _deadCountBuffer;
        
        private MaterialPropertyBlock _propertyBlock;

        private int _particlesPerEmit = 1;
        private int[] _counterArray;
        
        private int _capacity;
        private bool _initialized;

        public static PheromoneManager Instance;

        public ComputeBuffer SpatialIndicesBuffer => _spatialIndicesBuffer;
        public ComputeBuffer SpatialOffsetsBuffer => _spatialOffsetsBuffer;
        public GraphicsBuffer PositionBuffer => _positionBuffer;
        public GraphicsBuffer DataBuffer => _dataBuffer;
        public GraphicsBuffer AliveBuffer => _aliveBuffer;
        public GraphicsBuffer OldPositionBuffer => _oldPositionBuffer;
        public int Capacity => _capacity;
        public float HashCellSize => hashCellSize;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InitializeBuffers();
        }

        private void Update()
        {
            if (_initialized)
            {
                UpdateSpatialHash();
                GPUBitonicMergeSort.SortAndCalculateOffsets(sortShader, _spatialIndicesBuffer, _spatialOffsetsBuffer);

                
                int updateKernel = pheromoneComputeShader.FindKernel("Update");
                RunSimulation(updateKernel, Time.deltaTime);

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

            _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _oldPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _dataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 4 * sizeof(uint));
            _aliveBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 1 * sizeof(uint));
            
            _deadIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, _capacity, 1 * sizeof(uint));
            _deadIndexBuffer.SetCounterValue(0);

            _deadCountBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, 1 * sizeof(uint));
            _counterArray = new int[] {_capacity / _particlesPerEmit, 1, 1, _capacity};
            _deadCountBuffer.SetData(_counterArray);
            
            // Spatial Hash Buffers
            _spatialIndicesBuffer = new ComputeBuffer(_capacity, 3 * sizeof(int), ComputeBufferType.Structured);
            _spatialOffsetsBuffer = new ComputeBuffer(_capacity, 1 * sizeof(int), ComputeBufferType.Structured);

            int initKernel = pheromoneComputeShader.FindKernel("Init");
            RunSimulation(initKernel, Time.deltaTime);

            _initialized = true;
        }

        private void ReleaseBuffers()
        {
            _positionBuffer?.Release();
            _positionBuffer = null;

            _oldPositionBuffer?.Release();
            _oldPositionBuffer = null;

            _dataBuffer?.Release();
            _dataBuffer = null;
            
            _aliveBuffer?.Release();
            _aliveBuffer = null;
            
            _deadIndexBuffer?.Release();
            _deadIndexBuffer = null;
            
            _deadCountBuffer?.Release();
            _deadCountBuffer = null;

            _spatialIndicesBuffer?.Release();
            _spatialIndicesBuffer = null;

            _spatialOffsetsBuffer?.Release();
            _spatialOffsetsBuffer = null;
        }


        private void RunSimulation(int kernelId, float timeStep)
        {
            if (kernelId < 0)
            {
                Debug.LogError($"Kernel for ComputeShader {pheromoneComputeShader} is invalid", this);
                return;
            }

            pheromoneComputeShader.SetInt(PropertyIDs.TotalCount, _capacity);
            pheromoneComputeShader.SetFloat(PropertyIDs.HashCellSize, hashCellSize);
            
            pheromoneComputeShader.SetVector(PropertyIDs.WorldPos, transform.position);
            pheromoneComputeShader.SetMatrix(PropertyIDs.WorldMatrix, transform.localToWorldMatrix);
            pheromoneComputeShader.SetVector(PropertyIDs.SimulationSpace, simulationSpace);
            
            pheromoneComputeShader.SetFloat(PropertyIDs.Time, Time.time);
            pheromoneComputeShader.SetFloat(PropertyIDs.DeltaTime, timeStep);
            pheromoneComputeShader.SetFloat(PropertyIDs.LifeTime, lifeTime);

            pheromoneComputeShader.SetFloat(PropertyIDs.TargetDensity, targetDensity);
            pheromoneComputeShader.SetFloat(PropertyIDs.PressureMultiplier, pressureMultiplier);
            
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.PositionBuffer, _positionBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.OldPositionBuffer, _oldPositionBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.DataBuffer, _dataBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.AliveBuffer, _aliveBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.DeadIndexBuffer, _deadIndexBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.AliveIndexBuffer, _deadIndexBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.DeadCountBuffer, _deadCountBuffer);

            if (SdfShapeManager.Instance)
            {
                pheromoneComputeShader.SetBuffer(kernelId, SdfShapeManager.PropertyIDs.NodeBuffer, SdfShapeManager.Instance.NodeBuffer);
                pheromoneComputeShader.SetBuffer(kernelId, SdfShapeManager.PropertyIDs.SdfBuffer, SdfShapeManager.Instance.SdfBuffer);
                pheromoneComputeShader.SetInt(SdfShapeManager.PropertyIDs.NodeCount, SdfShapeManager.Instance.NodeCount);
            }
            
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialIndices, _spatialIndicesBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, _spatialOffsetsBuffer);
            
            pheromoneComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
        }

        public void EmitParticles(int count, Vector3 pos)
        {
            if (count <= 0)
                return;
        
            UpdateEmissionCount(count);
            
            int emissionKernel = pheromoneComputeShader.FindKernel("Emit");
            
            pheromoneComputeShader.SetFloat(PropertyIDs.Time, Time.time);
            pheromoneComputeShader.SetFloat(PropertyIDs.DeltaTime, Time.deltaTime);
            pheromoneComputeShader.SetFloat(PropertyIDs.LifeTime, lifeTime);
            
            pheromoneComputeShader.SetFloat(PropertyIDs.TargetDensity, targetDensity);
            pheromoneComputeShader.SetFloat(PropertyIDs.PressureMultiplier, pressureMultiplier);
            pheromoneComputeShader.SetVector(PropertyIDs.SpawnPos, pos);
            
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.PositionBuffer, _positionBuffer);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.OldPositionBuffer, _oldPositionBuffer);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.DataBuffer, _dataBuffer);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.AliveBuffer, _aliveBuffer);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.DeadIndexBuffer, _deadIndexBuffer);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.AliveIndexBuffer, _deadIndexBuffer);
            pheromoneComputeShader.SetBuffer(emissionKernel, PropertyIDs.DeadCountBuffer, _deadCountBuffer);
            
            pheromoneComputeShader.Dispatch(emissionKernel, Mathf.CeilToInt((float)count / THREAD_GROUP_SIZE), 1, 1);
        }

        
        private void UpdateSpatialHash()
        {
            int kernelId = pheromoneComputeShader.FindKernel("UpdateSpatialHash");

            pheromoneComputeShader.SetInt(PropertyIDs.TotalCount, _capacity);
            pheromoneComputeShader.SetFloat(PropertyIDs.HashCellSize, hashCellSize);
            
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialIndices, _spatialIndicesBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, _spatialOffsetsBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.PositionBuffer, _positionBuffer);
            pheromoneComputeShader.SetBuffer(kernelId, PropertyIDs.AliveBuffer, _aliveBuffer);
            
            pheromoneComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
        }
        
        private void UpdateEmissionCount(int count)
        {
            int kernel = pheromoneComputeShader.FindKernel("EmissionCountKernel");
            
            GraphicsBuffer.CopyCount(_deadIndexBuffer, _deadCountBuffer, 0);
            pheromoneComputeShader.SetInt(PropertyIDs.TargetEmitCount, count);
            pheromoneComputeShader.SetInt(PropertyIDs.ParticlesPerEmit, _particlesPerEmit);

            pheromoneComputeShader.SetBuffer(kernel, PropertyIDs.DeadCountBuffer, _deadCountBuffer);
            pheromoneComputeShader.Dispatch(kernel, 1, 1, 1);
        }



        private void RenderMeshes()
        {
            if (!mesh || !material)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetBuffer(PropertyIDs.PositionBuffer, _positionBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.OldPositionBuffer, _oldPositionBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.DataBuffer, _dataBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.DeadIndexBuffer, _dataBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.AliveBuffer, _aliveBuffer);

            RenderParams rp = new RenderParams(material)
            {
                camera = null,
                instanceID = GetInstanceID(),
                layer = gameObject.layer,
                lightProbeUsage = LightProbeUsage.Off,
                lightProbeProxyVolume = null,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.Off,
                worldBounds = new Bounds(transform.position, simulationSpace),
                matProps = _propertyBlock,
            };


            Graphics.RenderMeshPrimitives(in rp, mesh, 0, _capacity);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, simulationSpace);
        }
        
        
        public static class PropertyIDs
        {
            public static readonly int TotalCount              = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize            = Shader.PropertyToID("_HashCellSize");
            public static readonly int WorldPos                = Shader.PropertyToID("_WorldPos");
            public static readonly int WorldMatrix             = Shader.PropertyToID("_WorldMatrix");
            public static readonly int SimulationSpace         = Shader.PropertyToID("_SimulationSpace");
            public static readonly int Time                    = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime               = Shader.PropertyToID("_DeltaTime");
            public static readonly int PositionBuffer          = Shader.PropertyToID("_PositionBuffer");
            public static readonly int OldPositionBuffer       = Shader.PropertyToID("_OldPositionBuffer");
            public static readonly int DataBuffer              = Shader.PropertyToID("_DataBuffer");
            public static readonly int DeadIndexBuffer         = Shader.PropertyToID("_DeadIndexBuffer");
            public static readonly int AliveIndexBuffer        = Shader.PropertyToID("_AliveIndexBuffer");
            public static readonly int AliveBuffer        = Shader.PropertyToID("_AliveBuffer");
            public static readonly int LifeTime        = Shader.PropertyToID("_LifeTime");
            
            public static readonly int SpawnPos        = Shader.PropertyToID("_SpawnPos");
            
            
            public static readonly int DeadCountBuffer        = Shader.PropertyToID("_DeadCountBuffer");
            public static readonly int TargetEmitCount        = Shader.PropertyToID("_TargetEmitCount");
            public static readonly int ParticlesPerEmit        = Shader.PropertyToID("_ParticlesPerEmit");
            
            
            public static readonly int TargetDensity           = Shader.PropertyToID("_TargetDensity");
            public static readonly int PressureMultiplier      = Shader.PropertyToID("_PressureMultiplier");
            
            public static readonly int SpatialIndices              = Shader.PropertyToID("_PheromoneSpatialIndices");
            public static readonly int SpatialOffsets              = Shader.PropertyToID("_PheromoneSpatialOffsets");
        }
    }
}
