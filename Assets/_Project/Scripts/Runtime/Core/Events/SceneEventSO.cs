using Eflatun.SceneReference;
using UnityEngine;

namespace Beakstorm.Core.Events
{
    [CreateAssetMenu(menuName = ASSET_PATH + "Scene Load Event")]
    public class SceneLoadEventSO : AbstractEventSO<SceneLoadData>
    {
        
    }
    
    public struct SceneLoadData
    {
        public SceneReference[] ScenesToLoad;
        public bool SetFirstSceneActive;
        public bool LoadAdditively;

        public SceneLoadData(SceneReference[] scenes, bool setFirstActive, bool additiveLoad)
        {
            ScenesToLoad = scenes;
            SetFirstSceneActive = setFirstActive;
            LoadAdditively = additiveLoad;
        }

        public SceneLoadData(SceneReference scene, bool setFirstActive, bool additiveLoad)
        {
            ScenesToLoad = new [] { scene };
            SetFirstSceneActive = setFirstActive;
            LoadAdditively = additiveLoad;
        }
    }

}