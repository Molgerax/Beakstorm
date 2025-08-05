using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundDopplerParam : MonoBehaviour
    {
        [SerializeField] private string dopplerRtpc = "DopplerParam";
        [SerializeField] private float dopplerFactor = 1f;

        private uint _postId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

        private Vector3 _position;
        private Vector3 _oldPosition;
        private Vector3 _velocity;

        private void Update()
        {
            UpdatePosition();
            SetDoppler();
        }

        private void SetDoppler()
        {
            if (!PlayerController.Instance)
                return;
            
            if (string.IsNullOrEmpty(dopplerRtpc))
                return;
            
            float doppler = CalculateDoppler(PlayerController.Instance.Position, PlayerController.Instance.Velocity,
                _position, _velocity, dopplerFactor);

            AkUnitySoundEngine.SetRTPCValue(dopplerRtpc, doppler, gameObject);

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

            float dopplerPitch = (SpeedOfSound + (relativeSpeedB * dopplerFactor)) /
                                 (SpeedOfSound + (relativeSpeedA * dopplerFactor));

            return dopplerPitch;
        }
    }
}