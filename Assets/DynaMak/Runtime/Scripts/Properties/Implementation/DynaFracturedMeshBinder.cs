using DynaMak.Meshes;
using DynaMak.Particles;
using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Fractured Mesh")]
    public class DynaFracturedMeshBinder : DynaPropertyBinderBase<FracturedMeshAsset>, IParticleCountInfo
    {
        private int _vertexID, _indexID, _countID, _strideID, 
            _pivotTexID, _indexTexID, _lowerBoundsID, _upperBoundsID;

        protected override void SetPropertyIDs()
        {
            _vertexID            = Shader.PropertyToID(PropertyName + "VertexBuffer");
            _countID             = Shader.PropertyToID(PropertyName + "VertexCount");
            _strideID            = Shader.PropertyToID(PropertyName + "VertexStride");
            _indexID             = Shader.PropertyToID(PropertyName + "IndexBuffer");
            _pivotTexID          = Shader.PropertyToID(PropertyName + "PivotTex");
            _indexTexID          = Shader.PropertyToID(PropertyName + "IndexTex");
            _lowerBoundsID       = Shader.PropertyToID(PropertyName + "BoundsCenter");
            _upperBoundsID       = Shader.PropertyToID(PropertyName + "BoundsSize");
        }
        
        public override void Initialize()
        {
            base.Initialize();
            
            if(!Value) return;
            Value.Initialize();
        }

        public override void Release() { }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            if(!Value) return;
            
            cs.SetFracturedMesh(kernelIndex, Value, 
                _vertexID, _indexID, _strideID, _countID,
                _pivotTexID, _indexTexID, _lowerBoundsID, _upperBoundsID);
        }
        
        
        public override string[] DictKeys => new[] {"FRACTURED_MESH"};
        public override int DictParsingOffset => 2;


        #region Interface Implementation

        public int MaxParticleCount => Value.MaxParticleCount;
        public int RenderTriangleCount => Value.RenderTriangleCount;

        #endregion
    }
}
