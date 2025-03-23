using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Matrix")]
    public class DynaMatrixBinder : DynaPropertyBinderBase<Matrix4x4>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetMatrix(_propertyID, Value);
        }
        
        
        public override string[] DictKeys => new[]
        {
            "float2x2", "float3x3", "float4x4",
            "half2x2", "half3x3", "half4x4"
        };
    }
}
