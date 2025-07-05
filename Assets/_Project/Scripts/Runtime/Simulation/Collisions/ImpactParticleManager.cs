using System;
using Beakstorm.Pausing;
using Beakstorm.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Collisions
{
    public class ImpactParticleManager : MonoBehaviour
    {
        private const int THREAD_GROUP_SIZE = 256;
        
        [SerializeField] private int impactCount = 16384;
        [SerializeField] private ComputeShader compute;

        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        
        private int _impactCount;

        
        private GraphicsBuffer _impactBufferPing;
        private GraphicsBuffer _impactBufferPong;
        private GraphicsBuffer _impactArgsBuffer;
        private bool _swapBuffers;

        private MaterialPropertyBlock _propertyBlock;
        
        public GraphicsBuffer ImpactBufferWrite => _swapBuffers ? _impactBufferPong : _impactBufferPing;
        public GraphicsBuffer ImpactBufferRead => !_swapBuffers ? _impactBufferPong : _impactBufferPing;

        public int ImpactCount => _impactCount;
        
        public bool Initialized { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Update()
        {
            if (!PauseManager.IsPaused)
            {
                Tick(SimulationTime.DeltaTime);
            }
            RenderMeshes();
        }

        private void Initialize()
        {
            _impactCount = impactCount;
            
            _impactBufferPing = new GraphicsBuffer(GraphicsBuffer.Target.Counter, _impactCount, sizeof(float) * 8);
            _impactBufferPong = new GraphicsBuffer(GraphicsBuffer.Target.Counter, _impactCount, sizeof(float) * 8);

            _impactBufferPing.SetCounterValue(0);
            _impactBufferPong.SetCounterValue(0);
            
            _impactArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            GraphicsBuffer.IndirectDrawIndexedArgs[] indexedArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            indexedArgs[0].indexCountPerInstance = mesh.GetIndexCount(0);
            _impactArgsBuffer.SetData(indexedArgs);

            Initialized = true;
        }


        private void Release()
        {
            _impactBufferPing?.Dispose();
            _impactBufferPong?.Dispose();
            _impactArgsBuffer?.Dispose();

            Initialized = false;
        }

        private void Tick(float deltaTime)
        {
            CopyWriteCount();
            SwapBuffers();
            ClearWrite();
            UpdateImpacts(deltaTime);
        }

        private void UpdateImpacts(float deltaTime)
        {
            int kernel = compute.FindKernel("Update");
            
            compute.SetBuffer(kernel, PropertyIDs.ImpactBufferWrite, ImpactBufferWrite);
            compute.SetBuffer(kernel, PropertyIDs.ImpactBufferRead, ImpactBufferRead);
            compute.SetBuffer(kernel, PropertyIDs.ImpactArgsBuffer, _impactArgsBuffer);
            
            compute.SetInt(PropertyIDs.ImpactCount, ImpactCount);
            compute.SetFloat(PropertyIDs.DeltaTime, deltaTime);
            
            compute.Dispatch(kernel, _impactCount / THREAD_GROUP_SIZE, 1, 1);
            CopyWriteCount();
        }
        
        private void ClearWrite()
        {
            int kernel = compute.FindKernel("Clear");
            
            compute.SetBuffer(kernel, PropertyIDs.ImpactBufferWrite, ImpactBufferWrite);
            compute.SetInt(PropertyIDs.ImpactCount, ImpactCount);
            
            compute.Dispatch(kernel, _impactCount / THREAD_GROUP_SIZE, 1, 1);
            ImpactBufferWrite.SetCounterValue(0);
        }

        private void CopyWriteCount()
        {
            GraphicsBuffer.CopyCount(ImpactBufferWrite, _impactArgsBuffer, 1 * sizeof(uint));
        }
        
        private void SwapBuffers() => _swapBuffers = !_swapBuffers;

        public void SetImpactBuffer(ComputeShader cs, int kernel)
        {
            cs.SetBuffer(kernel, PropertyIDs.ImpactBufferWrite, ImpactBufferWrite);
            cs.SetInt(PropertyIDs.ImpactCount, ImpactCount);
        }
        
        private void RenderMeshes()
        {
            if (!mesh || !material)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetBuffer(PropertyIDs.ImpactBuffer, ImpactBufferWrite);

            RenderParams rp = new RenderParams(material)
            {
                camera = null,
                instanceID = GetInstanceID(),
                layer = gameObject.layer,
                lightProbeUsage = LightProbeUsage.Off,
                lightProbeProxyVolume = null,
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.Off,
                worldBounds = new Bounds(transform.position, Vector3.one * 1024),
                matProps = _propertyBlock,
            };

            
            Graphics.RenderMeshIndirect(in rp, mesh, _impactArgsBuffer);
        }
        

        private static class PropertyIDs
        {
            public static readonly int ImpactBufferWrite = Shader.PropertyToID("_ImpactBufferWrite");
            public static readonly int ImpactBufferRead = Shader.PropertyToID("_ImpactBufferRead");
            public static readonly int ImpactBuffer = Shader.PropertyToID("_ImpactBuffer");

            public static readonly int ImpactCount = Shader.PropertyToID("_ImpactCount");
            public static readonly int ImpactArgsBuffer = Shader.PropertyToID("_ImpactArgsBuffer");
            
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
        }
    }
}
