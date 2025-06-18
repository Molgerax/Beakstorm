using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Beakstorm.UI.Indicators
{
    public class OffscreenIndicator : MonoBehaviour
    {
        [SerializeField] private Image offscreenImage;

        [SerializeField] private BoundsIndicator boundsIndicator;
        [SerializeField] private float outOfSightOffset = 12f;
        
        private OffscreenTarget _target;
        private Camera _camera;
        private OffscreenIndicatorSettings _settings;

        private RectTransform _rect;
        private RectTransform _canvasRect;

        
        private IObjectPool<OffscreenIndicator> _objectPool;
        public IObjectPool<OffscreenIndicator> ObjectPool { set => _objectPool = value; }
        
        
        public void Initialize(OffscreenTarget target, Camera cam, Canvas canvas)
        {
            _target = target;
            _camera = cam;
            _settings = _target.Settings;

            _rect = GetComponent<RectTransform>();
            _canvasRect = canvas.GetComponent<RectTransform>();

            _target.Indicator = this;

            offscreenImage.sprite = _settings.IndicatorTexture;
            offscreenImage.color = _settings.Color;
        }
        
        public void Deactivate()
        {
            _objectPool.Release(this);
        }

        public void SetIndicatorPosition(Plane[] frustumPlanes)
        {
            Vector3 pos = _camera.WorldToScreenPoint(_target.transform.position);

            if (_target.IsVisible(frustumPlanes))
            {
                pos.z = 0f;
                
                SetOutOfSight(false, pos);
            }
            else if (pos.z >= 0f)
            {
                pos = OutOfRangeIndicatorPositionB(pos);
                SetOutOfSight(true, pos);
            }
            else
            {
                pos *= -1f;
                pos = OutOfRangeIndicatorPositionB(pos);
                SetOutOfSight(true, pos);
            }

            _rect.position = pos;
        }

        private void SetOutOfSight(bool value, Vector3 pos)
        {
            offscreenImage.enabled = value;

            if (boundsIndicator)
            {
                boundsIndicator.gameObject.SetActive(!value);
                if (!value)
                {
                    boundsIndicator.SetTransform(_target.GetBounds(), _camera);
                }
            }
            
            if (value)
            {
                offscreenImage.rectTransform.rotation = Quaternion.Euler(RotationOutOfSightTargetIndicator(pos));
            }
        }
        
        private Vector3 OutOfRangeIndicatorPositionB(Vector3 indicatorPosition)
    {
        //Set indicatorPosition.z to 0f; We don't need that and it'll actually cause issues if it's outside the camera range (which easily happens in my case)
        indicatorPosition.z = 0f;

        //Calculate Center of Canvas and subtract from the indicator position to have indicatorCoordinates from the Canvas Center instead the bottom left!
        Vector3 canvasCenter = new Vector3(_canvasRect.rect.width / 2f, _canvasRect.rect.height / 2f, 0f) * _canvasRect.localScale.x;
        indicatorPosition -= canvasCenter;

        //Calculate if Vector to target intersects (first) with y border of canvas rect or if Vector intersects (first) with x border:
        //This is required to see which border needs to be set to the max value and at which border the indicator needs to be moved (up & down or left & right)
        float divX = (_canvasRect.rect.width / 2f - outOfSightOffset) / Mathf.Abs(indicatorPosition.x);
        float divY = (_canvasRect.rect.height / 2f - outOfSightOffset) / Mathf.Abs(indicatorPosition.y);

        //In case it intersects with x border first, put the x-one to the border and adjust the y-one accordingly (Trigonometry)
        if (divX < divY)
        {
            float angle = Vector3.SignedAngle(Vector3.right, indicatorPosition, Vector3.forward);
            indicatorPosition.x = Mathf.Sign(indicatorPosition.x) * (_canvasRect.rect.width * 0.5f - outOfSightOffset) * _canvasRect.localScale.x;
            indicatorPosition.y = Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPosition.x;
        }

        //In case it intersects with y border first, put the y-one to the border and adjust the x-one accordingly (Trigonometry)
        else
        {
            float angle = Vector3.SignedAngle(Vector3.up, indicatorPosition, Vector3.forward);

            indicatorPosition.y = Mathf.Sign(indicatorPosition.y) * (_canvasRect.rect.height / 2f - outOfSightOffset) * _canvasRect.localScale.y;
            indicatorPosition.x = -Mathf.Tan(Mathf.Deg2Rad * angle) * indicatorPosition.y;
        }

        //Change the indicator Position back to the actual rectTransform coordinate system and return indicatorPosition
        indicatorPosition += canvasCenter;
        return indicatorPosition;
    }
        
        private Vector3 RotationOutOfSightTargetIndicator(Vector3 indicatorPosition)
        {
            //Calculate the canvasCenter
            Vector3 canvasCenter = new Vector3(_canvasRect.rect.width / 2f, _canvasRect.rect.height / 2f, 0f) * _canvasRect.localScale.x;

            //Calculate the signedAngle between the position of the indicator and the Direction up.
            float angle = Vector3.SignedAngle(Vector3.up, indicatorPosition - canvasCenter, Vector3.forward);

            //return the angle as a rotation Vector
            return new Vector3(0f, 0f, angle);
        }
    }
}
