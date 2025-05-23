using UnityEngine;

namespace Beakstorm.UI.Indicators
{
    public class OffscreenTarget : MonoBehaviour
    {
        [SerializeField] private OffscreenIndicatorSettings settings;

        [SerializeField] private bool useRenderers = true;
        [SerializeField] private Vector3 customSize = Vector3.one * 8;

        public OffscreenIndicator Indicator { get; set; }
        
        public OffscreenIndicatorSettings Settings => settings;

        private Renderer[] _renderers;

        private void Awake()
        {
            if (useRenderers)
                _renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnEnable()
        {
            OffscreenIndicatorManager.Register(this);
        }
        
        private void OnDisable()
        {
            OffscreenIndicatorManager.Unregister(this);
        }

        public void UpdateIndicator(Plane[] frustumPlanes)
        {
            if (!Indicator)
                return;
            
            Indicator.SetIndicatorPosition(frustumPlanes);
        }
        
        public void DeactivateIndicator()
        {
            if (!Indicator)
                return;
            
            Indicator.Deactivate();
            Indicator = null;
        }

        public bool IsVisible(Plane[] frustumPlanes)
        {
            if (!useRenderers || _renderers == null)
            {
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, new Bounds(transform.position, customSize)))
                    return true;
            }
            else
            {
                foreach (Renderer render in _renderers)
                {
                    if (GeometryUtility.TestPlanesAABB(frustumPlanes, render.bounds))
                        return true;
                }
            }

            return false;
        }
        
        public Bounds GetBounds()
        {
            if (!useRenderers || _renderers == null)
                return new Bounds(transform.position, customSize);

            Bounds bounds = _renderers[0].bounds;
            foreach (Renderer render in _renderers)
            {
                bounds.Encapsulate(render.bounds);
            }
            return bounds;
        }

        private void OnDrawGizmosSelected()
        {
            if (!useRenderers)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position, customSize);
            }
        }
    }
}
