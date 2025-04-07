using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Color")]
    public class DynaColorBinder : DynaPropertyBinderBase<Color>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVector(_propertyID, Value);
        }

        public override string[] DictKeys => new[] {"fixed3", "fixed4"};
    }
}
