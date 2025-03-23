using System;
using DynaMak.Utility;
using DynaMak.Volumes;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Mesh")]
    public class DynaMeshBinder : DynaPropertyBinderBase<Mesh>
    {
        private GraphicsBuffer _vertexBuffer, _indexBuffer;

        private int _vertexID, _indexID, _strideID, _countID;

        protected override void SetPropertyIDs()
        {
            _vertexID = Shader.PropertyToID(PropertyName + "VertexBuffer");
            _indexID =  Shader.PropertyToID(PropertyName + "IndexBuffer");
            _strideID = Shader.PropertyToID(PropertyName + "VertexStride");
            _countID =  Shader.PropertyToID(PropertyName + "VertexCount");
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            if (Value == null)
                return;
            
            _vertexBuffer = Value.GetVertexBuffer(0);
            
            Value.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            Value.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            Value.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _indexBuffer = Value.GetIndexBuffer();
        }

        public override void Release()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetBuffer(kernelIndex, _vertexID, _vertexBuffer);
            cs.SetBuffer(kernelIndex, _indexID, _indexBuffer);
            cs.SetInt(_strideID, Value.GetVertexBufferStride(0));
            cs.SetInt(_countID, _vertexBuffer.count);
        }
        
        
        public override string[] DictKeys => new[] {"MESH"};
        public override int DictParsingOffset => 2;
    }
}
