using System;
using UnityEngine;

namespace DynaMak.Meshes
{
    [CreateAssetMenu(menuName = "DynaMak/Mesh Assets/Mesh Asset", fileName = "MeshAsset")]
    public class MeshAsset : ScriptableObject
    {
        #region Serialize Fields

        [SerializeField] protected Mesh mesh;
        
        #endregion

        #region Private Fields

        protected GraphicsBuffer _vertexBuffer, _indexBuffer;

        #endregion
        
        #region Property Getters

        public Mesh Mesh => mesh;
        public GraphicsBuffer VertexBuffer => _vertexBuffer;
        public GraphicsBuffer IndexBuffer => _indexBuffer;
        
        #endregion


        #region Public Methods

        public virtual void Initialize()
        {
            Release();
         
            if(mesh == null) return;
            if (mesh.isReadable == false)
            {
                Debug.LogWarning($"{mesh} is not Read/Write enabled.");
            }

            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            _vertexBuffer = mesh.GetVertexBuffer(0);
            
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _indexBuffer = mesh.GetIndexBuffer();
        }

        public virtual void Release()
        {
            if(_vertexBuffer != null) _vertexBuffer.Release();
            if(_indexBuffer != null) _indexBuffer.Release();
        }

        #endregion


        protected void Awake()
        {
            Initialize();
        }

        protected virtual void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            Initialize();
        }

        protected virtual void OnDisable()
        {
            Release();
        }
    }
}
