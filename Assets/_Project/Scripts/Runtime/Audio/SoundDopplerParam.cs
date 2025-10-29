using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundDopplerParam : MonoBehaviour
    {
        [SerializeField] private AK.Wwise.RTPC doppler;
        [SerializeField] private float dopplerFactor = 1f;
        [SerializeField] private GameObject target;

        private Vector3 _position;
        private Vector3 _oldPosition;
        private Vector3 _velocity;
        
        private GameObject Target => target ? target : gameObject;


        private void Update()
        {
            UpdatePosition();
            SetDoppler();
        }

        private void SetDoppler()
        {
            if (!PlayerController.Instance)
                return;
            
            if (!doppler.IsValid())
                return;
            
            float dopplerPitch = CalculateDoppler(PlayerController.Instance.Position, PlayerController.Instance.Velocity,
                _position, _velocity, dopplerFactor);

            doppler.SetValue(Target, dopplerPitch);
        }
        
        private void UpdatePosition()
        {
            _oldPosition = _position;
            _position = transform.position;
            _velocity = (_position - _oldPosition) / Time.deltaTime;
        }
        
        private float CalculateDoppler(Vector3 posA, Vector3 velA, Vector3 posB, Vector3 velB, float dopplerFactor)
        {
            const float SpeedOfSound = 343;
            
            Vector3 diff = posB - posA;

            float relativeSpeedA = Vector3.Dot(diff, velA) / Mathf.Max(0.001f, diff.magnitude);
            float relativeSpeedB = Vector3.Dot(diff, velB) / Mathf.Max(0.001f, diff.magnitude);

            relativeSpeedA = Mathf.Min(relativeSpeedA, (SpeedOfSound / dopplerFactor));
            relativeSpeedB = Mathf.Min(relativeSpeedB, (SpeedOfSound / dopplerFactor));

            float dopplerPitch = (SpeedOfSound + (relativeSpeedA * dopplerFactor)) /
                                 (SpeedOfSound + (relativeSpeedB * dopplerFactor));

            return dopplerPitch;
        }
    }
}