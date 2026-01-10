using Beakstorm.UI;
using UnityEngine;

namespace Beakstorm.Gameplay.Cameras
{
    public class CameraCullUI : MonoBehaviour
    {
        [SerializeField] private bool cullUI;
        
        public void Activate()
        {
            ToggleableCanvasUI.Culled = cullUI;
        }
    }
}