using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Int Array")]
    public class DynaIntsBinder : DynaPropertyBinderBase<int[]>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetInts(_propertyID, Value);
        }
        
        public override string[] DictKeys => new[] {   
            "int2", "uint2", 
            "int3", "uint3", 
            "int4", "uint4"};
    }
}
