using Beakstorm.Inputs;
using UnityEngine;

namespace Beakstorm.UI.Menus
{
    public class InputFieldDisableNavigation : MonoBehaviour
    {
        public void EnableNavigation()
        {
            if (PlayerInputs.Instance)
                PlayerInputs.Instance.EnableNavigation();
        }
        
        public void DisableNavigation()
        {
            if (PlayerInputs.Instance)
                PlayerInputs.Instance.DisableNavigation();
        }
    }
}
