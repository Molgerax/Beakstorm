using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Float")]
    public class DynaFloatBinder : DynaPropertyBinderBase<float>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetFloat(_propertyID, Value);
        }

        public override string[] DictKeys => new[] {"float", "half"};
    }
}
