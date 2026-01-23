using Beakstorm.SceneManagement;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [PointEntity("scene_loader", "trigger", colour:"1.0 1.0 0.5", size:16)]
    public class TrembleSceneLoader : TriggerBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, Tremble("scene")] private SceneLoadCollection scene;

        [SerializeField, NoTremble] private SceneLoadPoster sceneLoadPoster;

        private bool _triggered;
        
        public override void Trigger()
        {
            if (_triggered)
                return;
            _triggered = true;
            
            sceneLoadPoster.LoadScene();
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            sceneLoadPoster = gameObject.GetOrAddComponent<SceneLoadPoster>();
            sceneLoadPoster.SetCollection(scene);
            sceneLoadPoster.SetChannel();
        }
    }
}
