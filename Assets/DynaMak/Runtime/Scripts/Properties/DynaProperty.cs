using DynaMak.Meshes;
using UnityEngine;
using DynaMak.Volumes;
using DynaMak.Utility;

namespace DynaMak.Properties
{
    public interface IDynaProperty
    {
        public void SetProperty(ComputeShader cs, int kernelIndex);
    }


    [System.Serializable]
    public abstract class DynaProperty : IDynaProperty
    {
        public string PropertyName;
        
        
        protected int _propertyID;
        
        protected virtual void SetPropertyIDs()
        {
            _propertyID = Shader.PropertyToID(PropertyName);
        }

        public virtual void Initialize()
        {
            SetPropertyIDs();
        }
        
        public virtual void Release() {}

        public abstract void SetProperty(ComputeShader cs, int kernelIndex);
    }
    
    [System.Serializable]
    public abstract class DynaProperty<T> : DynaProperty
    {
        public T Value;
    }

    
    
   [System.Serializable]
    public class DynaPropertyFloat : DynaProperty<float>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetFloat(_propertyID, Value);
        }
    }
    
    [System.Serializable]
    public class DynaPropertyInt : DynaProperty<int>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetInt(_propertyID, Value);
        }
    }
    
    [System.Serializable]
    public class DynaPropertyVector : DynaProperty<Vector4>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVector(_propertyID, Value);
        }
    }
    

    [System.Serializable]
    public class DynaPropertyFloats : DynaProperty<float[]>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetFloats(_propertyID, Value);
        }
    }
    
    [System.Serializable]
    public class DynaPropertyInts : DynaProperty<int[]>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetInts(_propertyID, Value);
        }
    }
    
    [System.Serializable]
    public class DynaPropertyVectors : DynaProperty<Vector4[]>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVectorArray(_propertyID, Value);
        }
    }
    
    
    [System.Serializable]
    public class DynaPropertyTexture : DynaProperty<Texture>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetTexture(kernelIndex, _propertyID, Value);
        }
    }
    
    
    [System.Serializable]
    public class DynaPropertyMesh : DynaProperty<Mesh>
    {
        private GraphicsBuffer _vertexBuffer, _indexBuffer;

        private int _vertexID, _indexID, _strideID, _countID;

        public override void Initialize()
        {
            _vertexBuffer = Value.GetVertexBuffer(0);
            
            Value.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            Value.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            Value.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _indexBuffer = Value.GetIndexBuffer();

            _vertexID = Shader.PropertyToID(PropertyName);
            _indexID =  Shader.PropertyToID(PropertyName + "Index");
            _strideID = Shader.PropertyToID(PropertyName + "Stride");
            _countID =  Shader.PropertyToID(PropertyName + "Count");
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
    }
    
    
    [System.Serializable]
    public class DynaPropertyFracturedMesh : DynaProperty<FracturedMeshAsset>
    {
        private int _vertexID, _indexID, _countID, _strideID, 
            _pivotTexID, _indexTexID, _lowerBoundsID, _upperBoundsID;
        
        public override void Initialize()
        {
            _vertexID            = Shader.PropertyToID(PropertyName + "VertexBuffer");
            _countID             = Shader.PropertyToID(PropertyName + "VertexCount");
            _strideID            = Shader.PropertyToID(PropertyName + "VertexStride");
            _indexID             = Shader.PropertyToID(PropertyName + "IndexBuffer");
            _pivotTexID          = Shader.PropertyToID(PropertyName + "PivotTex");
            _indexTexID          = Shader.PropertyToID(PropertyName + "IndexTex");
            _lowerBoundsID       = Shader.PropertyToID(PropertyName + "BoundsCenter");
            _upperBoundsID       = Shader.PropertyToID(PropertyName + "BoundsSize");
            
            Value.Initialize();
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetFracturedMesh(kernelIndex, Value, 
                _vertexID, _indexID, _strideID, _countID,
                _pivotTexID, _indexTexID, _lowerBoundsID, _upperBoundsID);
        }
    }
    
    
    [System.Serializable]
    public class DynaPropertyVolumeTexture : DynaProperty<VolumeComponent>
    {
        private int _textureID, _centerID, _boundsID, _resolutionID;
        
        public override void Initialize()
        {
            _textureID          = Shader.PropertyToID(PropertyName + "Volume");
            _centerID           = Shader.PropertyToID(PropertyName + "Center");
            _boundsID           = Shader.PropertyToID(PropertyName + "Bounds");
            _resolutionID       = Shader.PropertyToID(PropertyName + "Resolution");
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVolume(kernelIndex, Value.GetVolumeTexture(), _textureID, _centerID, _boundsID, _resolutionID);
        }
    }
}
