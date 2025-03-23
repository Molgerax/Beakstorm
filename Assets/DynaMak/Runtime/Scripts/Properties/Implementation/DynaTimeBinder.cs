using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Time")]
    public class DynaTimeBinder : DynaPropertyBinderBase<bool>
    {
        private float _time = 0;

        public void SetTime()
        {
            _time = Time.time;
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            if (Value) _time = Time.time;
            cs.SetFloat(_propertyID, _time);
        }
        
        public override string[] DictKeys => new[] {"TIME_PROPERTY"};
        public override int DictParsingOffset => 2;
    }
}
