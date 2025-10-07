using UnityEngine;

namespace Beakstorm.Rendering
{
    [ExecuteAlways]
    public class LightDimController : MonoBehaviour
    {
        [SerializeField] private RenderingLayerMask layerMask = 
                RenderingLayerMask.defaultRenderingLayerMask;
        [SerializeField, Range(0, 1)] private float dimFactor = 1f;


        private void Update()
        {
            Shader.SetGlobalFloat(DimFactor, dimFactor);
            Shader.SetGlobalInteger(DimMask, layerMask);
        }


        private readonly int DimFactor = Shader.PropertyToID("_DimFactor");
        private readonly int DimMask = Shader.PropertyToID("_DimMask");
    }
}
