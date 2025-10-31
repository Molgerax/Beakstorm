using Beakstorm.ComputeHelpers;
using Beakstorm.Pausing;
using Beakstorm.SceneManagement;
using Beakstorm.Simulation.Collisions;
using Beakstorm.Simulation.Collisions.SDF;
using Beakstorm.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    public class BoidGridManager : MonoBehaviour, IGridParticleSimulation
    {
        [SerializeField] 
        private int maxCount = 256;
        [SerializeField]
        private ComputeShader boidComputeShader;

        [Header("Rendering")] 
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        
        [Header("Boid Settings")]
        [SerializeField] 
        private BoidStateSettings neutralState;
        [SerializeField] 
        private BoidStateSettings exposedState;

        [Header("Noise")] 
        [SerializeField] private float noiseStrength = 1;
        [SerializeField] private float noiseScale = 1;

        [Header("Collision")]
        [SerializeField]
        private Vector3 simulationSpace = Vector3.one;

        [SerializeField, Min(0f)] private float avoidDistance = 10;

        [SerializeField] 
        private ImpactParticleManager impact;
        
        [Header("Spatial Subdivision")]
        [SerializeField]
        [Min(0.1f)]
        private float hashCellSize = 1f;

        [SerializeField]
        [Range(0.25f, 2f)] private float hashCellRatio = 1;
        
        [SerializeField]
        private ComputeShader cellShader;

        private GraphicsBuffer _spatialIndicesBuffer;
        private GraphicsBuffer _spatialOffsetsBuffer;
        

        private GraphicsBuffer _boidBuffer;
        private GraphicsBuffer _boidBufferRead;

        private MaterialPropertyBlock _propertyBlock;
        
        private int _capacity;
        private bool _initialized;

        public bool Initialized => _initialized;

        public static BoidGridManager Instance;

        public GraphicsBuffer GridOffsetsBuffer => _hash?.GridOffsetBuffer;
        public int AgentCount => _capacity;
        public float CellSize => GetHashCellSize();
        public Vector3 SimulationCenter => transform.position;
        public Vector3 SimulationSize => simulationSpace;

        private Vector4 _whistleSource;

        private SpatialGrid _hash;
        public SpatialGrid Hash => _hash;

        private bool _swapBuffers;

        public void SwapBuffers() => _swapBuffers = !_swapBuffers;
        
        public GraphicsBuffer AgentBufferWrite => _swapBuffers ? _boidBufferRead : _boidBuffer;
        public GraphicsBuffer AgentBufferRead => _swapBuffers ? _boidBuffer : _boidBufferRead;
        public int AgentBufferStride => 12;

        private float GetHashCellSize()
        {
            if (!neutralState && !exposedState)
                return hashCellSize;
            
            float largest = 0;
            if (neutralState) largest = Mathf.Max(largest, neutralState.LargestRadius);
            if (exposedState) largest = Mathf.Max(largest, exposedState.LargestRadius);

            hashCellSize = largest * hashCellRatio;
            return hashCellSize;
        }
        
        private void Awake()
        {
            Instance = this;
            _capacity = maxCount;
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
                    DecayWhistle(SimulationTime.DeltaTime);
                    
                    int updateKernel = boidComputeShader.FindKernel("Update");
                    RunSimulation(updateKernel, SimulationTime.DeltaTime);

                    _hash.Update();
                    SwapBuffers();
                }

                RenderMeshes();
            }
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
            Instance = null;
        }
        
        
        public void RefreshWhistle(Vector3 position, float duration)
        {
            _whistleSource = position;
            _whistleSource.w = duration;
        }

        private void DecayWhistle(float deltaTime)
        {
            _whistleSource.w = Mathf.Max(0, _whistleSource.w - deltaTime);
        }
        

        [ContextMenu("Re-Init")]
        private void InitializeBuffers()
        {
            _whistleSource = Vector4.zero;
            _capacity = maxCount;
            ReleaseBuffers();

            _swapBuffers = false;
            
            _boidBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, AgentBufferStride * sizeof(float));
            _boidBufferRead = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, AgentBufferStride * sizeof(float));

            _hash = new SpatialGrid(cellShader, this);
            
            int initKernel = boidComputeShader.FindKernel("Init");
            RunSimulation(initKernel, SimulationTime.DeltaTime);
            RunSimulation(initKernel, SimulationTime.DeltaTime);

            _initialized = true;
        }

        private void ReleaseBuffers()
        {
            _boidBuffer?.Release();
            _boidBuffer = null;

            _boidBufferRead?.Release();
            _boidBufferRead = null;

            _hash?.Dispose();
        }


        private void RunSimulation(int kernelId, float timeStep)
        {
            if (PauseManager.IsPaused)
                return;
            
            if (kernelId < 0)
            {
                Debug.LogError($"Kernel for ComputeShader {boidComputeShader} is invalid", this);
                return;
            }

            boidComputeShader.SetInt(PropertyIDs.AgentCount, _capacity);
            boidComputeShader.SetFloat(PropertyIDs.HashCellSize, hashCellSize);

            boidComputeShader.SetVector(PropertyIDs.WorldPos, transform.position);
            boidComputeShader.SetMatrix(PropertyIDs.WorldMatrix, transform.localToWorldMatrix);
            boidComputeShader.SetVector(PropertyIDs.SimulationCenter, SimulationCenter);
            boidComputeShader.SetVector(PropertyIDs.SimulationSize, SimulationSize);
            boidComputeShader.SetVector(PropertyIDs.WhistleSource, _whistleSource);

            boidComputeShader.SetFloat(PropertyIDs.Time, Time.time);
            boidComputeShader.SetFloat(PropertyIDs.DeltaTime, timeStep);

            Bounds spawnBounds = BoidSpawnArea.SpawnBounds;
            boidComputeShader.SetVector(PropertyIDs.SpawnBoundsCenter, spawnBounds.center);
            boidComputeShader.SetVector(PropertyIDs.SpawnBoundsSize, spawnBounds.size);

            boidComputeShader.SetFloat(PropertyIDs.AvoidDistance, avoidDistance);
            boidComputeShader.SetFloat(PropertyIDs.NoiseStrength, noiseStrength);
            boidComputeShader.SetFloat(PropertyIDs.NoiseScale, noiseScale);

            boidComputeShader.SetBoidStateSettings("_Neutral", neutralState);
            boidComputeShader.SetBoidStateSettings("_Exposed", exposedState);

            boidComputeShader.SetBuffer(kernelId, PropertyIDs.BoidBuffer, AgentBufferWrite);
            boidComputeShader.SetBuffer(kernelId, PropertyIDs.BoidBufferRead, AgentBufferRead);

            if (SdfShapeManager.Instance)
            {
                SdfShapeManager.Instance.SetShaderProperties(boidComputeShader, kernelId);
            }

            if (PheromoneGridManager.Instance)
            {
                PheromoneGridManager p = PheromoneGridManager.Instance;

                boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneSpatialOffsets, p.Hash.GridOffsetBuffer);
                boidComputeShader.SetFloat(PropertyIDs.PheromoneHashCellSize, p.CellSize);
                boidComputeShader.SetFloat(PropertyIDs.PheromoneSmoothingRadius, p.SmoothingRadius);
                boidComputeShader.SetInt(PropertyIDs.PheromoneTotalCount, p.AgentCount);
                
                boidComputeShader.SetVector(PropertyIDs.PheromoneCenter, p.SimulationCenter);
                boidComputeShader.SetVector(PropertyIDs.PheromoneSize, p.SimulationSize);
                boidComputeShader.SetInts(PropertyIDs.PheromoneCellDimensions, p.Hash.Dimensions);
                
                boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneBuffer, p.AgentBufferRead);
                boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneArgs, p.InstancedArgsBuffer);


                if (p.UseAttractors)
                {
                    boidComputeShader.SetBuffer(kernelId, PropertyIDs.AttractorBuffer, p.AttractorBuffer);
                    boidComputeShader.SetInt(PropertyIDs.AttractorCount, p.AttractorCount);
                }
                else
                    boidComputeShader.SetInt(PropertyIDs.AttractorCount, 0);
            }

            if (impact)
            {
                impact.SetImpactBuffer(boidComputeShader, kernelId);
            }
            
            boidComputeShader.SetInts(PropertyIDs.Dimensions, _hash.Dimensions);
            boidComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, _hash.GridOffsetBuffer);

            _hash.SetShaderProperties(boidComputeShader);
            
            boidComputeShader.DispatchExact(kernelId, _capacity);
            
            SwapBuffers();
        }
        

        private void RenderMeshes()
        {
            if (!mesh || !material)
                return;
            
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetBuffer(PropertyIDs.BoidBuffer, AgentBufferRead);
            
            RenderParams rp = new RenderParams(material)
            {
                camera = null,
                instanceID = GetInstanceID(),
                layer = gameObject.layer,
                lightProbeUsage = LightProbeUsage.Off,
                lightProbeProxyVolume = null,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.On,
                worldBounds = new Bounds(transform.position, simulationSpace * 100), 
                matProps = _propertyBlock,
            };
            
            
            Graphics.RenderMeshPrimitives(in rp, mesh, 0, _capacity);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, simulationSpace);
        }

        private static class PropertyIDs
        {
            public static readonly int AgentCount = Shader.PropertyToID("_AgentCount");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            public static readonly int WorldPos = Shader.PropertyToID("_WorldPos");
            public static readonly int WorldMatrix = Shader.PropertyToID("_WorldMatrix");
            public static readonly int SimulationCenter = Shader.PropertyToID("_SimulationCenter");
            public static readonly int SimulationSize = Shader.PropertyToID("_SimulationSize");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            
            public static readonly int BoidBuffer = Shader.PropertyToID("_BoidBuffer");
            public static readonly int BoidBufferRead = Shader.PropertyToID("_BoidBufferRead");
            
            public static readonly int WhistleSource = Shader.PropertyToID("_WhistleSource");
            public static readonly int Dimensions = Shader.PropertyToID("_Dimensions");
            
            public static readonly int AvoidDistance = Shader.PropertyToID("_AvoidDistance");
            public static readonly int NoiseStrength = Shader.PropertyToID("_NoiseStrength");
            public static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
            
            public static readonly int SpatialOffsets = Shader.PropertyToID("_BoidSpatialOffsets");
            
            public static readonly int PheromoneSpatialOffsets = Shader.PropertyToID("_PheromoneSpatialOffsets");
            public static readonly int PheromoneHashCellSize = Shader.PropertyToID("_PheromoneHashCellSize");
            public static readonly int PheromoneTotalCount = Shader.PropertyToID("_PheromoneTotalCount");
            public static readonly int PheromoneSmoothingRadius = Shader.PropertyToID("_PheromoneSmoothingRadius");
            public static readonly int PheromoneBuffer = Shader.PropertyToID("_PheromoneBuffer");
            public static readonly int PheromoneCenter = Shader.PropertyToID("_PheromoneCenter");
            public static readonly int PheromoneSize = Shader.PropertyToID("_PheromoneSize");
            public static readonly int PheromoneCellDimensions = Shader.PropertyToID("_PheromoneCellDimensions");
            public static readonly int PheromoneArgs = Shader.PropertyToID("_PheromoneArgs");

            public static readonly int AttractorBuffer = Shader.PropertyToID("_AttractorBuffer");
            public static readonly int AttractorCount = Shader.PropertyToID("_AttractorCount");

            public static readonly int SpawnBoundsCenter = Shader.PropertyToID("_SpawnBoundsCenter");
            public static readonly int SpawnBoundsSize = Shader.PropertyToID("_SpawnBoundsSize");
        }
    }
}