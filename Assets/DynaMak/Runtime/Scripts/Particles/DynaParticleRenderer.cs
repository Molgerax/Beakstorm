using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DynaMak.Particles
{
    public class DynaParticleRenderer : MonoBehaviour
    {
        #region Serialize Fields

        [Header("Render Settings")] 
        [SerializeField] private bool enable;
        [SerializeField] private Vector3 renderBoundsSize = Vector3.one;
        [SerializeField] private Material material;
        [SerializeField] private Material[] materials;
        private bool useGeometryShader = false;


        [Header("References")]
        [SerializeField] private DynaParticleComponent dynaParticleComponent;

        #endregion

        #region Private Fields

        private DynaParticle _dynaParticle;

        private ComputeBuffer _gpuInstancingArgsBuffer;
        private uint[] _gpuInstancingArgs = new uint[] {0, 0, 0, 0, 0};
        
        private Mesh _meshToRender; 
        private MaterialPropertyBlock _propBlock;
        private Bounds _renderBounds;


        private GraphicsBuffer _triangleRenderBuffer;

        private GraphicsBuffer _vertexRenderBuffer;
        private GraphicsBuffer _indexRenderBuffer;

        #endregion


        #region Shader Property IDs

        private readonly int _triangleBufferID = Shader.PropertyToID("_TriangleBufferShader");
        private readonly int _particleBufferID = Shader.PropertyToID("_ParticleBufferShader");
        private readonly int _submeshID = Shader.PropertyToID("_SubMeshId");

        #endregion


        #region Mono Methods

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            
            SetupGpuInstancingArgs();
            SetupPointMesh();
        }

        private void Start()
        {
            GetDynaParticleComponent();
        }


        private void LateUpdate()
        {
            if (enable)
            {
                BoundsToTransform();
                DrawParticles();
            }
        }

        private void OnDestroy()
        {
            Release();
        }

        #endregion

        
        
        #region Private Methods

        protected virtual void SetupPointMesh()
        {
            _meshToRender = new Mesh
            {
                vertices = new[] {new Vector3(0, 0),},
                colors = new[] {new Color(0, 0, 0, 0),},
                normals = new[] {new Vector3(0, 1, 0),}
            };
            _meshToRender.SetIndices(new[] { 0 }, MeshTopology.Points, 0);

        }

        protected virtual void SetupGpuInstancingArgs()
        {
            _gpuInstancingArgsBuffer = new ComputeBuffer(1, _gpuInstancingArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }
        
        
        protected virtual void DrawParticles()
        {
            if (!useGeometryShader) DrawProcedural();
            else DrawInstanced();
        }

        /// <summary>
        /// Draws the particles using a generic procedural shader, which uses the
        /// RenderTriangleBuffer. This means the Particle compute shader is responsible
        /// for creating its own mesh. 
        /// </summary>
        private void DrawProcedural()
        {
            _gpuInstancingArgs[0] = 3;
            _gpuInstancingArgsBuffer.SetData(_gpuInstancingArgs);
            _dynaParticle.CopyTriCountToBuffer(_gpuInstancingArgsBuffer, 4);
            
            _propBlock.Clear();
            _propBlock.SetBuffer(_triangleBufferID, _dynaParticle.RenderTriangleBuffer);

            if (materials.Length == 0)
            {
                Graphics.DrawProceduralIndirect(material, _renderBounds, MeshTopology.Triangles, _gpuInstancingArgsBuffer, 0, 
                null, _propBlock, ShadowCastingMode.On, true, gameObject.layer);
                
                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                _propBlock.SetInt(_submeshID, i);
                Graphics.DrawProceduralIndirect(materials[i], _renderBounds, MeshTopology.Triangles, _gpuInstancingArgsBuffer, 0, 
                    null, _propBlock, ShadowCastingMode.On, true, gameObject.layer);
            }
        }

        /// <summary>
        /// Draws the particles using a custom geometry shader which needs to be tailored to the
        /// Particle compute shader, as it immediately works with the
        /// ParticleBuffer. Also, geometry shaders are not as universal and performant
        /// as the procedural method.
        /// </summary>
        private void DrawInstanced()
        {
            _gpuInstancingArgs[0] = (_meshToRender is not null) ? _meshToRender.GetIndexCount(0) : 0;
            _gpuInstancingArgs[1] = (uint) _dynaParticle.GetBufferSize();
            _gpuInstancingArgsBuffer.SetData(_gpuInstancingArgs);
            
            
            _propBlock.Clear();
            _propBlock.SetBuffer(_particleBufferID, _dynaParticle.ParticleBuffer);

            Graphics.DrawMeshInstancedIndirect(_meshToRender, 0, material, _renderBounds, _gpuInstancingArgsBuffer, 0, _propBlock);

        }


        protected virtual void Release()
        {
            _gpuInstancingArgsBuffer?.Release();
        }
        
        private void GetDynaParticleComponent()
        {
            if(_dynaParticle != null) return;
            
            if (dynaParticleComponent == null)
            {
                if (TryGetComponent(out dynaParticleComponent))
                {
                    _dynaParticle = dynaParticleComponent.ParticleSystem;
                }
                else
                {
                    Debug.LogError("No DynaParticleComponent found to render!");
                }
            }
            else _dynaParticle = dynaParticleComponent.ParticleSystem;
        }


        private void BoundsToTransform()
        {
            _renderBounds.center = transform.position;
            _renderBounds.size = renderBoundsSize;
        }

        #endregion
    }
}
