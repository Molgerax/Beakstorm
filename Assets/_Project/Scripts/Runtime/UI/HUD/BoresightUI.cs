using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.UI.HUD
{
    public class BoresightUI : MonoBehaviour
    {
        [SerializeField] private float leadDistance = 100;
        [SerializeField] private GameObject hudElement;
        
        private Camera _camera;
        private Transform _target;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            UpdateBoresight();
        }

        private void UpdateBoresight()
        {
            if (!hudElement || !PlayerController.Instance)
                return;

            _target = PlayerController.Instance.transform;

            Quaternion rotation = Quaternion.Inverse(Quaternion.Inverse(_target.rotation) * _camera.transform.rotation);

            Vector3 worldPos = _target.position + _target.forward * leadDistance;
            Vector3 hudPos = TransformToHUDSpace(worldPos);

            float dist = Vector3.Distance(_camera.transform.position, worldPos);
            float fov = _camera.fieldOfView;
            float size = 2 * Mathf.Tan(fov / 2) * dist;
            
            if (hudPos.z > 0)
            {
                hudElement.SetActive(true);
                hudElement.transform.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
                hudElement.transform.localRotation = rotation;
                hudElement.transform.localScale = Vector3.one;
            }
            else
            {
                hudElement.SetActive(false);
            }
        }

        private Vector3 TransformToHUDSpace(Vector3 worldSpace) {
            var screenSpace = _camera.WorldToScreenPoint(worldSpace);
            return screenSpace - new Vector3(_camera.pixelWidth / 2f, _camera.pixelHeight / 2f);
        }
    }
}
