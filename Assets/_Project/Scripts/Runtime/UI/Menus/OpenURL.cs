using UnityEngine;

namespace Beakstorm.UI.Menus
{
    public class OpenURL : MonoBehaviour
    {
        [SerializeField] private string url;

        public void Open()
        {
            Application.OpenURL(url);
        }
    }
}