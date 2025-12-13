using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Rendering.ExplosionLights
{
    [ExecuteAlways]
    public class LightDimController : MonoBehaviour
    {
        [SerializeField] private RenderingLayerMask layerMask = 
                RenderingLayerMask.defaultRenderingLayerMask;
        [SerializeField, Range(0, 1)] private float dimFactor = 1f;


        public static List<ExplosionLight> ExplosionLights = new List<ExplosionLight>(32);
        

        private void Update()
        {
            float dim = dimFactor;

            foreach (ExplosionLight explosionLight in ExplosionLights)
            {
                if (explosionLight)
                    dim = Mathf.Min(dim, explosionLight.DimFactor);
            }

            float inverseDim = 1 - dim;
            
            Shader.SetGlobalFloat(DimFactor, inverseDim);
            Shader.SetGlobalInteger(DimMask, layerMask);
        }


        private readonly int DimFactor = Shader.PropertyToID("_DimFactor");
        private readonly int DimMask = Shader.PropertyToID("_DimMask");
    }
}
