using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Texture")]
    public class DynaTextureBinder : DynaPropertyBinderBase<Texture>
    {
        [SerializeField, Range(0, 7)] private int subMeshIndex = 0;

        private int _subMeshIndexID;

        protected override void SetPropertyIDs()
        {
            base.SetPropertyIDs();
            _subMeshIndexID = Shader.PropertyToID(PropertyName + "SubMeshIndex");
        }

        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetTexture(kernelIndex, _propertyID, Value);
            cs.SetInt(_subMeshIndexID, subMeshIndex);
        }
        
        public override string[] DictKeys => new[] {"Texture2D"};
        public override int DictParsingOffset => 2;
    }
}
