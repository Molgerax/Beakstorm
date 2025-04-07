using System;
using UnityEngine;

namespace DynaMak.Particles
{
    public class DestructionParticleRenderer : MonoBehaviour
    {
        #region Serialize Fields

        [Header("Render Settings")] 
        [SerializeField] private bool enable;
        [SerializeField] private Bounds renderBounds = new (Vector3.zero, Vector3.one);
        [SerializeField] private Material material;
        private bool useGeometryShader = false;
        
        [Header("References")]
        [SerializeField] private DestructionParticleComponent dynaParticleComponent;

        #endregion

        #region Private Fields

        private DynaParticle _dynaParticle;

        private ComputeBuffer _gpuInstancingArgsBuffer;
        private uint[] _gpuInstancingArgs = new uint[] {0, 0, 0, 0, 0};
        
        private Mesh _meshToRender; 
        private MaterialPropertyBlock _propBlock;


        private GraphicsBuffer _triangleRenderBuffer;

        private GraphicsBuffer _vertexRenderBuffer;
        private GraphicsBuffer _indexRenderBuffer;

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
            if (enable) DrawParticles();
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
                vertices = new Vector3[] {new Vector3(0, 0),},
                colors = new Color[] {new Color(0, 0, 0, 0),},
                normals = new Vector3[] {new Vector3(0, 1, 0),}
            };
            _meshToRender.SetIndices(new int[] { 0 }, MeshTopology.Points, 0);

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
            _propBlock.SetBuffer("_TriangleBufferShader", _dynaParticle.RenderTriangleBuffer);
            
            Graphics.DrawProceduralIndirect(material, renderBounds, MeshTopology.Triangles, _gpuInstancingArgsBuffer, 0, 
                null, _propBlock);
        }

        /// <summary>
        /// Draws the particles using a custom geometry shader which needs to be tailored to the
        /// Particle compute shader, as it immediately works with the
        /// ParticleBuffer. Also, geometry shaders are not as universal and performant
        /// as the procedural method.
        /// </summary>
        private void DrawInstanced()
        {
            _gpuInstancingArgs[0] = (_meshToRender != null) ? _meshToRender.GetIndexCount(0) : 0;
            _gpuInstancingArgs[1] = (uint) _dynaParticle.GetBufferSize();
            _gpuInstancingArgsBuffer.SetData(_gpuInstancingArgs);
            
            
            _propBlock.Clear();
            _propBlock.SetBuffer("_ParticleBufferShader", _dynaParticle.ParticleBuffer);

            Graphics.DrawMeshInstancedIndirect(_meshToRender, 0, material, renderBounds, _gpuInstancingArgsBuffer, 0, _propBlock);

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

        #endregion
    }
}
