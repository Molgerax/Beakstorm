using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Bool")]
    public class DynaBoolBinder : DynaPropertyBinderBase<bool>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetBool(_propertyID, Value);
        }
        
        public override string[] DictKeys => new[] {"bool"};
    }
}
