using System;
using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Time Array")]
    public class DynaTimeArrayBinder : DynaPropertyBinderBase<bool>
    {
        #region Serialize Fields
        [SerializeField, Range(1, 32)] private int arrayLength = 32;
        #endregion

        private int _timeArrayID, _timeCountID;

        #region Private Fields
        
        private Vector4[] _timeVectorArray;
        private int _count = 0;
        private int _index = 0;

        #endregion


        #region Public Methods

        public void SetTime()
        {
            if(_timeVectorArray is null) return;
            
            _timeVectorArray[_index / 4][_index % 4] = Time.time;
            _index = (_index + 1) % _count;
        }

        public void ResetTime(float defaultValue = -1000000)
        {
            for (int i = 0; i < _timeVectorArray.Length; i++)
            {
                _timeVectorArray[i] = Vector4.one * defaultValue;
            }
        }

        #endregion
        

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();
            _timeVectorArray = new Vector4[Mathf.CeilToInt(arrayLength / 4.0f)];
            _count = arrayLength;
            
            ResetTime();
        }

        protected override void SetPropertyIDs()
        {
            _timeArrayID = Shader.PropertyToID(Name + "Times");
            _timeCountID = Shader.PropertyToID(Name + "Count");
        }

        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            if(!Value) return;
            
            cs.SetVectorArray(_timeArrayID, _timeVectorArray);
            cs.SetInt(_timeCountID, _count);
        }
        
        public override string[] DictKeys => new[] {"TIME_ARRAY"};
        public override int DictParsingOffset => 2;
        #endregion
    }
}
