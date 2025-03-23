using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Int")]
    public class DynaIntBinder : DynaPropertyBinderBase<int>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetInt(_propertyID, Value);
        }
        
        public override string[] DictKeys => new[] {"int", "uint"};
    }
}
