using DynaMak.Utility;
using DynaMak.Volumes;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Volume Component")]
    public class DynaVolumeTextureBinder : DynaPropertyBinderBase<VolumeComponent>
    {
        private int _textureID, _centerID, _boundsID, _resolutionID;

        protected override void SetPropertyIDs()
        {
            _textureID          = Shader.PropertyToID(PropertyName + "Volume");
            _centerID           = Shader.PropertyToID(PropertyName + "Center");
            _boundsID           = Shader.PropertyToID(PropertyName + "Bounds");
            _resolutionID       = Shader.PropertyToID(PropertyName + "Resolution");
        }
        
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            if(!Value || Value.GetVolumeTexture().IsInitialized == false) return;
            cs.SetVolume(kernelIndex, Value.GetVolumeTexture(), _textureID, _centerID, _boundsID, _resolutionID);
        }
        
        
        public override string[] DictKeys => new[] {"VOLUME"};
        public override int DictParsingOffset => 4;
    }
}
