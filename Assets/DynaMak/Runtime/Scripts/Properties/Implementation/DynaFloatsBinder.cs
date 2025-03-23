using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Float Array")]
    public class DynaFloatsBinder : DynaPropertyBinderBase<float[]>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetFloats(_propertyID, Value);
        }
    }
}
