using Eflatun.SceneReference;
using UltEvents;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beakstorm.SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        #region Serialize Fields
        [SerializeField] private SceneReference scene;
        [SerializeField] private bool forceLoad = false;
        
        [SerializeField] private LoadSceneMode loadSceneMode;

        [Tooltip("The active scene dictates the used lighting settings.")]
        [SerializeField] private bool setAsActive = false;
        
        [Header("Callbacks")] 
        [SerializeField] private UltEvent onSceneLoaded;
        #endregion

        private AsyncOperation _sceneLoadOperation;

        public void LoadScene()
        {
            if (!scene.LoadedScene.IsValid() || forceLoad)
            {
                _sceneLoadOperation = SceneManager.LoadSceneAsync(scene.Name, loadSceneMode);
                _sceneLoadOperation.completed += OnSceneLoaded;
            }
        }

        public void SetAsActiveScene()
        {
            if(scene.LoadedScene.isLoaded)
                SceneManager.SetActiveScene(scene.LoadedScene);
        }

        private void OnSceneLoaded(AsyncOperation operation)
        {
            if(setAsActive) SetAsActiveScene();
            
            onSceneLoaded?.Invoke();
            _sceneLoadOperation.completed -= OnSceneLoaded;
        }
    }

}