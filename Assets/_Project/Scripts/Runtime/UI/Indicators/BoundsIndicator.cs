using Beakstorm.Utility;
using PrimeTween;
using UnityEngine;

namespace Beakstorm.UI.Indicators
{
    public class BoundsIndicator : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;

        private Vector2 _sizeDelta;
        private Tween _scaleTween;

        private float _sizeFactor;
        
        private void Reset()
        {
            rect = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            _sizeDelta = rect.sizeDelta;
        }

        public void Initialize(OffscreenIndicatorSettings settings)
        {
            _scaleTween.Stop();
            _sizeFactor = 1f;
            _scaleTween = Tween.Custom(
                target: this,
                settings.SelectSizeTween,
                (target, val) => target._sizeFactor = val
                );
        }
        
        public void SetTransform(Bounds bounds, Camera cam)
        {
            if (!rect)
                return;

            Rect r = BoundsUtility.BoundsInScreenSpace(bounds, cam);
            rect.sizeDelta = Vector2.Max(r.size, Vector2.one * 32) * _sizeFactor;
        }
        
        public void ResetTransform(float? size)
        {
            if (!rect)
                return;
            
            rect.sizeDelta = (size.HasValue ? (Vector2.one * size.Value) : _sizeDelta) * _sizeFactor; 
        }
        
        public void SetTransform(Vector3 center, float radius, Camera cam)
        {
            if (!rect)
                return;

            Rect r = BoundsUtility.SphereInScreenSpace(center, radius, cam);
            rect.sizeDelta = Vector2.Max(r.size, Vector2.one * 32) * _sizeFactor;
        }
    }
}
