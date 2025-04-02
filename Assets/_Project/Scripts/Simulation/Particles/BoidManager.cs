using Beakstorm.ComputeHelpers;
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
        private float _speed;

        [SerializeField] 
        private BoidStateSettings neutralState;
        [SerializeField] 
        private BoidStateSettings exposedState;

        [Header("Collision")]
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

        public ComputeBuffer SpatialIndicesBuffer => _spatialIndicesBuffer;
        public ComputeBuffer SpatialOffsetsBuffer => _spatialOffsetsBuffer;
        public GraphicsBuffer PositionBuffer => _positionBuffer;
        public GraphicsBuffer OldPositionBuffer => _oldPositionBuffer;
        public int Capacity => _capacity;
        public float HashCellSize => _hashCellSize;

        
        private void Start()
        {
            _capacity = maxCount;
            InitializeBuffers();
        }

        private void Update()
        {
            if (_initialized)
            {
                UpdateSpatialHash();
                GPUBitonicMergeSort.SortAndCalculateOffsets(_sortShader, _spatialIndicesBuffer, _spatialOffsetsBuffer);

                int updateKernel = _boidComputeShader.FindKernel("FlockingCS");
                RunSimulation(updateKernel, Time.deltaTime);
                
                RenderMeshes();
            }
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }
        

        private void InitializeBuffers()
        {
            ReleaseBuffers();

            _positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _oldPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _velocityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _normalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 3 * sizeof(float));
            _dataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, 1 * sizeof(uint));

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
            if (kernelId < 0)
            {
                Debug.LogError($"Kernel for ComputeShader {_boidComputeShader} is invalid", this);
                return;
            }

            _boidComputeShader.SetInt("_NumBoids", _capacity);
            _boidComputeShader.SetFloat("_HashCellSize", _hashCellSize);

            _boidComputeShader.SetVector("_WorldPos", transform.position);
            _boidComputeShader.SetMatrix("_WorldMatrix", transform.localToWorldMatrix);

            _boidComputeShader.SetFloat("_Time", Time.time);
            _boidComputeShader.SetFloat("_DeltaTime", timeStep);

            _boidComputeShader.SetFloat("_Speed", _speed);

            _boidComputeShader.SetFloat("_FloorYLevel", _floorYLevel);
            _boidComputeShader.SetFloat("_CollisionBounce", _collisionBounce);
            _boidComputeShader.SetFloat("_Gravity", _gravity);

            _boidComputeShader.SetFloat("_CollisionRadius", _collisionRadius);

            _boidComputeShader.SetVector("_NeutralStateSettings", neutralState);
            _boidComputeShader.SetVector("_ExposedStateSettings", exposedState);

            _boidComputeShader.SetBuffer(kernelId, "_BoidPositionBuffer", _positionBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_BoidOldPositionBuffer", _oldPositionBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_BoidVelocityBuffer", _velocityBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_BoidNormalBuffer", _normalBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_BoidDataBuffer", _dataBuffer);

            _boidComputeShader.SetBuffer(kernelId, "_SpatialIndices", _spatialIndicesBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_SpatialOffsets", _spatialOffsetsBuffer);

            _boidComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
        }


        private void UpdateSpatialHash()
        {
            int kernelId = _boidComputeShader.FindKernel("UpdateSpatialHash");

            _boidComputeShader.SetInt("_NumBoids", _capacity);
            _boidComputeShader.SetFloat("_HashCellSize", _hashCellSize);

            _boidComputeShader.SetBuffer(kernelId, "_SpatialIndices", _spatialIndicesBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_SpatialOffsets", _spatialOffsetsBuffer);
            _boidComputeShader.SetBuffer(kernelId, "_BoidPositionBuffer", _positionBuffer);

            _boidComputeShader.Dispatch(kernelId, _capacity / THREAD_GROUP_SIZE, 1, 1);
        }


        private void RenderMeshes()
        {
            if (!mesh || !material)
                return;
            
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetBuffer("_PositionBuffer", _positionBuffer);
            _propertyBlock.SetBuffer("_OldPositionBuffer", _oldPositionBuffer);
            _propertyBlock.SetBuffer("_VelocityBuffer", _velocityBuffer);
            _propertyBlock.SetBuffer("_NormalBuffer", _normalBuffer);
            
            RenderParams rp = new RenderParams(material)
            {
                camera = null,
                instanceID = GetInstanceID(),
                layer = gameObject.layer,
                lightProbeUsage = LightProbeUsage.Off,
                lightProbeProxyVolume = null,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.On,
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 100), 
                matProps = _propertyBlock,
            };
            
            
            Graphics.RenderMeshPrimitives(in rp, mesh, 0, _capacity);
        }
    }
}