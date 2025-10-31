using UltEvents;
using UnityEngine;

namespace Beakstorm.SceneManagement
{
    public class ScenesLoadedEvent : MonoBehaviour
    {
        [SerializeField] private UltEvent onScenesLoaded;

        private void OnSceneLoadFinished()
        {
            onScenesLoaded?.Invoke();
        }
        
        private void Awake()
        {
            if (GlobalSceneLoader.IsLoaded(OnSceneLoadFinished))
                OnSceneLoadFinished();
        }
    }
}
