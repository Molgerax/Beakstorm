using System;
using Beakstorm.ComputeHelpers;
using Beakstorm.Pausing;
using Beakstorm.Simulation.Collisions.SDF;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    /// <summary>
    /// Pulled heavily from: https://github.com/abecombe/VFXGraphStudy/blob/main/Assets/Scenes/Flocking/Scripts/Flocking.cs
    /// </summary>
    public class BoidManager : MonoBehaviour
    {
        private const int THREAD_GROUP_SIZE = 256;

        [SerializeField] 
        private int maxCount = 256;
        [SerializeField]
        private ComputeShader _boidComputeShader;

        [Header("Rendering")] 
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        
        [Header("Boid Settings")]
        [SerializeField] 
        private BoidStateSettings neutralState;
        [SerializeField] 
        private BoidStateSettings exposedState;

        [Header("Collision")]
        [SerializeField]
        private Vector3 simulationSpace = Vector3.one;
        [SerializeField]
        private float _floorYLevel = 0f;
        [SerializeField]
        private float _gravity = -9.8f;
        [SerializeField, Range(0f, 1f)]
        private float _collisionBounce = 0f;
        [SerializeField, Range(0f, 1f)]
        private float _collisionRadius = 0.1f;

        [Header("Bitonic Merge Sort")]
        [SerializeField]
        private ComputeShader _sortShader;

        [SerializeField]
        [Range(0.1f, 10f)]
        private float _hashCellSize = 1f;

        private ComputeBuffer _spatialIndicesBuffer;
        private ComputeBuffer _spatialOffsetsBuffer;
        

        private GraphicsBuffer _positionBuffer;        
        private GraphicsBuffer _oldPositionBuffer;
        private GraphicsBuffer _velocityBuffer;
        private GraphicsBuffer _normalBuffer;
        private GraphicsBuffer _dataBuffer;

        private MaterialPropertyBlock _propertyBlock;
        
        private int _capacity;
        private bool _initialized;

        public bool Initialized => _initialized;

        public static BoidManager Instance;

        public ComputeBuffer SpatialIndicesBuffer => _spatialIndicesBuffer;
        public ComputeBuffer SpatialOffsetsBuffer => _spatialOffsetsBuffer;
        public GraphicsBuffer PositionBuffer => _positionBuffer;
        public GraphicsBuffer OldPositionBuffer => _oldPositionBuffer;
        public int Capacity => _capacity;
        public float HashCellSize => _hashCellSize;

        private Vector4 _whistleSource;
        
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
                UpdateSpatialHash();
                GPUBitonicMergeSort.SortAndCalculateOffsets(_sortShader, _spatialIndicesBuffer, _spatialOffsetsBuffer);

                DecayWhistle(Time.deltaTime);
             
                int updateKernel = _boidComputeShader.FindKernel("Update");
                RunSimulation(updateKernel, Time.deltaTime);
                
                RenderMeshes();
            }
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
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

            _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _oldPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _dataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 4 * sizeof(uint));

            // Spatial Hash Buffers
            _spatialIndicesBuffer = new ComputeBuffer(_capacity, 3 * sizeof(int), ComputeBufferType.Structured);
            _spatialOffsetsBuffer = new ComputeBuffer(_capacity, 1 * sizeof(int), ComputeBufferType.Structured);

            int initKernel = _boidComputeShader.FindKernel("Init");
            RunSimulation(initKernel, Time.deltaTime);

            _initialized = true;
        }

        private void ReleaseBuffers()
        {
            _positionBuffer?.Release();
            _positionBuffer = null;

            _oldPositionBuffer?.Release();
            _oldPositionBuffer = null;

            _velocityBuffer?.Release();
            _velocityBuffer = null;

            _normalBuffer?.Release();
            _normalBuffer = null;

            _dataBuffer?.Release();
            _dataBuffer = null;

            _spatialIndicesBuffer?.Release();
            _spatialIndicesBuffer = null;

            _spatialOffsetsBuffer?.Release();
            _spatialOffsetsBuffer = null;
        }


        private void RunSimulation(int kernelId, float timeStep)
        {
            if (PauseManager.IsPaused)
                return;
            if (timeStep == 0)
                return;
            
            if (kernelId < 0)
            {
                Debug.LogError($"Kernel for ComputeShader {_boidComputeShader} is invalid", this);
                return;
            }

            _boidComputeShader.SetInt(PropertyIDs.TotalCount, _capacity);
            _boidComputeShader.SetFloat(PropertyIDs.HashCellSize, _hashCellSize);

            _boidComputeShader.SetVector(PropertyIDs.WorldPos, transform.position);
            _boidComputeShader.SetMatrix(PropertyIDs.WorldMatrix, transform.localToWorldMatrix);
            _boidComputeShader.SetVector(PropertyIDs.SimulationSpace, simulationSpace);
            _boidComputeShader.SetVector(PropertyIDs.WhistleSource, _whistleSource);

            _boidComputeShader.SetFloat(PropertyIDs.Time, Time.time);
            _boidComputeShader.SetFloat(PropertyIDs.DeltaTime, timeStep);
            
            _boidComputeShader.SetFloat(PropertyIDs.FloorYLevel, _floorYLevel);
            _boidComputeShader.SetFloat(PropertyIDs.CollisionBounce, _collisionBounce);
            _boidComputeShader.SetFloat(PropertyIDs.Gravity, _gravity);
            _boidComputeShader.SetFloat(PropertyIDs.CollisionRadius, _collisionRadius);

            _boidComputeShader.SetBoidStateSettings("_Neutral", neutralState);
            _boidComputeShader.SetBoidStateSettings("_Exposed", exposedState);

            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PositionBuffer, _positionBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.OldPositionBuffer, _oldPositionBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.VelocityBuffer, _velocityBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.NormalBuffer, _normalBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.DataBuffer, _dataBuffer);

            if (SdfShapeManager.Instance)
            {
                _boidComputeShader.SetBuffer(kernelId, SdfShapeManager.PropertyIDs.NodeBuffer, SdfShapeManager.Instance.NodeBuffer);
                _boidComputeShader.SetBuffer(kernelId, SdfShapeManager.PropertyIDs.SdfBuffer, SdfShapeManager.Instance.SdfBuffer);
                _boidComputeShader.SetInt(SdfShapeManager.PropertyIDs.NodeCount, SdfShapeManager.Instance.NodeCount);
            }

            if (PheromoneManager.Instance)
            {
                PheromoneManager p = PheromoneManager.Instance;

                _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneSpatialIndices, p.SpatialIndicesBuffer);
                _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneSpatialOffsets, p.SpatialOffsetsBuffer);
                _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromonePositionBuffer, p.PositionBuffer);
                _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneDataBuffer, p.DataBuffer);
                _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PheromoneAliveBuffer, p.AliveBuffer);
                _boidComputeShader.SetFloat(PropertyIDs.PheromoneHashCellSize, p.HashCellSize);
                _boidComputeShader.SetInt(PropertyIDs.PheromoneTotalCount, p.Capacity);
            }
            
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialIndices, _spatialIndicesBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, _spatialOffsetsBuffer);

            _boidComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
        }


        private void UpdateSpatialHash()
        {
            int kernelId = _boidComputeShader.FindKernel("UpdateSpatialHash");

            _boidComputeShader.SetInt(PropertyIDs.TotalCount, _capacity);
            _boidComputeShader.SetFloat(PropertyIDs.HashCellSize, _hashCellSize);

            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialIndices, _spatialIndicesBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, _spatialOffsetsBuffer);
            _boidComputeShader.SetBuffer(kernelId, PropertyIDs.PositionBuffer, _positionBuffer);

            _boidComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
        }


        private void RenderMeshes()
        {
            if (!mesh || !material)
                return;
            
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetBuffer(PropertyIDs.PositionBuffer, _positionBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.OldPositionBuffer, _oldPositionBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.VelocityBuffer, _velocityBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.NormalBuffer, _normalBuffer);
            _propertyBlock.SetBuffer(PropertyIDs.DataBuffer, _dataBuffer);
            
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

        public static class PropertyIDs
        {
            public static readonly int TotalCount              = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize            = Shader.PropertyToID("_HashCellSize");
            public static readonly int WorldPos                = Shader.PropertyToID("_WorldPos");
            public static readonly int WorldMatrix             = Shader.PropertyToID("_WorldMatrix");
            public static readonly int SimulationSpace         = Shader.PropertyToID("_SimulationSpace");
            public static readonly int Time                    = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime               = Shader.PropertyToID("_DeltaTime");
            public static readonly int FloorYLevel             = Shader.PropertyToID("_FloorYLevel");
            public static readonly int CollisionBounce         = Shader.PropertyToID("_CollisionBounce");
            public static readonly int Gravity                 = Shader.PropertyToID("_Gravity");
            public static readonly int CollisionRadius         = Shader.PropertyToID("_CollisionRadius");
            public static readonly int PositionBuffer          = Shader.PropertyToID("_PositionBuffer");
            public static readonly int OldPositionBuffer       = Shader.PropertyToID("_OldPositionBuffer");
            public static readonly int VelocityBuffer          = Shader.PropertyToID("_VelocityBuffer");
            public static readonly int NormalBuffer            = Shader.PropertyToID("_NormalBuffer");
            public static readonly int DataBuffer              = Shader.PropertyToID("_DataBuffer");
            
            public static readonly int WhistleSource         = Shader.PropertyToID("_WhistleSource");
            
            public static readonly int SpatialIndices              = Shader.PropertyToID("_BoidSpatialIndices");
            public static readonly int SpatialOffsets              = Shader.PropertyToID("_BoidSpatialOffsets");
            public static readonly int PheromoneSpatialIndices              = Shader.PropertyToID("_PheromoneSpatialIndices");
            public static readonly int PheromoneSpatialOffsets              = Shader.PropertyToID("_PheromoneSpatialOffsets");
            
            public static readonly int PheromonePositionBuffer              = Shader.PropertyToID("_PheromonePositionBuffer");
            public static readonly int PheromoneDataBuffer              = Shader.PropertyToID("_PheromoneDataBuffer");
            public static readonly int PheromoneAliveBuffer              = Shader.PropertyToID("_PheromoneAliveBuffer");
            public static readonly int PheromoneHashCellSize              = Shader.PropertyToID("_PheromoneHashCellSize");
            public static readonly int PheromoneTotalCount              = Shader.PropertyToID("_PheromoneTotalCount");
        }
    }
}