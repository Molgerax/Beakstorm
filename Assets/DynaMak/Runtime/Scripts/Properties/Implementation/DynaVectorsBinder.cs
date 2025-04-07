using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Vector Array")]
    public class DynaVectorsBinder : DynaPropertyBinderBase<Vector4[]>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVectorArray(_propertyID, Value);
        }
    }
}
