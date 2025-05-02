using System.Collections;
using System.Collections.Generic;
using Beakstorm.Core.Events;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beakstorm.SceneManagement
{
    [DefaultExecutionOrder(-100)]
    public class GlobalSceneLoaderListener : MonoBehaviour
    {
        #region Serialize Fields

        [Header("Base Scenes")]
        [SerializeField] private SceneReference bootScene;
        [SerializeField] private SceneReference loadingScreenScene;
        
        [Header("Event Channel")]
        [SerializeField] private SceneLoadEventSO sceneLoadChannel;

        #endregion

        #region Private Members

        //List of the scenes to load and track progress
        private List<AsyncOperation> _scenesToLoadAsyncOperations = new List<AsyncOperation>();

        private AsyncOperation _loadingScreenAsyncOperation;
        
        //List of scenes to unload
        private List<Scene> _scenesToUnload = new List<Scene>();
        //Keep track of the scene we want to set as active (for lighting/skybox)
        private SceneReference _activeScene;

        #endregion
        
        
        #region Mono Methods

        private void OnEnable()
        {
            sceneLoadChannel.Action += OnSceneLoadChannel;
        }
        private void OnDisable()
        {
            sceneLoadChannel.Action -= OnSceneLoadChannel;
        }
        

        #endregion

        #region Private Methods

        private void OnSceneLoadChannel(SceneLoadData sceneLoadData)
        {
            LoadScenes(sceneLoadData.ScenesToLoad, sceneLoadData.LoadAdditively, sceneLoadData.SetFirstSceneActive, !sceneLoadData.LoadAdditively);
        }

        private void LoadScenes(SceneReference[] scenesToLoad, bool loadAdditively, bool setFirstSceneActive, bool showLoadingScreen)
        {
            if(!loadAdditively) AddScenesToUnload();

            if((loadAdditively && setFirstSceneActive) || !loadAdditively)
                _activeScene = scenesToLoad[0];

            StartCoroutine(TrackLoadingProgress(showLoadingScreen, !loadAdditively, scenesToLoad, loadAdditively, setFirstSceneActive));
        }

        private void UnloadScenes()
        {
            if (_scenesToUnload != null)
            {
                for (int i = 0; i < _scenesToUnload.Count; ++i)
                {
                    //Unload the scene asynchronously in the background
                    SceneManager.UnloadSceneAsync(_scenesToUnload[i], UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                }
                _scenesToUnload.Clear();
            }
        }

        private IEnumerator TrackLoadingProgress(bool showLoadingScreen, bool unloadScenes, SceneReference[] scenesToLoad, bool loadAdditively, bool setFirstSceneActive)
        {
            _loadingScreenAsyncOperation =
                SceneManager.LoadSceneAsync(loadingScreenScene.Name, LoadSceneMode.Additive);
            
            while (!_loadingScreenAsyncOperation.isDone)
            {
                yield return null;
            }
            
            for (int i = 0; i < scenesToLoad.Length; ++i)
            {
                if (!scenesToLoad[i].LoadedScene.isLoaded)
                {
                    _scenesToLoadAsyncOperations.Add(SceneManager.LoadSceneAsync(scenesToLoad[i].Name,
                        LoadSceneMode.Additive));
                }
            }

            if ((loadAdditively && setFirstSceneActive) || !loadAdditively)
                _scenesToLoadAsyncOperations[0].completed += OnActiveSceneLoaded;
            
            if (unloadScenes)
                UnloadScenes();
            
            bool allDone = false;
            //When the scene reaches 0.9f, it means that it is loaded
            //The remaining 0.1f are for the integration
            while (!allDone)
            {
                allDone = true;
                //Iterate through all the scenes to load
                for (int i = 0; i < _scenesToLoadAsyncOperations.Count; ++i)
                {
                    //Adding the scene progress to the total progress
                    if(!_scenesToLoadAsyncOperations[i].isDone) allDone = false;
                }
                yield return null;
            }

            //Clear the scenes to load
            _scenesToLoadAsyncOperations.Clear();

            //Hide progress bar when loading is done
            if (showLoadingScreen)
            {
                _loadingScreenAsyncOperation = SceneManager.UnloadSceneAsync(loadingScreenScene.Name, UnloadSceneOptions.None);
                while (!_loadingScreenAsyncOperation.isDone)
                {
                    yield return null;
                }
            }
        }

        private void OnActiveSceneLoaded(AsyncOperation asyncOp)
        {
            SceneManager.SetActiveScene(_activeScene.LoadedScene);
        }
        
        private void AddScenesToUnload()
        {
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != bootScene.LoadedScene)
                {
                    Debug.Log("Added scene to unload = " + scene.name);
                    //Add the scene to the list of the scenes to unload
                    _scenesToUnload.Add(scene);
                }
            }
        }
        
        #endregion
    }

}
