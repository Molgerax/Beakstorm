using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Vector4")]
    public class DynaVectorBinder : DynaPropertyBinderBase<Vector4>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVector(_propertyID, Value);
        }
        
        public override string[] DictKeys => new[] {"half4", "float4", "double4",};
    }
}
