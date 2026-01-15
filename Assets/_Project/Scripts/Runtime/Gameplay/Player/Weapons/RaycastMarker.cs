using System;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    public class RaycastMarker : MonoBehaviour
    {
        [SerializeField] private Transform marker;
        [SerializeField] private LayerMask layerMask;

        [SerializeField] private float offset = 2f;
        
        [field:SerializeField] public float Distance { get; set; }

        private bool _hasHit;

        private bool HasHit
        {
            get => _hasHit;
            set
            {
                if (_hasHit != value)
                    marker.gameObject.SetActive(value);
                _hasHit = value;
            }
        }

        private void Awake()
        {
            HasHit = false;
        }

        private void Update()
        {
            Raycast(transform.position, transform.forward);
        }


        private void Raycast(Vector3 origin, Vector3 direction)
        {
            Ray ray = new Ray(origin + direction * offset, direction);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, Distance - offset, layerMask, QueryTriggerInteraction.Ignore))
            {
                var markerLocalPosition = marker.localPosition;
                markerLocalPosition.z = hitInfo.distance + offset;
                marker.localPosition = markerLocalPosition;
                HasHit = true;
            }
            else
                HasHit = false;
        }
    }
}
