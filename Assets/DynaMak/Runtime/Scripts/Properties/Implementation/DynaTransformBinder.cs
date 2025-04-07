using System;
using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Transform")]
    public class DynaTransformBinder : DynaPropertyBinderBase<Transform>
    {
        private int _positionId, _positionOldId, _matrixId;

        private Vector3 _currentPosition, _oldPosition;

        private void UpdatePosition()
        {
            _oldPosition = _currentPosition;
            _currentPosition = Value.position;
        }

        private void Update()
        {
            UpdatePosition();
        }


        public override void Initialize()
        {
            base.Initialize();
            if (!Value) Value = transform;
            _currentPosition = _oldPosition = Value.position;
        }

        protected override void SetPropertyIDs()
        {
            _positionId = Shader.PropertyToID(PropertyName + "Position");
            _positionOldId = Shader.PropertyToID(PropertyName + "PositionOld");
            _matrixId = Shader.PropertyToID(PropertyName + "WorldMatrix");
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVector(_positionId, _currentPosition);
            cs.SetVector(_positionOldId, _oldPosition);
            
            cs.SetMatrix(_matrixId, Value.localToWorldMatrix);
        }
        
        
        
        public override string[] DictKeys => new[] {"TRANSFORM"};
        public override int DictParsingOffset => 2;
    }
}
