using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    [ExecuteAlways]
    public class SdfShaderSlice : MonoBehaviour
    {
        [SerializeField] private SdfShapeManager manager;
        [SerializeField] private MeshRenderer meshRenderer;

        private MaterialPropertyBlock _propertyBlock;
        
        private void Update()
        {
            if (!manager || !meshRenderer)
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            
            meshRenderer.GetPropertyBlock(_propertyBlock);
            manager.SetShaderProperties(_propertyBlock);
            meshRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
