using UnityEngine;

namespace Beakstorm.Utility
{
    public class QuitGame : MonoBehaviour
    {
        public static void QuitApplication()
        {
            Application.Quit();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        
        public void Quit()
        {
            QuitApplication();
        }
    }
}
