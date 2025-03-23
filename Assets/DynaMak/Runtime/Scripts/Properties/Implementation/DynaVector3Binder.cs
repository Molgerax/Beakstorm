using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Vector3")]
    public class DynaVector3Binder : DynaPropertyBinderBase<Vector3>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVector(_propertyID, Value);
        }
        
        public override string[] DictKeys => new[] {"half3", "float3", "double3"};
    }
}
