using System;
using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Audio
{
    public class PlayerSoundParameters : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private string healthParameter = "player_Health";
        
        [SerializeField] private GliderController gliderController;
        [SerializeField] private string speedParameter = "player_Speed";
        [SerializeField] private float speedScaling = 1;


        private int _cachedHealth = 0;
        private float _cachedSpeed = 0;

        private void Update()
        {
            UpdateHealth();
            UpdateSpeed();
        }


        private void UpdateHealth()
        {
            if (!playerController)
                return;

            if (_cachedHealth == playerController.Health) return;
            
            _cachedHealth = playerController.Health;
            AkUnitySoundEngine.SetRTPCValue(healthParameter, playerController.Health01);
        }
        
        private void UpdateSpeed()
        {
            if (!gliderController)
                return;

            if (Math.Abs(_cachedSpeed - gliderController.Speed) < 0.01f) return;
            
            _cachedSpeed = gliderController.Speed;

            float speed = gliderController.Speed01;
            speed = speed * speed;
            
            AkUnitySoundEngine.SetRTPCValue(speedParameter, speed * speedScaling);
        }
    }
}