using System;
using System.Linq;
using DynaMak.Utility;
using DynaMak.Volumes;
using UnityEngine;
using UnityEngine.Rendering;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "SkinnedMesh")]
    public class DynaSkinnedMeshBinder : DynaPropertyBinderBase<SkinnedMeshRenderer>
    {
        private GraphicsBuffer _vertexBuffer, _texCoordBuffer, _indexBuffer;

        private Matrix4x4 _rootTransform, _inverseRootTransform;
        private Matrix4x4[] _boneMatrices, _inverseBoneMatrices;
        private Transform[] _boneTransforms;
        private int _boneCount = -1;

        private int[] _subMeshStartIndices;
        private ComputeBuffer _subMeshStartIndicesBuffer;
        private int _subMeshCount = -1;
        
        private int _vertexID, _strideID, _countID, _texCoordID, _texStrideID;
        private int _indexID, _indexCountID, _indexStrideID;
        private int _positionOffsetID, _normalOffsetID, _tangentOffsetID, _texCoordOffsetID, _colorOffsetId;
        private int _subMeshStartID, _subMeshCountID;
        private int _rootTransformID, _inverseRootTransformID, _boneMatricesID, _inverseBoneMatricesID, _boneCountID;

        private int _texCoordStream = 0;

        private bool _initialized;

        protected override void SetPropertyIDs()
        {
            _vertexID = Shader.PropertyToID(PropertyName + "VertexBuffer");
            _strideID = Shader.PropertyToID(PropertyName + "VertexStride");
            _countID =  Shader.PropertyToID(PropertyName + "VertexCount");
            
            _indexID =  Shader.PropertyToID(PropertyName + "IndexBuffer");
            _indexCountID =  Shader.PropertyToID(PropertyName + "IndexCount");
            _indexStrideID =  Shader.PropertyToID(PropertyName + "IndexStride");
            
            _texCoordID = Shader.PropertyToID(PropertyName + "TexCoordBuffer");
            _texStrideID = Shader.PropertyToID(PropertyName + "TexCoordStride");
            
            _positionOffsetID = Shader.PropertyToID(PropertyName + "PositionOffset");
            _normalOffsetID = Shader.PropertyToID(PropertyName + "NormalOffset");
            _tangentOffsetID = Shader.PropertyToID(PropertyName + "TangentOffset");
            _colorOffsetId = Shader.PropertyToID(PropertyName + "ColorOffset");
            _texCoordOffsetID = Shader.PropertyToID(PropertyName + "TexCoordOffset");
            
            _subMeshStartID = Shader.PropertyToID(PropertyName + "SubMeshStart");
            _subMeshCountID = Shader.PropertyToID(PropertyName + "SubMeshCount");

            _rootTransformID =  Shader.PropertyToID(PropertyName + "WorldMatrix");
            _inverseRootTransformID =  Shader.PropertyToID(PropertyName + "WorldMatrixInverse");
            _boneMatricesID =  Shader.PropertyToID(PropertyName + "BoneMatrices");
            _inverseBoneMatricesID =  Shader.PropertyToID(PropertyName + "BoneMatricesInverse");
            _boneCountID =  Shader.PropertyToID(PropertyName + "BoneCount");
        }

        public override void Initialize()
        {
            base.Initialize();
            GetBuffers();
        }

        public override void Release()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _texCoordBuffer?.Dispose();
            _subMeshStartIndicesBuffer?.Dispose();
        }
        
        

        private void GetBuffers()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _texCoordBuffer?.Dispose();

            if(!Value) return;
            
            Mesh mesh = Value.sharedMesh;
            _texCoordStream = mesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);
            
            Value.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            //mesh.indexFormat = IndexFormat.UInt32;
            
            _vertexBuffer = Value.GetVertexBuffer();
            _indexBuffer = mesh.GetIndexBuffer();

            int newSubMeshCount = mesh.subMeshCount;
            if (newSubMeshCount != _subMeshCount)
            {
                _subMeshCount = newSubMeshCount;
                _subMeshStartIndices = new int[4 * 8];
                
                for (int i = 0; i < _subMeshCount; i++)
                {
                    _subMeshStartIndices[i * 4] = (int) mesh.GetIndexStart(i);
                }
            }
            
            
            _boneTransforms = Value.bones;
            _boneCount = _boneTransforms.Length;
            _boneMatrices = new Matrix4x4[_boneCount];
            _inverseBoneMatrices = new Matrix4x4[_boneCount];
            

            if (_texCoordStream > 0)
            {
                _texCoordBuffer = mesh.GetVertexBuffer(_texCoordStream);
            }
            
            if (_vertexBuffer is null)
            {
                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                _vertexBuffer = mesh.GetVertexBuffer(0);
            }
            else
            {
                _initialized = true;
            }
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            if(!Value) return;
            
            if (!_initialized)
                GetBuffers();

            
            
            if (_vertexBuffer is not null)
            {
                cs.SetBuffer(kernelIndex, _vertexID, _vertexBuffer);
                cs.SetInt(_countID, _vertexBuffer.count);
                cs.SetInt(_strideID, Value.sharedMesh.GetVertexBufferStride(0));
                
                cs.SetInt(_positionOffsetID, Value.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Position));
                cs.SetInt(_normalOffsetID, Value.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Normal));
                cs.SetInt(_tangentOffsetID, Value.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Tangent));
                
                cs.SetInt(_colorOffsetId, Value.sharedMesh.GetVertexAttributeOffset(VertexAttribute.Color));
            }

            if (_texCoordBuffer is not null)
            {
                cs.SetBuffer(kernelIndex, _texCoordID, _texCoordBuffer);
                cs.SetInt(_texStrideID, Value.sharedMesh.GetVertexBufferStride(_texCoordStream));
                
                cs.SetInt(_texCoordOffsetID, Value.sharedMesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0));
            }

            if (_indexBuffer is not null)
            {
                cs.SetBuffer(kernelIndex, _indexID, _indexBuffer);
                cs.SetInt(_indexCountID,  _indexBuffer.count);
                cs.SetInt(_indexStrideID,   _indexBuffer.stride);
                
                cs.SetInts(_subMeshStartID, _subMeshStartIndices);

                cs.SetInt(_subMeshCountID, _subMeshCount);
            }

            if (Value.rootBone)
            {
                _rootTransform = LocalToParent( Value.rootBone);
                _rootTransform = FromTransformToTransform(Value.rootBone, Value.transform);
                _rootTransform = Value.rootBone.localToWorldMatrix;
            }
            else
            {
                _rootTransform = Matrix4x4.identity;
                _rootTransform = Value.transform.localToWorldMatrix;
            }
            
            cs.SetMatrix(_rootTransformID, _rootTransform);
            cs.SetMatrix(_inverseRootTransformID, _rootTransform.inverse);
            
            
            TransformToMatrix(ref _boneMatrices, ref _inverseBoneMatrices, ref _boneTransforms, _boneCount);
            cs.SetMatrixArray(_boneMatricesID, _boneMatrices);
            cs.SetMatrixArray(_inverseBoneMatricesID, _inverseBoneMatrices);
            cs.SetInt(_boneCountID, _boneCount);
        }
        
        public override string[] DictKeys => new[] {"SKINNED_MESH"};
        public override int DictParsingOffset => 2;


        #region Private Functions
        private Matrix4x4 LocalToParent(Transform t)
        {
            return Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
        }
        
        private Matrix4x4 FromTransformToTransform(Transform t, Transform parent)
        {
            return parent.worldToLocalMatrix.inverse * t.localToWorldMatrix;
        }

        private void TransformToMatrix(ref Matrix4x4[] matrixArray, ref Transform[] transformArray, int length)
        {
            for (int i = 0; i < length; i++)
            {
                matrixArray[i] = transformArray[i].localToWorldMatrix;
            }
        }
        
        private void TransformToMatrix(ref Matrix4x4[] matrixArray, ref Matrix4x4[] inverseMatrixArray, ref Transform[] transformArray, int length)
        {
            for (int i = 0; i < length; i++)
            {
                matrixArray[i] = transformArray[i].localToWorldMatrix;
                inverseMatrixArray[i] = Matrix4x4.Inverse(matrixArray[i]);
            }
        }
        #endregion
    }
}
