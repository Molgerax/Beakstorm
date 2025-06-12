using Beakstorm.Gameplay.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Beakstorm.UI.HUD
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image image;

        private void OnEnable()
        {
            image.fillAmount = 1;
        }

        private void Update()
        {
            if (PlayerController.Instance)
                image.fillAmount = PlayerController.Instance.Health01;
        }
    }
}
