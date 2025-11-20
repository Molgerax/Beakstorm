using UltEvents;
using UnityEngine;

namespace Beakstorm.SceneManagement
{
    public class ScenesLoadedEvent : MonoBehaviour, IOnSceneLoad
    {
        [SerializeField] private UltEvent onScenesLoaded;

        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.AfterAll;
        
        private void Awake()
        {
            GlobalSceneLoader.ExecuteWhenLoaded(this);
        }

        public void OnSceneLoaded()
        {
            onScenesLoaded?.Invoke();
        }

    }
}
