using Beakstorm.Core.Events;
using Eflatun.SceneReference;
using UnityEngine;

namespace Beakstorm.SceneManagement
{
    public class SceneLoadPoster : MonoBehaviour
    {
        #region Serialize Fields

        [SerializeField] private SceneLoadCollection collection;
        
        [SerializeField] private SceneReference[] scenesToLoad;
        [SerializeField] private bool loadAdditively;
        [SerializeField] private bool setFirstSceneActive;

        [Header("Event Channel")] 
        [SerializeField] private SceneLoadEventSO sceneLoadChannel;

        #endregion

        #region Private Fields

        private SceneLoadData _sceneLoadData;

        #endregion
        
        #region Public Methods

        public void LoadScene()
        {
            if (collection)
            {
                _sceneLoadData = collection.SceneLoadData;
                sceneLoadChannel.Raise(_sceneLoadData);
            }
            else
            {
                _sceneLoadData = new SceneLoadData(scenesToLoad, setFirstSceneActive, loadAdditively);
                sceneLoadChannel.Raise(_sceneLoadData);
            }
        }

        #endregion

    }
}
